# Levelling cap, Skill Point quests and Paragon — decided and implemented 2026-09-29

**Owner request:** *"Each level gives 1 SP. Current max level == max amount to spend SP. After
reaching max level a mechanic similar to Paragon level from Diablo 4 is for unlocking Ascension.
Vision: define a quest system for unlocking SP [...] I have in mind that 10% of SP are done via
quests."*

**Owner decisions (2026-09-29):** every recommendation P1–P8 taken, with the level cap, the SP per
level and the quest list replaced by the owner's own numbers below. The second half of this document,
**Alloy quests**, is a new proposal for review.

---

## Part 1 — What was built

### The budget

The skill tree costs **212 SP** (Combat 55, Construction 53, Industry 52, Command 52). This was
re-counted from the asset and from `SkillTreeRules.ResolveTotalTreeSpend`, which agree. The 211 seen in
play was the balance on Profile 2 after test grants, not the tree's cost, so the card collection quest
stays.

| Source | SP |
| --- | --- |
| Levels 1–70: 2 SP per level, level 1 included | 140 |
| Every tenth level (10, 20 … 70): 3 SP more | 21 |
| **Levels total** | **161** |
| 33 quests (below) | 51 |
| **Total = tree cost** | **212** |

At the cap with every quest done, the tree is exactly complete and can never be over-funded (P1-1).

### Levelling (P2, owner numbers)

- **Cap: level 70.** A level-up pays 2 SP, a tenth level 5 SP. A new profile starts at level 1 with
  its 2 SP already paid.
- **Curve:** linear-growing, `400 + 60 × (level − 1)` XP per level-up (P2-1's shape, retuned for the
  lower cap). The 69 level-ups cost **168,360 XP** in total — about **68 full runs, ~70 hours**, the
  same pace the recommendation aimed at.
- **XP sources:** kill 2, wave cleared 18, stage cleared 140, and now **boss kill 300 × stage**.

> **Still unmeasured:** the run estimates assume about 2,500 XP per complete five-stage run. No full
> run has been counted yet (Open Topics 2.9).

### Bosses (P3-2)

Bosses pay **no SP** any more. Each boss's first kill completes its quest (1 SP); every kill pays an
XP lump of 300 × stage, and Alloy exactly as before.

### Quests (P4, owner list; P6-1; P8-1)

| Group | Quest | Count | SP |
| --- | --- | --- | --- |
| Weapon kills | Defeat **1,000** enemies with {weapon} — any enemy type | 9 | 9 |
| Arsenal | Clear the game carrying six {weapon} (all six slots at the final win) | 9 | 9 |
| Weapon mastery | Every Alloy upgrade for {weapon} at its maximum | 9 | 27 |
| Collection | Collect every card | 1 | 1 |
| Bosses | Defeat the Stage N boss (first time) | 5 | 5 |
| | | **33** | **51** |

- **Kill credit:** a kill counts for the weapon whose shot, burn or freeze finished the enemy. Towers,
  the Core and adds that die with their boss count for nothing. `DamageContext` gained a `SourceId`
  for this.
- **Mastery** counts only the tracks the Upgrades tab offers that weapon (no Accuracy on a weapon
  without spread), using one shared rule in `UpgradeRules.IsTrackApplicable`.
- **Collection** reads the same "ever collected" record as the Records card grid.
- Quest progress is stored **per character** (P6-1). Levels are still per profile until a second
  character exists.
- **Shown** on a new **Quests** tab on the Progression screen, with progress bars. A completion
  toasts in-run and never pauses (P8-1).

### Paragon and Ascension (P7-3)

- Past level 70, XP fills **Paragon levels** at a flat **2,500 XP** each (about one full run). Each
  pays **1 Ascension Point (AP)**. The 4,480 XP of the last level-up would have halved the pace, so
  the flat cost is set per full run instead.
- **Ascension unlocks at the cap**, no longer at tree completion.
- **Ascension ring:** each branch's graph gains two gold nodes past its last key node. They are
  repeatable bonuses bought with AP: 1 AP per rank, one more per ten ranks. The pool grew from six to
  eight bonuses, so every branch has two: Industry gained "+2 Scrap at the start of every stage",
  Command "−1% tower and shop prices".
- **Keystone:** every tenth Paragon level grants a free choice of three from the pool, applied
  **×5**. This is the old Ascension offer, kept as the milestone reward.
- The main menu medallion and the profile rows now show **Level N** or **Paragon N**.

### Existing profiles

- Old XP is re-read against the new curve, and **a level is never taken away**. The old curve was
  cheaper for the first levels, so most profiles keep their level. Level SP are topped up to the new
  entitlement: Profile 2 went from 4 to 10.
- Bosses killed before the quests existed are marked **done, not paid again**.
- A weapon already fully upgraded, or a card collection already complete, is paid on first load.
  Profile 2's Rapid mastery paid 3 SP this way.
- Ascension ranks bought with SP under the old rules become ring ranks, and their SP are refunded.
- Profiles holding more SP than the budget (test grants) keep them.

### Also fixed on the way

`SkillModifierSnapshot` sized its array with the literal 29 while the enum had grown to 31, so the
two tree nodes granting **pickup magnet range** and **reward rerolls** have never done anything.
The array is now sized from the enum.

---

## Part 2 — Alloy quests (proposal for review, 2026-09-29)

P5-2 decided that quests beyond the SP budget pay Alloy or AP, never SP. Pricing them uncovered a
problem the mastery quests make urgent, so that comes first.

### The problem: mastery is priced far beyond what runs pay

An upgrade rank costs `base × rank`, so a track costs `base × n(n+1)/2`:

| Sink | Alloy |
| --- | --- |
| One weapon, every track (with / without Accuracy) | 1,565 / 1,460 |
| **All nine weapons** (the 27 SP of mastery quests) | **~13,100–14,100** |
| One tower, every track | 1,002 |
| All four towers | 4,008 |
| Every unlock | 460 |
| **Everything** | **~18,000** |

Income today is bosses at 6 / 8 / 10 / 12 / 14 per stage, so **50 per full run**, plus 1 per elite
and 50 once for first kills. That is roughly **60 Alloy per full run**, and **~4,100 by the level
cap** at 68 runs.

So by the time a player reaches level 70, they can afford the unlocks and **about two and a half
weapon masteries**. All nine would take **~225 full runs**. The 27 mastery SP are effectively out of
reach, which means the tree cannot be completed anywhere near the cap. That breaks the promise of
Part 1.

> The income figure is an estimate. The only recorded runs are short autopilot deaths, which earned
> 0–2 Alloy each. Measure it together with the XP per run.

### A1 — How to close the gap

Target: all nine masteries and every unlock affordable by about the level cap, which is ~14,000 Alloy
by run 68 against ~4,100 earned.

1. **Alloy quests pay the whole gap:** about 10,000 Alloy across the quests below. Large lumps, and
   the quests become mandatory rather than a bonus.
2. **Cheaper weapon tracks plus modest quests:** weapon track base cost 5 → 3. A weapon then costs
   876–939 and all nine ~8,200. Alloy quests worth ~4,000 on top of ~4,100 from runs then cover the
   unlocks and eight of the nine masteries by the cap, and the ninth a few runs later. Tower tracks
   become the long tail.
3. **More Alloy from bosses (×2.5) plus modest quests.** It works, but it inflates every other Alloy
   price too, and first-run players would afford everything at once.

**Recommendation: 2.** It fixes the cause, which is the price of mastery, rather than paying around
it. It keeps Alloy quests an enjoyable extra rather than a chore, and it leaves towers as the thing a
long-term player still saves for. Tower tracks stay at base 6.

### A2 — The Alloy quest catalogue (one-time, ~4,000 Alloy)

| Group | Quest | Count | Alloy each | Alloy |
| --- | --- | --- | --- | --- |
| Towers | Upgrade {tower} to its highest level in one run | 4 | 150 | 600 |
| Towers | Defeat 300 enemies with {tower} | 4 | 100 | 400 |
| Enemies | Defeat 1,000 {enemy type} | 5 | 150 | 750 |
| Elites | Defeat 50 elites with the {affix} affix | 3 | 100 | 300 |
| Factory | Reach chain tier 4 in a run | 1 | 200 | 200 |
| Collection | Collect every Common / Rare / Epic / Legendary card | 4 | 100 / 150 / 200 / 300 | 750 |
| Runs | Clear Stage N without the Core taking damage | 5 | 100 | 500 |
| Runs | Clear Stage N using only one weapon type | 5 | 100 | 500 |
| | | **31** | | **4,000** |

The rarity collection quests do not overlap the SP quest "collect every card". They are the
milestones on the way to it.

### A3 — One-time or repeatable?

1. **One-time**, like the SP quests.
2. **Repeatable** (weekly-style rotations).
3. **One-time, plus a repeatable tail after the level cap** paying AP.

**Recommendation: 1.** A singleplayer offline game has no reason for rotations, and repeatables turn
quests into a checklist of chores. After the cap, Paragon already pays AP per run.

### A4 — Where Alloy quests are shown

1. **The same Quests tab**, as a second section below the SP quests, rewards shown in Alloy.
2. **A separate tab.**

**Recommendation: 1.** One place for "what can I work towards", one list to scroll.

### A5 — Past the level cap

1. **Alloy quests keep paying Alloy.**
2. **Past the cap, they pay AP instead.**

**Recommendation: 1.** Alloy still has sinks past the cap (tower tracks, any unmastered weapon), and
AP should stay tied to Paragon so its pace is predictable.

### What A2 needs in code

- **Tower kill credit:** the new `DamageContext.SourceId` carries the tower id for tower shots. It is
  currently null for them.
- **Enemy-type and elite-affix kill counters,** from the victim in the same death handler that
  already feeds the weapon quests.
- **Stage-level checks** at stage clear: Core damage taken this stage, and distinct weapon ids in the
  loadout.
- **Factory tier:** `FactoryService.HighestChainTier` at stage clear.
- **Tower upgrade level:** checked when a tower is upgraded.
- The ledger gets an Alloy reward type. The Quests tab gets a second section.

**Rough size:** half a day for the cost change and the counters, half a day for the catalogue and the
second Quests section.
