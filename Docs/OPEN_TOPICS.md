# Open Topics — Corebound

Version 1.0 · 2026-09-27 · **The single live list.** Replaces `10_OPEN_POINTS.md`,
`SESSION_REPORT_2026-09-22.md`, `SPEC_REVIEW_2026-09-22.md` and the two `Specs/DIY/` proposal files,
all of which are deleted. Git history holds what was fixed and why; this holds only what is *open*.

`08_IMPLEMENTATION_BACKLOG.md` is the plan. This is the punch list.

**Priority:** **P1** blocks playable quality · **P2** needed for Early Access · **P3** worth doing.
**Sections marked "Decide"** need an owner answer before code; everything else is work.

---

## 1 · Unplayed and unbalanced (P1)

None of this has been played by a human; the numbers are first guesses.

- **Stage 2–5 waves.** 24 generated wave assets, 77 spawn groups. One autopilot run lost Stage 2 in
  17 seconds, so density is the first suspect.
- **Two-sided waves (2026-09-25)** made every multi-group wave arrive from two gates at once. That
  is a real difficulty increase stacked on top of the untested numbers above.
- **The salvage cache (2026-09-25).** Whether 0.55 of a wave's drop budget is worth the trip,
  whether the computed ~7s window is tight or generous, and whether a 35 %-per-wave chance reads as
  "be ready" or as "random".
- **Tower levels 4–7.** Costs run 4× to 22× the build cost; whether the late levels ever beat a
  weapon upgrade is unknown.
- **Boss difficulty.** Health ×2.5 and 90 % tower resistance landed together.
- **Six weapons added 2026-09-22.** Damage, magazines and Set Bonus ladders unplayed beyond "they
  fire".
- **Shop pacing.** One visit per stage after wave 3; prices rise 1.0 → 1.45 across the stages.

## 2 · Features no automated run reaches (P1)

Each needs a human or a new autopilot path: profile switching and delete, the starting-weapon
choice, reward reroll (Wider Draft), Magnetise, the pause Settings tab, Progression mouse-wheel
scrolling, and the rare-trigger skill nodes (Second Wind, Last Bastion, Overwatch, Executioner,
Field Repair).

---

## 3 · Art (P1)

### 3.1 Delivered but wired to nothing

Audited 2026-09-27 across every `.cs`, `.asset`, `.unity` and `.md`, matching both filenames **and
the guid in each file's `.meta`** — assets reference textures by guid, and a first pass that checked
only filenames wrongly reported the main menu logo and background as unused.

**112 art files, 100 wired, 12 unwired, 0 orphans.** The StreamingAssets mirror is complete and
consistent in both directions.

The twelve fall into three groups, and only one of them is a wiring job:

**Superseded — safe to delete once you confirm** (3). `weapon_rapid_sprite.png`,
`weapon_spread_sprite.png`, `weapon_beam_sprite.png`. These are not a gap: 11 of 12 projectiles draw
from `projectile_sheet_core.png` or `projectile_sheet_weapons_02.png`, which replaced these three
standalone files. They are leftovers. *(An earlier note in this file claimed every projectile was a
procedural square — that was wrong, and this replaces it. The one projectile that genuinely had no
art, `Projectile_BossBurst`, was wired on 2026-09-27.)*

**Needs a feature first, not a wiring change** (6):

| File | What is missing |
| --- | --- |
| `SkillTree/skill_node_key_frame.png` | There is no node graph. The Progression screen is a list of text rows (`MetaProgressionRouteTargetPresenter`), so node frames, the link segment and the portrait frame have nowhere to attach. Wiring them means *building a graphical skill tree*. |
| `SkillTree/skill_node_sub_frame.png` | as above |
| `SkillTree/skill_link_segment.png` | as above |
| `SkillTree/character_portrait_frame.png` | as above |
| `Tutorial/tutorial_prompt_panel.png` | Tutorial beats reuse the pause overlay (`WaveStatePresenter.TryShowTutorialBeat`). A dedicated tutorial panel does not exist. |
| `Tutorial/tutorial_glyph_strip.png` | as above |

**Genuinely just unwired** (3): `UI/HUD/factory_chain_tier_indicator.png` (see §6.2, which proposes
exactly this), `UI/MainMenu/route_panel_frame.png` and `UI/MainMenu/glyph_hint_capsule.png`. These
three have an obvious home and need no new system.

Wired on 2026-09-27: `vfx_impact_flame_frost.png` (Burn and Freeze now animate through their own
four-frame rows instead of sharing the generic still) and `vfx_impact_splash_burst.png` (splash
damage was previously dealt with nothing drawn at all). Both needed `CombatVfxBurstRuntime` to learn
frame sequences first — every VFX before this was a single still that grew and faded, which is why
multi-frame sheets had nowhere to go.

### 3.2 Audio delivered but not wired

Status added to `AI/AUDIO_ASSET_PROMPTS.md` on 2026-09-27, derived from disk rather than asserted:
**28 cues — 19 delivered and wired, 4 delivered but unwired, 5 not created.**

The four unwired ones are the per-stage boss loops (Audio 20–23: `music_boss_stage2..5_loop.mp3`).
They exist and sound right; nothing plays them, because `BossData` has no `_musicOverride` field for
`MusicStateController` to check. That is a one-field, one-branch change using the same optional-
override pattern `WaveAudioCueData.HasRealXClip` already uses — four finished music tracks are
currently doing nothing for want of it.

Not created: Audio 24 (run victory sting), 25 (credits loop), and 26–28, the arena-objective
spawn/claim/expire cues added 2026-09-27. The cache currently appears, is claimed and expires in
total silence, which for a timed opportunity you are meant to notice while fighting elsewhere is the
single most load-bearing missing sound in the game.

### 3.3 Still wrong

- `tower_info_card_frame.png` bakes English "Strong against / Weak against" into the artwork, so the
  German build shows English. Needs a re-render without text.
- `stage2/3/4/5_ground_tileset.png` are **JPEG data behind a `.png` name** (no alpha). Harmless at
  runtime — the loader reads raw bytes — but misleading, and it means those four can never carry
  transparency without a re-render.
- The splash sheet holds **seven frames of a mechanical mine detonating**, not the five-frame
  shockwave its prompt asked for. Usable, but it is not what Assets 104 specifies.

### 3.4 Folder structure — **Decide**

The current tree is `Assets/Art/<Domain>/<Subdomain>/` (`Combat/VFX`, `UI/MainMenu`,
`Environment/Arena`, …) and is honestly **fine**: it matches the runtime path constants, the
StreamingAssets mirror is a byte-for-byte parallel of it, and the audit found zero orphans and zero
mirror drift. Two inconsistencies are worth fixing, and nothing else is:

1. **`UI/Skilltree/` vs the prompts' `UI/SkillTree/`.** One casing, pick it, fix the prompts doc.
2. **`Art/Cards/Frames/` sits outside `Art/UI/`** although card frames are UI. Either move it to
   `Art/UI/Cards/` or accept that Cards is its own domain.

Recommendation: **do nothing structural.** A reorganisation means touching every path constant, the
mirror, and `StageThemeData`'s plain-string texture paths — where a rename is a silent null, not a
broken reference the Editor flags. That is a lot of risk to buy tidiness the audit says we already
have. Fix the casing, leave the rest.

---

## 4 · Decide — suggestions awaiting an answer

### 4.1 The mouse cursor is still an OS mouse pointer

Three options, cheapest first.

1. **A drawn crosshair that replaces the cursor.** `Cursor.SetCursor()` with a hardware cursor
   texture — one call at startup, no per-frame cost, and it still moves at OS refresh rate rather
   than framerate. A thin amber reticle in the established steel-blue/amber palette. *Cheapest,
   least risk, biggest immediate gain.*
2. **A world-space aim marker, cursor hidden.** `Cursor.visible = false` plus a sprite drawn at the
   aim point in the arena. It can then carry state the OS cursor cannot: tint red when over an
   enemy, dim when the weapon is reloading, snap subtly to a target. Costs a frame of latency
   (drawn at game framerate) which is noticeable on a twin-stick game at low fps.
3. **Both, chosen by input device.** Hardware cursor while the mouse is active, a controller-style
   aim indicator when `IsUsingGamepad` flips. The project is controller-first and already tracks
   that flag, so this is the one that matches the design stance — and it removes the current
   oddity where a gamepad player still sees a Windows arrow sitting in the arena.

**Recommendation: 3**, implemented as 1 then 2. Start with the hardware cursor (an hour), add the
device-aware swap once it looks right.

### 4.2 Wave → Boss and Stage → Stage transitions — **Decide**

Today both are a state flip with a HUD banner. Three levels of ambition:

1. **Framed beat, no new tech.** 2–3 seconds of: HUD slides out, arena desaturates and the camera
   pushes in ~8 %, the boss gate floods with light, the boss name and epithet type on, the music
   crossfades to the boss loop, HUD slides back. All of it is existing systems — a coroutine, the
   camera, the tint the arena presenter already owns, and the per-stage boss loops (Audio 20–23)
   that already exist but have no per-stage swap code. *A day. No art, no video.*
2. **Framed beat plus a painted card.** The same beat, with a full-screen illustration of the boss
   behind the name — one piece of art per boss, five total. Gives each stage a memorable "here it
   comes" image without any animation pipeline.
3. **Real pre-rendered clips.** Unity's `VideoPlayer` on a render texture, one clip per transition.
   Buys the most drama and costs the most: ~10 clips to produce, a video pipeline, per-platform
   codec testing, and tens of MB of build size. It also cannot react to run state — the clip is the
   same whether you arrived at full health or at 1 HP.

**Recommendation: 1 now, 2 when there is art budget.** Skip 3. The vision's "readable during intense
combat" and "a smaller, polished feature set" both point away from video, and a stage transition
that is *skippable and fast* will be replayed hundreds of times by the same player — a 2-second
authored beat ages far better than a 10-second clip they learn to resent.

For **Stage → Stage** specifically, the strongest cheap version is a *diegetic* one: the arena floor
tileset crossfades from the old stage to the new one while the Core stays fixed on screen, so the
player reads it as "the facility moved around me", not "a loading screen". That reuses the ground
tileset swap the arena presenter already performs on rebuild.

### 4.3 Stage 1 tower spots feel static — **Decide**

The real complaint is that the slots are the *same four positions every run*, so there is no
placement decision: you learn them once and never think again. Four options, increasing in cost:

1. **Randomise which authored slots are active.** Author 8–10 slots per stage, enable a random 4–5
   each run. Zero new systems (`_towerSlotPositions` is already a per-stage array), and it makes
   every run's geometry different. *Cheapest real fix.*
2. **Make slots differ from each other.** Give each slot a modifier — an elevated slot with +25 %
   range, a shielded slot that takes no damage, a cramped slot at −15 % cost. Placement becomes a
   trade-off rather than a formality, and it pairs naturally with 1.
3. **Tie slots to the gates.** With two-sided waves now in, mark each slot with which gates it
   actually covers and show that on the pad. The decision becomes "which side do I defend", which
   is exactly the tension two-sided spawning was meant to create and currently under-uses.
4. **Free placement inside buildable zones.** Maximum freedom, and explicitly rejected by the vision
   ("Tower placement — fixed per-stage buildable slots, not free placement"). Listed only so it is
   visibly ruled out.

**Recommendation: 1 + 3.** Together they cost little and directly answer both halves of the
complaint — the layout stops being memorised, and where you build starts interacting with where
enemies come from. 2 is the best follow-up once 1 and 3 have been played.

### 4.4 HP recovery — **Decide** (three suggestions, nothing implemented)

There is currently no way to heal, which is why a chip of damage in Stage 1 is still with you at the
Stage 5 boss. Three models:

1. **Repair at the Core, for Scrap.** Reuse the existing Core-repair interaction: during an
   Intermission, spend Scrap for a fixed HP chunk, at a price that rises each time within a run.
   *Fits every existing system, adds no new UI, and makes Scrap genuinely contested between
   survival and build power — which is the most interesting version.*
2. **A heal as a reward-screen option.** A "Field Repair" card in the offer pool that restores a
   percentage on pick. Costs a card slot, so healing competes with progression. Simple, but it is
   luck-dependent — the run that most needs it may never be offered it.
3. **Wave-clear regeneration, capped.** Clearing a wave without losing the Core restores a small
   fixed amount (say 8 %). Rewards clean play rather than hoarding, needs no interaction at all, and
   quietly reduces the "one bad wave ends the run" feeling. Risk: it makes chip damage meaningless
   and may remove the tension the Core is supposed to carry.

**Recommendation: 1**, with 3 at a very small number (3–5 %) if playtesting shows runs still dying
to accumulated chip damage rather than to a real threat. Avoid 2 as the primary source.

---

## 5 · Open design question (P1) — **Decide**

**A weapon that arrives with no free slot waits across a stage change.**

When a boss reward hands you a weapon and all six slots are full, the weapon goes "pending" and waits
for the next Intermission so you can choose what it replaces. If the boss was the *last* encounter of
a stage, the next stage begins with a wave, not an Intermission — so the pending weapon sits unusable
through that entire first wave before you are allowed to place it.

Nothing is lost (weapon cards are not offered while one is pending), but you are carrying an
invisible prize you cannot touch. Should a new stage open with a short Intermission when a weapon is
pending? *Previously written as a one-line note nobody could parse; this is the same question
spelled out.*

---

## 6 · P2 — needed for Early Access

### 6.1 Shop and Reward screens still look alike

Three card-shaped slots in a row with the same rarity frames; the wash colour and the price chip are
the only separators. An art-direction call, not code.

### 6.2 Factory amplification is invisible — **Decide** (suggestions)

The amplifier pays out with no on-screen sign. Vision §5.5 asks that a player can recognise a system
is working. Two options:

1. **Wire `factory_chain_tier_indicator.png`** (§3.1 — it exists and is used by nothing) into the HUD
   as a small tier readout that brightens a pip per chain tier, with the live amplifier bonus as a
   `×1.4` chip beside it. Cheapest, uses art already paid for.
2. **Make the payout visible in the arena** — a brief pulse along the Core when an amplified tick
   lands, plus the existing `NotificationPresenter` toast carrying the amplified figure rather than
   the base one. Ties the number to the moment instead of parking it in a corner.

**Recommendation: both**, 1 first. They answer different questions ("is it on?" and "did it just
pay?") and neither is more than a few hours.

### 6.3 `UIThemeData` colours are unread

`.Font` reaches eleven presenters through `UIFontResolver`, but `PrimaryTextColor`,
`SecondaryTextColor`, `AccentColor` and `PanelFrameSprite` are read by nothing — every presenter
hardcodes its own colours, and `CreateText`/`CreateImage` are duplicated across at least six of them
and have already drifted apart.

**The approach, agreed 2026-09-27:** build one `ProceduralUIBuilder` that owns `CreateText` /
`CreateImage` / `StretchToParent` and reads `UIThemeData`, then migrate presenters onto it one at a
time, verifying each against a screenshot before moving to the next. Doing it as a single sweeping
replacement across six presenters with no EventSystem and scene-authored layouts is how the canvas
regressions of 2026-09-22 happened.

---

## 7 · P3 — worth doing

### 7.1 God-object refactors

`WaveStatePresenter` (~1600 lines, five responsibilities including the pause overlay and tutorial
beats) and `StageArenaArtPresenter` (~1000) are the remaining large ones; `WaveManager` still carries
most of its original dozen. `WaveSpawnPlacement` was split out successfully and is the pattern.

**Agreed order (2026-09-27), smallest blast radius first:**

1. `WaveStatePresenter` → extract the **pause/overlay** (it is the only thing in the codebase that
   touches `Time.timeScale`, so it is a clean seam with one invariant).
2. `WaveStatePresenter` → extract the **HUD bars/labels** (pure render-from-state, no input).
3. `StageArenaArtPresenter` → extract **spawn-gate visuals** (already a self-contained list plus a
   pulse timer).
4. `WaveManager` → extract **stage-transition orchestration**.

Verify each step with an autopilot run before the next. The old reason for deferring ("no way to
verify") is gone.

### 7.2 Vision questions still open — **Decide** (suggestions)

`00_PROJECT_VISION.md` §20.1 and §20.2.

- **How deep should the Factory chain go?** Suggestions: (a) cap at the current tiers and spend the
  effort on making amplification legible (§6.2) — the Factory is explicitly a *supporting* system
  per boundary 2; (b) add exactly one tier gated behind a meta unlock, so depth becomes a
  progression reward rather than a starting complexity.
- **Shape of the meta-progression curve.** Suggestions: (a) flatten it — target "every run buys
  something", which is what keeps a losing run worth playing; (b) keep the current curve but front-
  load the first three unlocks so a new profile sees three purchases in its first hour.
- **Tower count**, where the vision text and the build disagree. Suggestions: (a) update the vision
  to the built count, since the build is the more considered artefact; (b) add the missing tower if
  its role (§4.3's slot modifiers may already cover the gap) is genuinely distinct.
- **"Resource conversion" versus the single-currency decision.** The vision still describes a
  conversion step that the single-currency decision removed. Suggestion: delete the paragraph — the
  decision is settled and the text is a fossil.
- **The unvalidated 60–70 minute run length.** No suggestion needed; the autopilot's `SUMMARY` line
  now measures it. Run five and read the number.

### 7.3 Smaller items — two suggestions each

**No automated coverage of boss state transitions.**
(a) Log every boss phase change the way wave-state transitions are logged, then assert the expected
sequence at run end. (b) Add a `bossN` autopilot variant that god-modes straight to the boss and
sits in the fight, so phases are reached in seconds rather than by playing a stage.

**`StageEnemyPoolData` is authored but read by no code.**
(a) Delete it — per-stage wave sets replaced it as the escalation mechanism and `Waves.md` already
says so. (b) Keep it and wire it as the *fallback* composition for procedurally generated waves, if
endless/ascension mode is ever built.

**`CardData.Tags` are drawn but ignored by offer selection and card effects.**
(a) Use tags to bias offers toward what the build already has (synergy) or away from it (variety) —
one weighting function, one knob. (b) Give at least one card an effect that reads tags ("+10 % damage
per Fire card owned"), which is what makes the tag visible as a mechanic rather than a label.

**Six multi-slot sprite sheets are unsliced or bypass the VFX data model.**
(a) Author proper `VFXSheetData` entries for all six so the grid lives in data, not in per-call
arguments. (b) Add the grid to the art validator so an unsliced or mis-gridded sheet fails
`Corebound > Validate Art Pipeline` instead of rendering a garbled cell at runtime.

**Boss HUD, Notification and Relic reveal are procedural with hardcoded offsets.**
(a) Move them into authored canvas scenes like Reward/Shop/HUD, which is where every other canvas
regression stopped happening. (b) Leave them procedural but route them through the
`ProceduralUIBuilder` of §6.3, so at least the offsets have one definition.

---

## 8 · Cleanup done 2026-09-27

Recorded so it is not re-litigated: `10_OPEN_POINTS.md`, `SESSION_REPORT_2026-09-22.md`,
`SPEC_REVIEW_2026-09-22.md`, `Specs/DIY/UI_PROPOSALS_2026-09-21.md` and
`Specs/DIY/ARENA_OBJECTIVES_2026-09-25.md` were merged into this file and deleted. The decisions they
recorded are either already in the code with comments explaining why, or are restated above.
