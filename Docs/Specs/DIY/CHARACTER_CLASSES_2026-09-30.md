# Character classes — concept for review, 2026-09-30

**Owner request:** *"Create a concept how different classes could look like in terms of what makes
them different to each other and what would be the fun playing them and how they can be
unlocked."*

**Decided 2026-09-30:** K1-3, K2-1, K3-2, K4-1, K5-1, K6-2, plus class-specific cards. **Built the same day** - see *As built* at the end; the concept text above it is kept as written.

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

---

## As built (2026-09-30)

Owner decisions: **K1-3** (all five), **K2-1** (strong signatures), **K3-2** (account level shared,
only the tree per class), **K4-1** (free, condition-based), **K5-1** (per-class tag leaning),
**K6-2** (a sprite per class), and "think as well about class specific cards".

### Data

- `CharacterData` (`Assets/Scripts/MetaProgression/Characters/`) - one asset per class in
  `Assets/ScriptableObjects/MetaProgression/Characters/`, listed on
  `MetaProgressionDefinition_Default._characters`. Signature and unlock are enums (append-only).
- Every class has its own `SkillTree_<Class>.asset`, listed on `_skillTrees`.
- `CharacterUnlockRules` reads the unlock from quests the ledger already tracks, account-wide.

### K3-2 in the save

Level, XP, Paragon and **Skill Points earned** stay account-wide. **Skill Points spent** and node
ranks are per class (`_characterSkillPointsSpent`, `SkillNodeRank.CharacterId`), so every class can
spend the whole account's SP in its own tree. The old `_skillPointsSpent` migrated to `vanguard`.
Quests stay account-wide under `QuestLedger.QuestOwnerId` (`vanguard`), so any class's run can finish
any quest and unlock any class. Alloy upgrades stay account-wide, as the concept said.

Since 2026-10-07 the **skill-tree version and the free respec** are per class too
(`_characterTreeVersions`, one `{characterId, version, freeRespecUsedVersion}` entry each), so a
content update that bumps one class's tree hands only that class a free respec. The old single
`_skillTreeVersion` / `_freeRespecUsedTreeVersion` migrate to the `vanguard` entry on load.

### Signatures (numbers in `CharacterEffects`)

| Class | Built | Stats and kit |
| --- | --- | --- |
| Vanguard | From two copies on, every archetype counts one copy more for its Set Bonus (3 Rapid read as Focused); +3% weapon damage per active Set Bonus tier. | as before |
| Engineer | Towers within 3 units fire 30% faster (adds to Overclock Beacon). Hold Interact within 1.8 of a tower in combat for 2 s: that tower deals double damage for 5 s and glows gold, once per tower per wave. The channel claims the Interact hold, so the Core guard does not also rise. | weapon damage x0.8; a Sentinel built free in the empty slot nearest the Core |
| Salvager | Kills drop x1.5 Scrap; +4 pickup magnet range. At each wave start, Scrap above 150 becomes +1% weapon damage per 10 for that wave (notification "Hoard: +N%"). Factory chains resolve at tier 1 at least. | +60 starting Scrap |
| Custodian | Within 5 units of the Core: a visible beam, the Core takes 30% less damage (cap 60%), shots pierce +1. Leaving the radius sends a shockwave from the Core (radius 4, knockback, 2 damage, 8 s cooldown). | health x1.25, speed x0.9, first Core repair free |
| Catalyst | Statuses it applies to enemies last x1.5. An enemy dying with two or more different statuses passes all of them to enemies within 2 units. | 10% poison chance on every hit from the start; weapon damage x0.85 |

"Signature power" is the one knob every class shares: tree nodes and class cards raise it, and each
class scales its headline number by it (the Vanguard's per-tier damage, the Engineer's aura, the
Salvager's hoard rate, the Custodian's Core reduction and shockwave, the Catalyst's duration).

### Trees

Each class tree is the Vanguard's tree with one **class keystone inserted second** in its heavy
branch (later nodes in that branch need 4 more branch SP): Engineer *Conductor* (Construction),
Salvager *Hoarder's Instinct* (Industry), Custodian *Anchor Point* (Command), Catalyst *Virulence*
(Combat). Each keystone raises signature power by 25% and carries four sub-nodes (the class's
secondary number, two supporting stats, +10% signature power per rank). The Vanguard's own tree got
*Full Kit* appended at the end of Combat (largest stack counts one more copy) with *Drill* and
*Sidearm*; appended, not inserted, so no existing save's ranks move and no respec is forced.

This is a first pass: the other three branches of each class tree are still the Vanguard's. The
concept's larger ideas (a fifth tower slot, reroll interest, "Shock arcs to poisoned enemies first")
are not built.

### Class cards

Ten cards, two per class, each with `_requiredCharacterId`. `CardService.CanOfferCard` never offers
them to another class, and the collection quests do not count them (one class alone could never
finish them). Records shows them in their own row after the 42 collection cards, and the tab count
stays the collection's; a selected class card names its class and the class-card count
("Class card - Engineer · Rare · class cards 3 / 10").

| Class | Rare | Epic |
| --- | --- | --- |
| Vanguard | Full Kit - largest stack counts +1 copy | Drill Sergeant - signature +50% |
| Engineer | Double Shift - +1 overcharge per tower per wave | Resonant Coils - signature +50% |
| Salvager | Deep Pockets - hoard from 100 Scrap | Compound Interest - signature +50% |
| Custodian | Long Leash - tether +1.5 | Bulwark Protocol - signature +50% |
| Catalyst | Airborne Strain - contagion +1.5 | Catalytic Bloom - signature +50% |

### K5-1 card leaning

`CardService.ResolveOfferWeight` multiplies a card's weight by 1.6 when it carries one of the class's
favoured tags, and the class sets the build-synergy knob (Open Topics 5, option b; 0.35 for every
class for now). Favoured: Vanguard Weapon + Critical, Engineer Tower + Support, Salvager Resource,
Custodian Support + Status Effect, Catalyst Poison, Shock, Burn, Freeze and Status Effect.

### Select and unlock

The New Run panel shows the class above the weapon list: **Q / Tab or LB / RB** cycles it, Up / Down
still picks the weapon. A locked class is shown with its unlock condition and cannot be started. An
unlocked class becomes the profile's selection as soon as it is shown, so the Progression screen then
opens on its tree; the header reads "Skill Points (Engineer)" and the tree's portrait shows the class.
The Main Menu status line on New Run announces a newly unlocked class until it has been looked at.

Unlocks: Engineer - any tower's mastery quest; Salvager - Factory quest progress at tier 3;
Custodian - any "Core untouched" stage quest; Catalyst - the Stage 3 boss defeated.

### Verified in play (autopilot `class <id>`, Profile 2, 2026-09-30)

- **Engineer:** kit tower present at wave 1, amber tint, aura 0.30; 12 overcharges landed over a
  Stage 1-3 god run (2.0-4.5 s each including the walk); towers dealt 43% of all damage.
- **Custodian:** health 10 (8 x 1.25), tether beam drawn to the Core, a shockwave fired on leaving.
- **Salvager:** 70 starting Scrap (10 + 60), +3 per kill instead of 2; handed 200 Scrap the hoard
  paid +11% then +13% and reset after each wave.
- **Catalyst:** statuses x1.5, poison without any poison card (11.6 damage by wave 3), 5 contagion
  spreads in three waves.
- **Vanguard:** 3 Rapid + 2 Spread resolved as Focused + Aligned (2 active tiers).
- New Run class line, the Progression header per class and the per-class tree (Engineer and
  Custodian keystones visible) photographed.

Verified 2026-10-07 (`class salvager`, German): magnet range 5.8 in the CLASS line; the chain floor
(tier 1 at the first Reward with no tower built yet - only the floor gives that); the class name
"Verwerter" on the New Run panel.

Not verified: the class cards being offered (they are Rare/Epic and did not come up), the unlock
announcement on a profile that actually meets a condition, and any balance - the numbers are the
concept's, untuned.

### Sprites (K6-2, delivered 2026-10-07)

Assets 110-113 (Engineer, Salvager, Custodian, Catalyst) and a redrawn Asset 14 (Vanguard) live in
`Assets/Art/Characters/Player/` and the StreamingAssets mirror. `CharacterData._spritePath` names
each class's sprite (empty for the Vanguard, which uses the base sprite);
`PlayerController.ApplyPlayerSprite` loads it through `DebugSpriteFactory.LoadProjectSpriteTrimmed`,
which crops to the figure's opaque bounds, so all five stand at the same on-screen size (0.5 units)
whatever the canvas around them. The hitbox no longer follows the sprite: it is the collider radius,
the same for every class. Textures are mipmapped (trilinear), which removed the grain at arena zoom.
The class tint stays as a fallback for a missing file.
