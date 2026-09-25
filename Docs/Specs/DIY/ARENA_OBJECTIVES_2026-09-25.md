# Arena Objectives — Decisions and Build, 2026-09-25

**Problem, in the owner's words (playtest 2026-09-25):** *"I currently don't like that it feels like
there is no need to use the whole arena."*

Two answers were picked out of that session's list. **Both are now in the build.**

| Answer | Status |
| --- | --- |
| **Attack from two sides at once.** A wave's spawn groups take opposed gates, so a central position cannot cover both. | **Shipped 2026-09-25.** Gate choice moved from per-enemy to per-spawn-group in `WaveSpawnPlacement`, gates ordered by angle so "opposite" means opposite in the arena and not in the authoring order, and Stage 1 gained a second gate. |
| **Timed arena objectives — a salvage cache that appears at a random spot mid-wave and expires.** | **Shipped 2026-09-25** to the owner's answers below. |

Two-sided waves change where enemies *come from*. They do not, on their own, give the player a
reason to be anywhere except between the Core and the nearest threat. The cache is what makes a far
corner of the arena worth standing in, so it is the half of the answer that actually moves the
player.

---

## Decisions

Asked as numbered options; the owner answered on 2026-09-25. Where the answer differs from the
recommendation that was offered, the answer wins and the recommendation is recorded as rejected.

| # | Question | Answer | Note |
| --- | --- | --- | --- |
| D1 | What the cache gives | **1 and 3** — a Scrap lump **and** a timed fire-rate buff | Both halves are independent fields on the data asset; either may be zeroed. |
| D2 | Where it appears | **1** — uniformly random inside the arena | Recommendation 2 (minimum distance from the player, Core exclusion) was **rejected**. |
| D3 | How it is claimed | **1** — walk over it | Recommendation 2 (a stand-still channel) was **rejected**. No new binding, no new interact-router claimant. |
| D4 | How long it lasts | **2** — computed, not fixed | |
| D5 | Telegraph | *(no answer)* | Built to the recommendation, minus audio — see "Not built" below. |
| D6 | When and how often | **3** — a per-wave chance rising with stage index | Recommendation 2 (one per wave from wave 2) was **rejected**. |
| D7 | If ignored | **1** — it expires and is gone | |
| D8 | Implementation traps | *(no answer — not a choice)* | All four honoured; see below. |
| D9 | Later variants | *(no answer)* | Not built. The data model keeps payload separate from placement so they stay cheap. |

## What that produced

**Payload (D1).** Claiming credits `ScrapFractionOfWaveBudget` (0.55) of the wave's own drop budget —
the sum of `count × EnemyRewardData.ResourceRewardAmount` across its spawn groups, so the reward
scales with the stage instead of needing a per-stage number. It goes through `AddReward`, not
`AddFlat`, so Scrap-gain cards and skills apply exactly as they do to an enemy drop. It also grants
a 20s fire-rate buff.

> **One correction worth recording.** The proposal said "+35% fire rate — say a 0.65 multiplier".
> Those are not the same thing: the field multiplies the fire *interval*, so the conversion is
> reciprocal, and 0.65 delivers **+54%**. The asset ships at **0.74**, which is the +35% that was
> actually agreed. A test run printed "+54% fire rate" and is what caught it.

**Placement (D2).** Uniformly random, with no distance floor and no Core exclusion, as chosen. The
roll picks from `StageArenaArtPresenter.WalkableInteriorPositions` — the open maze cells, already
used to place the Core — rather than rolling a raw point. That is reachability, not the rejected
constraint returning: props and maze blockers carry real `BoxCollider2D`s, so a raw uniform point
lands inside a wall often enough to matter, and a cache you can see but cannot walk to is the same
failure shape as the 2026-09-20 "enemies spawned outside the boundary" bug. The cells are themselves
spread across the arena, so the distribution is the uniform one that was asked for.

**Claim (D3).** Walk within `ClaimRadius` (0.75). No channel, no press.

**Lifetime (D4).** `max(distance to player, half the arena diagonal) / move speed × detour factor +
slack`, floored at 6s. The detour factor (1.6) is not in the original proposal and was added after
testing: straight-line time in Stage 1 came out at 1.5s, so every lifetime collapsed onto the
minimum — which is the fixed duration D4 rejected, wearing a different hat. Stage 1 now computes
around 7s.

**Frequency (D6).** `0.35 + 0.1 × (stageIndex − 1)`, capped at 0.8. Rolled **once at wave start**,
not per frame — a per-frame chance would effectively guarantee one in a long wave and never fire in
a short one. The cache then appears when half the wave's enemies have spawned: progress, not a
clock, so a build fast enough to delete a wave in six seconds still sees one.

**Expiry (D7).** Nothing credited, nothing punished. A toast says it was lost, so the player knows
the choice had a cost.

**The D8 traps, all honoured.** The objective is its own runtime and is *not* registered with
`ResourcePickupPool`, so `ForceCollectAll()` at wave end cannot hand the player every cache they
ignored — that would have deleted the whole mechanic silently, and only in a full playthrough. It
cannot appear during the 2s wave-start delay (the tick sits after that early return, so it never
competes with the gate pulse). It is dismissed silently — neither claimed nor expired — when the
wave ends, when the run fails, and when a boss encounter starts. Nothing it does opens an overlay or
touches `Time.timeScale`.

**Boss waves are excluded**, as the rejected D6 option 2 also wanted: boss encounters go through
`StartBossEncounter` and never reach the wave path that rolls for an objective.

## Files

New: `Gameplay/Arena/ArenaObjectiveData.cs` (the ScriptableObject),
`ArenaObjectiveRuntime.cs` (the world object), `ArenaObjectiveService.cs` (every decision), and the
asset `ScriptableObjects/Gameplay/Arena/ArenaObjective_SalvageCache.asset`, wired to the
`WaveManager` in `SampleScene`. Leaving that field empty turns the feature off with no code change.

Touched: `WaveManager` (roll, tick, and the three cleanup paths), `PlayerCombatController`
(`ApplyTimedFireIntervalBuff`, a run-scoped buff that is deliberately not a card, skill or weapon
modifier — those are permanent and saved), `PlayerController` (`CurrentMoveSpeed`, for the lifetime
computation), `StageArenaArtPresenter` (`WalkableInteriorPositions`).

## Not built

- **Audio.** Appear, claim and expire have no cues. Agents cannot hear audio and must not guess at
  it — this needs the owner to say which sounds.
- **Dedicated art.** The body is `resource_primary_icon.png` scaled up and the ring is a tinted
  square that shrinks as the timer drains. Both work and both are visibly placeholders; prompts are
  Assets 106 and 107 in `Docs/AI/GRAPHICS_ASSET_PROMPTS.md`.
- **The D9 variants** (Overload Vent, Relay Uplink, Breach). The runtime keeps payload separate from
  placement and claiming so they stay a data question, but none is authored.

## Unplayed

Nobody has played this. What a run actually feels like with a cache pulling the player off the Core
— whether 0.55 of the wave budget is worth the trip, whether ~7s is too tight or too generous,
whether a 35% chance reads as "be ready" or as "random" — is exactly the part an autopilot cannot
answer. Verified by machine: it appears at the right point in a wave, lands somewhere walkable,
expires when ignored, pays out both halves when claimed, and does not survive its wave.
