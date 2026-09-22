# Spec Review 2026-09-22 — completeness and consistency of `Docs/Specs`

Scope: all 29 files in `Docs/Specs/` (the `DIY/` folder excluded), read in full and checked against each other and, where a finding depends on it, against the code and assets on disk today.

Each finding gives **where**, **what is wrong or missing**, and **at least two ways to resolve it**. The option marked *(recommended)* is the one I would pick; the decision is yours.

Severity:
- **High**: the specs contradict each other or the build on a rule that decides behaviour.
- **Medium**: a gap that has already caused a bug, or will.
- **Low**: stale text, drift or broken references.

Findings are grouped as:
- **A**: contradictions between specs
- **B**: gaps no spec covers
- **C**: stale or drifted text inside a spec
- **D**: broken references

---

## A — Contradictions between specs

### A1 · One Interact button, three verbs at the Core (High)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Core.md (Guard: *hold* Interact within 2.2 u; Repair: *press* Interact within 2.4 u during Intermission), Towers.md (build: press Interact on a slot), Player.md (interaction priority list).

**Problem:** Player.md's priority list ("blocking UI → tower → reward/shop → pickups") does not mention the Core at all. Today one press near the Core during an Intermission starts a Guard hold *and* buys a repair. If a tower slot lies within 2.4 u of the Core, the same press also builds. Three components subscribe to the same input, and no spec says which one wins.

**Options:**
1. *(recommended)* Extend Player.md's priority list to "blocking UI → tower slot → Core repair → Core guard". Route Interact through one resolver that hands each press to exactly one owner.
2. Separate the verbs by state, and write it down: Guard only in Combat/BossCombat, Repair only in Intermission. Keep tower slots outside the Core's repair radius, and make that a stage-authoring rule in Stages.md.
3. Give Repair its own gesture (hold for 0.5 s with a fill ring), so tap = build and hold = repair/guard.

### A2 · Can towers be built during a wave? (High)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Towers.md "Festgelegte Entscheidungen" ("Towers dürfen auch während aktiver Waves gebaut und verbessert werden") vs `TowerService` (builds only in `WaveState.Intermission`), WeaponLoadout.md and Core.md (Intermission only).

**Options:**
1. *(recommended)* Change Towers.md to Intermission-only. Give the reason: building mid-wave turns every threat into a pause-and-pay, the same reason Core.md gives for repair.
2. Allow mid-wave building at a surcharge (e.g. +50%), so it is an emergency option rather than the default.
3. Allow mid-wave *upgrades* only, never new builds.

### A3 · Tower type selection: "one cycling gesture, no list" vs the build browser (Medium)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Towers.md Rules ("eine einzelne, wiederholbare Cycling-Geste, keine Liste mit Cursor-Navigation") vs the implemented `TowerBuildBrowserPresenter` (a roster with an upgrade card). Input.md names the action `CycleBuildTowerType`, Towers.md names it `PreviousTowerType`/`NextTowerType`, and neither matches the asset.

**Options:**
1. *(recommended)* Amend the rule: a browser is allowed if it opens on the slot, needs no cursor, and closes on Cancel. Replace both action names with the ones in `InputSystem_Actions.inputactions`.
2. Remove the browser and go back to pure cycling, keeping the upgrade info as a world-space tooltip.

### A4 · Refunds and selling: three incompatible rules (High)
**Decision (owner, 2026-09-22): option 1.**
**Where:**
- Shop.md: "Refunds für Shop-Käufe existieren im ersten Scope nicht"; "kein Inventarmanagement".
- WeaponLoadout.md: sell a weapon for 50%; declining a bought weapon refunds it; the loadout panel *is* inventory management.
- Towers.md: no tower refund.
- Economy.md: sell-back is "the only Scrap income outside combat". But Salvage Trickle (Factory), Dividend / Deep Reserves (Skill Tree) and Field Repair all create Scrap or value outside combat.

**Options:**
1. *(recommended)* Add one refund-policy table to Economy.md (per item type: card, weapon, tower, repair; refundable?, rate, when) and delete the scattered sentences. Drop the "only income outside combat" claim.
2. Make it symmetric: towers can be sold for 50% during an Intermission too, and Shop.md's "no refunds" becomes "no refunds for cards".

### A5 · Where Skill Points come from is stated three ways (High)
**Decision (owner, 2026-09-22): option 1.**
**Where:**
- Upgrades.md currency table: SP "earned by run outcomes".
- SkillTree.md Rules: "SP is earned only from run outcomes".
- SkillTree.md Acceptance Criteria: "Completing a stage, a boss and a run each grant SP".
- The binding decision (SkillTree.md, 2026-09-22) is *bosses only*.

**Options:**
1. *(recommended)* Put one earn-rate table in Economy.md for all three currencies, as the only source of truth, and link to it from SkillTree.md and Upgrades.md.
2. Fix the three sentences in place and leave the tables where they are.

### A6 · Respec now costs Alloy, which neither currency spec allows (Medium)
**Decision (owner, 2026-09-22): option 1.**
**Where:**
- Your answer: "once per tree" is free.
- The build (2026-09-22): the first respec per tree version is free, later ones cost 30 Alloy.
- Conflicts: SkillTree.md still lists respec cost as an open question; Upgrades.md lists Alloy's sinks as unlocks and stat upgrades only; SkillTree.md's "keeping them separate" argument is broken by an SP action priced in Alloy.

**Options:**
1. *(recommended)* Record the rule in SkillTree.md and list "paid respec" as Alloy's third sink in Upgrades.md. The cross-currency price is deliberate: it makes respec cost something a player also wants.
2. Price later respecs in SP instead, e.g. 10% of the refunded points are lost, so the currencies stay separate.
3. Keep every respec free but limit it to once per N runs.

### A7 · "No permanent currency may buy what a run can buy" is ambiguous (Medium)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Economy.md "Three currencies" and SkillTree.md Rules, against:
- Alloy tracks Magazine / Damage vs the in-run weapon tiers and Cooling Array (+3 magazine).
- Field Repair (a free Core repair) vs the Scrap-bought repair.
- Forward Foundations (tower discount) vs Scrap tower costs.

**Options:**
1. *(recommended)* Restate the rule: "no permanent currency buys a **run resource** (Scrap, a card, a weapon, a tower, a repair). Permanent *baselines* and *discounts* are allowed, within the ceilings in Upgrades.md."
2. Keep the strict reading and remove the overlapping tracks and nodes.

### A8 · Card stacking rules contradict the 2026-09-21 decision (Low)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Cards.md Rules: duplicates only with an explicit stack rule, and Legendary only in limited offer windows. The appended 2026-09-21 section says every card stacks. The pool size and rarity distribution (30 cards; 8/12/7/3) are stale: `CardPool_Default` holds 43 entries, including 9 weapon cards.

**Options:**
1. *(recommended)* Rewrite the Rules bullets to match universal stacking, and recount the pool from the asset.
2. Exempt Legendaries from stacking: one line, using the preserved `AuthoredStackable`.

### A9 · Four cards occupy the same effect slot (Medium)
**Decision (owner, 2026-09-22): option 2.**
**Where:** Cards.md Balancing ("keine kleine Gruppe von Always-Pick-Cards") and Build Diversity. Rapid Cycler (fire interval ×0.85), Hair Trigger (×0.93), Twin Barrel (×0.88) and Skirmisher's Kit (×0.92, plus projectile speed) all move the same number. Cooling Array was a fifth until today; it now shortens reloads and adds magazine. You heard two of them as "the same", and mechanically they were.

**Options:**
1. *(recommended)* Add a Cards.md rule: two cards of the same rarity may not share their full target set. Re-target the duplicates to targets that exist already, e.g. Hair Trigger → every 5th shot crits, Twin Barrel → +1 projectile and −10% damage.
2. Merge them into one stackable card per rarity (Common ×0.93, Rare ×0.85, …), since stacking is universal now.
3. Keep them, but give each a distinct card-pick cue and icon so that at least the *presentation* differs. This is the weakest option.

### A10 · Weapons.md still states single-weapon rules in its body (Low)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Weapons.md Rules ("eine aktive Weapon zur gleichen Zeit") and "Festgelegte Entscheidungen" (three archetypes), under a later note that supersedes them. Relics.md's Twin Chamber still says "die aktuelle Waffe". Nine weapons exist as of today.

**Options:**
1. *(recommended)* Edit the Rules in place and point the superseded bullets to WeaponLoadout.md.
2. Strike them through (`~~…~~`) so the history stays readable.

### A11 · The modifier composition order is defined five different ways (Medium)
**Decision (owner, 2026-09-22): option 1.**
**Where:**
- SkillTree.md: base → skill → meta unlocks → cards.
- Upgrades.md Rules: base → upgrades → set bonuses → cards.
- Upgrades.md Deviation: base → tier → upgrades → skill → set bonuses & cards.
- Towers.md: base → permanent upgrades → in-run level → cards.
- WeaponLoadout.md: set bonuses before cards.

**Options:**
1. *(recommended)* Add one "Modifier pipeline" table to Combat.md, owning the order for weapons and towers. The other specs link to it.
2. Declare that all multipliers are multiplicative, so order only matters for `Add` operations, and list which targets are additive (pierce, projectiles, magazine, crit chance).

### A12 · Aim assist and device detection are owned by three specs and none (Medium)
**Decision (owner, 2026-09-22): option 2.**
**Where:**
- Combat.md: controller gets medium soft-aim; M&K gets none.
- Weapons.md: global setting, weighted per weapon Low/Medium/High.
- Settings.md Open Questions: "global, per weapon or per device?" is still open.
- No spec says how the *active device* is chosen, which is why an Xbox 360 stick lost to the mouse position (fixed today in code: stick ≥ 0.25 claims, mouse must move ≥ 4 px to reclaim).

**Options:**
1. *(recommended)* Close the Settings question with a link to Combat.md, and add an accessibility slider "Aim assist strength". Add an Input.md rule for active-device switching with the thresholds that are now in code.
2. Make the device explicit: a Settings option "Input device: Auto / Controller / Mouse", with Auto using the thresholds above.

### A13 · What a boss pays is spread over five specs (Medium)
**Decision (owner, 2026-09-22): option 1.**
**Where:**
- Bosses.md: a reward moment.
- Relics.md: a random relic via `TryGrantRandomRelic`.
- WeaponLoadout.md: a guaranteed weapon offer.
- SkillTree.md: SP by stage, plus the first-kill bonus.
- Upgrades.md: Alloy by stage, plus the first-kill bonus.

No single place lists the bundle, so the Run Failed screen, the reward screen and the Bosses spec each have to reassemble it.

**Options:**
1. *(recommended)* Add a "Reward bundle" table to Bosses.md. The other specs keep their numbers and link to it.
2. Make it data: a `BossRewardData` asset (already named in Bosses.md's Data Model but never built) that owns the bundle per boss.

### A14 · Relics: "later expansion" vs shipped, and tuning for a two-stage run (Medium)
**Decision (owner, 2026-09-22): option 1.**
**Where:**
- Relics.md Rules: "Der erste Vertical Slice behandelt Relics als spätere Erweiterung". Relics are live, and Veteran's Cache / Relic Hunter (Skill Tree) grant them at run start, an acquisition path Relics.md does not list.
- The evolution thresholds (6 ranged kills, 8 waves) were "kalibriert" for "6 waves × 2 stages". Runs now have 5 stages.

**Options:**
1. *(recommended)* Add an acquisition table (boss, Skill Tree start relic) and re-derive the thresholds for a 5-stage run, e.g. 20 ranged kills and 18 waves, so evolution lands in Stage 3–4.
2. Keep the thresholds deliberately early, as a Stage-1 payoff, and write down that this is intended.

### A15 · Every stage plays the same waves (Medium)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Waves.md ("Schwierigkeit über Enemy-Komposition"), Stages.md (each stage has an identity through its enemy pool). On disk, Stage 1, 2 and 3 reference the same `WaveData` assets; Stages.md only says this was deliberate *for Stage 2* on 2026-09-08.

**Options:**
1. *(recommended)* Author a `WaveData` set per stage (6 × 5). Shared spawn groups are fine; the wave list should differ.
2. Keep shared waves and make `StageEnemyPoolData` substitute roles at spawn time. Document in Waves.md that this is the escalation mechanism (the field is currently "von keinem Code gelesen").

---

## B — Gaps no spec covers

### B1 · Spawn fairness has no numbers (Medium, has caused a bug)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Waves.md ("Spawn-Positionen müssen fair und lesbar sein"). Enemies spawned on top of the player until today. The code now keeps 3.5 u away from the player; no spec states it.

**Options:**
1. *(recommended)* Add to Waves.md: minimum spawn distance from the player (3.5 u) and from the Core, and what happens when a spawn point is blocked (push away, then mirror, as built).
2. Add a spawn telegraph as well (a 0.6 s marker), so a close spawn is still readable.

### B2 · Which services outlive a scene load (High, has caused a bug)
**Decision (owner, 2026-09-22): option 1.**
**Where:** SaveSystem.md and Audio.md are silent on object lifetime. `LocalProfileSaveManager` called `DontDestroyOnLoad` on the menu's shared GameObject, which carried AudioManager and MusicStateController along. Every return to the menu added a second music loop: the "two sounds after death". Fixed today: the save manager now persists on its own host, and scene instances forward to it.

**Options:**
1. *(recommended)* Add a SaveSystem.md rule that only the save manager persists, on a dedicated object, and every scene owns its own audio. Add an Audio.md rule that exactly one music source plays at any time; the `[AudioProbe]` log checks it.
2. Introduce one bootstrap scene that owns every persistent service (save, settings, audio, meta), with gameplay scenes owning none.

### B3 · No resolution or aspect-ratio contract (High, has caused three bugs)
**Decision (owner, 2026-09-22): option 1.**
**Where:** UI.md, Graphics.md. Three bugs came from resolution differences:
- HUD texts vanished in fullscreen (best-fit minimum larger than the rect).
- Main-menu entries overflowed with Continue shown.
- A blue frame surrounded the arena.

**Options:**
1. *(recommended)* Add a UI.md section:
   - reference resolution 1920×1080, `CanvasScaler` match 0.5;
   - every best-fit text has rect height ≥ 1.3 × its minimum size and vertical Truncate;
   - the camera always *covers* the arena, with backdrop tiles outside it.

   Graphics.md then points to it.
2. Add a resolution test matrix (1280×720, 1920×1080, 2560×1440, 3440×1440) that the autopilot photographs every run.

### B4 · Main menu has no rule for more entries than fit (Medium)
**Decision (owner, 2026-09-22): option 3.**
**Where:** MainMenu.md lists the entries (Continue, New Run, Progression, Settings, Tutorial, Quit) but no layout rule. In fullscreen with Continue visible the list did not fit. Today's build shrinks entries to a 46 px minimum and re-lays them out on resize.

**Options:**
1. Shrink to fit, as built now: simple, but it caps at about 8 entries before text gets small.
2. A scrolling list whose focus is always visible (controller-friendly, but hides entries).
3. *(recommended)* A **two-tier layout**: the one or two primary actions large in the centre, and the global routes as a row of icon buttons along the bottom. Focus moves up/down between the tiers and left/right within the row:

```
            C O R E B O U N D
         ┌─────────────────────┐
         │      CONTINUE       │   ← primary, large
         └─────────────────────┘
         ┌─────────────────────┐
         │       NEW RUN       │
         └─────────────────────┘

  [Progression]  [Tutorial]  [Settings]  [Quit]   ← secondary row
```

### B5 · UI rule "hierarchies live in the scene" vs growing procedural UI (Low)
**Decision (owner, 2026-09-22): option 1.**
**Where:** UI.md Rules (scene is the source of truth; procedural is an editor fallback only) vs pause tabs, loadout panel, tower upgrade card, crit popups, repair prompt and Progression screen, all procedural. CLAUDE.md's "canvas layout is scene-file-driven" applies to three canvases only.

**Options:**
1. *(recommended)* Relax the rule: scenes own frames and fixed positions; dynamic lists, tooltips and world-space prompts may be procedural.
2. Migrate each procedural panel into a canvas scene over time, one per slice.

### B6 · Pause menu: no Settings, and missing from the UI layer list (Low)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Settings.md ("aus erlaubten In-Game-Menüs geöffnet") vs a pause overlay without a settings entry. UI.md's seven layers omit pause, shop, loadout and progression.

**Options:**
1. *(recommended)* Add a fourth pause tab "Settings" (audio volumes as a minimum) and update the UI.md layer list.
2. State in Settings.md that settings are main-menu-only, and why (fullscreen/resolution confirmations mid-run).

### B7 · Tutorial predates half the game (Medium)
**Decision (owner, 2026-09-22): option 2.**
**Where:** Tutorial.md steps cover movement, weapon, resources, tower, card and Core. Nothing teaches the loadout panel (I / Select), reloading, Set Bonuses, Core repair, Core guard, the shop gate or the Progression screen.

**Options:**
1. *(recommended)* Use contextual one-time hints instead of more steps: the first full magazine, the first time a weapon is pending, the first damaged Core in an Intermission. This matches the existing Guard toast.
2. Extend the guided first run with two steps (open the loadout; repair the Core).

### B8 · Keyboard bindings exist only in the asset and in CLAUDE.md (Low)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Input.md has no binding table. The loadout key (I), the QWERTZ rule ("no layout-dependent keys") and the F9/F10 debug keys are documented only in CLAUDE.md or nowhere. The rebind coverage question is still open.

**Options:**
1. *(recommended)* Add an Input.md table (action → Gamepad → Keyboard) generated from the `.inputactions` asset, plus the layout rule.
2. Show the table in Settings → Controls and keep the spec pointing there.

### B9 · No acceptance criteria for delivered art (Medium)
**Decision (owner, 2026-09-22): option 1.**
**Where:** Graphics.md. Assets 85 (`skill_node_key_frame`) and 90 (`weapon_slot_frame`) arrived with a checkerboard *baked into the pixels*, and 86 (`skill_node_sub_frame`) is a node diagram rather than a frame. The same happened earlier to `vfx_boss_telegraph_lance.png` and `character_portrait_frame.png`. Only a human look caught it; the validator checks paths, not content.

**Options:**
1. *(recommended)* Extend `Corebound > Validate Art Pipeline`: flag images whose centre or corners are opaque when the asset is declared a frame or icon, and flag the two-tone checker pattern.
2. Put the requirement into every GRAPHICS_ASSET_PROMPTS entry ("transparent PNG, no background, no checkerboard"), plus a manual checklist on delivery.

### B10 · Balancing targets nobody measures (Low)
**Decision (owner, 2026-09-22): option 1.**
**Where:**
- Waves.md: 60–70 min run.
- WeaponLoadout.md: specialised ±15% of diversified; the budget reachable only by 6-Spread; six slots ≈ three tower upgrades.
- SkillTree.md: 30% branch power cap.

None of these is measured.

**Options:**
1. *(recommended)* Have the autopilot print a run summary: duration per stage, damage by source (weapon / tower / splash), and peak projectile budget.
2. Keep a balancing sheet in `Docs/` with the targets and the last measured value, updated per playtest.

### B11 · Starting weapon: an unlock silently changes it (Medium)
**Decision (owner, 2026-09-22): option 1.**
**Where:** MetaProgression.md / Upgrades.md say unlocks only *widen the pool*. `PlayerCombatController.ResolveWeaponFromMetaProgressionSave` also makes the *last* purchased weapon unlock the run's starting weapon. No spec says so, and the player cannot choose. The three weapon licences added today (Railgun, Nova, Launcher) deliberately carry no `_weaponReward`, so buying them does not change the start weapon.

**Options:**
1. *(recommended)* Split the two meanings: a licence only adds the weapon to offers; the starting weapon is chosen in the New Run panel from every unlocked weapon.
2. Document the current rule ("the most recently licensed starter weapon") and keep one starter licence per weapon.

---

## C — Stale or drifted text inside a spec

These are places where a spec still says something that stopped being true. Most still carry an "open question" that another spec or the build already answered. None of them changes how the game plays. They matter because the next agent (or you) will read the spec, believe it, and work from a wrong premise. That has happened before; see CLAUDE.md, "treat these docs as claims".

### C1 · Economy.md asks three questions that Shop.md already answered (Low)
**Where:** Economy.md, "Open Questions".

**Problem:** It still asks: should the shop use fixed prices or dynamic offers? Is there a reroll? Is there a refund rule? Shop.md settled all three (fixed price categories, no reroll, no card refunds), and A4 now puts the refund rule in one table. A reader of Economy.md alone thinks these are undecided.

**Options:**
1. *(recommended)* Replace the three questions with one line per answer and a link to Shop.md, and to the refund table from A4.
2. Delete the section from Economy.md and track open questions only in `10_OPEN_POINTS.md`, for every spec.

### C2 · MetaProgression.md asks three questions that SkillTree.md and Upgrades.md answered (Low)
**Where:** MetaProgression.md, "Open Questions".

**Problem:** It asks which permanent bonuses are allowed, whether progression is linear or branching, and what is unlocked from the start. The Skill Tree (branching, four branches, Ascension), Upgrades (per-stat ceilings) and the unlock list answer all three. The questions make the meta system look undesigned when it is built.

**Options:**
1. *(recommended)* Answer each question in one line with a link to SkillTree.md or Upgrades.md.
2. Delete the section.

### C3 · SaveSystem.md asks whether there are several save slots (Low)
**Where:** SaveSystem.md, "Open Questions".

**Problem:** "One continuable run or several slots? Cloud saves?" MainMenu.md already decided one local profile with one continuable run. Cloud save was never decided. On Steam it is mostly Steam Cloud configuration, not code.

**Options:**
1. *(recommended)* Mark the slot question as answered (one profile, one run, link MainMenu.md) and keep cloud save as the one real open question.
2. Keep both, but label them "after Early Access".

### C4 · Towers.md asks whether tower-type selection needs an overlay (Low)
**Where:** Towers.md, "Open Questions".

**Problem:** It asks whether the text line is enough or a visual overlay is needed. The build browser (a panel with the tower roster, costs and the upgrade card) is that overlay, and it shipped. Your A3 decision makes the browser the rule.

**Options:**
1. *(recommended)* Close the question: "answered by the build browser (2026-09-22), see A3".
2. Turn it into a new question about the browser itself, e.g. whether it should also show range on the ground.

### C5 · Shop.md says the shop appears after wave 3 of Stage 1 (Low)
**Where:** Shop.md, "Technical Design".

**Problem:** The text describes the first prototype: one shop, after wave 3, Stage 1 only. Today the shop opens after every second wave (seen after waves 2 and 4 on Stage 1 and Stage 3), controlled by `ShopData._openAfterWaveNumbers`.

**Options:**
1. *(recommended)* Update the sentence to the real rule, and name the data field so the rule has one place to change.
2. Change the data to match the old text (shop after wave 3 only). Fewer shop visits make each purchase weigh more, but it is a balance change.

### C6 · Economy.md says every stage has its own shop (Low)
**Where:** Economy.md, "Data Model".

**Problem:** "Jede Stage besitzt ihr eigenes ShopData-Asset" with its own price multiplier. Only `Shop_Stage1` exists, and every stage falls back to it, so prices never rise with stage depth.

**Options:**
1. Author a shop per stage (5 small assets) with rising price multipliers and stage-appropriate offers.
2. *(recommended until there is content to differ)* Change the text to "Stage 1's shop serves every stage; per-stage shops are optional", so the spec matches the build.

### C7 · Factory.md says both tower types feed the Factory; there are four (Medium)
**Where:** Factory.md, "Festgelegte Entscheidungen".

**Problem:** The spec says the gap "only the Support tower produces" was closed by giving both tower types (Ballista, Support) a production node. Mortar and Sentinel were added later and produce nothing, so a player who builds them gets no Factory benefit and cannot tell why.

**Options:**
1. *(recommended)* Add a tower-pulse production node for Mortar and Sentinel, the same data shape as the existing two.
2. Add a Factory.md rule that a tower may be combat-only, and show that on its build-browser card.

### C8 · SkillTree.md says key nodes are rule changes, but four are plain numbers (Low)
**Where:** SkillTree.md, "Structure → Key Node" ("grants a named, rule-changing effect, not a percentage").

**Problem:** Industry key nodes 3–4 (Scrap Surge +15%, Deep Reserves +25 Scrap) and Command 3–4 (War Chest, Relic Hunter) are numeric boosts. The Implementation section admits it, but the rule above still says they are not allowed.

**Options:**
1. *(recommended)* Change the rule to "usually a rule change; a strong numeric effect is allowed where no fitting system exists yet", and list the four exceptions.
2. Build the two missing systems (a pickup magnet and a reward reroll) and replace the four numeric nodes with real rule changes.

### C9 · WeaponLoadout.md: Arsenal impossible, tier cost 0.75 — already fixed (Low)
**Where:** WeaponLoadout.md, "Implementation".

**Problem:** It said Arsenal could not trigger and that tier upgrades cost 0.75 × price × (tier + 1). Both changed on 2026-09-22 (nine weapons; cost factor 1.25).

**Status:** updated in the same pass that created this review. Nothing to decide.

### C10 · Weapons.md: "three weapon archetypes" — already fixed (Low)
**Where:** Weapons.md, "Festgelegte Entscheidungen".

**Problem:** It listed the first three weapons as the whole content wave.

**Status:** the new section "Weapon set 2026-09-22" documents all nine. The old bullet is part of A10 (your decision: edit in place). Nothing else to decide.

### C11 · Towers.md says the Sentinel has no sprite (Low)
**Where:** Towers.md, update of 2026-09-10.

**Problem:** It says the Sentinel uses a coloured square because no art existed. Art exists now (`Assets/Art/Towers/tower_sentinel_topdown.png`), but it is loaded from a path hard-coded in `TowerRuntimeController`, not from `Tower_Sentinel.asset` (its `_sprite` is empty). The spec is wrong, and the build breaks the project's data-driven rule: a renamed file fails silently, and the art validator cannot see it.

**Options:**
1. *(recommended)* Move the path into the tower's data, as every other tower does, remove the hard-coded one, and update the paragraph.
2. Only update the paragraph to say where the art comes from today.

### C12 · Nineteen data types are described that do not exist (Low)
**Where:** the "Data Model" sections of Player.md, Combat.md, Cards.md, Towers.md, Factory.md, MetaProgression.md, Resources.md and WeaponLoadout.md.

**Problem:** These specs list ScriptableObject types as if they existed:
- Combat and Player: `CombatEncounterData`, `PlayerStatProgressionData`, `PlayerFeedbackData`, `PlayerInteractionData`.
- Cards and Towers: `CardTagData`, `TowerTargetingData`, `TowerEffectData`, `TowerSlotData`.
- Factory: `ProductionTriggerData`, `ResourceConversionData`.
- Meta progression: `MetaCurrencyData`, `UnlockData`, `UnlockTrackData`, `ProgressRewardData`.
- Resources: `ResourceDropTableData`, `ResourceGenerationData`, `ResourceSpendRuleData`, `ResourcePickupData`.
- Weapon loadout: `WeaponLoadoutData`.

None of these exists. The data lives somewhere else (for example, unlocks are entries inside `MetaProgressionDefinitionData`) or is not needed. An agent asked to "add a new tower slot type" would go looking for `TowerSlotData`.

**Options:**
1. *(recommended)* Mark each one "planned, not built" and say in one line where that data lives today, the way Economy.md and Core.md already do.
2. Delete them from the specs.
3. Build the few that would carry real data. `TowerSlotData` is the likeliest; slot positions are currently per-stage arrays.

---

## D — Broken references

### D1 · Seven specs link to a VFX.md that does not exist (Low)
**Where:** Cards.md, Combat.md, Core.md, Enemies.md, Graphics.md, Stages.md, Towers.md all link `./VFX.md`.

**Problem:** There is no `VFX.md`. The VFX documentation is split over `VFX_combat_vfx_sheet_essentials.md`, `VFX_stage1_ground_tileset.md` and `VFX_stage1_prop_set.md`, so every one of those links leads nowhere.

**Options:**
1. *(recommended)* Create a short `VFX.md` that lists the three files and what each covers; every existing link then works.
2. Change each of the seven links to point at `VFX_combat_vfx_sheet_essentials.md`.

### D2 · Two links use the wrong capitalisation (Low)
**Where:** Stages.md and Graphics.md link `vfx_stage1_prop_set.md`; the file is `VFX_stage1_prop_set.md`.

**Problem:** Windows ignores the difference, so the link works on your machine. It breaks on GitHub, a Linux build server, or any other case-sensitive system.

**Options:**
1. *(recommended)* Fix the two links to the real file name.
2. Rename the file to lowercase and update every link to it.

---

## Status and next step

Decisions for A1–A15 and B1–B11 were recorded on 2026-09-22 (the **Decision** line under each finding). C9 and C10 are already done. C1–C8, C11, C12, D1 and D2 are still open for a decision; each has a recommended option.
