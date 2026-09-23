# Open Points Register

Version: 3.0 · Created 2026-09-20 · Last updated 2026-09-23 · Status: Active

The single live list of what is still open. `08_IMPLEMENTATION_BACKLOG.md` is the *plan*; this is
the *punch list*. Closed items are removed, not archived — the session reports
(`Docs/SESSION_REPORT_*.md`) and the git history hold the record of what was fixed and why.

Priority: **P1** blocks playable quality · **P2** needed for the Early Access target · **P3** worth
doing, not urgent.

---

## P1 — Blocks playable quality

### 1. Balance of everything added on 2026-09-22 and 2026-09-23

None of this has been played by a human; the numbers are first guesses:

- **Stage 2-5 waves.** 24 generated wave assets with 77 spawn groups, escalating by density and
  role mix. One autopilot run lost Stage 2 in 17 seconds, so the density is the first suspect.
- **Tower levels 4-7.** Costs run 4x to 22x the build cost. Whether the late levels are ever worth
  buying against a weapon upgrade is unknown.
- **Boss difficulty.** Health x2.5 and 90 % tower resistance landed together; the autopilot now
  loses to the Stage 1 boss, which may be right or may be too much.
- **Six new weapons.** Damage, magazines and Set Bonus ladders are unplayed beyond "they fire".
- **Shop pacing.** One visit per stage after wave 3, prices rising 1.0 to 1.45 across the stages.

### 2. Features that exist but no automated run reaches

Each needs either a human or a new autopilot path: profile switching and delete, the starting-weapon
choice, the reward reroll (Wider Draft), Magnetise, the pause Settings tab, mouse-wheel scrolling on
the Progression screen, and the skill nodes that only fire in rare moments (Second Wind, Last
Bastion, Overwatch, Executioner, Field Repair).

### 3. Art still missing or defective

**Defective (need a re-render, all the same fault - the transparency checkerboard is painted into
the pixels instead of being real alpha):**

- Asset 103 `vfx_impact_flame_frost.png` and 104 `vfx_impact_splash_burst.png` (delivered
  2026-09-23, fully opaque, not wired).
- Asset 85 `skill_node_key_frame.png` and 90 `weapon_slot_frame.png` (90 is drawn border-only as a
  workaround).
- `vfx_boss_telegraph_lance.png` (checkerboard at the tip) and `character_portrait_frame.png`
  (checkerboard centre; it also sits in `Assets/Art/UI/Skilltree/` while its prompt says
  `SkillTree/`).

**Wrong content:** Asset 86 `skill_node_sub_frame.png` is a node diagram, not a frame.

**Missing:** the ten Zone Denier / Relay Warden sprites (Assets 75-84). Both roles currently borrow
other sheets, separated only by tint.

**Other:** `tower_info_card_frame.png` bakes English "Strong against / Weak against" into the art;
two files still carry a doubled `.png.png` extension; `stage2_ground_tileset.png` and
`stage3_ground_tileset.png` are JPEG data behind a `.png` name (no alpha).

`Corebound > Validate Art Pipeline` now catches the checkerboard and opaque-frame cases
automatically.

### 4. Open design question: a weapon pending across a stage change

A weapon from the boss reward arriving with a full loadout waits until the next Intermission, and a
new stage starts without one - so it sits through the whole first wave. Weapon cards are not offered
while one is pending, so nothing is lost, but the wait is odd. Should a new stage open with a short
Intermission when a weapon is pending?

---

## P2 — Needed for the Early Access target

### 5. Shop and Reward screens still look alike

Structurally the same: three card-shaped slots in a row with the same rarity frames. The wash colour
and the price chip are the only separators. Closing this properly is an art-direction call, not code.

### 6. `UIThemeData` colours are unread

`.Font` reaches eleven presenters through `UIFontResolver`, but `PrimaryTextColor`,
`SecondaryTextColor`, `AccentColor` and `PanelFrameSprite` are read by nothing - every presenter
hardcodes its own colours. Best done together with a shared procedural-UI builder, since
`CreateText`/`CreateImage` are duplicated across at least six presenters and have already drifted.

### 7. Factory amplification is invisible

The amplifier pays out with no on-screen sign. The Intermission line names the chain tier, but
nothing shows that an amplifier is live or what it is adding. Vision Section 5.5 asks that a player
can recognise a system is working.

---

## P3 — Worth doing, not urgent

### 8. God-object refactors

`WaveStatePresenter` (~1600 lines, five responsibilities including the pause overlay and the
tutorial beats) and `StageArenaArtPresenter` (~1000) are the remaining large ones; `WaveManager`
still carries most of its responsibilities. `WaveSpawnPlacement` was split out in an earlier pass
and is the pattern to follow. The old reason for deferring (no way to verify) is gone: the autopilot
plays a full run end to end.

### 9. Unresolved vision questions

`00_PROJECT_VISION.md` §20.1: how deep the Factory chain should go, and the shape of the
meta-progression curve. §20.2 lists where the vision text and the build still disagree - tower
count, "resource conversion" versus the single-currency decision, and the unvalidated 60-70 minute
run length. The autopilot's `SUMMARY` line now measures run length, so the last one is checkable.

### 10. Smaller items carried forward

- No automated coverage of boss state transitions.
- `StageEnemyPoolData` is authored but read by no code; per-stage wave sets replaced it as the
  escalation mechanism (Waves.md).
- `CardData.Tags` is drawn on the reward screen and in the pause menu, but offer selection and card
  effects still ignore tags.
- Six multi-slot sprite sheets are imported but unsliced, or bypass their documented VFX data model
  with custom runtime grid maths.
- Remaining canvases built procedurally with hardcoded offsets: Boss HUD, Notification, Relic
  reveal.
