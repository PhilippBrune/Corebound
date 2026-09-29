# New card mechanics for the ten repurposed cards — concept for review, 2026-09-29

**Owner request:** *"Create new cards to fill up those free cards with e.g. poisoning effects or other
support effects which are not implemented / integrated for now."*

Answer each decision with its code, e.g. `C1-1, C2-1 ...`. Nothing here is implemented.

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
