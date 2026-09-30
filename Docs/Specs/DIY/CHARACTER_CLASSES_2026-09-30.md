# Character classes — concept for review, 2026-09-30

**Owner request:** *"Create a concept how different classes could look like in terms of what makes
them different to each other and what would be the fun playing them and how they can be
unlocked."*

Answer each decision with its code, e.g. `K1-1, K2-2 ...`. Nothing here is implemented.

**Already decided (vision, 2026-09-21):** each playable character has its **own skill tree** and is
**levelled separately**, so a second character starts at zero. The save already carries a
`SelectedCharacterId` (today always `vanguard`), quest progress is stored per character, and the
character-select portrait frame (Asset 100) is delivered.

---

## What makes a class a class

A class must change **what the player does from second to second**, not just their numbers. Five
levers, in order of how much they change play:

| Lever | What it is | Example |
| --- | --- | --- |
| **1. Signature rule** | One passive that changes how an existing system works. Never a flat stat. | "Towers you stand next to fire 30% faster." |
| **2. Own skill tree** | Same four branches (Combat, Construction, Industry, Command), different node contents and keystones. Already decided. | The Engineer's Construction branch has twice the nodes. |
| **3. Starting kit** | Start weapon, slots, one pre-built tower or a Scrap head start. | The Salvager starts with 60 Scrap and no tower. |
| **4. Card leaning** | A per-class tag affinity on top of the global knob built on 2026-09-30. | Engineer offers lean to Tower and Support. |
| **5. Base stats** | Health and move speed, at most ±25%. | The Custodian has +25% health, −10% speed. |

Everything a class does must **expand an existing system** (Vision §19.8): towers, Factory, Core,
cards, statuses. No class adds a new resource, a new screen or free tower placement.

Alloy upgrades stay **account-wide**. They improve the *equipment* (Upgrades.md), and a weapon
upgraded on one class stays upgraded on another; only levels, SP, the tree and quests are per class.

---

## The five classes

### Vanguard — the gunner *(exists; the default)*

- **Role:** frontline weapon build. The class that teaches the game.
- **Signature — Loadout Discipline:** every Set Bonus tier needs one copy fewer — Aligned at 2,
  Focused at 3, Synchronised at 4, Perfected at 5 (Paired is skipped, Aligned replaces it). The
  sixth slot is then free for another weapon, so Vanguard is the only class that can run a
  Perfected archetype *and* a sidearm.
- **Kit:** start weapon of choice, 1 slot (as today).
- **Tree:** Combat-heavy; keystone ideas: "every sixth volley is a full ring", "reload refunds on
  kill".
- **The fun:** power fantasy through the loadout — six Rapids chewing through a wave, or six
  different guns firing at once. Easiest to read, most direct feedback.

### Engineer — the tower conductor

- **Role:** the towers do the killing; the Engineer runs between them and makes them better.
- **Signature — Overclock:** towers within 3 units of the Engineer fire 30% faster; holding
  **Interact** next to a tower during combat channels an overcharge (2 s) that doubles its damage
  for 5 s, once per tower per wave. The player stays active in combat (Vision §19.1) — they just
  fight *through* the towers.
- **Kit:** starts with one tower already built in the slot nearest the Core; weapon damage −20%.
- **Tree:** Construction-heavy (tower range, overcharge length, a fifth tower slot as a keystone).
- **Cards lean:** Tower, Support.
- **The fun:** a rhythm game of positioning — sprinting from the left flank to the right flank to
  overcharge the Mortar just as the Bruisers arrive. Every placement decision matters twice.

### Salvager — the greedy one

- **Role:** economy and Factory; out-builds everyone by wave 4 if they survive waves 1–3.
- **Signature — Strip for Parts:** kills drop 50% more Scrap, and pickups within 4 units fly to the
  Salvager; **Scrap above 150 at the start of a wave becomes temporary weapon damage** (+1% per
  10 Scrap, for that wave). Hoarding is a weapon, spending is an investment — the player chooses
  every intermission.
- **Kit:** 60 extra starting Scrap, no tower, Factory chains start at tier 1.
- **Tree:** Industry-heavy (interest on held Scrap, cheaper rerolls in the shop, chain tier caps).
- **Cards lean:** Resource.
- **The fun:** greed. Running into a crowd for a cache, deciding whether to cash in or ride the
  hoard into the boss. Big swings, big snowballs.

### Custodian — the anchor

- **Role:** holds the Core. The class for two-sided waves.
- **Signature — Tether:** a visible beam links the Custodian to the Core while within 5 units.
  While tethered the Core takes 30% less damage and the Custodian's shots pierce one more enemy;
  **breaking the tether** (leaving the radius) releases a shockwave at the Core that knocks enemies
  back. Stay and hold, or leave and punish — both are active choices.
- **Kit:** +25% health, −10% move speed, the Core starts with a free repair.
- **Tree:** Command-heavy (tether radius, Core regeneration, the shockwave as a keystone).
- **Cards lean:** Support, Status Effect.
- **The fun:** hold-the-line tension. Everything comes to you; your space is a circle and you
  decide when to step out of it.

### Catalyst — the plague doctor *(late unlock)*

- **Role:** damage over time and chains; needs the 2026-09-30 card mechanics to shine.
- **Signature — Contagion:** every status effect the Catalyst applies (Burn, Freeze, Poison,
  Shock) lasts 50% longer, and an enemy dying with **two or more** different statuses spreads all
  of them to enemies within 2 units.
- **Kit:** weapon hits have a 10% chance to Poison from the start; −15% direct weapon damage.
- **Tree:** Combat and Industry split (status duration, spread radius, "Shock arcs to poisoned
  enemies first").
- **Cards lean:** Poison, Shock, Burn, Freeze, Status Effect.
- **The fun:** watching a wave rot. The build-around class for players who have seen everything
  else — the payoff is emergent chains, not clicks.

---

## How classes are unlocked

Every unlock condition asks the player to **already have tried the thing the class specialises
in** — so the class arrives as the reward for curiosity, not for grind, and its first run feels
familiar.

| Class | Unlock condition | Existing hook |
| --- | --- | --- |
| Vanguard | start | — |
| Engineer | Upgrade any tower to its highest level in one run | Alloy quest "Tower mastery" (built 2026-09-29) |
| Salvager | Reach Factory chain tier 3 in one run | Factory quest counter (built) |
| Custodian | Clear any stage without the Core taking damage | Stage quest "untouched Core" (built) |
| Catalyst | Defeat the Stage 3 boss with any class | Boss first-kill quest (built) |

No currency is spent: SP and Alloy are already contested, and paying to *try* a class punishes
exactly the curiosity the unlock should reward. The Main Menu announces the unlock; the character
select (Asset 100 frame) shows locked classes with their condition.

---

## Decisions

### K1 — How many classes in the first wave?

1. **Vanguard + Engineer**, then the others one at a time after playtests.
2. **All four core classes** (not Catalyst) at once.
3. **All five.**

**Recommendation: 1.** Engineer is the most different from Vanguard and uses systems that are
already complete (towers, interact, tower quests). One well-tuned second class teaches more about
what a class needs than four rough ones (Vision §19.9).

### K2 — Signature strength

1. **Strong** (as written above): the signature defines the run.
2. **Mild:** half the numbers; the tree carries the identity.

**Recommendation: 1.** A mild signature makes a class a reskin until level 20, which is the thing
the per-class tree decision was meant to prevent.

### K3 — Per-class levels: what happens to the current profile?

1. **The current level, SP and quests become the Vanguard's.** New classes start at level 1.
2. **Account level stays shared**; only the tree is per class.

**Recommendation: 1.** It is what the vision already decided, and the save is already keyed by
character for quests.

### K4 — Unlock rule

1. **Condition-based and free**, as tabled.
2. **Condition plus an Alloy price.**
3. **Alloy price only.**

**Recommendation: 1.**

### K5 — Card leaning

1. **Per-class tag affinity** (a class-specific set of favoured tags feeding the offer weight).
2. **None;** classes only differ by rule, kit, tree and stats.

**Recommendation: 1.** It costs one field per class and makes the tag system built on 2026-09-30
carry class identity for free.

### K6 — Visuals

1. **A tinted variant of the current player sprite** plus a portrait per class.
2. **A new top-down sprite per class** (five art prompts).

**Recommendation: 2 for Engineer, 1 as the placeholder until then.** A class the player cannot tell
apart at a glance in a busy arena fails Vision §19.6 (readability).

---

## What the code needs

- `CharacterData` ScriptableObject: id, names and bio EN/DE, portrait, start weapon, starting kit,
  health and speed multipliers, signature rule (an enum, like `SkillEffectTarget`'s rules), tag
  affinity overrides, skill tree reference. A `CharacterCatalog` lists them.
- Per-character levels and SP in `MetaProgressionSaveData` (quests already are), with the current
  values migrated to `vanguard`.
- A character select in the Main Menu's New Run flow and the Progression screen (Asset 100).
- One runtime class per signature, reading the same event bus the cards use.
- Unlock checks on the existing quest ledger.

**Rough size:** a day for the data, the save split and the select screen; a day per class for
signature, tree and tuning; the art on top.
