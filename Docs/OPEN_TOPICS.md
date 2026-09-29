# Open Topics — Corebound

Version 2.1 · 2026-09-29 · **The single live list.** Closed topics are deleted, not archived — git
history holds what was fixed and why. Do not start a new report file; add here.

`08_IMPLEMENTATION_BACKLOG.md` is the plan. This is the punch list.

**Closed 2026-09-28** and removed from this file: balance of the 2026-09-22/23 additions, features
no automated run reaches, the arena work being unplayed, Shop/Reward looking alike, and the open
vision questions. Also settled: a weapon pending across a stage change stays as it is — **no** short
Intermission is added.

---

## 1 · Art and audio still open

### 1.1 Art wired to nothing (12 files)

Audited across every `.cs`, `.asset`, `.unity` and `.md`, matching filenames **and** the guid in each
`.meta`. **112 art files, 100 wired, 12 unwired, 0 orphans.** StreamingAssets mirrors it exactly.

**Superseded — safe to delete on your word** (3): `weapon_rapid_sprite.png`,
`weapon_spread_sprite.png`, `weapon_beam_sprite.png`. Eleven of twelve projectiles draw from
`projectile_sheet_core.png` or `projectile_sheet_weapons_02.png`, which replaced these.

**Blocked on the node graph** (4): `skill_node_key_frame.png`, `skill_node_sub_frame.png`,
`skill_link_segment.png`, `character_portrait_frame.png`. Concept written —
[`Specs/DIY/SKILL_TREE_NODE_GRAPH_2026-09-28.md`](Specs/DIY/SKILL_TREE_NODE_GRAPH_2026-09-28.md),
awaiting answers to N1–N4 and L1–L4.

**Blocked on a screen that does not exist yet** (2): `tutorial_prompt_panel.png` and
`tutorial_glyph_strip.png`. Tutorial beats reuse the pause overlay
(`WaveStatePresenter.TryShowTutorialBeat`); there is no tutorial panel for these to attach to.

**Genuinely just unwired — real work, nobody is blocked** (3):
`UI/HUD/factory_chain_tier_indicator.png` (this is §2.2 and should land with it),
`UI/MainMenu/route_panel_frame.png` and `UI/MainMenu/glyph_hint_capsule.png`.

Wired since the last version: both impact VFX sheets and `Projectile_BossBurst`. That is all — an
earlier draft of this section claimed five more, which was wrong.

### 1.2 Audio still open (2 cues)

**28 cues — 26 delivered and wired, 2 delivered and unwired.**

- **Audio 24, run victory sting** — needs a run-end payoff moment, which does not exist yet.
- **Audio 25, credits loop** — needs a credits screen, which does not exist yet.

Both are blocked on a feature rather than on wiring.

> **A caution about both audits.** Each has under-reported wiring once, in the same way: matching one
> kind of reference and missing another (filenames but not guids; ScriptableObjects but not scenes).
> The scanners are fixed, but treat any figure from them as a starting point, not a fact.

### 1.3 Art still wrong

- `tower_info_card_frame.png` bakes English "Strong against / Weak against" into the artwork, so the
  German build shows English. Needs a re-render without text.
- `stage2/3/4/5_ground_tileset.png` are JPEG data behind a `.png` name. Harmless at runtime, but
  those four can never carry transparency without a re-render.
- The splash sheet is seven frames of a mechanical mine, not the five-frame shockwave Asset 104
  specifies. Wired and usable; just not what was asked for.

---

## 2 · Agreed work — status after the 2026-09-29 pass

### 2.1 UI theme unification — **started**

`ProceduralUIBuilder` exists and resolves `UIThemeData`'s colours and panel frame, which nothing had
ever read. **One presenter migrated** (the factory chain indicator). The remaining five-plus are the
job, one at a time with a screenshot between each - a single sweep is how the 2026-09-22 canvas
regressions happened.

### 2.2 Factory amplification visibility — **done**

HUD readout (pip per chain tier, live multiplier) answers "is it on?"; the toast now names the
multiplier and an amplified tick pulses the Core, answering "did it just pay?". Wires
`factory_chain_tier_indicator.png`.

### 2.3 God-object refactors — **step 1 done**

`GameplayTimeScaleGuard` owns the pause freeze, which is the invariant that seam was chosen for.
Steps 2-4 (HUD bars out of `WaveStatePresenter`, spawn-gate visuals out of `StageArenaArtPresenter`,
stage-transition orchestration out of `WaveManager`) are still open.

### 2.4 Run timer and history — **done**

Both clocks, persisted across sessions; per-profile `history.json` keeping 50 recent plus the best
per stage; a Records route on the main menu showing lifetime totals, the best run and recent runs.

Playtest 2026-09-29 fixes: playtime now counts continuously (`PlaytimeTracker`, menus included)
instead of only when a run was won or lost, which is why it read 0:00; "runs started" is counted;
each run line lists its weapons. **Lost runs had recorded no weapons or towers at all** - the run
save is wiped on death before the history is written - so the build is now read from the live
loadout and tower service, and the tutorial flag is cached at run start for the same reason.
Records gained a **Cards** tab: every card in the pool, blurred until collected once.

### 2.5 Levelling — **done**

Per-profile level on a x1.15 curve from weighted kills, wave clears and stage clears; one Skill Point
per level; boss SP reduced to two thirds so the two sources sum to roughly what bosses alone paid.
HUD bar shares the run-timer Settings toggle.

**Next: decide** the level cap, SP quests and Paragon concept,
`Specs/DIY/LEVELLING_QUESTS_PARAGON_2026-09-29.md` (P1-P8). There is no level cap today; the x1.15
curve walls out around level 30-35 against a 212 SP tree.

### 2.6 Skill tree node graph — **done, one deviation**

On a branch tab the graph *is* the screen now (playtest 2026-09-29: "I see the old and the new
system - it should be only one"). The view pans to the active branch at a readable size, the other
branches and the portrait stay visible dimmed at the edges, a details column shows the selected
node, and Respec is a button. Stick and arrows move across the graph (nearest node in a cone); the
mouse hovers to select and clicks to buy; tabs are clickable.

**Deviation from N3:** LB/RB switch *branch*, as on every other tabbed screen, rather than jump
tiers - Up/Down along the branch does the tier move. Authored `_graphPosition` values are still
unset; the computed layout is readable without them.

### 2.7 HP recovery — **done**

Repair yourself at the Core during an Intermission for Scrap, rising 1.45x per use, at an interact
priority below repairing the Core itself.

### 2.8 Duplicate cards — **done**

Nine duplicate groups resolved to zero by repurposing the weaker card of each into a mechanic the
pool lacked. The five cards I had flagged as possibly doing nothing were fine - they use
`WeaponModifierData`, which that scan did not read.

### 2.9 Five-run test campaign — **still open**

Single-stage verification runs were used throughout this pass, but the five-run campaign has not
been done.

### 2.10 Mouse everywhere — **done, clicks unverified**

Added 2026-09-29: Progression tabs, rows and graph nodes; Settings tabs, rows (click steps forward,
right-click back) and Apply / Cancel buttons; Records tabs and card grid; pause tabs, settings rows
and Resume / Main Menu buttons; a click dismisses Start, Run Failed and tutorial beats after a 0.6 s
grace; build-browser cards (click selects, click again builds or upgrades); right-click skips the
shop. Every element was screenshotted, but **no click was made** - synthetic OS input does not
reach the Input System, so each needs one real mouse pass.

### 2.11 Aim reticle — **second design**

The four-dash reticle "looked strange" (playtest 2026-09-29). Replaced by a dot inside a ring whose
bright arc is the magazine (refills grey during a reload), red over an enemy, on its own screen-space
canvas. It hides and the Windows pointer returns while any pointer menu is open - pause, reward,
shop, loadout. The first version stayed hidden under those overlays with the OS pointer also hidden.

---

## 3 · Decide — second round of suggestions

The first round is in git history; these are fresh takes, not a re-print.

### 3.2 Wave → Boss and Stage → Stage transitions

1. **Framed beat, no new tech.** 2–3s: HUD slides out, arena desaturates, camera pushes in ~8 %, the
   boss gate floods with light, the name types on, music crossfades to that stage's boss loop
   (already wired), HUD slides back. *A day, no art.*
2. **Framed beat plus a painted card** — a full-screen boss illustration behind the name. Five
   pieces of art, no animation pipeline.
3. **Diegetic stage change.** For Stage → Stage specifically: the ground tileset crossfades from the
   old stage to the new while the Core stays fixed on screen, so it reads as *the facility moved
   around me* rather than a loading screen. Reuses the tileset swap the arena presenter already
   performs on rebuild.
4. **Pre-rendered clips** via `VideoPlayer`. Most drama, most cost — ~10 clips, a video pipeline,
   codec testing, tens of MB — and it cannot react to run state: the same clip whether you arrived
   at full health or at 1 HP.

**Recommendation: 1 for Wave → Boss, 3 for Stage → Stage, 2 later.** Skip 4. A transition replayed
hundreds of times by the same player has to be short and skippable; a 2-second authored beat ages
far better than a 10-second clip they learn to resent, and both boundaries in Vision §19 about
readability and a polished smaller feature set point the same way.

### 3.3 Stage 1 tower spots feel static

Raised again on 2026-09-28. The complaint is that the slots are the same positions every run, so
placement is memorised once and never thought about again.

1. **Randomise which authored slots are active.** Author 8–10 per stage, enable a random 4–5 each
   run. `_towerSlotPositions` is already a per-stage array, so this is data plus a shuffle.
2. **Tie slots to the gates.** Two-sided waves are in; mark each slot with which gates it covers and
   show that on the pad. The decision becomes *which side do I defend* — the tension two-sided
   spawning was built to create and currently under-uses.
3. **Give slots their own character** — an elevated slot at +25 % range, a shielded slot that takes
   no damage, a cramped slot at −15 % cost. Placement becomes a trade-off rather than a formality.
4. **Let the arena change mid-run.** A slot destroyed by the boss, or a new one unlocked by clearing
   a wave without Core damage, so the layout moves while you are standing in it.

**Recommendation: 1 + 2 together, 3 next.** They are cheap, and they answer both halves: the layout
stops being memorised, and where you build starts interacting with where enemies come from. 4 is the
most interesting and the most expensive; it only makes sense once 1–3 have been played. Free
placement stays ruled out by the vision.

---

## 4 · Smaller items — second round, two suggestions each

**No automated coverage of boss state transitions.**
(a) A `bossN` autopilot variant that god-modes straight into the fight, so phases are reached in
seconds instead of by playing a stage. (b) Log every phase change the way wave transitions are
logged and assert the expected sequence at run end — which also gives the first real regression test
for boss pacing.

**`StageEnemyPoolData` is authored but read by no code.**
(a) Delete it; per-stage wave sets replaced it and `Waves.md` says so. (b) Keep it as the
composition source for an endless/ascension mode, where hand-authoring waves stops scaling — but
only if that mode is actually planned, otherwise (a).

**`CardData.Tags` are drawn but ignored by offer selection and card effects.**
(a) One weighting function that biases offers toward the build's existing tags (synergy) or away
from them (variety), with a single knob. (b) A card whose effect *reads* tags ("+10 % damage per
Fire card owned"), which is what turns a tag from a label into a mechanic — and makes the icons
already on the cards worth looking at.

**Six multi-slot sprite sheets are unsliced or bypass the VFX data model.**
(a) Author `VFXSheetData` entries so the grid lives in data rather than in per-call arguments — now
more valuable than before, since `CombatVfxBurstRuntime` can play frame sequences and more sheets
will be wired. (b) Teach `Corebound > Validate Art Pipeline` the expected grid, so a mis-gridded
sheet fails validation instead of rendering a garbled cell at runtime.

**Boss HUD, Notification and Relic reveal are procedural with hardcoded offsets.**
(a) Route them through the `ProceduralUIBuilder` of §2.1, so the offsets have one definition — the
cheap half, and it falls out of that work anyway. (b) Move them into authored canvas scenes like
Reward/Shop/HUD, which is where canvas regressions stopped happening; more work, and the only option
that removes the hardcoded numbers entirely.

---

## 5 · Unplayed

Everything fixed on 2026-09-27 and 2026-09-28 has been verified to compile and, where an autopilot
run could reach it, to run. None of it has been played by a human: the enemy stuck-watchdog, the
tower projectiles, the 5-second spawn delay, the reward layout changes, the relic text fitting, HUD
scale in the pause menu, and the second attempt at the profile-swap Continue fix.

The Continue fix in particular is worth a deliberate check, because the first attempt looked correct
and was not.
