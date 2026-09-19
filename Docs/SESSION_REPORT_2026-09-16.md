# Session Report — 2026-09-16 (overnight pass #2)

This covers the playtest-feedback pass that followed the first overnight run. Written to a file per your request since this doesn't fit comfortably in chat.

---

## 1. Bugs found and fixed this pass

### 1.1 Reward/Shop screen layout broken (card frames not in slot, title/description mispositioned)

**Root cause:** `RewardCanvas.unity`/`ShopCanvas.unity` were extracted from `SampleScene.unity` last session with several `Image` components saved with **no sprite assigned** (`m_Sprite: {fileID: 0}`) and stale positions. The Editor-only code that used to author correct sprite/position data (`EnsureSceneRewardLayout`/`EnsureSceneShopLayout`) had been disconnected in an earlier fix (to stop it building a duplicate visible panel) and was never reconnected — and even reconnected, it uses `AssetDatabase`, which doesn't exist in a real build.

**Fix:** `CardRewardPresenter.TryBindAuthoredLayout()` and `ShopPresenter.TryBindAuthoredLayout()` now repair sprite and position data themselves, every time they bind — using the build-safe `DebugSpriteFactory.LoadProjectSprite` (not the Editor-only `AssetDatabase`) for sprites, and the same anchor/offset math the original authoring code used for positions. Sprites are only assigned when missing (never overwrites art you set by hand); positions are always reasserted (they're meant to be code-defined, not hand-tuned per instance — see the note on editing these yourself, below).

**Verified live**: entered Play Mode with a pending reward — title now sits at the top of the panel, all three card frames render correctly inside their slots, description sits at the bottom. Confirmed via screenshot.

### 1.2 HUD showing as a blank white box after MainMenu → New Run (but fine when opening SampleScene directly)

**Root cause — this was the important one, and it explains several of your other reports too:**

`RewardCanvas.unity`, `ShopCanvas.unity`, and `HUDCanvas.unity` load *additively* at runtime, after the `SampleScene` transition starts. `CardRewardPresenter`/`ShopPresenter`/`WaveStatePresenter`'s `Awake()` used to call a fallback-capable `EnsureInitialized()` — and since the companion scene hadn't loaded yet at that point, the bind attempt failed and a **local fallback panel got built instead** (a duplicate, badly-positioned UI tree parented under the presenter's own GameObject in `SampleScene`). Building that fallback also set the exact field (`_slotRoots[0]`, `_pausePanel`) that `EnsureInitialized()` uses as its "already done" guard — so when `Start()` ran later (by which point the real companion scene *had* loaded), the guard was already satisfied and the retry never happened. The wrong, locally-built panel stayed forever.

This is also why it worked fine when you opened `SampleScene.unity` directly in the Editor: the companion scenes were already open in the Editor (via last session's `AdditiveCanvasSceneAutoLoader` fix) *before* Play Mode even started, so the very first bind attempt succeeded and the bug never triggered.

**Fix:** `Awake()`/`OnEnable()` now only ever *attempt* a bind (`TryBindAuthoredLayout()` / `TryBindAuthoredHud()` directly) — this is a safe no-op if the companion scene isn't loaded yet, and critically does **not** build anything. Only `Start()` (guaranteed to run after the additive load completes) calls the fallback-capable `EnsureInitialized()`. The "already initialized" guard was also decoupled from the bind result itself (a new `_isFullyInitialized` flag) so a bind that happens to succeed early doesn't skip `RegisterWithUIManager()` later.

This directly addresses your "After starting one MainMenu the scenes Shop and Reward are empty. Reimport all fixed it" report — Reimport All forces a full domain reload, which reset the (previously-satisfied) guard and gave the retry a fresh chance to succeed, purely by luck of timing. That's no longer needed.

**Not yet re-verified through the literal MainMenu → New Run click path** — synthetic input doesn't reach Unity's Input System (a standing limitation all session), so I can't click "New Run" myself. I verified the underlying mechanism by tracing the exact Awake/OnEnable/Start/additive-load ordering in code, and confirmed the fix doesn't regress the "open SampleScene directly" path (still renders correctly). Please retest the actual MainMenu → New Run flow when you're back.

### 1.3 Cross-scene reference warning spam (40+ warnings per Play session)

**Found while testing 1.2 above, not something you reported directly — but likely contributing to intermittent binding failures.** The `_authored*` fields (`_authoredPresenterRoot`, `_authoredTitleText`, etc.) on all three presenters were marked `[SerializeField]`, but they're cross-scene references (e.g. `CardRewardPresenter` lives in `SampleScene`, but `_authoredTitleText` points at an object in `RewardCanvas.unity`). Unity doesn't support serializing cross-scene references, so on every domain reload / Play Mode exit it logged "Cross scene references are not supported" and **nulled the field out** — which could then be read as a stale null before the next rebind ran.

**Fix:** removed `[SerializeField]` from all of these fields across `CardRewardPresenter`, `ShopPresenter`, `WaveStatePresenter`. They were always re-resolved fresh via `GameObject.Find()` anyway, never actually relying on a serialized value — so nothing is lost. Verified: 0 warnings in a subsequent Play session (down from 40+).

### 1.4 Ranged enemy telegraph looked like a second, redundant shot

Last session's change (telegraph now flies toward the target using the real bolt sprite, so the "warning" and the "real shot" look identical) reads, in practice, as *two* shots being fired per attack — confusing, not a clear telegraph. The dedicated telegraph art (`ranged_pressure_telegraph.png`) that used to distinguish it is gone (deleted during this session's repo cleanup, and — I should flag this clearly — **it was never committed to git, so it's not recoverable from history**).

**Fix (best available without new art):** the telegraph keeps the bolt shape and its "flies toward target" motion, but is now tinted bright warning-yellow (matching the existing `BruiserChargeWarningColor` visual language already used elsewhere for the Bruiser's charge attack) and starts translucent, becoming more opaque as the windup completes. This should read clearly as "incoming, not yet resolved" rather than "a second shot." Not live-verified (would need to watch actual combat, which combat-via-synthetic-input can't reliably trigger) — please confirm this reads correctly next time you're testing.

### 1.5 Reward screen playing two sounds at once

**Investigated, not conclusively resolved.** Found one *already-existing, already-fixed* case in the code: `FactoryAudioCuePlayer` explicitly skips its `ResourceGenerated` cue when triggered by `WaveCompleted`, specifically because it used to overlap `WaveAudioCuePlayer`'s wave-complete fanfare "right as the Card Reward screen opens" (per the existing code comment — this was clearly already diagnosed once before). I traced every subscriber to `WaveEvents.WaveCompleted` and didn't find a second concrete overlapping sound source in code.

I want to be upfront: **I have no way to actually hear audio** — I can read code and reason about event timing, but I can't listen to a Play session to confirm what's actually overlapping. If the two-sounds issue persists, the most useful next step would be either telling me which two sounds specifically (e.g., "the wave-complete fanfare and X"), or a short screen recording with audio I could describe back to you differently — reasoning from the waveform/timing isn't something I can do blind.

### 1.6 Tower slots missing / can't reach Wave 2

Checked whether this shares 1.2's root cause — it doesn't. `TowerService.CreateSlotRuntimes()` (called from `Awake()`) reads slot positions straight from `WaveManager.StageDataAsset.TowerSlotPositions`, a direct serialized reference with no additive-scene involvement at all, so the MainMenu-transition timing bug can't be the explanation here. This is a genuinely separate, still-open issue — not independently root-caused this pass given remaining time, and I can't reliably reproduce it myself (placing a tower requires the same synthetic-input-to-Input-System path that doesn't work). Worth a fresh look next session with a screenshot of the exact moment slots are missing, ideally with the Hierarchy showing whether any `TowerSlot_N` GameObjects exist under the TowerService GameObject at all (missing GameObjects vs. GameObjects existing but not rendering would point to very different bugs).

---

## 2. Confirmed from your list

- **Tutorial scripted pauses**: confirmed okay as first implementation.
- **Repo cleanup**: confirmed.

---

## 3. Still open from the previous report (unchanged, not touched this pass)

- **Weapons.md** — audited, not implemented (`WeaponModifierData`/`WeaponPresentationData` assets, correctly-named weapon events, weapon UI, weapon audio, Tower-side weapon modification all still missing).
- **Remaining canvas splits** — MainMenu route panels, Boss HUD, Notification, Relic reveal still procedurally generated with hardcoded offsets. Same regression risk identified last time (several presenters build UI via unguarded `new GameObject(...)` calls) still applies.
- **Slice 12 gap** — missing "Großangriff" (major attack) boss audio cue; no automated test coverage for boss state transitions.
- **Debug stage-select** — still not click-tested through the real MainMenu.

---

## 4. Unused-assets review (for your approval — nothing deleted)

**Update 2026-09-17 — actioned per your approval.** All four categories below are now resolved:
- Godot `.import` cruft (28 files + their `.meta` sidecars): **deleted**.
- Real orphaned per-stage enemy sprites (12 of the 13 files — `route_panel_frame.png` wasn't part of this): **wired in**, not deleted. `EnemyRuntimeController` now resolves sprites by `(EnemyId, active stage theme)`, same precedent as `BossRuntimeController`.
- Audio (4 boss music tracks): **wired in**. `MusicStateController` now picks the track by `BossEventArgs.BossId`.
- ScriptableObject data assets (4 files): **deleted**, verified zero GUID references first.
- Scripts (`PlayerInteractionPriorityResolver.cs`, `SerializedMainMenuStateProvider.cs`): **kept**, per your explicit "dont remove the Scripts."

Original analysis below kept as-written for the record.

Method: for every candidate file, extracted its GUID from the `.meta` and grepped the entire `Assets/` tree for the file's full path, bare filename, and GUID (all three, since this project mixes GUID-based references with string-path loading via `AssetDatabase.LoadAssetAtPath`/`DebugSpriteFactory.LoadProjectSprite`). Everything below returned **zero hits** outside its own `.meta`. Nothing was deleted — this is for you to review and approve.

### Godot `.import` cruft — 28 files, ~27 KB, highest confidence

These aren't even Unity files — they're Godot's texture-import metadata format, sitting alongside PNGs as leftover cross-tool contamination (consistent with the Godot project scaffold already found and removed from the repo root this session). Unity auto-generated a `.meta` for each, so deleting the `.import` means deleting its `.meta` too.

`card_frame_common/epic/legendary/rare.png.import`, `card_tag_icon_sheet.png.import`, `boss_stage1_topdown.png.import`, `enemy_bruiser/chaser/ranged_pressure_topdown.png.import`, `player_base_topdown.png.import`, `projectile_sheet_core.png.import`, `weapon_beam/rapid/spread_sprite.png.import`, `combat_vfx_sheet_essentials.png.import`, `core_main_sprite.png.import`, `stage1_ground_tileset.png.import`, `stage1_prop_set.png.import`, `resource_primary_icon.png.import`, `tower_slot_marker.png.import`, `hud_frame_panel.png.import`, `wave_banner.png.import`, `button_primary_frame.png.import`, `corebound_logo.png.import`, `focus_marker.png.import`, `mainmenu_background.png.import`, `tutorial_glyph_strip/prompt_panel.png.import` (all under their respective `Assets/Art/**` folders).

### Real orphaned sprites — 13 files, ~11.5 MB, high confidence

Per-stage enemy reskins that nothing loads: `EnemyRuntimeController.cs` only has base sprite-path constants (no stage suffix), unlike `BossRuntimeController.cs` which genuinely has `BossStage1..5SpritePath`. No stage-aware enemy reskin logic exists anywhere in code or in any `StageTheme_*`/`StageEnemyPool_*` asset.

- `Assets/Art/Characters/Enemies/enemy_bruiser_stage2/3/4/5_topdown.png` (4 files, ~4.3 MB)
- `Assets/Art/Characters/Enemies/enemy_chaser_stage2/3/4/5_topdown.png` (4 files, ~3.1 MB)
- `Assets/Art/Characters/Enemies/enemy_ranged_pressure_stage2/3/4/5_topdown.png` (4 files, ~3.7 MB)
- `Assets/Art/UI/MainMenu/route_panel_frame.png` (0.61 MB) — `MainMenuRoutePanelData.cs` has no sprite/frame field at all, only text.

*(Worth noting: this is a real gap, not just cleanup fodder — Stage 3/4/5 have distinct enemy art sitting unused. Wiring per-stage enemy reskins into `EnemyRuntimeController` the same way `BossRuntimeController` already does would be a legitimate, low-risk visual improvement using art you already have, rather than deleting it. Flagging as an option, not doing it without your steer.)*

### Audio — 4 files, ~27.3 MB, high confidence

`MusicState_Boss.asset` only references `music_boss_loop.mp3` — no stage-aware boss music selection exists anywhere in code, matching the enemy-sprite finding above.

- `Assets/Audio/Music/music_boss_stage2/3/4/5_loop.mp3` (4 files)

*(Same note as above: this is stage-specific boss music sitting unused, not clearly "junk" — could be wired into `BossRuntimeController`/`MusicStateController` alongside the visual reskins if you want stage variety, rather than deleted.)*

### ScriptableObject data assets — 4 files, negligible size

- `Assets/ScriptableObjects/Input/InputContext_Build.asset` — sibling contexts (Gameplay/MainMenu/Reward) are wired into manager fields; this one isn't.
- `Assets/ScriptableObjects/Gameplay/Enemies/EnemyAttack_Default.asset` — siblings (`_Bruiser/Chaser/Ranged`) are referenced; this fallback isn't.
- `Assets/ScriptableObjects/Gameplay/Enemies/EnemyElite_Default.asset` — same pattern (`_Anchor/Relentless/Splitshot` are wired, this isn't).
- `Assets/ScriptableObjects/UI/MainMenu/TutorialEntry_Default.asset` — zero references; its backing class (`TutorialEntryData.cs`) also isn't used as a field type anywhere else, so the whole type looks like dead scaffolding.

### Scripts — 2 files, negligible size

- `Assets/Scripts/Gameplay/Player/PlayerInteractionPriorityResolver.cs` — static utility, `GetPriority()` never called anywhere.
- `Assets/Scripts/UI/MainMenu/SerializedMainMenuStateProvider.cs` — an `IMainMenuStateProvider` implementation never attached to anything; `SaveBackedMainMenuStateProvider.cs` (a sibling) is the one actually wired into `MainMenu.unity`.

**Lower-confidence, flagged only:** `Assets/Scripts/UI/MainMenu/TutorialEntryData.cs` has exactly one reference (the orphaned `TutorialEntry_Default.asset` above) — not a clean zero, but would become fully dead if that asset is removed.

### Total: 51 files, ~39 MB

My recommendation: delete the Godot `.import` cruft and the two dead scripts outright (unambiguous). For the enemy sprites and boss music, I'd lean toward **wiring them in** rather than deleting — you already paid for that art/audio and it's a straightforward, low-risk extension of a pattern (`BossRuntimeController`'s per-stage sprite resolution) that already exists and works. Your call either way — nothing touched yet.

---

## 5. Skills assessment — what would actually help vs. what wouldn't

You asked specifically about a "visual design for Unity" skill so I could better understand your UI issues. Honest answer: **I don't think that's the actual gap.** I already have full vision — I can look at your screenshots directly, and did, all session (comparing before/after states, reading Inspector fields, spotting the missing sprite in the "Source Image" field, etc.). The `artifact-design`/`artifact-diagramming` skills I do have are for building *new* web-based designs (HTML/CSS mockups) — they wouldn't add anything for reading an *existing* Unity Editor screenshot, since that's not a design-authoring task, it's an image-reading task I can already do.

The actual bottlenecks this session were:

1. **No way to drive Unity's Input System with synthetic input.** This blocked live-testing the MainMenu → New Run flow, tower placement, and reaching Wave 2 — probably the single biggest limitation all session. This is a real, buildable gap: Unity's **Input System package ships an `InputTestFixture` / `InputSimulation` API** designed exactly for this (queuing synthetic input events directly into the Input System's event queue, bypassing the OS-level `SendInput` approach the current `unity-editor-control` skill uses, which the Input System deliberately ignores). Building a small custom Editor tool or Test Runner integration around this would let me actually click through menus, place towers, and fire weapons programmatically instead of relying on save-file manipulation as a workaround. **This is the one I'd actually recommend approving** — it would have let me verify almost everything in this report end-to-end instead of reasoning from code alone.

2. **No way to hear audio.** Fundamental — no skill fixes this, it's a capability gap, not a missing tool. Flagging it so you know why 1.5 above is unresolved rather than assuming I checked and it's fine.

3. **A Unity RectTransform/layout-bounds inspector** (a small custom Editor script that dumps computed screen-space bounds for a UI hierarchy, flags overlaps) would have made diagnosing 1.1 faster than manually reading YAML anchor/offset math — moderate value, not urgent, and it's a small enough tool I could just build it directly in the project (as an Editor menu item) rather than needing it as a Claude Code "skill."

**My recommendation, if you want to approve one thing:** the Input System synthetic-input capability (#1). It's the one that actually would have changed what I could verify this session, versus the others which are either not real gaps (visual design) or not fixable by tooling (audio).

---

## 6. How to manually adapt RewardCanvas.unity yourself

You asked to learn this, so here's the structure and the rule that matters most.

**Hierarchy** (`Assets/Scenes/RewardCanvas.unity`):
```
RewardCanvas (Canvas root)
└─ RewardScreen (CanvasGroup - controls show/hide via alpha)
   ├─ Overlay (full-screen dim background)
   └─ CardRewardChoicePanel (the visible frame)
      ├─ Title
      ├─ Resources
      ├─ TowerInfo
      ├─ Description
      ├─ RewardSlot0 / RewardSlot1 / RewardSlot2
      │  ├─ SlotFrame (the card border art)
      │  ├─ SlotTitle
      │  ├─ SlotRarity (currently hidden by design)
      │  ├─ SlotDescription
      │  └─ TagIcon0-3
```

**Updated 2026-09-17 — this is now the non-code-driven system you asked for.** Per your feedback ("Can this be disabled? ... I want the no code driven way"), `TryBindAuthoredLayout()` on `CardRewardPresenter`/`ShopPresenter` no longer reasserts any RectTransform anchor/position/size, ever. Everything below about "reasserted by code every time" and "drag it and it snaps back" is **no longer true** — it described the old behavior and is kept here only so the contrast is clear.

What actually happens now:

- **Position and size are read from the scene file only.** `RewardCanvas.unity`/`ShopCanvas.unity` are the sole source of truth. Drag something in the Scene view, save the scene (Ctrl+S), and it stays exactly where you put it — nothing will snap it back.
- **Sprites are still self-healing**, but only the sprite: each element is found by name (`FindText`/`FindImage`) and, if its Image component's sprite is currently `None`, filled in from the known project art path. If a sprite is already assigned (including one you dragged on by hand), it's left alone. This is what lets a corrupted/stale scene file recover its art automatically without also fighting you over layout.
- **To see this repair applied and baked into the .unity files directly** (not just live in memory during Play Mode), use the `Corebound > Repair Authored Canvases` menu command (`Assets/Editor/AuthoredCanvasRepairTool.cs`) — opens all four scenes together, runs each presenter's repair pass, and saves the three canvas scenes. Useful after editing art paths in code, or if a sprite reference ever goes stale again.
- **If you add a genuinely new visual element**, you now do it by hand in the Scene view (or Prefab), naming it to match what the presenter's `FindText`/`FindImage` calls look for if you want it auto-wired at runtime — no C# constants to edit for position anymore.
- **Seeing the images in the Scene view**: if a canvas scene ever looks blank/no-sprite in the Scene window even though Play Mode renders it correctly, that's stale sprite data baked into the file — run `Corebound > Repair Authored Canvases` once to fix it on disk.

---

## 7. Second pass — 2026-09-17, responding to your point-by-point feedback on this report

You came back with per-section feedback on everything above (regression reports on 1.2, new complaints on 1.4/1.5/1.6, and explicit approvals on 4/5/6). This section covers what changed in response.

### 7.1 The actual root cause of "WaveBanner again in HUD" / "Paus Menus again in HUD" (1.2 regression)

This was the important find this pass. `WaveStatePresenter.EnsureOverlayWidgets()` needs to parent the wave banner and pause/failed modal on the **full-screen** canvas, not the small top-right HUD status box — its own comment said as much. It resolved that full-screen canvas via `_authoredHudRoot.GetComponentInParent<Canvas>()`. The bug: `GetComponentInParent` includes the object it's called on, not just true ancestors — and the small HUD status box (`_authoredHudRoot`, named "HUD") carries **its own nested `Canvas` component** (added at some point to give it a local sort order). So this always resolved to that nested Canvas on "HUD" itself, `overlayParent` silently collapsed to `_authoredHudRoot`, and since WaveBanner/PausePanel actually live one level up (parented directly under `HUDCanvas`'s true root), the `Find()` lookup always missed and created fresh duplicates *inside* "HUD" — exactly matching what you saw, and exactly why it kept coming back across multiple earlier "fixes" that only ever patched around the symptom (a 2026-09-15 comment in the same method already describes chasing this same duplication once before, without finding this root cause).

**Fix:** switched to `Canvas.rootCanvas`, which walks past any number of nested Canvases to the actual outermost one, regardless of how the hierarchy is structured. Verified by running the repair tool three times in the same Editor session (previously guaranteed to duplicate by the second or third run) and entering Play Mode from a clean single-scene state — WaveBanner/PausePanel stayed at exactly one each every time.

This also turned out to be the same underlying cause behind **"Player % and Core % and currency are missing" (also 1.2)** — the same broken binding chain. Fixed for free: a fresh Play Mode screenshot now shows `Scrap: 0 · Player 100% · Core 100% · Stage 1 | Wave 1/6` rendering correctly in the HUD status box.

### 7.2 Ranged enemy hit effect on the wrong entity, projectile size (1.4)

Confirmed and fixed. `DamageContext.HitPoint` was set once at attack-fire time from the attacker's position and never updated as the projectile traveled, so `CombatVfxSpawner` always rendered the hit flash at the *shooter's* position instead of the actual impact point. `CombatProjectile.OnTriggerEnter2D()` now rebuilds the `DamageContext` with `transform.position` (the projectile's real position at the moment of impact) right before delivering the hit — a blanket fix, since every damage source (player, enemy, boss, tower) routes through this same method. Enemy melee/contact damage had the identical bug in `EnemyRuntimeController` (used its own `transform.position` instead of the target's) and got the same fix. `Projectile_EnemyBolt.asset`'s `_radius` went from 0.2 → 0.3 for visibility, per your request.

### 7.3 Main menu playing two sounds after a run (new, 1.5)

`MainMenuAudioCuePlayer.OnEnable()` played its "panel opened" cue unconditionally every time `MainMenu.unity` loaded — landing in the same frame as `MusicStateController`'s own `SwitchToState(MusicState.Calm)` (also scene-load-triggered) and audibly overlapping it. Removed; the cue only fires now for genuine in-menu navigation (Navigate/Confirm/Cancel), not the scene load itself.

### 7.4 Tower slots "still static placement" / Stage 2 "empty (Blue background)" (1.6)

Two separate, previously-undiagnosed bugs, both now fixed:

- **Tower slots**: `TowerService.CreateSlotRuntimes()` only ever ran once, from `Awake()`. The Dynamic Arena Slice doc (`Docs/Slices/15_DYNAMIC_ARENA_SLICE.md`) already flagged this as a known, deliberately-deferred gap — a player reaching Stage 2 mid-run kept seeing Stage 1's slot layout. Added `TowerService.RebuildSlotsForNewStage()`, called from `WaveManager.AdvanceToNextStage()` right next to the existing arena-rebuild call. Already-built towers carry over onto the new stage's slots at the same index (the underlying `_builtTowerIds` data is untouched).
- **Stage 2 "empty"**: the ground texture was never actually broken — it's a real, detailed, intentionally blue-toned coolant/reactor tileset (I looked at the raw file to confirm). The actual bug: decorative prop placement (`StageArenaArtPresenter.CreatePropClusters`/`CreateMazeBlockers`) always used Stage 1's grid-coordinate assumptions, regardless of which stage's prop sheet was active. `stage1_prop_set.png` approximates a uniform grid; `stage2_prop_set.png` is a completely different hand-composed collage (pipe cluster, two energy orbs, three wire-junction boxes, two tall pylons), so Stage 1's coordinates cropped empty or unrelated regions of it — the arena rendered its correct blue floor with almost no readable scattered decoration on top, reading as "empty." Measured Stage 2's actual item bounds via connected-component analysis of the sheet's alpha channel (same method a code comment says was already used for Stage 1's sheet) and added real per-item placements, selected by the active `StageThemeData.ThemeId`.

  Verified visually via a new `Corebound > Preview Stage 1/2 Arena` Editor menu command (temporarily repoints a `StageArenaArtPresenter`'s `_stageData` and calls its existing `RebuildForNewStage()` — preview-only, never saved) — Stage 2 now shows its props scattered around the arena, clear of the core, all four tower slots, and both spawn points.

**Not yet re-verified**: "still no shop popping up after 3 waves" — unchanged from the previous report's finding (all relevant data/logic checked out; most likely explained by the same additive-scene-load race already fixed for Reward/HUD, needs a live retest rather than further code changes). "Reward screen positions... not as expected" (1.1) — with position now scene-file-driven (§6 above), I checked the actual current layout by temporarily forcing the reward panel's `CanvasGroup.Alpha` to 1 in the Editor (never saved) to see it without playing through to a reward moment: it renders as a complete, non-overlapping panel — title banner at top, three evenly-spaced card slots, description at bottom. Each slot's title text sits near the bottom of its card border rather than the top, which might be what read as "not as expected" — but that's a style choice, not a defect, and it's now something you can drag to a different position directly in the Scene view and have it stick.

**"Enter always ends Intermission even with nothing built"** and **"Run Failed states the actual cause"**: both done. `WaveManager.OnConfirmPressed()` no longer gates on `_requiresIntermissionTowerAction`. `WaveManager.FailureCauseLabel` returns "Core Destroyed" or "Player Died" based on which one the killing blow actually hit, shown in both the HUD state line and the full-screen Failed overlay.

### 7.5 Unused-assets cleanup and wiring (§4 above)

Actioned in full — see the updated §4 status banner.

### 7.6 What's still genuinely open

- The two "approved for use" skills (Input System synthetic-input capability, RectTransform/layout-bounds inspector tool) — these were recommendations for tooling *I* could build to work more effectively, not player-facing fixes, and haven't been built yet. Lower priority than the bugs above; say the word if you want either built out.
- Weapons.md UI/Audio/modifier-data implementation, remaining canvas splits (MainMenu route panels, Boss HUD, Notification, Relic reveal), Slice 12's missing boss audio cue and test coverage — all carried over unchanged from earlier reports, not touched this pass.

---

## 8. Third pass — 2026-09-17, tooling, doc accuracy, and localization

Both §7.6 tools got built. `Corebound > Input Simulator` (`Assets/Editor/InputSimulatorWindow.cs`) drives real Input System device state via `InputSystem.QueueStateEvent` — confirmed working at the raw device level (`Keyboard.current.enterKey.isPressed` genuinely flips), but a bound `InputAction`'s `.performed` callback never fires from it, root cause not found despite ruling out the obvious suspects (stale action-map state, wrong map enabled, wrong asset reference, event coalescing, Game View OS focus). Also tried `InputTestFixture` as a structurally different alternative (drives input from inside a test rather than injecting into a live Play session) — hit a different, harder wall: it needs its own dedicated assembly, and this project's game code has no `.asmdef` of its own, so a test assembly can see `InputTestFixture` or the game's own types, never both, without first splitting `Assets/Scripts/` into its own assembly (a real restructuring, deliberately not attempted here). Both findings are documented in `InputSimulatorWindow.cs`'s header comment so nobody re-chases either blind. Net effect: live-input verification (the shop-after-wave-3 retest, general combat feel) is still something that needs you to press the keys once, not something I can script around yet. `Corebound > RectTransform Inspector` (`Assets/Editor/RectTransformInspectorWindow.cs`) worked cleanly on the first try — no Play Mode dependency, since it just reads `GetWorldCorners()` — and independently re-confirmed the §7.1 HUD fix by showing WaveBanner/PausePanel both correctly parented under HUDCanvas.

Separately, cross-referenced `Docs/00_PROJECT_VISION.md`'s Section 20 "Open Design Questions" against the actual implementation (reading `PlayerCombatController`, `CardService`, `ResourceWalletService`, `CoreController`, etc. directly) and annotated 16 of 18 questions with what the game actually does today, leaving genuinely open: whether the Core can be stat-upgraded, the deeper Factory production-chain design, and the full meta-progression curve.

Then found that `Docs/08_IMPLEMENTATION_BACKLOG.md` itself had drifted from the code in the *other* direction — several "Noch offen" (still open) items were actually already done, which is worse than a missing feature because it wastes future effort re-discovering or re-implementing something that exists. Checking every claim in the doc's "Recommended Next Slice" list against the actual code/assets (GUID-tracing ScriptableObject reference chains, not just reading class names) turned up seven stale entries, all corrected: Shop Tower Upgrade offers were fully wired end-to-end (asset included) despite the doc and `ShopOfferType.cs`'s own comment saying otherwise; card stacking rules were fully enforced via `CardData.Stackable`; the Beam weapon already has real pierce behavior via `ProjectileData.PierceCount`, not just different numbers from Rapid; four tower types exist and are catalogued, not one; `UIThemeData.Font` is wired into 11 presenters (only colors/panel sprite genuinely aren't); and 23 real `.mp3` files are imported and fill every checked audio-cue slot, with a working `MusicStateController` handling per-stage boss music — both flatly contradicting the doc's "no real audio files, no music-state system" claim. Bumped the doc to Version 1.4 with all of this corrected in place.

Last, extended actual German localization coverage (`Docs/08_IMPLEMENTATION_BACKLOG.md`'s UI section had flagged this as thin). `CardData` gained `DisplayName`/`Description` German fields with all 30 cards translated — since both the Card Reward screen and the Shop read `CardData` directly, this covers both surfaces in one pass. `ShopPresentationData`/`ShopService`/`ShopPresenter` got the same treatment. While verifying this, found a real, narrower bug: the four `MainMenuRoutePanelData` assets (Settings/MetaProgression/NewRunConfirm/QuitConfirm) already had `_titleDe`-style fields in code, but those fields were added *after* the assets were last saved, so they deserialize as empty strings and silently fall back to English every time — the German toggle has been doing nothing for those four screens. Authored real German text directly into each asset's YAML to fix it. Still unlocalized and left for later: `WaveStatePresenter`, `RelicRevealPresenter`, `MetaProgressionRouteTargetPresenter`, `SettingsRouteTargetPresenter`, `MainMenuCanvasPresenter`, and `TowerData` (including the runtime-composed tower-upgrade strings in `TowerService`).

Nothing in this pass touched live gameplay behavior — all of it was either Editor tooling, documentation accuracy, or data/string content, verified by compilation and direct GUID/reference tracing rather than by playing the game.

---

## 9. Fourth pass — 2026-09-19, the MainMenu-path root cause

One defect explains most of §7's leftovers, and it only ever reproduces from **MainMenu → New Run** — which is exactly why opening `SampleScene` directly kept hiding it.

`SceneManager.LoadScene(..., Additive)` is **not synchronous**. Unity defers it to the end of the current frame. `AdditiveCanvasSceneLoader` assumed the opposite: it raised `CanvasSceneReady` immediately after *requesting* the load, and nothing ever subscribed to that event anyway, so all three presenters bound in their own `Start()` — which runs in the same frame the load was requested, i.e. strictly before `HUDCanvas`/`ShopCanvas`/`RewardCanvas` objects exist. Every bind from the MainMenu path failed, and each presenter then built its own local placeholder UI.

That is:

- **7.1, "Player %, Core % and currency shown shortly in white, then disappear."** The white placeholder HUD renders for a frame or so and the real `HUDCanvas` then arrives on top of it.
- **"Shop still not popping up at all."** `ShopPresenter` bound to nothing, so `Refresh()` had no canvas to show.
- Part of the reward-screen instability.

Fixed by raising `CanvasSceneReady` from `SceneManager.sceneLoaded` (the first moment the objects genuinely exist), subscribing all three presenters to it, and making each one *wait* rather than build its fallback while its canvas is still in flight (`IsScenePending`).

**Stage 2 / Stage 3 arena had no ground.** `StageTheme_Stage2`/`_Stage3` pointed at `stageN_ground_tileset.png.jpg`, which stopped existing when those files were renamed to `.png` — the texture silently resolved to null. Both paths corrected. Worth knowing: those two files are still **JPEG data behind a `.png` name**; the loader sniffs content so it works, but a JPEG carries no alpha.

**StreamingAssets mirrored only Stage 1 — 39 files missing.** `DebugSpriteFactory` tries `StreamingAssets/Art/` first and falls back to `Assets/Art/` *only inside the Editor*, so every Stage 2–5 arena texture, all 12 per-stage enemy reskins, the 4 extra boss sprites, the Mortar/Sentinel towers and 7 VFX sheets would have shipped missing while looking correct in the Editor. The mirror also held a **stale** `card_reward_choice_panel.png`, so the game kept drawing the previous reward-panel art after it was replaced. Synced, and added `Corebound > Validate Art Pipeline` so both mirror drift and broken `StageThemeData` paths are detectable instead of silent.

**Reward screen rebuilt to the supplied reference image.** Measured the panel art directly (connected-component analysis) rather than guessing: title plaque at `(361,50,393,65)`, top bar `(163,135,792,33)`, slot recesses `(175/448/724, 188, ~216, 262)`, bottom bar `(261,484,591,56)`. Slots were `282x414` against recesses that are really `228x294`, the card title sat at the *bottom* of the card and the description above it. Now: card title at the top of each card, description in the card body, currency and tower info at the two ends of the top bar, selected-card explanation in the bottom bar. Card text is best-fit, so no card name or description can overflow or be silently truncated.

**Two input defects.**
- `E` was bound to **both** `Gameplay/Interact` (build a tower — and hold-to-guard the Core) and `Build/NextSlot` (cycle slot), with both maps enabled simultaneously, so every build attempt also moved the selection. `NextSlot` moved to `Tab`.
- A single Confirm press could finish a mandatory Intermission gate *and* end the Intermission, because `WaveManager` and `ShopService` both subscribe to `ConfirmPressed` and the gate flag clears mid-event — so whether buying also skipped the rest of the Intermission depended on subscription order. Guarded by frame.

**Debug stage-select was unreachable, not missing.** It was bound to `[` and `]` — on the German QWERTZ keyboard actually in use, those physical keys are `ü` and `+`, and the hint text named keys that are not on the keyboard. Its only feedback was `NotificationPresenter`, which `MainMenu.unity` does not contain, so even the right key produced nothing. Moved to **F9 / F10** (same physical position on every layout) and drawn on screen. Verified live in Play Mode on `MainMenu.unity`: the Game view shows `DEBUG · New Run starts on Stage 1 of 5 · F9 / F10 to change`, console clean.

This also explains **"Tower slots → no change visible"**: Stage 1 uses `TowerService`'s default slots while Stage 2 and 3 define their own (4 and 5 distinct positions), so the data was already differentiated — but Stage 2 was unreachable without a working stage-select.

**Still not fixed: 7.3 (two sounds on the Main Menu after a run).** The one concrete overlap found previously is gone — `MainMenuAudioCuePlayer.OnEnable()` no longer fires its open cue — and `MusicStateController` has no `DontDestroyOnLoad`/singleton, so a surviving duplicate from the gameplay scene is ruled out. I cannot hear audio, and guessing further is not useful: the fastest path is naming the two sounds (e.g. "the calm music and a UI click") or a recording.
