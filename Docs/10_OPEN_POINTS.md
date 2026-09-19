# Open Points Register

Version: 1.0 · Created 2026-09-20 · Status: Active

The single live list of what is still open. `08_IMPLEMENTATION_BACKLOG.md` is the *plan*; this is the *punch list*. Every entry states what is missing, why it matters, and what "done" looks like, so none of them has to be re-derived next session.

Priority: **P1** blocks playable quality · **P2** needed for the Early Access target · **P3** worth doing, not urgent.

---

## P1 — Blocks playable quality

### 1. Dynamic Arena Slice is only one third implemented

`Docs/Slices/15_DYNAMIC_ARENA_SLICE.md`. Arena *size* is per-stage data and spawn points and tower slots are authorable per stage — but **arena geometry is still Stage 1's maze algorithm for every stage**. A larger or differently shaped stage renders Stage 1's layout stretched or cropped to fit, which is the "arena is still fixed in size" report.

Still missing from that slice:

- `StageArenaLayoutData` asset type (the slice's own proposed architecture) — currently the data is spread across `StageData` and `StageThemeData` instead.
- Per-stage arena *shape*: `StageArenaMazeGenerator` is Stage-1-only and is the sole layout source.
- Per-stage prop placements beyond the hardcoded Stage 1 and Stage 2 sets in `StageArenaArtPresenter`.
- `BuildableZones` — tower slots are a flat point list, not shape-constrained placement.

**Done looks like:** each stage's arena differs in size *and* shape from data alone, with no stage-specific branch anywhere in `WaveManager`, `TowerService` or `StageArenaArtPresenter` (the slice's own acceptance criterion).

### 2. Weapons.md is substantially unimplemented

Three weapon archetypes exist and work, but the spec's supporting layer does not:

- `WeaponModifierData` and `WeaponPresentationData` asset types **do not exist**. Modifiers are loose float properties on `CardService` read directly by `PlayerCombatController.FireProjectile()` — functionally equivalent for today's four multipliers, but there is no data asset, so no per-weapon audio, visual or modifier profile can be authored.
- Of the spec's events (`OnWeaponFired`, `OnProjectileSpawned`, `OnWeaponModified`, `OnWeaponStateChanged`) only `PlayerCombatController.WeaponFired` exists, and only `TutorialService` consumes it.
- Consequently the spec's **UI** section (current weapon identity, cooldown/readiness, active modifiers) and **Audio** section (distinct per-weapon fire/hit/crit cues) have no data feeding them and are entirely absent.

**Done looks like:** each weapon sounds and looks distinct in play, and a card can modify a weapon through a declared data hook rather than a hardcoded float.

### 3. Enemy variety is the one Early Access content target still short

Vision Section 9 asks for **five distinct enemy types**; three roles exist (Chaser, Ranged Pressure, Bruiser). The three Elite variants are modifiers on those same roles, not new types. Either build two more roles or restate the target — but decide, because it is currently a silent miss.

---

## P2 — Needed for the Early Access target

### 4. Shop does not meet Shop.md

Reviewed against the spec on 2026-09-20. What works: ScriptableObject data model, central `EconomyService`/wallet validation, fixed price categories plus `StagePriceMultiplier`, controller-first navigation, rarity-tinted frames, no reroll, and Tower Upgrade offers are fully wired. All five stages reference a shop. Gaps:

- **No weighting.** `ShopOfferTableData` holds only `Offers[]` and `MaxOfferCount`; offers are taken in array order until the cap. The spec asks for "Pools, **Gewichtungen** und Stage-spezifische Verfügbarkeiten". Per-offer `MinStageIndex`/`MaxStageIndex` covers availability, so weighting is the missing half. `CardService` already does rarity weighting and is the obvious precedent.
- **Audio is entirely absent.** `ShopEvents` raises all five events (`ShopOpened`, `OfferSelected`, `PurchaseRequested`, `PurchaseSucceeded`, `ShopClosed`) and **nothing anywhere subscribes to them** — they are dead API. There is no `ShopAudioCuePlayer`, although Wave, Boss, MainMenu and Factory all have one. The spec requires distinct open / hover / buy / insufficient-funds sounds.
- **Buyability is not shown.** The spec requires the UI show "Kosten, **Kaufbarkeit**, Seltenheit". Affordability is only evaluated when Confirm is pressed; an unaffordable offer looks identical to an affordable one.
- **Visually too close to the Card Reward screen.** The spec wants the shop to "sich vom Combat-Bild und von Card-Reward-Screens **unterscheiden**"; it currently mirrors the reward panel's structure closely.
- **Deliberate deviation to confirm:** the spec says refunds do not exist in the first scope, but `ShopService.RefundPurchase()` exists. It only rolls back a purchase whose *application* failed after payment — a correctness guard, not a player-facing refund. Recommend keeping it and noting the exception in the spec.

### 5. Main Menu plays two sounds after a run — unresolved

The one concrete overlap found is fixed (`MainMenuAudioCuePlayer.OnEnable()` no longer fires its open cue), and a surviving duplicate `MusicStateController` is ruled out (no `DontDestroyOnLoad`, no singleton). Still reported. **Blocked on information, not effort:** agents cannot hear audio. Needs either the two sounds named, or a recording. A per-cue `Debug.Log` behind the debug flag would also settle it.

### 6. Localization coverage is partial

Done: `CardData` (all 30), `TowerData`, `TowerLevelData`, Shop, the HUD status line, banners, pause/failed overlays, tower placement line, and the four MainMenu route panels. Still English-only: `CardRewardPresenter`'s own strings, `RelicRevealPresenter`, `MetaProgressionRouteTargetPresenter`, `SettingsRouteTargetPresenter`, `MainMenuCanvasPresenter`. There is also no Settings control to switch language — `LocalizationService.IsGerman` is only settable in code.

### 7. `UIThemeData` colours are still unread

`.Font` reaches 11 presenters via `UIFontResolver`, but `PrimaryTextColor`, `SecondaryTextColor`, `AccentColor` and `PanelFrameSprite` are read by nothing; every presenter hardcodes its own colours. Best done together with a shared procedural-UI builder, since `CreateText`/`CreateImage` are duplicated across at least six presenters and have already drifted apart.

---

## P3 — Worth doing, not urgent

### 8. Factory chain has no amplification step

One chain exists (`ProductionChain_Default`) covering all three permitted triggers and both tower-pulse producers. The vision's loop is a *closed, amplifying* circuit, and the amplifying half is missing. See the Amplifier-node proposal in `00_PROJECT_VISION.md` §20.1. Also still open: real balance iteration (needs playtest) and any persistent on-screen indication that a chain is producing.

### 9. God-object refactors

`WaveManager` (~15 responsibilities), `WaveStatePresenter` (four), `StageArenaArtPresenter`. Deliberately deferred — these need Editor access to verify and carry real regression risk. The read-only label resolution was already extracted to `WaveEncounterLabelResolver` as the safe part.

### 10. Unresolved vision questions

From `00_PROJECT_VISION.md` §20.1, each with a concrete proposal awaiting a yes/no: whether the Core can be stat-upgraded, how deep the Factory chain goes, and the meta-progression curve. §20.2 additionally lists where the vision text and the build disagree (tower count, "resource conversion" vs. the single-currency decision, unvalidated 60–70 minute run length).

### 11. Smaller items carried forward

- Slice 12: missing boss "Großangriff" audio cue; no automated coverage of boss state transitions.
- Remaining canvas splits still built procedurally with hardcoded offsets: MainMenu route panels, Boss HUD, Notification, Relic reveal.
- `StageEnemyPoolData` is fully authored but read by no code (intentionally deferred to a Stage 2+ weighted-selection feature).
- `CardData.Tags` is authored on several cards but read by nothing.
- Six multi-slot sprite sheets are imported but unsliced/unused, or bypass their documented VFX data model with custom runtime grid maths.
- `stage2_ground_tileset.png` and `stage3_ground_tileset.png` are **JPEG data behind a `.png` name**. The runtime loader sniffs content so it works, but JPEG carries no alpha — worth re-exporting as real PNG if either ever needs transparency.
