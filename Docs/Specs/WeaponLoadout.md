# Weapon Loadout Specification

Status: Requirements — not implemented. Written 2026-09-21 from the owner decision that the player should carry up to six weapons, Brotato-style, with synergy bonuses for carrying duplicates.

Depends On: Weapons, Combat, Cards, Economy, Shop, UI, Input, SaveSystem, MetaProgression

Extends: [Weapons.md](Weapons.md) — that spec still owns what a single weapon *is*. This one owns how many the player carries and what carrying several of a kind does.

---

## Purpose

`Weapons.md` was written around one active weapon (`PlayerCombatController._activeWeapon` is a single field). The owner's decision changes that: the player carries **up to six weapons at once**, all firing automatically, and carrying several of the same kind grants escalating synergy bonuses.

This turns weapon acquisition from a one-time run-start choice into a running build decision, and gives the shop something meaningful to sell every single visit.

---

## Design Goals

- Six weapons firing at once must stay readable, per vision Section 5.5. More weapons must not mean less legible combat.
- Specialising (six of one kind) and diversifying (six different kinds) must both be viable, with different strengths.
- The player must never have to micro-manage which weapon fires. Aiming is the skill; weapon management is the build.
- No weapon slot may be a dead slot. An empty slot is a *choice*, not an oversight.
- Controller-first: the whole loadout must be inspectable and rearrangeable without a mouse.

---

## Loadout

| Property | Value |
| --- | --- |
| Slots | 6 |
| Slots unlocked at run start | 1 |
| Additional slots | unlocked through the Skill Tree and meta progression, never bought with Scrap |
| Empty slots allowed | yes |
| Duplicates allowed | yes, that is the point |
| Swapping | during Intermission only |

### Why slots unlock rather than all being free

Starting with all six open means the first six shop visits are forced purchases with no decision in them. Starting with one and earning the rest makes each new slot a felt reward, and makes "do I fill the slot or upgrade what I have" a real question.

Slot unlock sources, in order a new player meets them:

| Slot | Source |
| --- | --- |
| 1 | starting loadout |
| 2 | first stage cleared in any run (permanent) |
| 3 | Skill Tree, Combat branch |
| 4 | Skill Tree, Combat branch |
| 5 | meta progression unlock (third currency) |
| 6 | meta progression unlock (third currency) |

---

## Firing

All equipped weapons fire **simultaneously and automatically**, each on its own cooldown, all at the player's single aim direction.

- There is no "selected" weapon and no weapon-switch input.
- Each weapon keeps its own `WeaponAttackPatternData` fire interval, spread and projectile count.
- Each weapon keeps its own `WeaponPresentationData` audio and colour identity.
- Hold-to-fire remains the input model from `Weapons.md`: one Primary Fire input drives every equipped weapon.

### Readability rules

These are requirements, not suggestions. Six weapons firing at once is the single biggest threat to combat legibility in the game.

1. **Audio de-duplication.** A weapon's fire cue is suppressed if an identical cue played within `MinFireCueIntervalSeconds`. Six copies of one weapon produce one cue per interval, not six.
2. **Staggered cooldowns.** When two weapons with the same fire interval are equipped, the second is offset by half an interval on equip, so duplicates spread their shots rather than firing in a single burst.
3. **Projectile budget.** A global cap on simultaneously live player projectiles. When the cap is reached, the weapon whose shot would exceed it skips that shot rather than despawning someone else's projectile. The cap is data, not a constant.
4. **Enemy projectiles are never hidden.** Player projectiles render on a sorting layer below enemy projectiles and telegraphs, so a wall of friendly fire can never obscure the thing that kills you.

---

## Synergy

Carrying multiple weapons of the same `WeaponId` grants a **Set Bonus** that scales with the count.

| Copies | Tier | Effect |
| --- | --- | --- |
| 2 | Paired | small, always numeric |
| 3 | Aligned | medium numeric, plus a small rule change |
| 4 | Focused | larger numeric |
| 5 | Synchronised | rule change specific to that weapon archetype |
| 6 | Perfected | the archetype's defining effect |

Set Bonuses are **per weapon archetype**, authored on the weapon itself, so a new weapon ships with its own identity rather than inheriting a generic curve.

### Example: Rapid

| Copies | Effect |
| --- | --- |
| 2 | +10% fire rate to all Rapid weapons |
| 3 | +10% fire rate, and Rapid projectiles pierce one extra enemy |
| 4 | +20% fire rate |
| 5 | every sixth Rapid shot is a guaranteed critical |
| 6 | Rapid weapons fire in a continuous stream; fire interval is halved |

### Example: Spread

| Copies | Effect |
| --- | --- |
| 2 | +1 projectile per Spread weapon |
| 3 | +1 projectile, and spread angle narrows by 15% |
| 4 | +2 projectiles |
| 5 | Spread projectiles that travel their full lifetime without hitting anything explode |
| 6 | Spread weapons fire a full 360-degree ring |

### Example: Beam

| Copies | Effect |
| --- | --- |
| 2 | +15% Beam damage |
| 3 | +15% damage, and Beam pierces one extra target |
| 4 | +30% damage |
| 5 | Beam applies a short slow on hit |
| 6 | Beam damage ramps the longer fire is held, resetting on release |

### Diversity bonus

Carrying **six different** weapon archetypes grants the **Arsenal** bonus: +12% damage to all weapons and +1 projectile budget headroom. One bonus, deliberately modest, so that diversity is a valid strategy without being the obvious default.

### Stacking rules

- Set Bonuses at different tiers do **not** stack; only the highest reached tier applies.
- Set Bonuses from different archetypes **do** stack (3 Rapid + 3 Spread grants both Aligned bonuses).
- The Arsenal bonus is mutually exclusive with every Set Bonus by definition, since it requires no duplicates.
- Set Bonuses compose with `WeaponModifierState` multiplicatively, and are applied **before** card modifiers.

---

## Acquisition

Weapons are acquired the same ways cards are, per the vision's already-settled answer:

- **Shop purchase** during Intermission, priced by rarity. This becomes the shop's primary weapon channel.
- **Post-wave reward**, occasionally offered as one of the three choices.
- **Boss drop**, guaranteed one weapon offer after each stage boss.

When all six slots are full and a new weapon is acquired, the player is asked which slot to replace, or may decline. Declining must always be possible.

### Selling

A weapon in a slot may be sold during Intermission for **50% of its purchase price**, rounded down. This exists so a player who over-committed to an archetype is not locked out of the run, and it is the only Scrap *income* outside combat.

---

## Data Model

### `WeaponSetBonusData` (new)
| Field | Type | Notes |
| --- | --- | --- |
| `_requiredCopies` | int | 2-6 |
| `_displayName` / `_displayNameDe` | string | |
| `_description` / `_descriptionDe` | string | shown in the loadout UI |
| `_modifiers` | `WeaponModifierData[]` | reuses the existing modifier assets |

### `WeaponData` additions
| Field | Type | Notes |
| --- | --- | --- |
| `_setBonuses` | `WeaponSetBonusData[]` | ordered by required copies |
| `_rarity` | `CardRarity` | reuses the existing rarity enum for shop pricing and offer weighting |
| `_basePurchaseCost` | int | |

### `WeaponLoadoutData` (new, run-scoped runtime state, not an asset)
Holds the six slots and computes the archetype counts.

### Save data
`RunSaveData.EquippedWeaponId` (single string) is **replaced** by `EquippedWeaponIds` (a six-entry list, empty strings for empty slots). This is a save format change and needs a version bump plus a migration that promotes the old single id into slot 0.

`MetaProgressionSaveData` gains `UnlockedWeaponSlotCount`.

---

## Runtime Design

- **`PlayerCombatController`** stops owning a single `_activeWeapon`. It owns a `WeaponLoadoutRuntime` and ticks each equipped weapon's cooldown independently.
- **`WeaponSetBonusResolver`** (plain class, testable) takes the six equipped `WeaponData` references and returns the active Set Bonuses plus the Arsenal flag. Pure function of the loadout; no Unity dependencies.
- **`WeaponModifierState`** gains the Set Bonus contributions as a distinct input, so the HUD can show "from your loadout" separately from "from your cards".
- `WeaponEvents.WeaponFired` gains the firing `WeaponData` so audio and VFX can stay per-weapon with six sources active.

---

## UI

### In combat
A compact loadout strip, six small slots, each showing the weapon icon and a cooldown sweep. No text during combat. This replaces the single-weapon HUD widget from `Weapons.md`.

Active Set Bonuses are shown as small tier pips beside the strip, not as text.

### Intermission
A full loadout panel:

- Six slots, navigable with the d-pad, each showing weapon name, archetype, and rarity.
- The active Set Bonus for each represented archetype, with its effect in plain words and the next tier's requirement ("3/4 toward Focused").
- Sell and rearrange actions.
- The projectile budget, shown as a used-of-total figure, so a player who is hitting the cap can see why their damage stopped scaling.

---

## Audio

- Each weapon keeps its own fire and critical cue from `WeaponPresentationData`.
- The de-duplication rule above is an audio requirement first: six identical weapons must sound like a heavier version of one weapon, never like six.
- Reaching a new Set Bonus tier during Intermission plays a distinct ascending cue, because it is a build milestone.

---

## Balancing

- Six copies of one weapon should land within ±15% of six different weapons in total damage. The difference should be in *shape*: specialised is spikier and more conditional, diverse is flatter and more reliable.
- No Set Bonus tier may more than double the archetype's output on its own.
- The projectile budget should be reachable by a 6-Spread build at Perfected, and not reachable by any 6-different build. That is what makes the cap a real specialisation cost rather than an invisible constant.
- Weapon prices should scale so that filling all six slots costs roughly as much as three tower upgrades, keeping the tower and weapon economies competing.

---

## Rules

- Every weapon fires automatically. There is no weapon-selection input, ever.
- Loadout changes happen during Intermission only.
- A weapon's identity stays in its own data. Set Bonuses may not be defined centrally in code.
- The projectile budget is a data field. It will need tuning and must never become a magic number.
- Player projectiles never render above enemy projectiles or telegraphs.
- Declining a weapon offer is always allowed.

---

## Open Questions

- Should an empty slot give a small bonus (e.g. +3% move speed per empty slot), so "travel light" is a deliberate strategy? Recommend no for the first pass: it adds a strategy that competes with the whole feature before the feature has been tuned.
- Do weapons carry their own upgrade levels, or is upgrading handled entirely by `Upgrades.md`'s third currency? Recommend the latter, so there is exactly one upgrade system.
- Should duplicate weapons of *different rarities* count toward the same Set Bonus? Recommend yes; counting by `WeaponId` alone keeps the rule explainable in one sentence.

---

## Acceptance Criteria

- The player can equip up to six weapons, and all equipped weapons fire simultaneously on a single Primary Fire input.
- Slots unlock through the specified sources and the unlocked count persists between runs.
- Equipping N copies of one archetype activates exactly the highest Set Bonus tier at or below N, and no lower tier.
- Set Bonuses from different archetypes apply together.
- Six distinct archetypes activate the Arsenal bonus and no Set Bonus.
- Six identical weapons produce at most one fire cue per `MinFireCueIntervalSeconds`, not six.
- Live player projectiles never exceed the configured budget.
- A run saved with six equipped weapons restores all six, and a save written under the old single-weapon format loads with that weapon in slot 0.
- The Intermission loadout panel is fully operable with a controller.
