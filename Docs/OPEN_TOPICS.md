# Open Topics — Corebound

Version 2.2 · 2026-09-30 · **The single live list.** Closed topics are deleted, not archived — git
history holds what was fixed and why. Do not start a new report file; add here.

`08_IMPLEMENTATION_BACKLOG.md` is the plan. This is the punch list. Every open point carries a
suggestion; points that need an owner decision are marked **Decide**.

**Closed 2026-09-30** (overnight session, all committed; details in the commits and the linked
docs):
- **Alloy quests** built as decided (A1-2, A2, A3-1, A4-1, A5-1): weapon tracks at base 3, 31 Alloy
  quests in a second section of the Quests tab — `Specs/DIY/LEVELLING_QUESTS_PARAGON_2026-09-29.md`.
- **New card mechanics** built as decided (C1-1 … C6-1): Poison, Shock, Target Painter, four
  support cards, Sentry Drone — `Specs/DIY/NEW_CARD_MECHANICS_2026-09-29.md`.
- **Playtest 2026-09-29:** XP per wave (25 × stage) and a boss lump (250 × stage), shown on the
  wave banner and the run-end screen; quest-list scrolling; the Core never spawns within 4.5 units
  of a gate; the aim jump (three causes, all fixed — see the session log below).
- **3.1 → 1** hardware cursor; **3.2 → 3** diegetic stage change (the arena crossfades, the Core
  holds its place); **3.3** tower slot traits (option 3, built 2026-09-29).
- **Section 4, all five decided items:** `bossN` autopilot variant with phase logging (a);
  `StageEnemyPoolData` deleted (a); card offers weighted by tag with one knob (a); every
  multi-cell sheet's grid in `SpriteSheetData` assets (a); Boss HUD, notification and relic reveal
  in the authored `OverlayCanvas` scene (b).
- **Found and fixed on the way:** weapon cards in rewards had never rolled a rarity since
  2026-09-25 (misplaced code); the autopilot kited at 5.5 units with the 2.7-unit Scorcher and lost
  every run in wave 1; the autopilot never targeted the boss itself; Poison, Shock and the mark
  first used colours VFX.md reserves; five art prompts wrongly said "not wired".

---

## 1 · Decide — new concepts

### 1.1 Character classes — **Decide K1–K6**

`Specs/DIY/CHARACTER_CLASSES_2026-09-30.md`: five classes (Vanguard, Engineer, Salvager,
Custodian, Catalyst), each a signature rule plus its own tree, kit, card leaning and small stat
differences; free unlocks through quests that already exist.
**Suggestion:** K1-1 (Engineer as the only second class first), K2-1, K3-1, K4-1, K5-1, K6-2 for
Engineer. Engineer uses only finished systems and is the most different from Vanguard.

### 1.2 Story — **Decide S1–S5**

`Specs/DIY/STORY_2026-09-30.md`: the Core speaks in one or two lines at moments the game already
pauses; the full story (premise, five stages, boss lines EN/DE, ending, twelve Core Logs) is
written. **Suggestion:** S1-2, S2-1, S3-1, S4-1, S5-1. The in-run lines are half a day of wiring
and would be the first thing a player notices.

### 1.3 Files that can go — **Decide** (nothing is deleted without your word)

- `weapon_rapid_sprite.png`, `weapon_spread_sprite.png`, `weapon_beam_sprite.png` — superseded by
  the two projectile sheets.
- `combat_vfx_sheet_essentials.png` — superseded on 2026-09-08 by the individual VFX files; only
  code comments still name it.
- The ten `WeaponModifier_*` and nine `CardEffect_*` assets the old repurposed cards used
  (listed in NEW_CARD_MECHANICS "As built") — referenced by nothing since 2026-09-30.

**Suggestion:** delete all of them in one commit; git keeps them.

---

## 2 · Art and audio

### 2.1 Art wired to nothing (8 files; 4 of them are 1.3)

Re-audited 2026-09-30 with code comments excluded: **112 files, 104 wired, 8 unwired.** Five that
the last version listed are wired now (four skill-graph frames, the factory tier indicator).

- **Tutorial (2):** `tutorial_prompt_panel.png`, `tutorial_glyph_strip.png` — tutorial beats reuse
  the pause overlay. **Suggestion:** give the beats their own small panel in the `OverlayCanvas`
  scene (authored since today), with the glyph strip showing the button for the current device.
- **Main menu (2):** `route_panel_frame.png`, `glyph_hint_capsule.png` — nobody is blocked.
  **Suggestion:** wire the frame behind the route panels and the capsule behind each glyph hint;
  one screenshot pass of the main menu.

### 2.2 Art to deliver

- **Asset 108** — third row of the card tag icon sheet (Poison, Shock, Support, Summon). Until
  then those tags borrow existing icons.
- **Asset 109** — the Sentry Drone sprite (a cyan diamond until then).

**Suggestion:** both prompts are in GRAPHICS_ASSET_PROMPTS with delivery notes; after delivery,
mirror and run Validate Art Pipeline.

### 2.3 Audio still open (2 cues)

**28 cues — 26 wired, 2 unwired**, both blocked on a missing moment:
- **Audio 24, run victory sting** — **Suggestion:** play it on the story's ending card (S4-1),
  which is the first-clear moment it was written for.
- **Audio 25, credits loop** — **Suggestion:** a plain scrolling credits panel reachable from the
  ending card and the Records screen.

### 2.4 Art still wrong

- `tower_info_card_frame.png` bakes English "Strong against / Weak against" into the art, so the
  German build shows English. **Suggestion:** re-render without text; the presenter already
  draws labels.
- `stage2/3/4/5_ground_tileset.png` are JPEG data behind a `.png` name. **Suggestion:** leave
  until one of them needs transparency, then re-export as real PNG.
- The splash sheet is a seven-frame mine, not the five-frame shockwave Asset 104 asked for; it now
  plays all eight cells (2026-09-30). **Suggestion:** keep; it reads well at speed.

---

## 3 · Agreed work still open

### 3.1 Five-run test campaign — **still open**

Never run. It is now also the only way to settle three numbers that are arithmetic today: XP per
complete run (~6,500 estimated), Alloy per run (~60), and whether the Paragon cost (2,500, set for
the old XP) still means "about one run".
**Suggestion:** an autopilot `campaignN` variant that plays N runs back to back on a scratch
profile without god mode, and logs XP, Alloy, SP, stage reached and time per run. Then retune
from real numbers.

### 3.2 UI theme unification — **started**

`ProceduralUIBuilder` resolves `UIThemeData`; one presenter migrated. **Suggestion:** do the
Records panel next (it gained the Cards tab and would gain the Logs tab of S1-2), one presenter per
session with a screenshot between each. The Boss HUD, notification and relic reveal no longer
need this — they are authored now.

### 3.3 God-object refactors — **step 1 of 4 done**

Open: HUD bars out of `WaveStatePresenter`, spawn-gate visuals out of `StageArenaArtPresenter`,
stage-transition orchestration out of `WaveManager`.
**Suggestion:** stage transitions next. Today's crossfade added a second path through
`AdvanceToNextStage` (fade vs. instant rebuild), which is exactly the seam to cut along.

### 3.4 Stage atlases onto `SpriteSheetData`

The six multi-cell sheets moved into data today; the stage ground tilesets (8 × 4 constants) and
the prop sheets (a 47-entry pixel-rect dictionary in `StageArenaArtPresenter`) did not.
**Suggestion:** one sheet asset per stage prop set with named cells; it also shrinks the presenter
for 3.3.

### 3.5 Skill graph authored positions

The computed layout is readable; `_graphPosition` values are unset.
**Suggestion:** leave until a layout complaint.

---

## 4 · Verification debt — built, compiled, not proven

Each line says what is missing and how to close it.

- **Card mechanics:** the two heals (the autopilot never dropped below full health before the
  boss), Overclock Beacon, Core Aegis regeneration, Target Painter stacking, the mark ring and
  poison tint on screen. **Suggestion:** a `cards` run without god mode on Stage 3, plus a
  screenshot probe at the first poisoned or marked enemy.
- **Alloy quest counters and the arsenal / collection quests** in a real run. **Suggestion:**
  log quest progress deltas at run end in every autopilot run.
- **Per-wave XP after a real death** at the Stage 1 boss (≈ 210 XP expected). **Suggestion:** read
  it off the Run Failed screen in your next playtest — one glance.
- **Hardware cursor** (3.1 → 1): screenshots cannot capture the OS cursor. **Needs your eyes.**
- **Main-menu profile label** showed "Level 5" at level 70 after leaving the Progression screen
  (2026-09-29). The summary now flushes queued saves before reading; not re-checked.
- **Validate Art Pipeline** now checks sprite sheets, but its dialog is modal, so no agent can run
  it. **Suggestion:** a request-file trigger that writes the report to the log without a dialog.

---

## 5 · Smaller items — two suggestions each

**"Boss Defeated" notice overlaps the relic reveal** (both appear at once after a boss).
(a) Move the notice lower in `OverlayCanvas` — a scene edit, no code. (b) Hold the relic reveal
until the notice has faded.

**Weapon cards now really roll Rare/Epic/Legendary** (they never did before 2026-09-30), so runs
will see stronger weapon copies than anyone has played with.
(a) Watch it in the campaign (3.1) before touching numbers. (b) Halve the Legendary chance to 1%
now as a precaution.

**Bosses die in 5–12 s** in `bossN` — but Profile 2's tree is full from the ascension test.
(a) Measure with `boss1` on a fresh profile first. (b) If it holds, scale boss health with the
number of weapon slots filled.

**Tag affinity default +0.35.**
(a) Keep and log how often offered cards match the build (the OFFER log already prints weights).
(b) Make it per class (K5-1).

**`GetComponent<T>() ?? AddComponent<T>()` on native components** — in the Editor a missing
component is a fake-null that `??` treats as present. It broke today's stage change once;
`CardRewardPresenter`, `ShopPresenter`, `WaveStatePresenter` and `ProceduralUIFactory` use the same
pattern on `CanvasGroup`, `CanvasRenderer`, `Image` and `Text`.
(a) One `GetOrAdd<T>` helper and a sweep. (b) Leave the ones on components that are always present
and fix only `CanvasGroup`.

**Stage 1 is named "Stage 1"** while Stages 2–5 have names (and the art and story call it the
Foundry).
(a) Rename it "Foundry". (b) Keep numbers everywhere and show names as subtitles.

---

## 6 · Unplayed

Verified to compile and, where an autopilot run reaches it, to run — but not played by a human:
- **2026-09-30:** all ten new cards; the stage crossfade; tag-weighted offers and rolled weapon
  rarities; the three canvases moved to `OverlayCanvas`; the sheet-driven projectiles and icons;
  Poison/mark/shock colours.
- **2026-09-29:** Alloy quests; the quest scroll fix; XP per wave; Core placement away from
  gates; the aim-jump fixes and the hardware cursor.
- **2026-09-27/28:** the enemy stuck-watchdog, tower projectiles, the 5-second spawn delay, the
  reward layout, relic text fitting, HUD scale in the pause menu, and the second attempt at the
  profile-swap Continue fix — worth a deliberate check, because the first attempt looked correct
  and was not.

---

## Session log

**2026-09-30 (overnight).** Implemented both decided concepts, the playtest fixes, both decided
transitions and all five Section-4 items; wrote the class and story concepts. Verified with the
autopilot on Profile 2: `cards1`, `cardsgod1`, `god1`, `boss1` (all five bosses, 2/2 phases),
`arsenalgod1`. The aim jump had three causes: synchronous save writes on every kill and hit
(frame hitches — writes are now batched), a resting pad's stick drift taking the device back from
the mouse on every key release (a stick now needs a real push to take over), and a software cursor
drawn per frame (now the OS hardware cursor). One process error: a compile while an autopilot run
was still playing produced an exception flood from the domain reload; no code was affected.
