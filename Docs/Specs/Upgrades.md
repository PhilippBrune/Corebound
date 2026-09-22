# Upgrades and Unlocks Specification

Status: **Implemented 2026-09-22** (Slice 16). Written 2026-09-21 from the owner decision that a third currency should fund both unlocks and permanent weapon/tower stat upgrades. The "Implementation" section at the end records what shipped and where it differs.

Depends On: MetaProgression, Weapons, WeaponLoadout, Towers, Economy, SaveSystem, UI

---

## Purpose

The project now has three currencies with three jobs. This spec defines the third and the two things it buys.

| Currency | Scope | Earned by | Spent on | Spec |
| --- | --- | --- | --- | --- |
| **Scrap** | one run | enemies, waves, Factory | towers, shop offers, weapons | `Economy.md` |
| **Skill Points** | permanent | run outcomes | the skill tree | `SkillTree.md` |
| **Alloy** | permanent | bosses and elites | unlocks, and weapon/tower stat upgrades | this document |

Skill Points shape *the character*. Alloy improves *the equipment*. Keeping them separate is what stops one pool being obviously better spent on the other.

---

## Design Goals

- One currency, two sinks, and the player must always understand which sink they are feeding.
- Upgrades must be visible in numbers the player already sees on the weapon, not hidden multipliers.
- Unlocking must always widen the pool, never raise a baseline, so vision Section 10 holds.
- Upgrading is allowed to raise a baseline, but only within a hard per-stat ceiling.
- Everything is per weapon and per tower, so investment is a commitment and not a global slider.

---

## Currency: Alloy

| Property | Value |
| --- | --- |
| Starting balance | 0 |
| Earned by | **boss kills and elite kills** (owner decision 2026-09-22); nothing for finishing or failing a run |
| Spent on | unlocks (weapons, towers, starting loadouts) and stat upgrades |
| Refundable | no |
| Persisted in | `MetaProgressionSaveData.AlloyBalance` |

### Earning rates

| Event | Alloy |
| --- | --- |
| Defeat the Stage 1 / 2 / 3 / 4 / 5 boss | 6 / 8 / 10 / 12 / 14 |
| First-time kill of a boss (once per boss id) | +10 |
| Kill an elite enemy | 1 |
| Complete or fail a run | 0 |

A full run pays 50 Alloy plus its elites (the first full run about 100), inside the 60-90 target once a typical run's 10-20 elites are counted. Values live on `MetaProgressionDefinitionData`.

### Relationship to the existing meta currency

`MetaProgressionService` already grants a run-completion currency that gates unlocks through `IsUnlockSatisfied`. **Alloy is that currency, named and extended** — this is not a fourth pool. The existing field is renamed, the existing unlock gating is kept exactly as it is, and stat upgrades are added as a second sink alongside it.

---

## Sink 1: Unlocks

Unchanged in principle from the current `MetaProgressionDefinitionData` design, now priced in Alloy.

- Unlocks add weapons, towers and starting loadouts to the pool that runs draw from.
- **Hard rule: an unlock may never raise a base stat.** It only ever makes a new *thing* available.
- Unlock costs follow the 1× / 2× / 4× curve already proposed in the vision's §20.1: 20 / 40 / 80 Alloy per tier.
- Weapon slot 5 and slot 6 (see `WeaponLoadout.md`) are unlocks at 60 and 120 Alloy.

---

## Sink 2: Stat Upgrades

Permanent, per-item, purchased between runs.

### Weapon stats

Each stat upgrades independently, per `WeaponId`.

| Stat | Affects | Per rank | Max rank | Ceiling |
| --- | --- | --- | --- | --- |
| Damage | `DamageProfileData.BaseDamage` | +6% | 10 | +60% |
| Fire Speed | `WeaponAttackPatternData.FireIntervalSeconds` | −4% | 10 | −40% |
| Magazine | shots before reload | +1 | 8 | +8 |
| Reload Speed | reload duration | −5% | 8 | −40% |
| Accuracy | `SpreadAngleDegrees` | −5% | 6 | −30% |
| Critical Chance | `WeaponModifierTarget.CriticalChance` | +1.5pp | 10 | +15pp |
| Critical Damage | `WeaponModifierTarget.CriticalDamageMultiplier` | +8% | 10 | +80% |

**Magazine and Reload Speed use a real reload loop** (owner decision 2026-09-22, option a). Every weapon copy has a magazine: a volley spends one round, an empty magazine reloads for the pattern's `_reloadSeconds`, and releasing the trigger tops a partial magazine up after 0.9 s (`PlayerCombatData._idleReloadDelaySeconds`), so fighting in bursts is rarely punished and holding forever is. Base values: Rapid 24 rounds / 1.2 s, Spread 8 / 1.4 s, Beam 12 / 1.3 s - two and a half to four seconds of held fire per magazine. The HUD shows each copy's ammo and reload.

### Tower stats

Each stat upgrades independently, per `TowerId`.

| Stat | Affects | Per rank | Max rank | Ceiling |
| --- | --- | --- | --- | --- |
| Damage | `TowerLevelData` damage | +6% | 10 | +60% |
| Fire Speed | tower fire interval | −4% | 10 | −40% |
| Range | tower range | +4% | 8 | +32% |
| Build Discount | Scrap build cost | −3% | 6 | −18% |

### Cost curve

Rank *n* of any stat costs `BaseCost * n` Alloy, where `BaseCost` is 5 for weapons and 6 for towers. Rank 1 costs 5, rank 2 costs 10, rank 10 costs 50 — a fully upgraded weapon stat is 275 Alloy, and all seven stats on one weapon is roughly 1,900.

That is deliberately a long-term goal. A player should fully upgrade one favourite weapon long before they broadly upgrade several.

### Rules

- Upgrades apply at run start and are frozen for the run, exactly like skill effects.
- Upgrades apply to a weapon **per copy**: six copies of a fully upgraded Rapid all benefit. This is what makes upgrading interact with `WeaponLoadout.md` rather than competing with it.
- Upgrades stack additively with Set Bonuses and multiplicatively with card modifiers, in that order: base → upgrades → set bonuses → cards.
- No stat may exceed its ceiling by any combination of upgrades. Cards and set bonuses are **not** bound by the ceiling; only permanent upgrades are.
- Upgrading a locked weapon is not possible. Unlock first.

---

## Data Model

### `UpgradeTrackData` (ScriptableObject)
| Field | Type | Notes |
| --- | --- | --- |
| `_trackId` | string | e.g. `weapon_damage` |
| `_displayName` / `_displayNameDe` | string | |
| `_stat` | `UpgradableStat` | enum, **append-only** |
| `_valuePerRank` | float | |
| `_maxRank` | int | |
| `_baseCost` | int | |
| `_appliesTo` | `UpgradeTargetKind` | Weapon or Tower |

### `UpgradableStat` (enum, append-only)
`Damage`, `FireSpeed`, `Magazine`, `ReloadSpeed`, `Accuracy`, `CriticalChance`, `CriticalDamage`, `Range`, `BuildDiscount`

### Save data
`MetaProgressionSaveData` gains:

| Field | Type |
| --- | --- |
| `AlloyEarned` | int |
| `AlloySpent` | int |
| `UpgradeRanks` | `List<UpgradeRank>` where `UpgradeRank { string ItemId; UpgradableStat Stat; int Rank; }` |

Balance is always `AlloyEarned - AlloySpent`, never stored, for the same reason as Skill Points.

### Runtime
- **`UpgradeService`** resolves an `UpgradeSnapshot` per item id at run start.
- `PlayerCombatController` and `TowerRuntimeController` read the snapshot, never the save data.
- The snapshot is a plain class with no Unity dependencies, so it is testable.

---

## UI

One route, two tabs, reached from the main menu.

**Unlocks tab.** The existing meta-progression list, priced in Alloy.

**Upgrades tab.**
- Left: the item list, weapons then towers, locked items greyed with their unlock cost.
- Right: the selected item's seven (or four) stat tracks, each as a pip row showing current rank, next cost, and the resulting value.
- Every track shows the **actual resulting number**, not the percentage. "Damage 12 → 12.7" is legible; "+6%" is not.
- The ceiling is drawn as the end of the pip row, so the player can see where a track stops before spending into it.
- Alloy balance is permanently in the header, alongside a one-line reminder of what Alloy is for.

---

## Audio

Reuses the Skill Tree cues, since both are between-run purchases in the same menu family. A purchase that completes a track's final rank gets the larger confirmation.

---

## Balancing

- A player's first full run should yield roughly 60-90 Alloy: one unlock, or the first two ranks of a stat. Enough to matter, not enough to transform.
- Fully upgrading one weapon across all stats should take on the order of 20-25 completed runs.
- Upgrade ceilings are set so that a fully upgraded weapon is roughly 1.6× a fresh one in raw output. Cards within a single run can already exceed that, which is the intended relationship: permanent progress must never outweigh in-run build decisions.
- Build Discount is capped low on purpose. Cheap towers compound with the Factory, and that is the fastest route to a broken economy.

---

## Open Questions

- ~~Magazine and Reload Speed need an ammo mechanic.~~ **Answered 2026-09-22: (a), a real reload loop** - see Weapon stats. This changes the feel of every weapon; the magazine sizes above are the first tuning and should be revisited after the owner has played it.
- ~~Should Alloy and Skill Points drop from the same events?~~ **Answered 2026-09-22: from bosses and elites.** Alloy comes from bosses and elites, Skill Points from bosses only (the SkillTree.md answer), so the two pools have different sources.
- ~~Should there be a respec for upgrades?~~ **Answered 2026-09-22: no, upgrades are permanent.** There is no refund path in `UpgradeRules` at all, and the Upgrades tab says so on every track.

---

## Acceptance Criteria

- Alloy is earned at the specified rates and persists between runs.
- The existing meta currency is Alloy, renamed, with existing unlock gating unchanged.
- Each stat track can be upgraded to its max rank and no further, with cost `BaseCost * rank`.
- A stat's resulting value never exceeds its ceiling from upgrades alone.
- Upgrades apply to every equipped copy of a weapon.
- The order base → upgrades → set bonuses → cards is observable in the resulting numbers.
- Upgrading an unowned or locked item is refused with a clear reason.
- The upgrade UI shows resulting values, not only percentages, and is fully controller-operable.
- Upgrade state survives a save/load cycle and cannot be observed changing mid-run.

---

## Implementation (2026-09-22)

### Code

| Piece | Where |
| --- | --- |
| Tracks | `UpgradeCatalog_Default.asset` - eleven `UpgradeTrackData` entries (seven weapon, four tower), inline |
| Rules | `UpgradeRules` - locked, max rank, cost `BaseCost x rank`, no refunds |
| Snapshot | `UpgradeSnapshot` - per item id and stat, clamped to max rank, frozen into `RunProgressionSnapshot` |
| Currency | `MetaProgressionSaveData` v2 `AlloyEarned` / `AlloySpent`; the v1 `_metaCurrencyAmount` migrates into `AlloyEarned` |
| Payout | `MetaRewardLedger` (boss and elite kills), written at the kill |
| Weapons | `PlayerCombatController`: damage, fire interval, magazine, reload, spread, crit chance and damage per `WeaponId`, per copy |
| Towers | `TowerRuntimeController` damage, fire interval, range per `TowerId`; `TowerService` applies Build Discount to build costs only |
| UI | the Progression route's Unlocks and Upgrades tabs |

### Deviations

- **Tracks are inline in one catalog asset**, not eleven `UpgradeTrackData` assets.
- **The Accuracy track is hidden for weapons with no spread** (Rapid, Beam): it would cost Alloy and change nothing.
- **Unlock prices** follow the 20 / 40 / 80 curve: Support Tower 20, Spread 20, Status Cards 40, Engineer loadout 80, weapon slot 5 60, slot 6 120. Weapon licences added 2026-09-22: Nova Pulse 35, Scatter Launcher 40, Railgun 45. They gate the weapon's card and shop offer only; unlike Spread's licence they carry no `_weaponReward`, so buying one never changes the starting weapon.
- **Paid respec** (2026-09-22) is a third Alloy sink: 30 Alloy for every Skill Tree respec after the free one per tree version (SkillTree.md).
- **Composition order** is base -> tier -> upgrades -> skill tree -> set bonuses and cards. Upgrades and set bonuses are multiplied rather than added; with the current values the difference is under 2% and it keeps one rule for every multiplier.
- **Existing profiles** keep what they had: 5 Core Shards became 5 Alloy on the owner's profile, and new profiles start at 0.

### Verification (autopilot, 2026-09-22)

- The meta save migrated v1 -> v2 on first load (5 Core Shards -> 5 Alloy, purchases kept).
- A failed run paid nothing (previously +1).
- `progression1`: Rapid Damage bought to rank 2 through the Upgrades tab (5 + 10 Alloy), shown as "2.2 -> 2.4" with the next price 15; the fifth weapon slot unlock bought (60) and present in the next run (2 slots).
- Not yet verified in a run: a tower track's effect in combat, and Build Discount.
