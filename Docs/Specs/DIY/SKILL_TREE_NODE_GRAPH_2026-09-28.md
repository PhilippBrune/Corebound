# Skill Tree Node Graph + Levelling — Concept for review, 2026-09-28

**Owner request:** *"Create a concept for a node graph. This concept should also contain a levelling
system and each level grants a SP."*

**Nothing here is implemented.** Decisions are numbered; answer them and the build follows.

---

## The good news: the graph already exists in data

This is not a new system. `SkillTreeDefinitionData` is already a graph and has been all along:

```
SkillTreeDefinitionData
└── SkillBranchData[]            branchId, displayName, accentColor
    └── SkillKeyNodeData[]       cost, prerequisiteNodeId, branchSpendRequirement, iconSpritePath
        └── SkillSubNodeData[]   costPerRank, maxRank, effects
```

`prerequisiteNodeId` is an edge. `branchSpendRequirement` is a gate. `maxRank` is a pip count.
Everything a node graph needs to *be* a graph is authored and enforced by `SkillTreeRules` today.

What is missing is only the **presentation**: `MetaProgressionRouteTargetPresenter` renders the
whole thing as a list of text rows. That is also why four finished art files —
`skill_node_key_frame.png`, `skill_node_sub_frame.png`, `skill_link_segment.png` and
`character_portrait_frame.png` — have been sitting unused: there is nothing for them to attach to.

So the work is a presenter plus one new authored field (position), not a redesign.

## N1 — How node positions are decided

1. **Authored per node.** Add `Vector2 _graphPosition` to `SkillKeyNodeData`; sub-nodes are placed
   on a small arc around their key node automatically.
2. **Computed layout.** Lay the graph out at runtime from the prerequisite edges (a layered/tree
   layout), no authoring.
3. **Authored per branch, computed within.** Each branch gets a screen sector; nodes inside it are
   placed by depth along the prerequisite chain.

**Recommendation: 1.** A skill tree's shape *is* content — the moment where two chains converge, or
a keystone sits deliberately far out, is a design statement and should not be a side effect of a
layout algorithm. It is also one `Vector2` per node, authored once. Option 2 looks cheaper and then
fights you forever the first time a node lands somewhere that reads wrong.

## N2 — Overall shape

1. **Three vertical branch columns** side by side, root at the bottom, keystones at the top.
2. **Radial**, branches fanning from a central portrait.
3. **Free-form canvas**, pan and zoom, no imposed structure.

**Recommendation: 2.** `character_portrait_frame.png` exists and is unused, which is a strong hint
about the intended centre. Radial also reads the branch identity instantly through direction, which
matters because `accentColor` is already authored per branch and currently shows as nothing but text
colour. Option 1 is the safe fallback if radial authoring proves fiddly.

## N3 — Navigation (controller-first is binding)

1. Free 2D navigation — the stick picks the nearest node in that direction.
2. Branch tabs (existing) plus up/down within a branch.
3. Both: tabs switch branch, the stick moves within it, shoulder buttons jump between tiers.

**Recommendation: 3.** Pure 2D nearest-node navigation on a radial graph is where controller
tree-UIs usually become frustrating — diagonal neighbours are ambiguous. Keeping the existing tab
model as the coarse axis means the stick only ever disambiguates within one branch, which is a much
easier problem, and it preserves the navigation players already learned.

## N4 — What a node shows without being selected

At minimum: its frame (key vs sub art), its branch accent, its rank pips (`0/3`), and its state —
locked / affordable / unaffordable / owned. `SkillTree.md` already requires that unaffordable nodes
are visibly distinct from locked ones, and the list UI satisfies that today; the graph must not
regress it.

Suggested encoding, so state is readable without colour alone: locked = dimmed frame with no fill,
unaffordable = full frame with an empty fill and a cost chip, affordable = accent-tinted pulse,
owned = filled with rank pips lit.

---

## The levelling system

This is the substantive new mechanic, and it changes where Skill Points come from.

### Today

Skill Points come only from **boss defeats** (`MetaRewardLedger.GrantBossDefeat`, with a first-kill
bonus). Elites pay Alloy but no SP. That means a run that dies in Stage 2 earns **zero** SP — the
run bought nothing, and the failure is uncompensated.

### L1 — What earns XP

1. **Enemy kills only.**
2. **Kills, wave clears and stage clears**, weighted — a wave clear is worth a handful of kills, a
   stage clear a large lump.
3. **Damage dealt**, so contribution rather than last-hits.

**Recommendation: 2.** Kills alone reward grinding a wave you have already beaten and punish a
build that lets towers do the work. Weighting clears means the XP tracks *progress*, which is what
the level is supposed to represent, while kills keep the bar moving continuously so it never feels
static. Option 3 is invisible to the player and hard to explain.

### L2 — Is the level per profile or per run?

1. **Per profile**, persisting across runs — a long-term account level.
2. **Per run**, reset each time.
3. Both: a run level that feeds a smaller profile level.

**Recommendation: 1.** The request is "each level grants a SP", and SP is meta currency spent in a
meta screen; a per-run level that grants meta currency would make farming an early stage the optimal
strategy. A per-profile level also directly fixes the "a failed run bought nothing" problem above —
every run moves the bar, even a bad one.

### L3 — The curve

1. **Linear:** every level costs the same XP.
2. **Gentle geometric:** cost × 1.15 per level. Early levels arrive fast, later ones are a
   commitment.
3. **Banded:** cheap for the first ten, then a flat higher rate.

**Recommendation: 2**, tuned so that a *completed* stage is roughly one level early on and a
completed run is three to four levels by the time the player is deep. That keeps the promised "each
level grants a SP" meaningful without flooding the tree — the tree's total cost should stay the
budget it was designed around.

> **This needs balancing against the existing SP economy, not layered on top of it.** Boss SP is the
> only source today and the tree's costs were set against it. If levels add a second source without
> the boss numbers moving, the tree trivialises. Suggested starting point: reduce boss SP by roughly
> a third and let levels supply the difference, so the *total* stays near today's and the
> distribution becomes smoother.

### L4 — What the player sees

1. An XP bar on the run HUD.
2. An XP bar on the run-end summary and the main menu only.
3. Both, with the HUD bar behind the same Settings toggle as the run timer.

**Recommendation: 3**, reusing the toggle the run-timer design (D2) already introduces. The vision's
readability boundary is the reason: another always-on bar during combat is exactly the clutter
section 19 warns about, and players who want it can switch it on.

A level-up during a run should be a `NotificationPresenter` toast, not an overlay — nothing may
touch `Time.timeScale` mid-combat.

---

## Data and save changes

- `SkillKeyNodeData` gains `_graphPosition` (N1).
- `MetaProgressionSaveData` gains `_experience` and `_level`, appended never inserted, with
  `GrantExperience(int)` returning how many levels were crossed so the caller can grant SP and toast
  once per level.
- `MetaRewardLedger` gains `GrantExperience(...)`, called from the kill and wave/stage-clear paths —
  the same single-chokepoint shape it already uses, so no caller can forget.
- The existing `RunProgressionSnapshot` needs no change; levels grant SP, and SP already flows.

## What this replaces in the docs

`OPEN_TOPICS.md` §3.1 lists the four skill-tree art files as "needs a feature first". This is that
feature. Once N1–N4 are answered, those four files stop being a backlog item and become the first
thing the graph presenter asks for.

## Rough size

Two days for the graph presenter (authored canvas, radial layout, controller navigation), half a day
for the levelling data and grant path, half a day for the XP bar and level-up toast. The graph is the
expensive half, and almost all of that is navigation feel rather than drawing.
