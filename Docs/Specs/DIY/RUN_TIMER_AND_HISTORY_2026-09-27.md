# Run Timer and Run History — Design for review, 2026-09-27

**Owner request:** *"I want a Timer function to the game to see how long runs go, and history with what
the player got, how far — not implement first, design and review."*

**Nothing here is implemented.** Decisions are numbered in the A/B form of the earlier spec review;
answer them and the build follows.

---

## What already exists (and what is quietly broken)

Worth knowing before designing on top of it, because three of these are traps:

| Thing | State |
| --- | --- |
| `StatisticsSaveData._totalPlaytimeSeconds` + `AddPlaytimeSeconds()` | **Exists and is never called by anything.** A dead field that has been silently reading 0 since it was written. |
| `StatisticsSaveData` totals (runs started / completed / failed, highest stage, total kills) | Recorded correctly by `WaveStatisticsRecorder`… |
| …and displayed | **Nowhere.** No presenter reads `StatisticsSaveData`. Every number the game already collects is invisible to the player. |
| `RunSaveData` | Holds only the **current** run, overwritten as it progresses. Carries `_lastUpdatedUtcTicks`, stage/wave index, wallet, weapons + tiers, cards, relics, towers. |
| Autopilot `SUMMARY` | Already measures total and per-stage seconds — but it is an Editor tool, not a game feature. |

So the honest summary: the game has a statistics *store* with no clock feeding it and no screen
reading it. This work is less "add a timer" and more "finish the system that was started".

---

## D1 — What the timer actually measures

1. **Wall-clock from run start to run end.** Simplest; includes every pause, menu, alt-tab and the
   twenty-second Intermissions.
2. **Unpaused game time.** Accumulate `Time.deltaTime` while a run is active, which stops
   automatically whenever an overlay holds `Time.timeScale` at 0 (pause, tutorial beat, reward).
3. **Both, stored separately:** `elapsedRealSeconds` and `elapsedActiveSeconds`.

**Recommendation: 3, showing 2.** They answer different questions and the difference between them is
itself informative — a big gap means the player is spending their run in menus, which is exactly the
kind of thing the 60–70 minute target in the vision needs to be measured against. Storing both costs
two floats. Show the unpaused one on the HUD, because a timer that ticks while the game is paused
feels broken.

> **Trap:** `Time.deltaTime` is 0 while `timeScale` is 0, so option 2 needs no special-casing — but
> it also means option 1 must use `Time.unscaledTime` or `DateTime.UtcNow`, *not* an accumulation of
> `deltaTime`. This is the same bug that froze the autopilot in front of a tutorial beat on
> 2026-09-25.

## D2 — Where the running timer is shown

1. **Always on the HUD**, beside the wave label.
2. **Only during Intermission**, on the intermission panel — invisible during combat.
3. **Off by default, toggled in Settings** ("Show run timer").
4. Not shown live at all; only on the run-end summary.

**Recommendation: 3, defaulting to off, plus the run-end summary regardless.** A permanent clock
turns a roguelite into a speedrun for people who did not ask for that, and the vision's readability
boundary argues against another always-on number. Players who want it will find the toggle; the
Settings tab already exists and takes new rows cheaply.

## D3 — When a history entry is written

1. On run end only (victory or death).
2. On run end **and** on abandon (quitting to menu mid-run).
3. Continuously, so a crash still leaves a partial record.

**Recommendation: 2.** Abandoned runs are real data — a run quit at Stage 2 wave 3 says something
about pacing that a missing row does not. Mark the outcome explicitly so the three cases stay
distinguishable. Option 3 costs a write per wave for a case (crash) that should be rare and is
already partly covered by `RunSaveData`.

## D4 — What one history entry holds

This is the "what the player got, how far" half of the request. Proposed record:

| Field | Why |
| --- | --- |
| `startedUtc`, `outcome` (Completed / Died / Abandoned) | Identity and result |
| `elapsedActiveSeconds`, `elapsedRealSeconds` | D1 |
| `stageReached`, `waveReached` | "How far", the headline number |
| `killedByBoss` / `killedByWave` label | *Why* it ended — the most actionable single field |
| `weaponIds` + `tiers` | The build, and `RunSaveData` already carries both arrays |
| `cardIds`, `relicIds` | The build's other half |
| `towerIds` + levels | Whether towers were used at all |
| `scrapEarned`, `alloyEarned`, `skillPointsEarned` | What the run paid out |
| `enemiesKilled` | Effort |
| `profileId`, `ascensionRank` | Context, so entries stay comparable |

Deliberately **not** stored: per-wave timings (that is a profiling feature, not a player-facing one)
and anything derivable from the above.

Estimated size: ~400 bytes of JSON per entry. A hundred runs is 40 KB — a non-issue.

## D5 — Where history is stored

1. Extend `statistics.json` with a `runs[]` array.
2. A new per-profile `history.json`.
3. In `meta.json` beside the other meta progression.

**Recommendation: 2.** `statistics.json` is a small fixed-size record that is read and rewritten on
every kill (`WaveStatisticsRecorder.RecordEnemyKill`); appending an unbounded array to it means
re-serialising the whole history on every enemy death. A separate file is written once per run and
read only when the history screen opens. `meta.json` is the wrong home for the same reason plus it
is the file the progression autopilot variants stomp.

## D6 — Retention

1. Keep everything.
2. Keep the last N (say 50) plus the best run per stage reached.
3. Keep the last N only.

**Recommendation: 2.** Unbounded growth is fine on size but not on the history screen, which becomes
unreadable. Keeping bests alongside recents means the screen can answer both "how am I doing lately"
and "what is my best" without a second data structure.

## D7 — Where history is read

1. A new **Records** route on the main menu.
2. A tab inside the existing Progression screen.
3. A panel on the run-end screen showing only how this run compares to previous ones.

**Recommendation: 1 + 3.** `MainMenuRoute` is an enum serialized by index, so a new route is appended
(never inserted) exactly as `Profile` was on 2026-09-22 — the comment in that file says so. The
run-end comparison is where the number actually lands emotionally: *"Stage 3, wave 4 — your best is
Stage 4"* does more work than any menu screen.

This is also the natural moment to surface the aggregate totals that `StatisticsSaveData` has been
collecting invisibly since it was written.

---

## Shape of the implementation (for sizing, not for building yet)

- `RunTimerService` — a small component owned by the run, accumulating both clocks, surviving the
  additive scene loads, and paused by nothing but `timeScale` itself.
- `RunHistoryEntry` + `RunHistorySaveData` — plain serializable classes beside `StatisticsSaveData`,
  written through `ISaveManager` like every other save type.
- `RunHistoryRecorder` — a static with one `RecordRunEnded(...)`, called from the three places
  `WaveManager` already ends a run (`CompleteStage` victory path, `OnCombatantDied` failure path, and
  the abandon path). This mirrors `MetaRewardLedger`: one chokepoint, so the record cannot be missed
  by a caller.
- `RunHistoryPresenter` — the Records screen. Authored canvas, not procedural, per §7.3 of
  `OPEN_TOPICS.md`.
- Wire `AddPlaytimeSeconds` at last, so the dead field starts meaning something.

Rough size: a day for the timer and the recording, a day for the screen, assuming the Records canvas
is authored rather than built in code.

## Two things to decide that are not on the list above

- **Does the timer keep running during the Shop and the build browser?** They are Intermission
  states with `timeScale` at 1, so under D1-2 they count as active play. That is probably right —
  build decisions are part of the run — but it is worth saying out loud, because it means a slow
  shopper's run reads as longer than a fast one's with identical combat.
- **Do tutorial runs get a history entry?** They already earn no Alloy or SP
  (`MetaRewardLedger.IsTutorialRun`). Suggest excluding them for the same reason: a guided run is not
  a performance.
