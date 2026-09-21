# Skill Tree Specification

Status: Requirements — not implemented. Written 2026-09-21 from the owner decision that a second currency and a Diablo-4-style skill tree should exist.

Depends On: MetaProgression, Economy, Resources, SaveSystem, UI, Input

---

## Purpose

COREBOUND has one in-run currency (Scrap) and one run-completion currency (meta currency). Neither expresses **long-term character growth**: today a player who has finished fifty runs starts run fifty-one with exactly the same character as run one, only with a wider option pool.

The Skill Tree is the between-run growth spine. It answers "what did I get out of that run, permanently" with something the player chooses rather than something handed to them.

---

## Design Goals

- Growth the player **spends and shapes**, not growth they passively accrue.
- Every point spent must be visible in the next run within the first minute.
- The tree must widen options far more than it raises raw numbers, so Section 10 of the vision ("should not completely replace player skill") stays true by construction.
- Readable on a controller at a glance, with no free-form panning required to find anything.
- Endless at the top, so a player at 100 hours still has somewhere for points to go.

---

## Currency: Skill Points

**Skill Points (SP)** are the second currency. They are distinct from Scrap (in-run, spent on towers and shop offers) and from the third currency defined in `Upgrades.md` (spent on unlocks and weapon/tower stat upgrades).

| Property | Value |
| --- | --- |
| Starting balance | 0 |
| Earned by | completing a stage, defeating a boss, completing a run, first-time milestones |
| Spent on | skill tree nodes |
| Refundable | yes, full respec (see Respec) |
| Persisted in | `MetaProgressionSaveData` |
| Cap | none; see Ascension for what happens past the tree's own maximum |

### Earning rates

These are the starting values for balancing, not final.

| Event | SP |
| --- | --- |
| Clear a stage | 1 |
| Defeat a stage boss | 2 |
| Complete a full run (all 5 stages) | 5 |
| Fail a run, per stage cleared | 1 |
| First-time boss kill (once per boss) | 3 |
| First-time full-run clear (once) | 10 |

A failed run must still pay something, or a player who is stuck stops progressing entirely and the tree becomes a wall rather than a ramp.

### Rule: SP never buys power the run itself can buy

SP must not purchase Scrap, towers, cards, or anything obtainable during a run. If SP and Scrap can buy the same thing, the in-run economy stops mattering. SP buys *starting conditions and rule changes*, never run resources.

---

## Structure

The tree is built from **Key Nodes** and **Sub Nodes**, in the Diablo 4 arrangement the owner asked for.

### Key Node

A Key Node is a large, named, build-defining node. It is the entry point to a cluster.

- Costs **3 SP**.
- Grants a named, rule-changing effect (not a percentage).
- Unlocks its own Sub Nodes for purchase.
- Has at most one prerequisite Key Node, forming a directed acyclic graph, never a cycle.

### Sub Node

A Sub Node is a small numeric or conditional modifier attached to exactly one Key Node.

- Costs **1 SP**.
- Requires its parent Key Node to be purchased.
- May be ranked: a Sub Node with `MaxRank > 1` can be bought that many times, each rank costing 1 SP.
- Between 3 and 6 Sub Nodes per Key Node.

### Branch

Key Nodes are grouped into **Branches**, one per gameplay pillar, so a controller player always knows which direction to hold.

| Branch | Theme | Example Key Nodes |
| --- | --- | --- |
| Combat | the player's own weapons and survivability | Opening Volley, Second Wind, Executioner |
| Construction | towers and buildable slots | Forward Foundations, Reinforced Plating, Field Repair |
| Industry | Factory, Scrap economy, pickups | Salvage Instinct, Overclocked Chain, Magnetise |
| Command | run structure, rewards, shop, cards | Wider Draft, Bargain Struck, Contingency |

Four branches × 4 Key Nodes × ~4 Sub Nodes is the first content target: **16 Key Nodes, roughly 64 Sub Nodes, ~112 SP to fill completely.**

### Gating

A Key Node becomes purchasable when **both** hold:

1. Its prerequisite Key Node (if any) is purchased.
2. The player has spent at least `BranchSpendRequirement` SP anywhere in the same Branch.

`BranchSpendRequirement` is 0 for the first Key Node of a branch, then 4, 10, 18. This is what stops a player rushing straight to the deepest node and produces the "fill out a cluster, then move on" rhythm.

---

## Ascension (the Paragon equivalent)

When every Key Node and every Sub Node rank has been purchased, the tree is **complete**. Further SP goes into **Ascension**.

- Ascension is a flat, endless list of **Ascension Ranks**, not a second tree.
- Each rank costs `5 + floor(rank / 10)` SP, so the curve stretches slowly rather than walling.
- Each rank grants a choice of one of **three randomly offered small bonuses**, drawn from a fixed Ascension pool.
- The offer is rerolled only when the rank is purchased, so the choice is real but not farmable.

### Ascension pool

Ascension bonuses are deliberately small and numeric, because they are the part with no ceiling:

- +2% weapon damage
- +2% tower damage
- +2% Scrap from all sources
- +1% move speed
- +1% Core max health
- +0.5% critical chance

**Hard rule:** no Ascension bonus may unlock, enable, or change a rule. Ascension is numbers only. Every rule change lives in the tree proper, which is finite. This is what keeps a 500-hour player from having a categorically different game rather than a slightly stronger one.

---

## Respec

- Full respec only; no partial refunds. Partial respec turns every choice into a temporary one.
- Refunds 100% of SP spent in the tree. Ascension ranks are **not** refunded, because their offers were rolled.
- Costs nothing the first time per "tree version"; a content update that changes the tree grants a free respec automatically.
- Available only from the main menu, never mid-run, so a player cannot respec around a boss.

---

## Data Model

All ScriptableObjects, matching the project's existing conventions.

### `SkillBranchData`
| Field | Type | Notes |
| --- | --- | --- |
| `_branchId` | string | stable id, used in save data |
| `_displayName` / `_displayNameDe` | string | localised via `LocalizationService.Resolve` |
| `_accentColor` | Color | branch identity in the UI |
| `_keyNodes` | `SkillKeyNodeData[]` | ordered; index drives the spend requirement |

### `SkillKeyNodeData`
| Field | Type | Notes |
| --- | --- | --- |
| `_nodeId` | string | stable id |
| `_displayName` / `_displayNameDe` | string | |
| `_description` / `_descriptionDe` | string | |
| `_cost` | int | default 3 |
| `_prerequisiteNodeId` | string | empty for a branch root |
| `_branchSpendRequirement` | int | 0 / 4 / 10 / 18 |
| `_effects` | `SkillEffectData[]` | |
| `_subNodes` | `SkillSubNodeData[]` | |
| `_iconSpritePath` | string | same StreamingAssets-first convention as every other art path |

### `SkillSubNodeData`
| Field | Type | Notes |
| --- | --- | --- |
| `_nodeId` | string | |
| `_displayName` / `_displayNameDe` | string | |
| `_description` / `_descriptionDe` | string | |
| `_costPerRank` | int | default 1 |
| `_maxRank` | int | default 1 |
| `_effects` | `SkillEffectData[]` | scaled by rank |

### `SkillEffectData`
Mirrors the existing `CardEffectData` shape so the two systems can share a resolver.

| Field | Type | Notes |
| --- | --- | --- |
| `_target` | `SkillEffectTarget` | enum, **append-only** (serialized by index) |
| `_operation` | `CardEffectOperation` | Add or Multiply, reused as-is |
| `_value` | float | per rank |

### `SkillTreeDefinitionData`
Top-level asset: `_branches`, `_ascensionPool`, `_ascensionBaseCost`.

### Save data

`MetaProgressionSaveData` gains:

| Field | Type |
| --- | --- |
| `SkillPointsEarned` | int (lifetime total, for display) |
| `SkillPointsSpent` | int |
| `PurchasedNodeRanks` | `List<NodeRank>` where `NodeRank { string NodeId; int Rank; }` |
| `AscensionRank` | int |
| `AscensionChoices` | `List<string>` (chosen bonus ids, in order) |

Available SP is always `SkillPointsEarned - SkillPointsSpent`, never stored directly, so the two can never disagree.

---

## Runtime Design

- **`SkillTreeService`** (MonoBehaviour, on the persistent meta object) owns purchase validation, respec, and the resolved effect snapshot.
- **`SkillModifierSnapshot`** is rebuilt whenever the purchase set changes, exactly like `CardModifierSnapshot`. Gameplay systems read the snapshot, never the tree.
- Run start composes: base stats → skill snapshot → meta unlocks → in-run card snapshot. Skill effects apply **before** cards, so a card that multiplies damage multiplies the already-skilled value.
- `SkillTreeService` raises `SkillPointsChanged`, `NodePurchased`, `TreeRespecced`, `AscensionRankGained`.

---

## UI

A dedicated Skill Tree route from the main menu, alongside Meta Progression.

- One branch on screen at a time; shoulder buttons switch branch. This is what keeps it controller-navigable without free panning.
- Key Nodes are laid out on a fixed vertical spine; Sub Nodes fan around their parent.
- Every node shows: name, cost, current rank / max rank, and its effect in plain words.
- Unaffordable nodes are visibly distinct from locked ones. "You cannot afford this" and "this is not available yet" must never look the same.
- Available SP is permanently visible in the header.
- Ascension is a separate tab in the same route, showing current rank, next cost, and the three offered bonuses.

---

## Audio

- Node purchase: a firm, low confirmation distinct from the shop's buy cue.
- Key Node purchase: a larger version of the same cue, since it is a build-defining moment.
- Respec: a single descending sweep, deliberately unsatisfying.
- Insufficient SP: the shared insufficient-funds cue already used by the shop.

---

## Balancing

- A player should finish their first full run with roughly 8-10 SP: enough for two Key Nodes plus a few Sub Nodes, so the first completed run visibly changes the second.
- Filling the tree should take approximately 25-35 completed runs.
- No single Branch may contain more than 30% of the tree's total numeric power, or one branch becomes mandatory.
- Ascension must stay below 0.5% total power per rank so that rank 200 is a meaningful but not transformative advantage.

---

## Rules

- SP is earned only from run outcomes, never purchased, never granted by cards or relics.
- The tree is read at run start and frozen for the run's duration; no mid-run respec, no mid-run purchase.
- Skill effects use the same `CardEffectOperation` semantics as cards, so a player who understands one understands the other.
- `SkillEffectTarget` is append-only. It is serialized by index in the node assets, exactly like `CardEffectTarget` and `EnemyRoleType`.
- Every node must be describable in one sentence. A node that needs a paragraph is two nodes.

---

## Open Questions

- Should the tree be shared across all future playable characters, or one tree per character? Recommend shared until a second character actually exists.
- Should failing a run on Stage 1 pay 0 SP or 1? Currently specified as 1 per stage *cleared*, so a Stage 1 failure pays 0. Worth revisiting if early-run failure feels punishing in playtest.
- Does Ascension need a visible prestige marker (a rank badge in the main menu)? Recommend yes, but after the tree itself ships.

---

## Acceptance Criteria

- A new player starts with 0 SP and an entirely unpurchased tree.
- Completing a stage, a boss and a run each grant SP, and the amounts match the table above.
- A Key Node cannot be purchased without its prerequisite and its branch spend requirement satisfied, and the UI says which of the two is missing.
- A ranked Sub Node can be purchased up to `MaxRank` times and its effect scales per rank.
- A full respec returns exactly the SP spent in the tree and leaves Ascension untouched.
- Purchases survive a save/load cycle and apply to the next run started.
- With the tree fully purchased, further SP can be spent on Ascension ranks indefinitely, with a cost that grows on the specified curve.
- No skill effect can be observed changing mid-run.
