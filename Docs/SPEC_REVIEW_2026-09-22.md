# Spec Review 2026-09-22 — decisions and outcome

All 39 findings of the original review (A1-A15 contradictions, B1-B11 gaps, C1-C12 stale text,
D1-D2 broken references) were decided by the owner on 2026-09-22 and implemented the same day. The
findings themselves are gone from this file - they are solved, and the specs now say what is true.
What remains is the record of what was decided, so a later reader can see why a spec changed.

Scope of the original pass: all 29 files in `Docs/Specs/`, read in full and checked against each
other and against the code and assets on disk.

---

## A — Contradictions between specs

| # | What was wrong | Decision | Where it now lives |
| --- | --- | --- | --- |
| A1 | One Interact press could build a tower, buy a Core repair and raise the Core guard at once | Priority list, one owner per press | `Player.md` interaction priority; `InteractRouter` |
| A2 | Towers.md allowed building mid-wave, the code never did | Intermission only | `Towers.md` Rules |
| A3 | "One cycling gesture, no list" vs the build browser; three different action names | Browser allowed, names corrected | `Towers.md` Rules, `Input.md` bindings |
| A4 | Three incompatible refund rules across four specs | One refund table | `Economy.md`, "Refunds und Verkauf" |
| A5 | Skill Points earned three different ways | One currency table | `Economy.md`, "Währungen und ihre Quellen" |
| A6 | Paid respec existed in no currency spec | Recorded as Alloy's third sink | `SkillTree.md`, `Upgrades.md` |
| A7 | "No permanent currency may buy what a run buys" was ambiguous | Rule restated: no run *resources*; baselines and discounts allowed | `Economy.md`, "Trennungsregel" |
| A8 | Card rules contradicted universal stacking; pool count stale | Rules rewritten | `Cards.md` Rules |
| A9 | Four cards shared the fire-interval effect | Merged to one per rarity: Hair Trigger (Common), Rapid Cycler (Rare) | Twin Barrel out of the pool, Skirmisher's Kit is now projectile speed only |
| A10 | Weapons.md still described one active weapon and three archetypes | Edited in place | `Weapons.md` Rules and decisions |
| A11 | The modifier order was defined five ways | One pipeline table | `Combat.md`, "Modifier-Pipeline" |
| A12 | Aim assist owned by three specs; no device rule | Device setting Auto / Controller / Mouse | `Settings.md`, `Input.md`; `SettingsOptionKey.InputDevice` |
| A13 | Boss rewards spread over five specs | One reward bundle table | `Bosses.md`, "Reward-Bundle" |
| A14 | Relics "later expansion"; evolution tuned for a two-stage run | Acquisition table; thresholds 20 kills / 18 waves | `Relics.md`, "Erwerb und Evolution" |
| A15 | Every stage played the same six waves | Own `WaveData` set per stage | `Waves.md`; `Wave_S2_01` .. `Wave_S5_06` |

## B — Gaps no spec covered

| # | What was missing | Decision | Where it now lives |
| --- | --- | --- | --- |
| B1 | No numbers for fair spawns | 3.5 u from the player, push then mirror | `Waves.md` Rules |
| B2 | Nothing said which services survive a scene load | Only the save manager persists, on its own object | `SaveSystem.md`, `Audio.md` |
| B3 | No resolution or aspect-ratio contract | Reference 1920x1080, best-fit rules, camera covers the arena | `UI.md`, "Auflösung und Seitenverhältnis" |
| B4 | The menu had no rule for more entries than fit | Two-tier layout | `MainMenu.md`, "Layout"; `MainMenuCanvasPresenter` |
| B5 | "Scene owns the hierarchy" vs growing procedural UI | Frames in the scene, dynamic lists procedural | `UI.md` Rules |
| B6 | No settings in the pause menu | Fourth pause tab | `Settings.md`; `WaveStatePresenter` |
| B7 | The tutorial predated half the game | Two more guided steps | `Tutorial.md`; `TutorialStep_WeaponLoadout`, `TutorialStep_CoreRepair` |
| B8 | Bindings existed only in the asset | Binding table plus the layout rule | `Input.md`, "Bindungen" |
| B9 | No acceptance criteria for delivered art | Five criteria, two checked automatically | `Graphics.md`; `Corebound > Validate Art Pipeline` |
| B10 | Balancing targets nobody measured | Autopilot prints a run summary | `[Autopilot] SUMMARY` line |
| B11 | A weapon licence silently changed the starting weapon | Licence and starting weapon split | `MetaProgression.md`, "Startwaffe"; New Run panel |

## C — Stale text, and D — broken references

| # | What was wrong | Decision |
| --- | --- | --- |
| C1 | Economy.md asked three questions Shop.md had answered | Answered in place, with links |
| C2 | MetaProgression.md asked three answered questions | Answered in place, with links |
| C3 | "One run or several slots?" | **Three profiles**, each with its own save state - `ProfileSelection`, a Profile route in the main menu |
| C4 | Towers.md asked whether type selection needs an overlay | Closed: the build browser is that overlay |
| C5 | Shop.md said the shop appears after wave 3 of Stage 1 | Data now matches the text: one shop per stage, after wave 3 |
| C6 | Economy.md claimed a shop per stage | Authored: `Shop_Stage1` .. `Shop_Stage5`, price multiplier 1.0 to 1.45 |
| C7 | Factory.md said both tower types produce; there are four | Mortar and Sentinel production nodes added |
| C8 | Key nodes were meant to be rules, four were percentages | Built the missing systems: **Magnetise** (pickup magnet) and **Wider Draft** (reward reroll) replace Scrap Surge and War Chest |
| C9, C10 | Arsenal "impossible", tier cost 0.75, "three archetypes" | Already corrected earlier the same day |
| C11 | Towers.md said the Sentinel has no sprite | Art path moved from code into `TowerData` for every tower |
| C12 | Nineteen data types were described that do not exist | Deleted from the specs, with one line saying where the data lives |
| D1 | Seven specs linked a `VFX.md` that did not exist | Written: `Docs/Specs/VFX.md` |
| D2 | Two links had the wrong capitalisation | Fixed |

---

## What is still open

- The new wave sets for stages 2-5 are generated compositions, not playtested. Density and elite
  placement are first guesses (`Waves.md`, "Eigene Wellen pro Stage").
- The three-profile UI switches profiles but cannot delete one; a profile is emptied by deleting its
  folder.
- Assets 101-105 (weapon projectiles, weapon icons, flame/frost impacts, splash burst, nova ring)
  are specified in `GRAPHICS_ASSET_PROMPTS.md` and not yet created. The icon sheet is already wired
  by grid cell, so the HUD picks it up the moment the file exists.
- Cloud saves remain the one genuinely open question in `SaveSystem.md`.
