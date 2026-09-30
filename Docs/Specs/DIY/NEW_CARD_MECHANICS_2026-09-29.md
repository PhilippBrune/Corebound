# New card mechanics for the ten repurposed cards — concept for review, 2026-09-29

**Owner request:** *"Create new cards to fill up those free cards with e.g. poisoning effects or other
support effects which are not implemented / integrated for now."*

**Owner decisions (2026-09-29, night):** C1-1, C2-2, C3-1, C4-1 (with an art prompt), C5-1, C6-1.
**Status: implemented 2026-09-30.** See "As built" at the end for the numbers, the places the build
differs from the proposal, and what has and has not been seen working.

---

## Where the ten slots come from

The duplicate-card cleanup (2026-09-28) resolved ten duplicates by giving the weaker card of each a
mechanic the pool lacked. The only mechanics available were weapon modifiers, and those are the
**Set Bonus rule changes** from WeaponLoadout.md. So these ten cards now hand out, one card at a
time, what a Set Bonus was meant to be special for:

| Card file | Shown as | Rarity | Current effect (weapon modifier) |
| --- | --- | --- | --- |
| Card_OverchargedRounds | Piercing Rounds | Common | +1 pierce |
| Card_ScrapMagnet | Wide Magazine | Common | +3 magazine |
| Card_TwinBarrel | Focused Barrel | Rare | spread ×0.75 |
| Card_AssemblyLine | Rapid Reload | Rare | reload ×0.75 |
| Card_RelayOverclock | Chilling Rounds | Rare | slow on hit |
| Card_FortifiedRelay | Marked Volley | Rare | every 4th volley crits |
| Card_StatusArchive | Impact Charges | Epic | every hit bursts |
| Card_BulkSalvage | Volatile Shells | Epic | misses explode |
| Card_SiegeBattery | Sustained Fire | Epic | +12% damage per second of held fire |
| Card_ReactorBleed | Nova Discharge | Legendary | volley fires as a full ring |

Their **tags** were still the old cards' (Wide Magazine showed the Resource icon, Chilling Rounds the
Tower icon). That was corrected on 2026-09-29 to match the current effects, since tags only drive the
icons.

The game has two status effects today, **Burn** and **Freeze**, and no support effects beyond flat
stat multipliers. Everything below is new.

---

## The proposal: ten new cards

| Slot | New card | Rarity | Mechanic | Effect (starting numbers) |
| --- | --- | --- | --- | --- |
| Piercing Rounds | **Toxic Rounds** | Common | Poison | 15% chance per hit to apply 1 Poison stack |
| Wide Magazine | **Field Dressing** | Common | Support: heal | Heal 8% of max health at each wave clear |
| Focused Barrel | **Arc Coil** | Rare | Shock | 20% chance a hit arcs to 2 enemies within 2.5 units for 50% damage |
| Rapid Reload | **Target Painter** | Rare | Vulnerable | Hits mark the target for 3 s; marked enemies take +15% damage from every source, towers included |
| Chilling Rounds | **Reactive Plating** | Rare | Support: shield | A shield blocks one hit, recharging after 12 s |
| Marked Volley | **Overclock Beacon** | Rare | Support: towers | Towers within 3 units of you fire 20% faster |
| Impact Charges | **Virulent Strain** | Epic | Poison | A poisoned enemy that dies passes its stacks to enemies within 2 units |
| Volatile Shells | **Leech Rounds** | Epic | Support: heal | Heal 0.5 health per kill, up to 5 per wave |
| Sustained Fire | **Core Aegis** | Epic | Support: Core | The Core regenerates 1% of max health per second during an Intermission and takes 10% less damage |
| Nova Discharge | **Sentry Drone** | Legendary | Summon | A drone orbits you and fires at the nearest enemy for 35% of your primary weapon's damage |

Poison gets a Common opener and an Epic payoff, so a Poison build has a path. Four support cards
cover the three things a player defends (self, Core, towers), which the pool has had nothing for
except flat multipliers.

---

## Decisions

### C1 — Replace the ten, or add on top?

1. **Replace all ten** with the new cards. The Set Bonus rules go back to being Set Bonus-only.
2. **Replace some**, keeping the plain stat ones (Wide Magazine, Rapid Reload) as they are.
3. **Add the ten new cards on top**, keeping the current ten. The pool grows to 52.

**Recommendation: 1.** The current ten dilute Set Bonuses: a player who can buy "full ring" as a
Legendary card has less reason to build six Novas for it. The request was to *fill* these slots, and
replacing keeps the pool at 42 cards, which the Records grid and the "collect every card" quest are
sized around.

### C2 — How Poison works

1. **Stacking damage over time.** Each stack deals a small amount per second for 4 s, and
   re-applying refreshes the duration. Up to 8 stacks.
2. **Like Burn:** one non-stacking tick that re-applying refreshes.
3. **Percentage of max health** per second, so it scales against bosses.

**Recommendation: 1.** Stacking is what makes Poison feel different from Burn. It rewards fast-firing
weapons (Rapid, SMG), which fire-and-forget Burn does not. Option 3 makes bosses the best Poison
target, which inverts the "boss is a build check" rule.

### C3 — Which support effects

1. **The four above:** heal on wave clear, shield, tower aura, Core aura.
2. **Heal and shield only**, the player-focused pair.
3. **The four, plus a pickup magnet and a Scrap interest card.**

**Recommendation: 1.** One card each for the player, the Core and the towers is the tower-defence
side of the game finally getting cards of its own. Pickup magnet already exists as a skill-tree node,
fixed on 2026-09-29.

### C4 — Tags and icons

The tag icon sheet is a 4 × 2 grid with no Poison, Shock or Support icon.

1. **Extend the sheet to 4 × 3** with Poison, Shock, Support and Summon. New art prompt, with a
   status line, in GRAPHICS_ASSET_PROMPTS.
2. **Reuse existing icons** (StatusEffect for Poison and Shock, Tower or Resource for support).

**Recommendation: 1.** The icons are how a player reads a card before its text, and a Poison card
under the generic StatusEffect icon would look like a Burn or Freeze card.

### C5 — Showing Poison and marks on enemies

1. **A tint plus a small stack number** over a poisoned enemy, and an outline on a marked one.
2. **Tint only.**
3. **Nothing extra**, the damage numbers carry it.

**Recommendation: 1.** Without it Target Painter and Virulent Strain are invisible, which is the
"cannot see a system working" problem the vision (5.5) warns about. The stack number only appears
from two stacks up, to keep the screen quiet.

### C6 — Order

1. **Poison first** (Toxic Rounds, Virulent Strain), then support, then Shock and Mark, then the
   drone.
2. **All at once.**

**Recommendation: 1.** Poison is what was asked for, and it establishes the new status path the
others reuse. The drone is the largest piece and the easiest to judge last.

---

## What the code needs

- `StatusEffectType.Poison` and `DamageKind.Poison`, appended, with stacking in `CombatHitReceiver`'s
  status loop. Kill credit already follows the last player hit, so Poison kills count towards the
  weapon quests.
- Appended weapon modifier targets: `PoisonChance`, `PoisonSpreadOnDeath`, `ShockChance`,
  `VulnerableOnHit`. A vulnerable multiplier is read wherever damage is taken, for towers too.
- Appended card effect targets: `HealOnWaveClear`, `HealOnKill`, `ShieldRechargeSeconds`,
  `TowerAuraFireRate`, `CoreIntermissionRegen`, `CoreDamageTaken`.
- `CardEffectDescriber` must describe every new target. **A target it does not know prints nothing,
  which is exactly how the "missing description" bug looked.**
- A drone runtime reusing the tower firing code, parented to the player.
- Four icons and the enemy status overlay (C4, C5).

## Rough size

A day for Poison with its two cards and the overlay. A day for the four support cards. Half a day for
Shock and Mark. A day for the drone. Plus the icon art.

---

## As built (2026-09-30)

All ten cards were replaced in place (C1-1). The card files and `_cardId`s are unchanged
(`Card_OverchargedRounds` is now Toxic Rounds), so collection progress, the Records grid and the
"collect every card" quest keep counting the same 42 ids. The Set Bonus rule changes are Set
Bonus-only again.

| Card | Rarity | Tags | Built as |
| --- | --- | --- | --- |
Every card can be taken more than once (owner decision 2026-09-21, `CardData.Stackable` is always
true), so the last column says what a further copy does.

| Card | Rarity | Tags | Built as | Another copy |
| --- | --- | --- | --- | --- |
| Toxic Rounds | Common | Poison | 15% per projectile to poison. | +15% chance |
| Field Dressing | Common | Support | Heal 8% of max health at each wave clear. | +8% |
| Arc Coil | Rare | Shock | 20% per projectile: arcs to 2 enemies within 2.5 units for 50% of the hit. A chained hit never chains again. | +20% chance |
| Target Painter | Rare | Status Effect, Tower | Every hit marks for 3 s; a marked enemy takes +15% from every source - towers and damage over time included. | +15% on the mark |
| Reactive Plating | Rare | Support | Blocks one hit, recharges in 12 s. A block grants the normal hit invulnerability so the rest of a volley cannot land straight through. | recharge x0.8 |
| Overclock Beacon | Rare | Support, Tower | Towers within 3 units of the player fire 20% faster. | +20% |
| Virulent Strain | Epic | Poison | A poisoned enemy that dies poisons every enemy within 2 units, **plus 10% poison chance** (see below). | +10% chance; the radius stays 2 |
| Leech Rounds | Epic | Support | +0.5 health per player kill, at most 5 healing kills per wave. | +0.5 per kill |
| Core Aegis | Epic | Support | Core regenerates 1% of max health per second during an Intermission and takes 10% less damage. | +1%/s, x0.9 damage |
| Sentry Drone | Legendary | Summon | One drone orbits at 1.1 units and fires every 0.5 s at the nearest enemy within 7 units for 35% of the primary weapon's damage (tier, upgrades, skills and card damage included; no crit, no status, no on-hit roll). Its shots count against the projectile budget. | another drone, up to 4 |

**Where the build differs from the proposal, and why**

- **Poison does not stack** (C2-2): one effect like Burn, refreshed on re-application, 0.35 damage
  every 0.5 s for 4 s. Virulent Strain therefore passes *Poison* on rather than "its stacks".
- **No stack number over poisoned enemies** (C5-1): with C2-2 there is only ever one stack, so the
  number would always read 1. A poisoned enemy is tinted green and a marked one wears an orange
  ring; the tint survives hit flashes because the flash now returns to a "rest colour".
- **Virulent Strain carries its own 10% poison chance.** Without Toxic Rounds it would otherwise be
  an Epic that does nothing.
- **"Up to 5 per wave"** for Leech Rounds is read as five healing kills (2.5 health), not five
  health: the player has 8 health, and five health per wave would out-heal every other source.
- **C6-1 (order)** was about sequencing the work. Everything was built in one pass during the
  overnight session; Poison was built and tested first.
- The hit effects are rolled **per projectile** when the shot is fired, which is the closest thing
  to "per hit" known at that moment; a Spread volley rolls each pellet separately.
- Chains, spreads and the drone only ever pick enemies and bosses, never the Core or a tower.
- **Target Painter stacks on the mark.** A mark is on or off, so a second copy first did nothing;
  each copy now adds its 15% (found 2026-09-30 when the autopilot was offered repeats).

**New code**: `CombatHitEffects` (the flags and `RunCombatEffects`, the run-wide values combat code
reads), `PlayerSupportRules` (the fixed numbers the card text and the code share),
`PlayerSupportEffects` (heals, shield, drone; attached by `PlayerCombatController`),
`CombatShockArcRuntime` (the bolt). `CardEffectDescriber` has a sentence for every new target - an
unknown target printing nothing was the original "missing description" bug.

**New assets**: `StatusEffect_Poison` (added to every enemy, every boss and the scene dummy),
eight `CardEffect_*` and four `WeaponModifier_*` assets. The ten weapon-modifier assets the old
cards used (`WeaponModifier_OverchargedRounds`, `_ScrapMagnet`, `_TwinBarrel`, `_AssemblyLine`,
`_RelayOverclock`, `_FortifiedRelay`, `_StatusArchive`, `_BulkSalvage`, `_SiegeBattery`,
`_ReactorBleed`) and nine `CardEffect_*` assets of the same names are now referenced by nothing.
They are left in place until the owner says they can go.

**Art**: GRAPHICS_ASSET_PROMPTS Asset 108 (third row of the tag icon sheet) and Asset 109 (the
drone). Both are looked for by code and fall back to a placeholder.

**Verified 2026-09-30** with the new autopilot variant `cards1` (all ten cards applied at run start,
Profile 2): every card applied and printed its description; over a Stage 1 run Poison was applied
38 times, Shock and the drone dealt damage every wave, and the shield blocked 5 hits. The boss
screenshot shows the shield ring and the drone.

**Not verified**: the two heals (the autopilot stayed at full health until the boss, where both
correctly have nothing to do), the tower aura and the Core regeneration (not measured), the mark
ring and the poison tint (not caught in a screenshot), and the tag-icon fallback on a real reward
screen.
