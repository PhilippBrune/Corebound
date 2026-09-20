# Open Points Register

Version: 1.3 · Created 2026-09-20 · Last updated 2026-09-20 · Status: Active

The single live list of what is still open. `08_IMPLEMENTATION_BACKLOG.md` is the *plan*; this is the *punch list*. Every entry states what is missing, why it matters, and what "done" looks like, so none of them has to be re-derived next session.

Priority: **P1** blocks playable quality · **P2** needed for the Early Access target · **P3** worth doing, not urgent.

---

## P1 — Blocks playable quality

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

One chain exists (`ProductionChain_Default`) covering all three permitted triggers and both tower-pulse producers. The vision's loop is a *closed, amplifying* circuit, and the amplifying half is missing. See the Amplifier-node proposal in `00_PROJECT_VISION.md` §20.1. Also still open: real balance iteration (needs playtest) and any persistent on-screen indication that a chain is producing.

### 9. God-object refactors

`WaveManager` (~15 responsibilities), `WaveStatePresenter` (four), `StageArenaArtPresenter`. Deliberately deferred — these need Editor access to verify and carry real regression risk. The read-only label resolution was already extracted to `WaveEncounterLabelResolver` as the safe part.

### 10. Unresolved vision questions

From `00_PROJECT_VISION.md` §20.1, each with a concrete proposal awaiting a yes/no: whether the Core can be stat-upgraded, how deep the Factory chain goes, and the meta-progression curve. §20.2 additionally lists where the vision text and the build disagree (tower count, "resource conversion" vs. the single-currency decision, unvalidated 60–70 minute run length).

### 11. Smaller items carried forward

- Slice 12: missing boss "Großangriff" audio cue; no automated coverage of boss state transitions.
- Remaining canvas splits still built procedurally with hardcoded offsets: MainMenu route panels, Boss HUD, Notification, Relic reveal.
- `StageEnemyPoolData` is fully authored but read by no code (intentionally deferred to a Stage 2+ weighted-selection feature).
- ~~`CardData.Tags` is authored on several cards but read by nothing.~~ **Wrong when written:**
  `CardRewardPresenter.ApplyTagIcons()` reads it and draws up to four tag icons per reward
  slot through `CardTagIconAtlas`. What is true is that nothing *else* reads it: offer
  selection and card effects still ignore tags.
- Six multi-slot sprite sheets are imported but unsliced/unused, or bypass their documented VFX data model with custom runtime grid maths.
- `stage2_ground_tileset.png` and `stage3_ground_tileset.png` are **JPEG data behind a `.png` name**. The runtime loader sniffs content so it works, but JPEG carries no alpha — worth re-exporting as real PNG if either ever needs transparency.
