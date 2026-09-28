# Open Topics — Corebound

Version 2.0 · 2026-09-28 · **The single live list.** Closed topics are deleted, not archived — git
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

## 2 · Agreed work, not yet done

### 2.1 Unify the UI theme — *"make it similar"*

`UIThemeData.Font` reaches eleven presenters through `UIFontResolver`, but `PrimaryTextColor`,
`SecondaryTextColor`, `AccentColor` and `PanelFrameSprite` are read by nothing. Every presenter
hardcodes its own colours, and `CreateText`/`CreateImage` are duplicated across at least six of them
and have drifted apart.

**Approach:** build one `ProceduralUIBuilder` owning `CreateText` / `CreateImage` /
`StretchToParent` and reading `UIThemeData`, then migrate presenters onto it **one at a time**,
verifying each against a screenshot before the next. A single sweeping replacement across six
presenters, with no EventSystem and scene-authored layouts, is how the 2026-09-22 canvas regressions
happened.

### 2.2 Make factory amplification visible — *"make it visible"*

The amplifier pays out with no on-screen sign; Vision §5.5 asks that a player can recognise a system
is working. Both halves, indicator first:

1. `factory_chain_tier_indicator.png` in the HUD as a tier readout — a pip per chain tier, with the
   live amplifier bonus as a `×1.4` chip beside it. Answers *"is it on?"*
2. A brief pulse along the Core when an amplified tick lands, and the existing
   `NotificationPresenter` toast carrying the **amplified** figure rather than the base one. Answers
   *"did it just pay?"*

### 2.3 God-object refactors — *"start implementing"*

Agreed order, smallest blast radius first, verifying with an autopilot run between each:

1. `WaveStatePresenter` → extract the **pause/overlay**. It is the only thing in the codebase that
   touches `Time.timeScale`, so it is a clean seam with exactly one invariant.
2. `WaveStatePresenter` → extract the **HUD bars and labels** (pure render-from-state, no input).
3. `StageArenaArtPresenter` → extract **spawn-gate visuals** (already a self-contained list plus a
   pulse timer).
4. `WaveManager` → extract **stage-transition orchestration**.

`WaveStatePresenter` is ~1600 lines across five responsibilities; `StageArenaArtPresenter` ~1000.
`WaveSpawnPlacement` was split out successfully and is the pattern to copy.

### 2.4 Run timer and history

Answered in full — see
[`Specs/DIY/RUN_TIMER_AND_HISTORY_2026-09-27.md`](Specs/DIY/RUN_TIMER_AND_HISTORY_2026-09-27.md).
Both clocks stored, HUD readout behind a Settings toggle, per-profile `history.json` keeping the last
50 plus the best run per stage, a new Records route, no entry for abandoned or tutorial runs. Not
implemented.

### 2.5 Five-run test campaign

Requested and not yet done. One progression run verified the purchase path and the visual fixes, but
the autopilot's combat is slow enough that five full runs needs its own session.

---

## 2.6 Duplicate cards — audited 2026-09-28, **Decide**

Owner asked for a review of cards that differ only by a number. Scanning all 43 cards by their
*effect signature* (target + operation, ignoring magnitude) found **nine groups, 19 cards**, where
two or more cards at the same rarity do the same thing:

| Rarity | Effect | Cards (value) |
| --- | --- | --- |
| Common | Weapon damage | Overcharged Rounds (1.10) · Rapid Chamber (1.15) |
| Common | Resource gain | Scrap Magnet (1.10) · Salvage Protocol (1.15) |
| Rare | Fire interval | Rapid Cycler (0.85) · Twin Barrel (0.88) |
| Rare | Resource gain | Assembly Line (1.20) · Harvest Protocol (1.25) |
| **Rare** | **Tower damage** | **Relay Overclock (1.15) · Fortified Relay (1.20) · Reinforced Emitters (1.22)** |
| Epic | Weapon damage | Status Archive (1.15) · Volatile Payload (1.40) |
| Epic | Resource gain | Bulk Salvage (1.40) · Endless Supply Line (1.45) |
| Epic | Tower damage | Siege Battery (1.35) · Twin Turret Array (1.40) |
| Legendary | Weapon + tower damage | Apex Overload (1.30/1.30) · Reactor Bleed (1.25/1.25) |

The three Rare tower-damage cards are the worst: 1.15 / 1.20 / 1.22 is not a choice, it is a lottery
for the best number. The Epic weapon-damage pair is the other extreme — 1.15 against 1.40 at the
same rarity is not a duplicate so much as one card being strictly worse.

Three ways to resolve, and this needs an owner call because it is content design:

1. **Differentiate.** Give one card of each pair a second axis — Relay Overclock becomes tower *fire
   rate*, Twin Barrel becomes *projectile count*, Status Archive leans on status duration. Most work,
   best result, and it uses effect targets that already exist.
2. **Merge and replace.** Delete the weaker of each pair and author new cards in the gaps the pool
   has (there is nothing at all for pierce, crit rate on towers, or Core repair).
3. **Make them stack deliberately.** Keep them, rename them as an explicit family, and let the
   duplication be the point — the deckbuilding equivalent of two copies.

**Recommendation: 1 for the three Rare tower cards and the Epic weapon pair** (the two that read
worst), **2 for the rest**. Not started — say which and it is a data-only change.

Separately, the same scan found **14 cards with no resolvable effects**. Nine are weapon-grant cards,
which is correct — they hand over a weapon rather than a modifier. The other five are not:
**Cooling Array, Critical Focus, Critical Amplifier, Split Barrel and Unstable Payload** reference
effect assets the scan could not read as a target/operation pair, which means either they use a
mechanism the audit does not understand or they genuinely do nothing. Worth a look before the
duplicate work, since a card that does nothing outranks a card that duplicates.

---

## 3 · Decide — second round of suggestions

The first round is in git history; these are fresh takes, not a re-print.

### 3.1 What to draw instead of the OS mouse cursor

1. **Hardware cursor swap.** `Cursor.SetCursor()` with an amber reticle. One call at startup, no
   per-frame cost, and it still tracks at OS refresh rate rather than framerate. *An hour.*
2. **Device-aware:** hardware cursor while the mouse is active, hidden the moment `IsUsingGamepad`
   flips. The project is controller-first and already tracks that flag, and today a gamepad player
   still has a Windows arrow parked in the arena.
3. **A cursor that reports weapon state.** Hidden OS cursor, world-space reticle that opens while
   reloading, closes when ready, and tints on a valid target. This is the one that earns its place —
   it replaces information the HUD currently has to carry, rather than adding decoration. Costs a
   frame of latency, which matters at low framerate.
4. **Contextual cursor.** As 3, plus it changes shape over a tower slot, the Core, and a salvage
   cache — so "you can interact here" is readable without reading text.

**Recommendation: 2 now, then 3.** Start with the swap and the gamepad hide, which is an hour and
removes an obvious wart. 3 is worth doing once the reticle art exists, because it pays for its
latency by moving reload state to where the player is already looking.

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
