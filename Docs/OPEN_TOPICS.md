# Open Topics — Corebound

Version 2.3 · 2026-10-01 · **The single live list.** Closed topics are deleted, not archived — git
history holds what was fixed and why. Do not start a new report file; add here.

`08_IMPLEMENTATION_BACKLOG.md` is the plan. This is the punch list. Every open point carries a
suggestion; points that need an owner decision are marked **Decide**.

**Closed 2026-09-30 / 10-01** (second session, all committed; details in the commits and the
linked docs):
- **Character classes** built as decided (K1-3, K2-1, K3-2, K4-1, K5-1, K6-2) with ten class cards —
  `Specs/DIY/CHARACTER_CLASSES_2026-09-30.md`, "As built".
- **Story** built as decided (S1-2, S2-1, S3-2, S4-1, S5-1): the Core's voice, premise, ending,
  Core Logs and credits — `Specs/DIY/STORY_2026-09-30.md`, "As built". Audio 24 (victory sting) and
  Audio 25 (credits loop) are wired, so all 28 audio cues are in use.
- **Assets 108 and 109** delivered and wired (Poison/Shock/Support/Summon tag icons, the drone).
- **1.3** the 23 superseded files deleted.
- **Section 3, all five:** the five-run campaign (3.1, results below), the Records view on the UI
  theme (3.2), the three remaining god-object cuts (3.3: stage transitions, spawn gates, health
  bars), stage ground and prop sheets in `SpriteSheetData` (3.4), skill-graph positions baked (3.5).
- **Section 5, all six decided items:** the relic reveal waits for "Boss Defeated" (b); weapon
  rarities watched in the campaign (a); boss times measured on a fresh profile (a); tag affinity
  per class (b); `GetOrAdd` helper and a 21-site sweep (a); Stage 1 is the Foundry, every stage has
  a German name (a).
- **Section 2.1:** tutorial beats have their own panel (Assets 06 and 28); the main-menu route
  panels and glyph hints use Assets 29 and 30.
- **Section 4:** Validate Art Pipeline runs from `ArtPipelineRequest.txt` without a dialog; every
  autopilot run logs its quest deltas (the Alloy, arsenal and collection quests count in real runs);
  a death at the Stage 1 boss paid +204 and +206 XP (≈ 210 expected); the main-menu profile label
  matched the save in every menu screenshot (Level 3, Paragon 113 and 115).

---

## 1 · Decide

### 1.1 Balance from the campaign — **Decide**

The campaign (3.1) ran twice on a wiped Profile 2, as the Vanguard with the start weapon and no
Skill Points or Alloy spent between runs (the autopilot does not shop between runs):

| | Real runs (`campaign5`, no god mode) | Full clears (`campaign2 god`) |
| --- | --- | --- |
| Outcome | 4 runs: died Stage 1 wave 4, wave 4, boss, boss; the 5th wedged (autopilot bug, fixed) | 2 of 2 cleared all five stages |
| Time | 121 / 107 / 175 / 424 s | 755 / 678 s |
| XP | +105 / +99 / +204 / +206 | **+6,556 / +6,550** (estimate was ~6,500) |
| Levels, SP | level 3 after four runs; +2 SP per boss-reaching run | +10 / +5 levels (to 11, then 16); +28 / +10 SP |
| Alloy | +0 / +0 / +2 / +2 | +1,117 / +217 (first clears pay one-time quests) |
| Bosses | Stage 1 boss lost in 12–13 s, twice | S1 20–30 s, S2 14 s, S3 8–9 s, S4 6–9 s, S5 6 s |
| Weapon offers | 8 offers: 7 Common, 1 Rare | 36 offers: 21 Common, 12 Rare, 3 Epic, 0 Legendary |

Three of the four real losses were "Core destroyed". The first clear's Alloy is inflated twice by
the test itself: god mode completes all five "Core untouched" quests, and a one-slot loadout all
five "one weapon type" quests; the repeatable part of a clear is boss and elite Alloy, about 50–70.

Four numbers to decide:
- **Paragon cost.** A clear pays ~6,550 XP; a Paragon level costs 2,500, so a clear is ~2.6
  Paragon levels, not the intended "about one". (a) Raise it to ~6,500. (b) Keep 2,500 and let
  Ascension come faster. Level 70 needs ~205,000 XP, about 31 clears either way.
- **Boss health (Open Topics 5 (a), measured).** From Stage 3 on, bosses die in 6–9 s even on a
  fresh profile with no tree — the cards and weapons a run collects are enough. (a) Raise Stage 3–5
  boss health (×2 would put them at 12–18 s). (b) Scale boss health with the weapon slots filled,
  as suggested before.
- **Stage 1 for a new player.** The autopilot, weaker than a person and spending nothing, never
  beat the Stage 1 boss in four tries. (a) Wait for a human first run before touching it. (b)
  Soften waves 4–7 and the boss now.
- **Legendary weapon cards (5 (a), watched).** None in 44 offers; at the configured chance about
  one was expected. (a) Keep watching. (b) Log the roll itself, not only the result.

**Suggestion:** Paragon (a); bosses wait for a human full run; Stage 1 (a); Legendary (b).

### 1.2 Class trees are a first pass — **Decide**

Each class tree is the Vanguard's tree with one class keystone inserted; the other three branches
are still the Vanguard's. The concept's bigger nodes (a fifth tower slot, reroll interest, "Shock
arcs to poisoned enemies first") are not built.
(a) Play the classes first and decide per class what its tree is missing.
(b) Build the concept's three bigger nodes now.
**Suggestion:** (a) — none of the five has been played by a human yet.

### 1.3 Profile 1 — **Decide**

`Profile_01` holds an old test profile (last written 2026-09-29, with a
`run.json.bak_before_fresh_test`). The autopilot no longer touches it.
(a) Delete it. (b) Keep it.
**Suggestion:** your call; nothing depends on it.

---

## 2 · Art and audio

### 2.1 Art to deliver

- **Assets 110–113** — Engineer, Salvager, Custodian and Catalyst top-down sprites (K6-2). Until
  then each class wears the Vanguard sprite in its colour. **Suggestion:** Engineer first; after
  delivery, mirror and run Validate Art Pipeline.

### 2.2 Art still wrong

- `tower_info_card_frame.png` bakes English "Strong against / Weak against" into the art, so the
  German build shows English. **Suggestion:** re-render without text; the presenter already draws
  labels.
- `stage2/3/4/5_ground_tileset.png` are JPEG data behind a `.png` name. **Suggestion:** leave until
  one needs transparency, then re-export as real PNG.
- The validator flags five frames with an opaque centre (`gameplay_modal_frame`,
  `tower_info_card_frame`, `button_primary_frame`, `route_panel_frame`, `skill_node_sub_frame`). All
  five are used as panels with their fill, where an opaque centre is right. **Suggestion:** keep;
  teach the validator a "panel" exception list rather than re-rendering.

---

## 3 · Agreed work still open

### 3.1 UI theme unification — **three presenters done**

`ProceduralUIBuilder` resolves `UIThemeData`; the skill graph, the Factory chain indicator and the
Records view are on it.
**Suggestion:** next the route panel itself (`MainMenuRoutePanelPresenter`), one presenter per
session with a screenshot between each.

---

## 4 · Verification debt — built, compiled, not proven

Each line says what is missing and how to close it.

- **Classes:** the Salvager's magnet range and tier-1 chain floor (no log line shows either), the
  class cards actually being offered (Rare/Epic, none came up in the probes), and the unlock
  announcement on a profile that really meets a condition. **Suggestion:** one `class salvager`
  run with a Factory log line, and the announcement in your first playtest of a class.
- **Card mechanics:** Leech Rounds and Field Dressing are verified (2026-10-01: after two probe hits
  the player healed on 9 kills and 2 wave clears); Poison, Shock, the drone and Reactive Plating
  are verified by their counters. Still open: Overclock Beacon's tower speed-up, Core Aegis
  regeneration (the Core was never hurt in an intermission), Target Painter stacking, and the mark
  ring and poison tint on screen — the probe photographs both moments, but at capture scale they
  cannot be told apart from the impacts around them. **Needs your eyes** for the two visuals;
  the other three need a counter each.
- **Story:** the victory sting and the credits loop by ear, and the German lines on screen.
  **Needs your ears and eyes** — switch the language once in Settings.
- **Hardware cursor** (3.1 → 1 of 2026-09-30): screenshots cannot capture the OS cursor. **Needs
  your eyes.**
- **The tutorial beat panel** was photographed at the first beat only. **Suggestion:** the
  `tutorial` variant through to the end, one screenshot per beat.
- **Health bars after the 3.3 move:** drawn after a stage change, but their drain and hit flash
  were not re-observed (god mode takes no damage). **Suggestion:** any non-god run's screenshot
  after a hit.

---

## 5 · Smaller items — two suggestions each

**The loadout HUD shows the Vanguard's counted copies** — 3 Rapid read "Rapid x4", because the
Set Bonus counts one more.
(a) Show "3 (+1)" so the bonus is visible as a bonus. (b) Leave it; the tier name is what matters.

**The skill-tree version is account-wide** while trees are now per class: bumping one class's
tree version would hand every class a free respec.
(a) Store the version per class next to the node ranks. (b) Keep one version and bump all five
trees together by convention.

**Records hides the ten class cards**, so the collection grid and quest agree.
(a) A class row under the grid, counted separately. (b) Leave them out.

**The campaign autopilot never spends Skill Points or Alloy**, so five campaign runs are five
first runs with more XP, not a player growing.
(a) Let the campaign buy the cheapest available node and upgrade after each run. (b) Keep it as
the worst case and read it that way.

**Profile 3 was repaired by hand on 2026-09-30** after one autopilot god run played on it (see
the session log). Two values in the repair are best estimates: the Scorcher kill counter (25) and
total playtime (≈ 584 s). Both the repaired and the unrepaired save are kept in
`%USERPROFILE%/AppData/LocalLow/DefaultCompany/Corebound/Backups_2026-09-30/`.
(a) Keep the repaired save. (b) Put the unrepaired one back and keep what the god run paid
(1,101 Alloy, 19 SP, ten quests).

---

## 6 · Unplayed

Verified to compile and, where an autopilot run reaches it, to run — but not played by a human:
- **2026-09-30 / 10-01:** all five classes and their ten cards; the New Run class picker and the
  per-class Progression screen; the Core's voice, the premise, the ending, Core Logs and credits; the
  relic reveal held behind the boss notice; the tutorial beat panel; the new main-menu frames.
- **2026-09-30 (overnight):** the ten new cards; the stage crossfade; tag-weighted offers and rolled
  weapon rarities; the canvases in `OverlayCanvas`; the sheet-driven projectiles and icons.
- **2026-09-29:** Alloy quests; the quest scroll fix; XP per wave; Core placement away from gates;
  the aim-jump fixes and the hardware cursor.
- **2026-09-27/28:** the enemy stuck-watchdog, tower projectiles, the 5-second spawn delay, the
  reward layout, relic text fitting, HUD scale in the pause menu, and the second attempt at the
  profile-swap Continue fix — worth a deliberate check, because the first attempt looked correct
  and was not.

---

## Session log

**2026-09-30 → 10-01 (second session).** Built the five classes with their trees and ten class
cards, the story, Assets 108/109, the 1.3 deletions, all six Section 5 decisions, all five Section
3 items, the four Section 2.1 UI files, and the open Section 4 probes; rewrote this file (v2.3).
Verified with the autopilot on Profile 2 throughout — new variants `class`, `campaign`, `tutorial`,
`records` and `stop` — see each item above for what was seen and what was not.

Three things went wrong and are worth knowing:
- **One god run played on the owner's Profile 3** (2026-09-30, `boss1`): the active profile had
  been switched back for the owner and nothing switched it again before the next run, so it paid
  1,101 Alloy, 19 SP and ten quests into the real save. Profile 3 was rebuilt by hand from
  `history.json.bak` and the run's own history entry; two values are estimates (see Section 5).
  The autopilot now always switches to Profile 2 and restores the owner's profile when Play ends.
  Both saves are in `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Corebound/Backups_2026-09-30/`.
- **`Time.timeScale` outlives Play Mode in the Editor.** Stopping Play while an overlay held the
  freeze started the next session — main menu included — at 0. It cost three stalled runs before
  it was found; every session now starts unfrozen.
- **The fifth real campaign run wedged** in front of a Relay Warden in a corner: the autopilot's
  wall push held it out of the Scorcher's reach. Fixed; Play Mode was then ended from outside the
  autopilot, so that run has no record.

Profile 2 was wiped twice for the campaign and restored afterwards from its backup (Paragon 115,
full trees).

**2026-09-30 (overnight).** Implemented both decided concepts, the playtest fixes, both decided
transitions and all five Section-4 items; wrote the class and story concepts. Verified with the
autopilot on Profile 2: `cards1`, `cardsgod1`, `god1`, `boss1` (all five bosses, 2/2 phases),
`arsenalgod1`. The aim jump had three causes: synchronous save writes on every kill and hit
(frame hitches — writes are now batched), a resting pad's stick drift taking the device back from
the mouse on every key release (a stick now needs a real push to take over), and a software cursor
drawn per frame (now the OS hardware cursor). One process error: a compile while an autopilot run
was still playing produced an exception flood from the domain reload; no code was affected.
