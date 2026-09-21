# Session Report — 2026-09-21

Playtest follow-up. Everything below was found or confirmed by **playing the game**, not by reading it: the autopilot from the previous session is now the primary diagnostic tool, and it earned its keep immediately.

---

## The headline: two bugs, one cause

The shop never appearing and the build intermission never appearing were the same defect.

Confirming the card reward closes the reward screen, which synchronously raises `StateChanged` → `WaveManager.OnRewardStateChanged` → `StartIntermission` → `ShopService.TryOpenForIntermission`. All of that runs **inside the `ConfirmPressed` invocation list**, and both `ShopService` and `WaveManager` are also subscribed to `ConfirmPressed`. So the single press that opened the shop reached the shop's own handler a moment later and resolved it, and reached WaveManager's handler and ended the intermission.

Both were opening and closing within one frame, every rotation. That is why three playtests in a row reported "shop still not shown" while the offers were being built correctly the whole time — `shopOffers=3` and `shopOpen=False` in the same log line was the tell.

The fix is a one-frame guard in each place. `ShopService` ignores Confirm and Cancel in the frame it opened; `WaveManager` tracks `_intermissionStartedFrame` alongside the existing `_intermissionGateResolvedFrame`, which only ever covered a *gate being cleared*, never the intermission being *started*.

**Evidence, before and after, same stage, same harness:**

| | Shop seen | Loop per rotation |
| --- | --- | --- |
| Before | `sawShop=False` across a full six-wave run | Combat → Reward → Combat |
| After | `SHOP OPENED at wave 2 with 3 offers, canvas=VISIBLE a=1` | Combat → Reward → Intermission → Combat |

---

## Everything else in this pass

**Build intermission every rotation.** It used to be suppressed on any wave the shop opened on, so the one rotation with the most to spend resources on was also the only one that skipped building. The two gates are independent now.

**Shop frequency.** Opens after waves 2 and 4 rather than only wave 3, so a stage offers it twice.

**Boss and phase spawns randomised.** The boss scatters on an arc around its authored point (which still carries the stage's intent) and is pushed at least five units from the player, so an encounter never opens with an instant body-block. Phase helpers get a per-phase ring rotation plus angle and radius jitter instead of the old fixed 90-then-30-degree lattice.

**Arena outer edge rebuilt.** Three compounding faults: every wall segment was stretched into a box whose aspect depended on the arena's size (and the Dynamic Arena Slice made that differ per stage, so each stage got its own distortion); each repeat was a separate quad butted against the next, so every seam showed; and corner patches were drawn at 1.35× the wall height, so all four corners bulged out of the band. Now it is one tiled `SpriteRenderer` per edge — Unity's Tiled draw mode repeats the art at its native aspect at any arena size — and extending the horizontal bands over the corners removes the mismatched corner crop entirely.

**Factory Amplifier — accepted and built.** An amplifier is an ordinary node whose bonus multiplies every *other* node in its chain while its tower condition holds. Not a new node type, not a new trigger. Three guards against snowballing: it never amplifies itself, multiple amplifiers add rather than multiply, and the result still passes `ClampAmount`.

**God-object refactor, first cut.** `WaveSpawnPlacement` is extracted from `WaveManager`: all spawn geometry, no wave state, independently constructible. It is deliberately the first piece, because it is where last session's worst bug hid.

---

## Autopilot changes

- **Off by default, with a real toggle.** `Corebound > Autopilot > Enabled`, plus a settings window with stage and time-cap. The setting lives in `EditorPrefs`, so it cannot turn itself back on after a domain reload.
- **The trigger file moved to `AutopilotRequest.txt` in the project root.** `Temp/` was a bad choice: Unity owns it, wipes it, and hides it from the Project window, so the one file needed to drive this was the one file that could not be found. Writing `enable` into it turns the toggle on; writing `disable` turns it off; writing a number runs that stage.
- **Stall detection.** A signature of wave state, alive count and rounded enemy positions; if nothing changes for 12 seconds it logs an error with the full state. That is the class of bug a human reports as "I killed them and nothing happened", and the harness now names it instead of hanging.
- **Unstick logic.** The player reverses its orbit and pushes toward the arena centre when wedged, rather than grinding into a wall for the rest of the run.
- **State transition logging.** Periodic sampling missed Reward and Intermission entirely, since both are routinely shorter than the sample interval. Without transition logs, "the shop never opened" and "the shop opened between two samples" are indistinguishable — and that distinction was the whole investigation.

---

## Decisions recorded

Written into `00_PROJECT_VISION.md` §20.3.

- **Factory Amplifier: yes.** Built.
- **§20.2 disagreements: accepted as built.** The document gets corrected, not the game.
- **2.5D with current assets: no.** Every environment prompt specifies flat top-down orthographic, and Asset 46 exists *only* because one boundary corner came back in 3/4 perspective and clashed badly enough to need regenerating — direct evidence that mixing perspectives in this art set reads as broken rather than deep. Characters have no angled views, and a top-down ground tileset becomes both floor and wall under a tilted camera. What *is* reachable, and what the style rules already ask for, is fake depth on a flat camera: URP 2D lights with normal maps, drop shadows, a parallaxed background, Y-sorting. A true 2.5D move needs a full art re-commission and should be its own project decision.

---

## Still open

- **The rest of the god-object work.** `WaveStatePresenter` (1358 lines) and `StageArenaArtPresenter` (1001) are untouched. `WaveManager` is down to ~1100 but still carries most of its responsibilities. Now that the autopilot can verify a full run end to end, the original reason for deferring these is gone — this is a scheduling question, not a risk question.
- **Amplifier visibility.** It pays out invisibly. Section 5.5 requires the player be able to *recognise* that a system is working, so the Intermission HUD line still needs the active chain surfaced.
- **Bespoke art for Zone Denier and Relay Warden.** Prompts exist as Assets 75-84; the roles still borrow other sheets separated only by tint.
- **Core upgrade track and meta-progression curve.** Both remain proposals in §20.1.

---

# Second pass — 2026-09-21 (later)

## Asset audit

Every `Target:` path in the prompts file is now checked against disk and carries a `**Status:**` line.

| Result | Count |
| --- | --- |
| Present | 76 |
| Not created yet | 11 |
| Misnamed (doubled `.png.png`) | 2 |
| Only in the StreamingAssets mirror | 1 |

**All 15 enemy sprites had been deleted from the working tree** with nothing put in their place. They were still in git and still in the mirror, so nothing was lost, and they are restored. The deletion was invisible in play because the runtime reads the mirror first — which is exactly why it went unnoticed.

## Playtest fixes, all verified in a live run

- **Shop allows unlimited purchases.** Offers went 3 → 2 after a buy and the shop stayed open. It closes when empty or on Cancel.
- **Killing a boss clears the field.** Adds are killed rather than despawned, so each still pays Scrap and its kill statistic.
- **Tower slot selection follows the player.** Nearest slot is selected; Prev/Next still override for that intermission only.
- **HUD is visible when starting on a later stage.** Measured `ONSCREEN` on Stage 5 with live values. The cause was a stale procedural fallback HUD left in the same screen corner after the authored HUD bound — the player saw the dead panel while every diagnostic read the live one, which is why it kept reading as "sporadic".
- **Stage 5's edges are visible.** The camera frames the arena height instead of using one fixed zoom, capped so a large arena cannot shrink the player to a speck.
- **Tower upgrades cost roughly double**, scaled off each tower's build cost.
- **`wave_banner.png` backs the Wave label** and **`resource_primary_icon.png` sits beside the Scrap count**, both as additive siblings that copy the authored rect.

## Boss variety

The diagnosis was worse than "uneven": only two attack patterns existed and all five bosses shared them. `BossAttackShape` adds Fan, Ring, Spiral, Volley and Lance as one data field on the existing pattern. Stage 1 keeps Fan; Stage 2 Ring, Stage 3 Volley, Stage 4 Spiral, Stage 5 Ring into Lance.

**This creates an urgent art dependency.** The existing telegraph is a ghost projectile flying at the target, which only describes a Fan. Three of the five bosses now telegraph a direction they do not have, which is worse than no warning. Assets 96-98 cover it.

## Factory made visible

`FactoryService.DescribeActiveProduction()` reports tier, active node count, whether an amplifier is live, and what the next tier needs. `WaveStatePresenter` appends it to the Intermission line. The default chain now has three authored tiers with its five nodes gated across them, so the feature is live rather than dormant.

## God mode

F8. A damage veto rather than a health top-up, so no damage event or Core warning fires during a timed run. It announces itself on screen the whole time.

## Not implemented, written up instead

`Docs/Specs/DIY/UI_PROPOSALS_2026-09-21.md` carries concrete proposals for four items. One of them contradicts `Shop.md` outright, and the note says so: the spec asks the shop to differ from the reward screen, the playtest asks for the opposite, and the playtest wins.

The `stage1_spawn_gate.png` finding is worth repeating here: it **is** drawn, at one fixed position, while enemies spawn from a rotated and jittered fan somewhere else. The art currently lies about where enemies come from.

The card tag sheet **is** correctly sliced and **is** drawn on reward cards. The gap is that most `CardData` assets have an empty `_tags` array, so there is nothing to draw.
