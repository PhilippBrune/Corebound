# Levelling cap, Skill Point quests and Paragon — Concept for review, 2026-09-29

**Owner request:** *"Each level gives 1 SP. Current max level == max amount to spend SP. After
reaching max level a mechanic similar to Paragon level from Diablo 4 is for unlocking Ascension.
Vision: define a quest system for unlocking SP, e.g. one quest for each weapon to defeat 500 of each
enemy, and each of those quests grants 1 SP. Another quest for each weapon to equip 6 weapons of the
same kind, and so on. I would need to know: what is the current max level? I have in mind that 10%
of SP are done via quests."*

This is a design document. Nothing in it is implemented. Answer each decision with its code, e.g.
`P1-1, P2-1, P3-2 ...`, the same way as the node-graph concept.

---

## The answer first: what is the current max level?

**There is none.** `MetaProgressionSaveData.ResolveLevelCost` is `120 × 1.15^(level − 1)` with no
cap, so the level is unbounded in code. In practice the curve becomes a wall:

| Level | XP for the next level | Total XP to reach it | Full runs to reach it (est.) |
| --- | --- | --- | --- |
| 10 | 422 | 2,015 | ~1 |
| 20 | 1,708 | 10,584 | ~4 |
| 30 | 6,909 | 45,259 | ~18 |
| 40 | 27,951 | 185,538 | ~74 |
| 50 | 113,077 | 753,046 | ~300 |

So the **practical** cap is **around level 30–35**, which is 30–35 SP from levels. The skill tree
costs **212 SP** (Combat 55, Construction 53, Industry 52, Command 52). Today the rest has to come
from bosses: repeat kills pay about **6 SP per full run** (stage 1–5: 1, 1, 1, 1, 2 after the ×0.67
scale), first kills about **13 SP once**.

> **Unmeasured:** "full run" above assumes about **2,500 XP per complete five-stage run** (kills × 2,
> waves × 18, stages × 140). A full run has never been timed or counted (vision §10, open point 2).
> Every run-count estimate in this document scales with that number. It should be measured with a
> `god5`-style autopilot run before the curve is committed.

---

## Decisions at a glance

| Code | Question | Recommendation |
| --- | --- | --- |
| P1 | What does "max level == max SP" mean? | **1** — levels + quests together pay exactly the tree cost |
| P2 | Level cap and curve | **1** — cap **190**, linear-growing cost `150 + 8 × (level − 1)` |
| P3 | What happens to boss Skill Points? | **2** — first kills become quests, repeat kills pay XP instead of SP |
| P4 | Which quests pay SP? | **The 23 listed below** (weapon kills, weapon sets, boss first kills) |
| P5 | Quests beyond the SP budget ("and so on") | **2** — they pay Alloy or Ascension Points, never SP |
| P6 | Quest progress per character or per profile? | **1** — per character, like the tree |
| P7 | What happens after the cap (Paragon)? | **3** — Paragon levels fund an Ascension ring on the existing graph |
| P8 | Where quests are shown | **1** — a Quests tab on the Progression screen, plus an in-run toast |

---

## P1 — What "max level == max amount to spend SP" means

1. **Levels plus quests pay exactly the tree cost.** 212 SP in total: about 90% from levels, about
   10% from quests. At the cap with every quest done, the tree is complete; nothing is left over.
2. **Levels alone pay the whole tree, quests are extra.** Cap = 212. Quests then add about 10% on
   top, which would finish the tree before the cap and leave 23 SP with nowhere to go but Ascension.
3. **The cap equals the tree cost, and quests speed it up.** Same as 2, described differently.

**Recommendation: 1.** It is the only reading where both of your statements hold at once — "max level
== max SP" and "10% via quests". It also keeps one invariant forever: *the tree can be completed,
and it can never be over-funded*, which is what stops the tree trivialising when SP sources are added.

## P2 — Level cap and curve

With P1-1 and the 23 quest SP of P4: **cap = 212 − 23 + 1 = level 190** (a new character starts at
level 1 with 0 SP; each of the 189 level-ups pays 1 SP). 189 + 23 = 212.

1. **Linear-growing:** each level costs `150 + 8 × (level − 1)` XP.
2. **Keep ×1.15 and raise the XP income** so level 190 becomes reachable.
3. **Banded:** fast first 20 levels, then flat.

**Recommendation: 1.** The ×1.15 curve cannot reach 190 at any sane income (level 60 alone needs
three million XP). A linear-growing cost is still front-loaded where it matters and bounded at the top:

| Level | XP for the next level | Total XP to reach it | Full runs (est.) |
| --- | --- | --- | --- |
| 2 | 158 | 150 | a third of a stage |
| 10 | 222 | 1,638 | < 1 |
| 20 | 302 | 4,218 | ~2 |
| 50 | 542 | 16,758 | ~7 |
| 100 | 942 | 53,658 | ~21 |
| 150 | 1,342 | 110,558 | ~44 |
| **190 (cap)** | — | **170,478** | **~68** |

At 60–70 minutes per full run (vision §10) that is **about 70 hours to the cap**, which leaves the
vision's "reasons to continue after 100 hours" to Paragon (P7). A new player who dies in Stage 1 or 2
still gains 2–4 levels per run, which keeps the "a failed run bought nothing" fix from the levelling
concept intact.

## P3 — Boss Skill Points

Today bosses pay SP directly. Under P1-1 every SP must come from a level or a quest, or the budget
leaks.

1. **Keep boss SP** and lower the cap by the expected boss SP. The cap would then depend on how
   often a player kills bosses, so "max level == max SP" stops being exact.
2. **First kills become quests; repeat kills pay XP.** "Defeat the Stage N boss" ×5 are quests worth
   1 SP each. A repeat kill pays a large XP lump (suggested 300 × stage) and its Alloy as today.
3. **Remove boss SP entirely** and pay only XP.

**Recommendation: 2.** The first kill of each boss is already special-cased
(`RecordBossDefeated`), so it maps onto a quest cleanly, and repeat kills still feel rewarding
because a big XP lump visibly moves the bar.

## P4 — The SP quest catalogue (23 SP, 10.8%)

Each quest pays **1 SP once**. With 9 weapons and 5 stages today:

| Group | Quest | Count | SP |
| --- | --- | --- | --- |
| Weapon mastery | Defeat 500 enemies with **{weapon}** | 9 | 9 |
| Weapon arsenal | Carry six **{weapon}** at once (a full Set Bonus loadout) | 9 | 9 |
| Boss firsts | Defeat the **Stage N** boss | 5 | 5 |
| | | **23** | **23** |

Notes:

- "Defeat 500 of each enemy" in your message: this reads it as **500 enemies of any kind with that
  weapon**. If you meant 500 of *each enemy type* per weapon, that is 5 × 500 = 2,500 kills per weapon
  — about 20 full runs per weapon, and 180 runs for the set. The quest count stays the same; only the
  grind changes. Say which you meant.
- **Adding content changes the budget.** A tenth weapon adds two SP quests. Under P1-1 the cap would
  have to drop by two, which players would notice. The clean rule: *the SP quest list is fixed to the
  launch roster; new weapons get P5-style quests.* Alternatively the tree grows by the same amount.

## P5 — Quests beyond the SP budget ("and so on")

There are many more good quests than 23 SP can pay for:

- Towers: build each tower to its maximum upgrade level; defeat 300 enemies with each tower type.
- Enemies: defeat 1,000 of each enemy type; defeat each elite affix 50 times.
- Factory: reach chain tier 4 in a run.
- Collection: collect every card of a rarity (ties into the new Records card grid).
- Runs: clear a stage without the Core taking damage; clear a stage with one weapon type only.

1. **Pay SP**, and let the SP share grow past 10%.
2. **Pay Alloy or Ascension Points (P7)**, never SP.
3. **Pay cosmetics / titles only.**

**Recommendation: 2.** It keeps P1's invariant exact and still makes every quest worth doing. Alloy
early (it funds unlocks and upgrades), Ascension Points once the player is past the cap.

## P6 — Per character or per profile?

The vision says each playable character has its own tree and is levelled separately.

1. **Per character.** Quest progress and rewards belong to the character that earned them.
2. **Per profile.** A quest done once pays every character.

**Recommendation: 1.** SP is spent in a character's tree, so it must be earned by that character, or
a second character starts with a pre-filled tree and the vision's "a second character starts at zero"
breaks. There is only one character today, so this costs nothing yet — it only decides where the
counters are stored.

## P7 — After the cap: Paragon

In Diablo 4, experience keeps flowing after the level cap into Paragon levels, and each Paragon level
pays a point that is spent on a board. Here the target is Ascension, which already exists: today it
unlocks when the tree is complete and costs SP per rank (5 + rank / 10), offering a choice of three
small bonuses.

In every option below, XP past level 190 fills **Paragon levels** at a **flat 1,650 XP each** (the
cost of the last normal level-up, about two thirds of a full run). Each pays **1 Ascension Point (AP)**.
Ascension unlocks at the level cap, as you described, not at tree completion.

1. **Ascension as it is, paid in AP.** One rank per AP, choice of three per rank. Smallest change.
2. **A Diablo 4 style board.** A grid per character, entered from one edge, with normal, magic and
   rare nodes and glyph sockets; a new board every 20 Paragon levels. The most faithful, and by far
   the most authoring and UI work.
3. **An Ascension ring on the existing graph.** Past each branch's last key node, the radial graph
   gains an outer ring: repeatable small nodes (1 AP per rank, soft-capped per node), and every 10th
   Paragon level a **Keystone** — the existing choice-of-three offer, with stronger bonuses.

**Recommendation: 3.** It keeps the Paragon *feel* — spending points on a board, choosing where to
grow — on the graph that was just built, instead of a second board screen. It keeps Ascension's
choice-of-three as the Keystone rather than throwing it away, and it stays inside the vision's
"endless track of small numeric bonuses".

## P8 — Where quests are shown

1. **A Quests tab on the Progression screen**, grouped as in P4, with progress bars (`312 / 500`) and
   the reward, plus a `NotificationPresenter` toast in-run when one completes.
2. **A Quests tab in Records.**
3. **Both.**

**Recommendation: 1.** Quests pay SP, and SP is spent on the Progression screen, so that is where a
player looks for "how do I get more". The toast must not pause the game (vision: nothing touches
`Time.timeScale` mid-combat).

---

## Existing profiles

- Level and XP are re-evaluated against the new curve. If the new level is higher, the difference is
  paid in SP. A level is never taken away.
- Bosses already killed count as their first-kill quests, **done but not paid again** — that SP was
  paid when they died.
- A profile holding more SP than the new budget (debug grants, Profile 2) keeps it. The invariant is
  for real play, not for test profiles.

## What the code needs

- `MetaProgressionSaveData`: level cap, new `ResolveLevelCost`, Paragon level and XP, AP balance,
  quest counters and completion flags per character (fields appended, never inserted).
- **Kill attribution.** `DamageContext` knows only `SourceType` (Player, Tower, ...), not *which*
  weapon fired. Weapon mastery needs a source id on it, set where projectiles are fired. This is the
  one change that touches combat code.
- `MetaRewardLedger`: boss first kill routes to the quest, repeat kill pays XP; Paragon XP past the
  cap; a single `QuestLedger.Record(...)` chokepoint in the same shape.
- Loadout change event for the "six of one weapon" quests.
- Progression screen: Quests tab (P8), Ascension ring on the graph (P7-3).

## Rough size

Half a day for the curve, cap, migration and boss change. One day for the quest ledger, kill
attribution and the Quests tab. Two days for the Ascension ring on the graph, most of it navigation
and the Keystone offer.
