# Arena Objectives — Suggestions, 2026-09-25

**Problem, in the owner's words (playtest 2026-09-25):** *"I currently don't like that it feels like
there is no need to use the whole arena."*

Two answers were picked out of that session's list. One is already in the build; this document is
the design work for the other.

| Answer | Status |
| --- | --- |
| **Attack from two sides at once.** A wave's spawn groups take opposed gates, so a central position cannot cover both. | **Shipped 2026-09-25.** Gate choice moved from per-enemy to per-spawn-group in `WaveSpawnPlacement`, gates ordered by angle so "opposite" means opposite in the arena and not in the authoring order, and Stage 1 gained a second gate. |
| **Timed arena objectives — a salvage cache that appears at a random spot mid-wave and expires.** | **Not built.** Decisions below. |

Two-sided waves change where enemies *come from*. They do not, on their own, give the player a
reason to be anywhere except between the Core and the nearest threat. The cache is what makes a
far corner of the arena worth standing in, so it is the half of the answer that actually moves the
player.

Everything below is a decision to make, in the A/B form of `SPEC_REVIEW_2026-09-22.md`: numbered
options, a recommendation, and the reasoning. Nothing is implemented.

---

## D1 — What the cache gives

1. **A lump of Scrap**, sized off the wave's own drop budget (suggested: 50–60% of what the wave's
   enemies drop in total), so it stays worth crossing the arena for in stage 5 as well as stage 1.
2. **A free card**, as a one-off reward offer.
3. **A timed combat buff** for the remainder of the wave — say +35% fire rate for 20s.
4. **A tower charge**: one free level on any tower, or a temporary range boost to every tower.

**Recommendation: 1.** Scrap is the currency every other system already reads, so the cache is
instantly meaningful to towers, cards and Core repair without inventing a new reward type, and it
needs no new UI at all — the wallet HUD already shows it land. It also matches the fiction the name
carries.

Option 2 is the strongest pull on paper and the worst fit in practice: every card offer in the game
opens an overlay, and an overlay holds `Time.timeScale` at 0 (`WaveStatePresenter.OpenOverlay`).
Stopping a live wave to shop is the opposite of making the arena matter. Option 3 is the best
*second* cache type once the runtime exists — see D9. Option 4 overlaps with the shop.

## D2 — Where it appears

1. Uniformly random inside the arena bounds.
2. Random, but constrained: at least ~40% of the arena diagonal from the player at the moment it
   appears, outside a Core exclusion radius, and not inside an obstacle.
3. On authored anchor points per stage, like tower slots, picked at random.

**Recommendation: 2**, with **3 as the fallback**. The point of the cache is to pull the player off
their position, so a roll that lands it at their feet has done nothing — the distance constraint is
the whole feature, not a polish detail.

The obstacle check is not optional. `StageArenaArtPresenter` puts real `BoxCollider2D`s on stage
props and on the obstacle-grid maze cells, so a uniformly random point can easily be inside a wall,
where the cache is visible and unreachable. That is the same failure shape as the 2026-09-20
"enemies spawned outside the boundary and the wave hung forever" bug, and it deserves the same
treatment: a `Physics2D.OverlapCircle` test against the obstacle layer, a handful of re-rolls, and
a safe fallback point if none pass. If that proves fiddly per stage, switch to option 3 — authored
anchors are guaranteed reachable and cost one array per stage.

**Deliberately not constrained: tower coverage.** The cache should be allowed to appear where your
towers do not reach. That is precisely what makes where you built matter.

## D3 — How the player claims it

1. **Walk over it**, like a resource pickup.
2. **Stand in it for ~1.5s**, a channel with a radial fill.
3. **Interact press**, routed through `InteractRouter` at a new priority below `TowerSlot`.

**Recommendation: 2.** Walking over it makes the cache a free detour — you were going to move
anyway, and nothing was risked. Standing still for a second and a half in the far corner of the
arena while a wave is live is the actual cost, and it is what turns "I collected a thing" into a
decision. It adds no binding, works identically on controller and keyboard, and reads at a glance
as a filling ring.

Two rules that matter for how it feels:

- **The channel breaks on leaving the ring, not on taking damage.** A damage-break turns it into a
  reflex test and makes it impossible while anything is shooting you; a position-break keeps it a
  commitment you choose to make and can defend with a tower or a dash out and back.
- **Progress persists while you are in the ring** but resets when you leave, so you cannot nibble at
  it across three visits.

Option 3 adds a button press without adding any risk, and puts a fourth claimant into the interact
router that finding A1 just finished untangling.

## D4 — How long it lasts

1. A fixed duration, e.g. 8s.
2. **Computed**: the time to cross from the Core to the cache at base move speed, plus the channel
   duration, plus ~3s of slack.

**Recommendation: 2.** A fixed number that is generous in Stage 1's 16×10 arena is a cruel joke in
Stage 5's, and nobody will remember to retune it when a stage is resized. Computing it keeps the
window *just* achievable everywhere, which is the only setting at which the choice is interesting —
too long and you always go, too short and you never do.

## D5 — How it is telegraphed

The camera contains the whole arena, so no off-screen indicator is needed; the cache can simply be
seen. What it needs:

- A world sprite with a pulsing beacon, reusing the spawn-gate glow treatment so the arena's
  "something is happening here" vocabulary stays consistent.
- A countdown on the object itself — a draining ring is readable at arena scale where digits are
  not — plus one HUD line while a cache is live.
- Distinct cues on appear, on claim and on expire. **Do not guess at the audio**; ask which sounds.
- The last ~2s read differently (faster pulse, colour shift), because the decision to abandon a run
  for it happens in those two seconds.

## D6 — When and how often

1. One per wave, every wave.
2. One per wave from wave 2 onward, skipping the boss wave.
3. A per-wave chance that rises with stage index.

**Recommendation: 2.** Predictable enough to plan around — "there will be one, be ready to go" is a
better thought than "maybe there will be one". Wave 1 stays clean because the tutorial already owns
it, and boss waves are excluded because a boss fight already dictates where the player stands, so a
cache would compete with the fight rather than shape it.

**Trigger on wave progress, not on a clock.** Spawn the cache when roughly half the wave's enemies
have been spawned. A fixed "8 seconds in" timer means a build with enough DPS to delete the wave in
six seconds never sees one, and the players most able to go for the cache are exactly the ones the
timer would exclude.

## D7 — What happens when it is ignored

1. It expires and is gone.
2. It expires and the wave gets harder.
3. It expires but part of its value is credited at wave end anyway.

**Recommendation: 1.** A missed opportunity should be a missed opportunity. Option 2 converts a
temptation into an obligation, which is how a "use the arena" mechanic becomes a "run to the corner
on schedule" chore. Option 3 removes the decision entirely — if you get it anyway, there was
nothing to decide. Design boundary 5 ("resources must create interesting choices rather than
unnecessary busywork") points the same way.

## D8 — The one implementation trap worth naming up front

`ResourcePickupPool.ForceCollectAll()` runs at wave end and vacuums every live pickup into the
player. If the cache is built on the pickup runtime, an ignored cache gets hoovered up for free the
moment the wave clears and the entire design evaporates — silently, and only in a full playthrough.
The cache must be its own runtime object that the wave-end vacuum does not touch, or it must
explicitly opt out.

Three smaller ones in the same family:

- It must not appear during the 2s wave-start spawn delay, or it competes with the gate pulse that
  tells the player where the wave is coming from.
- It must despawn when the wave ends, whichever came first.
- Nothing about it may open an overlay or touch `Time.timeScale` (design boundary 6).

## D9 — Where this goes next

The value of building this is that the second objective type is nearly free. A timed ring at a
chosen spot with a claim condition and a payload is one runtime; everything below is a different
payload on it, which is design boundary 8 ("new content should expand existing systems") working as
intended:

| Variant | Payload | What it does to positioning |
| --- | --- | --- |
| **Salvage Cache** | Scrap | Pulls the player away from the Core |
| **Overload Vent** | Stand in it or the Core takes damage | *Pushes* — the arena decides where you go, instead of tempting you |
| **Relay Uplink** | Disables an enemy buff aura for the rest of the wave | Rewards reading the enemy composition, not just the map |
| **Breach** | An extra gate stays open until someone closes it | Makes ignoring it visibly, immediately worse |

Only the Salvage Cache needs deciding now. The others are listed so the runtime is not built in a
shape that excludes them — chiefly, the claim condition and the payload should be separate from the
"timed ring somewhere in the arena" part.

---

## Rough size

New: an `ArenaObjectiveData` ScriptableObject (payload, durations, distance constraints) and an
`ArenaObjectiveRuntime` MonoBehaviour, spawned and ticked by `WaveManager` alongside the spawn
groups. Touched: `WaveManager` (trigger and cleanup), `WaveStatePresenter` (the HUD line),
`StageArenaArtPresenter` (the beacon, reusing the gate-glow code). New art: a cache sprite and a
claim ring; new audio: appear, claim, expire.

Call it a day and a half of work, most of it in placement validation and in making the claim read
clearly — not in the reward, which is two lines against the existing wallet.
