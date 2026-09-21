# Open Points Register

Version: 1.6 · Created 2026-09-20 · Last updated 2026-09-21 · Status: Active

The single live list of what is still open. `08_IMPLEMENTATION_BACKLOG.md` is the *plan*; this is the *punch list*. Every entry states what is missing, why it matters, and what "done" looks like, so none of them has to be re-derived next session.

Priority: **P1** blocks playable quality · **P2** needed for the Early Access target · **P3** worth doing, not urgent.

---

## P1 — Blocks playable quality

### 0a. Shop never appeared, and the build intermission was skipped — FIXED (2026-09-21)

Reported three playtests running. Both were the same defect, and both are fixed.

Confirming the card reward closes the reward screen, which synchronously raises `StateChanged` ->
`WaveManager.OnRewardStateChanged` -> `StartIntermission` -> `ShopService.TryOpenForIntermission`,
all inside the `ConfirmPressed` invocation list. `ShopService` and `WaveManager` are both also
subscribed to `ConfirmPressed`, so the single press that opened the shop resolved it, and ended the
intermission, in the same frame. `shopOffers=3` next to `shopOpen=False` in one log line was the
tell: offers were being built correctly the whole time.

Fixed with a one-frame guard in each: `ShopService` ignores Confirm and Cancel in its opening
frame, and `WaveManager` tracks `_intermissionStartedFrame` beside the existing
`_intermissionGateResolvedFrame`, which only covered a gate being cleared and never the
intermission being started.

Verified in play. Before: `sawShop=False` across a six-wave run, loop Combat -> Reward -> Combat.
After: `SHOP OPENED at wave 2 with 3 offers, canvas=VISIBLE a=1`, loop Combat -> Reward ->
Intermission -> Combat on every rotation.

The build gate is also no longer suppressed on shop waves, and the shop opens after waves 2 and 4
rather than only wave 3.

### 0b. Arena outer edge — REBUILT (2026-09-21)

Third report on the same area. Three compounding faults: segments stretched into boxes whose aspect
depended on arena size (so each stage distorted differently once the Dynamic Arena Slice gave them
different sizes), one quad per repeat so every seam showed, and corner patches at 1.35x the wall
height so all four corners bulged. Now one tiled `SpriteRenderer` per edge, repeating the art at its
native aspect at any size, with the horizontal bands extended over the corners so the mismatched
corner crop is gone entirely.

### 0c. Boss and phase-helper spawns — RANDOMISED (2026-09-21)

The boss scatters on an arc around its authored point and is pushed at least five units from the
player. Phase helpers get a per-phase ring rotation plus angle and radius jitter instead of a fixed
90/30-degree lattice.


### 0. Live playtesting now works without a human at the keyboard (2026-09-20)

`Assets/Editor/DebugAutopilot.cs` plays a real run end to end. Synthetic OS input still cannot
reach Unity's Input System, so the autopilot writes the same state the `InputAction` callbacks
write, through an editor-only surface on `CoreboundInputReader`. Every downstream system is
already wired to that one instance, so this is not a simulation: it is the game, driven from the
same seam a controller drives.

It always starts from `MainMenu.unity`, picks the stage through the F9/F10 debug static, navigates
the menu by route rather than by index, kites while firing, and logs stage, wave state, live enemy
count, every enemy's position with an in/outside-arena flag, arena size and all four HUD label
values every two seconds.

Two things it needs to keep working, both learned the hard way:

- **It must announce itself as a gamepad.** `PlayerAimResolver` treats `Aim` as a screen position
  in mouse mode and as a stick direction in gamepad mode. Feeding a world direction in mouse mode
  aims every shot at a screen corner, which looks exactly like "projectiles do no damage".
- **It must bind to the input reader in the player's scene.** MainMenu and SampleScene each carry
  their own `CoreboundInputReader` and both are loaded at once, so the first one found is often the
  menu's. Driving that one leaves the player standing still.

Trigger it by writing a stage index into `Temp/autopilot_request.txt`, or from the
`Corebound > Autopilot` menu. Note that `Assets/Editor` compiles into
`Assembly-CSharp-Editor.dll`, not `Assembly-CSharp.dll` - check the right one when verifying a
build.

### 0b. Wave 1 of Stage 1 could never be completed — FIXED (2026-09-20)

Reported as "could not reach stage 1 wave 2 anymore, 2 enemies spawned, I killed them and nothing
happened", with Stage 2 working correctly.

Root cause, confirmed live: Stage 1 spawns from a single origin at (0, -4.5) with a fan radius of
(5.5, 3.5) in a 16x10 arena whose floor stops at y = -5, so the fan reaches y = -8. That never
mattered while every enemy in a spawn group resolved to the same angle. Spreading spawns across the
fan and rotating the fan per wave (both added 2026-09-19) started landing enemies outside the
boundary colliders, where they are walled off from the arena and can never be reached or killed.
`RemainingAliveEnemies` then never returns to zero and the wave hangs forever. The autopilot caught
one at `Chaser@(1.3, -5.9) OUTSIDE` while the wave sat at 1/6 indefinitely.

Fixed by clamping every resolved spawn position into the arena bounds with a one-unit margin, in
`WaveManager.ClampToArena`, applied to both regular and boss-helper spawns. Clamping there rather
than shrinking each stage's fan radius fixes it for every stage at once, including ones authored
later. Verified: Stage 1 now runs 1 -> 2 -> 3 -> 4, and Stage 2 runs all six waves.


### 1. Dynamic Arena Slice — DONE (2026-09-20)

`StageArenaLayoutData` exists and is the live layout source for all five stages. Arena size,
boundary thickness, layout strategy, obstacle grid, prop placements and buildable zones are all
per-stage data; the last stage-specific branch (`theme_stage_02`) is gone from
`StageArenaArtPresenter`. Tower slots now rebuild both on a mid-run stage transition and when
resuming a save past Stage 1. Stage 1 keeps empty prop and zone lists so it falls back to its
original behaviour bit for bit. Detail in `Docs/Slices/15_DYNAMIC_ARENA_SLICE.md`,
Implementation Note (2026-09-20).

**Still to confirm in play:** Stages 2-5 have never been seen live with their new layouts.
Synthetic input cannot reach Unity's Input System, so that check needs a human at the keyboard.

### 2. Weapons.md — DONE (2026-09-20)

All five data assets from the spec now exist, the four events are raised, and both the UI and
Audio sections have a data source:

- `WeaponModifierData` carries a target, a value and an optional `RequiredWeaponId`. Crit chance,
  crit damage, Multishot and status chance — the four hooks that previously had nowhere to live —
  are reachable from data. `CardData` and `RelicData` both carry an optional modifier array.
- `WeaponModifierState` resolves those together with the three pre-existing `CardService` float
  multipliers, so existing cards are unaffected. Crit is rolled once per trigger pull, not per
  projectile.
- `WeaponPresentationData` holds the icon, accent colour, muzzle/impact colours and the fire and
  critical audio cues, and is wired into all three weapons at clearly separated pitch registers.
- `WeaponEvents` raises `WeaponFired`, `ProjectileSpawned`, `WeaponModified` and
  `WeaponStateChanged`. `WeaponAudioCuePlayer` and `WeaponHudPresenter` consume them; both are on
  `CombatRuntime` in `SampleScene.unity`.
- Four new cards (`Critical Focus`, `Critical Amplifier`, `Split Barrel`, `Unstable Payload`)
  reach the player only through the new hook and are registered in `CardPool_Default`.

**Still to confirm in play:** the HUD widget, the crit cue and the Multishot fan have not been
seen live, for the same input-system reason.

**Deliberate deviations from the spec text:**

- Event names drop the spec's `On` prefix to match every other event hub in the project.
- Muzzle and impact colours are authored but not yet drawn; `WeaponPresentationData` holds them
  ready for the VFX pass. The spec's Visual section is therefore still open.
### 3. Enemy variety — DONE (2026-09-20)

Vision Section 9 asks for five distinct enemy types. Two new roles close the gap, and both names
come from Enemies.md's own list of example roles rather than being invented:

- **Zone Denier** (`AreaControl`, "Flaechenkontrolle"). Ignores the Player entirely, advances on
  the Core until it reaches its hold distance, then anchors and fires full 360-degree projectile
  rings on a long, readable windup. Its threat is positional: the ground around it becomes
  expensive, and the player chooses between spending time on it and routing around it. Built
  from the existing `ProjectileBurst` attack data; the only genuinely new thing is where it
  decides to stand.
- **Relay Warden** (`Support`, "Support fuer andere Enemies"). Never meaningfully attacks.
  Steers toward the enemies it is reinforcing, backs away when the Player closes, and multiplies
  nearby enemies' move speed and damage through `EnemySupportAuraRegistry`. Two of them buff
  each other, which is the intended escalation. It is the correct thing to shoot first.

Both are appended to `EnemyRoleType`, never inserted, because that enum is serialized by index in
the `EnemyBehaviour_*.asset` files. They appear in Waves 4, 5 and 6 of Stage 1.

`EnemyData` also gained `SpriteTint`, which applies even when a real sprite is present (unlike
`DebugColor`, which only covers the no-art fallback).

**Open follow-up:** neither role has bespoke art. They borrow the ranged and chaser sheets and
are separated only by tint, green for the Zone Denier and violet for the Relay Warden. That is a
deliberate placeholder, not a finished look. Neither has been seen in play.
## P2 — Needed for the Early Access target

### 4. Shop vs Shop.md — three of four gaps closed (2026-09-20)

Reviewed against the spec on 2026-09-20. What already worked: ScriptableObject data model,
central `EconomyService`/wallet validation, fixed price categories plus `StagePriceMultiplier`,
controller-first navigation, rarity-tinted frames, no reroll, and fully wired Tower Upgrade
offers. All five stages reference a shop.

**Closed since:**

- **Weighting.** `ShopOfferData` gained `SelectionWeight` (1 is neutral, so every existing asset
  keeps its odds relative to the others). `ShopService.BuildOffers` now collects every available
  offer and draws without replacement by weight, instead of taking the first three in array
  order. A weight of 0 makes an offer unreachable without deleting the asset. Four new offers
  for the weapon-modifier cards are authored with deliberately uneven weights.
- **Audio.** `ShopAudioCueData` and `ShopAudioCuePlayer` exist and are on `CombatRuntime`.
  `ShopEvents` now has a subscriber; it was dead API before. Distinct open / hover / buy /
  insufficient-funds cues, the last of which needed a new `PurchaseDeclined` event, since no
  existing event fired on a refused purchase. The close cue is skipped when the shop closed
  because something was bought, so a purchase does not play two one-shots in one frame.
- **Buyability.** `ShopService.CanAffordOffer` is public and `ShopPresenter` reads it per offer:
  an unaffordable offer turns its cost text red, dims its frame and mutes its title. Two cues,
  not one, because colour alone is not a safe carrier.

**Still open:**

- **Visually too close to the Card Reward screen.** A first step is done: the shop's dim wash was
  byte-identical to the reward screen's cool near-black and is now a warm amber ground, in both
  `ShopPresenter` and `ShopCanvas.unity`. The deeper similarity is structural, though: three
  card-shaped slots in a row using the same rarity frame art. Closing that properly needs an art
  direction call, not code.
- **Deliberate deviation to confirm:** the spec says refunds do not exist in the first scope, but
  `ShopService.RefundPurchase()` exists. It only rolls back a purchase whose *application* failed
  after payment, a correctness guard rather than a player-facing refund. Recommend keeping it and
  noting the exception in the spec.
### 5. Main Menu plays two sounds after a run — unresolved

The one concrete overlap found is fixed (`MainMenuAudioCuePlayer.OnEnable()` no longer fires its open cue), and a surviving duplicate `MusicStateController` is ruled out (no `DontDestroyOnLoad`, no singleton). Still reported. **Blocked on information, not effort:** agents cannot hear audio. Needs either the two sounds named, or a recording. A per-cue `Debug.Log` behind the debug flag would also settle it.

### 6. Localization — DONE (2026-09-20)

Every presenter that held English-only literals now resolves through `LocalizationService`:
`CardRewardPresenter` and `CardRewardController`, `RelicRevealPresenter`, and
`MainMenuCanvasPresenter` (prompt, status line, all six route descriptions and all six route
confirmations). `MetaProgressionRouteTargetPresenter` and `SettingsRouteTargetPresenter` compose
their rows from controller data rather than literals, so they follow whatever those rows carry.

The language is now switchable in game. `SettingsOptionKey.Language` was **appended** to the enum
(never inserted: the enum is serialized by index in `SettingsDefinition_Default.asset`), backed by
`SettingsSaveData.LanguageIndex`, exposed as an English/Deutsch choice under Accessibility, and
applied in `SettingsManager.ApplyRuntimeSettings` — which runs on preview as well as on commit, so
arrowing through the option retranslates the Settings screen itself immediately.

**Still to confirm in play:** the option has not been exercised through the live Settings UI.
### 7. `UIThemeData` colours are still unread

`.Font` reaches 11 presenters via `UIFontResolver`, but `PrimaryTextColor`, `SecondaryTextColor`, `AccentColor` and `PanelFrameSprite` are read by nothing; every presenter hardcodes its own colours. Best done together with a shared procedural-UI builder, since `CreateText`/`CreateImage` are duplicated across at least six presenters and have already drifted apart.

---

## P3 — Worth doing, not urgent

### 8. Factory chain has no amplification step

**Amplifier accepted and built 2026-09-21.** `ProductionNodeData` gained `AmplifierOutputBonus`,
`AmplifierRequiredTowerCount` and `AmplifierRequiredTowerId`; an amplifier is an ordinary node whose
bonus multiplies every *other* node in its chain while its tower condition holds, not a new node type
or trigger. `ProductionNode_NetworkAmplifier.asset` ships the proposal's +25%-at-three-towers example.
It never amplifies itself, multiple amplifiers add rather than multiply, and the result still passes
`ClampAmount`.

**Still open:** the amplifier pays out invisibly. Section 5.5 requires the player be able to
*recognise* that a system is working, so the Intermission HUD line still needs the active chain
surfaced. Also still open: real balance iteration (needs playtest) and any persistent on-screen indication that a chain is producing.

### 9. God-object refactors

**Owner asked for these to be fixed (2026-09-20). First cut done 2026-09-21.**

`WaveSpawnPlacement` is extracted from `WaveManager`: all spawn geometry, no wave state,
independently constructible. Deliberately first, because it is where the wave-hang bug hid.
`WaveManager` is ~1100 lines from ~1230.

Remaining: `WaveStatePresenter` (1358 lines, four responsibilities) and `StageArenaArtPresenter`
(1001). `WaveManager` still carries most of its responsibilities.

**The original reason for deferring no longer applies.** These were held back because they needed
Editor access to verify and carried regression risk; the autopilot now verifies a full run end to
end, including every UI state transition. This is a scheduling question now, not a risk one.

### 10. Unresolved vision questions

From `00_PROJECT_VISION.md` §20.1, each with a concrete proposal awaiting a yes/no: whether the Core can be stat-upgraded, how deep the Factory chain goes, and the meta-progression curve. §20.2 additionally lists where the vision text and the build disagree (tower count, "resource conversion" vs. the single-currency decision, unvalidated 60–70 minute run length).

### 11. Smaller items carried forward

- Slice 12: the boss "Grossangriff" audio cue now exists. `BossEvents.OnBossHeavyAttack` is
  raised at the start of an attack windup, not when the shot lands, so the cue arrives with the
  telegraph as a warning rather than a report; `BossAudioCuePlayer` plays it at the lowest and
  longest of the five boss tones so it stays distinct from the phase-change chime. Still open
  from that slice: no automated coverage of boss state transitions.
- Remaining canvas splits still built procedurally with hardcoded offsets: MainMenu route panels, Boss HUD, Notification, Relic reveal.
- `StageEnemyPoolData` is fully authored but read by no code (intentionally deferred to a Stage 2+ weighted-selection feature).
- ~~`CardData.Tags` is authored on several cards but read by nothing.~~ **Wrong when written:**
  `CardRewardPresenter.ApplyTagIcons()` reads it and draws up to four tag icons per reward
  slot through `CardTagIconAtlas`. What is true is that nothing *else* reads it: offer
  selection and card effects still ignore tags.
- Six multi-slot sprite sheets are imported but unsliced/unused, or bypass their documented VFX data model with custom runtime grid maths.
- `stage2_ground_tileset.png` and `stage3_ground_tileset.png` are **JPEG data behind a `.png` name**. The runtime loader sniffs content so it works, but JPEG carries no alpha — worth re-exporting as real PNG if either ever needs transparency.
