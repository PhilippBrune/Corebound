# Session Report — 2026-09-22

Scope: the four UI proposals (`Docs/Specs/DIY/UI_PROPOSALS_2026-09-21.md`), the owner's answers to
the open questions in SkillTree.md, WeaponLoadout.md and Upgrades.md, and a full implementation of
all three systems. Plan: `Docs/Slices/16_PROGRESSION_AND_LOADOUT_SLICE.md`.

## Owner decisions folded in

| Question | Answer |
| --- | --- |
| Stage 1 failure pays SP? | 0. SP only for defeating bosses, more for deeper bosses. |
| Ascension prestige marker? | Yes - a rank badge in the main menu. |
| Empty-slot bonus? | No; possible future character passive. |
| Per-weapon upgrade levels? | Yes, each weapon upgraded separately. |
| Different rarities count toward one Set Bonus? | Yes. |
| Magazine / Reload | (a) a real reload loop. |
| Alloy / SP sources | Bosses and elites (SP: bosses only). |
| Respec for upgrades? | No, permanent. |

## What shipped

**UI proposals (all four).** Spawn gates per spawn point with an orange light pool on the gates the
next wave uses; card tags on 32 of 34 cards and icons in reward slots and a pause-menu grid; the
shop on the reward screen's exact slot geometry; a tower build browser from
`tower_info_card_frame.png`. Boss wind-ups draw the Ring / Lance / Volley telegraph art.

**Currencies.** The meta currency is Alloy, stored as earned/spent. Boss kills pay SP and Alloy on a
stage curve plus a first-kill bonus, elites pay Alloy, and nothing is paid for finishing or failing
a run. Meta save v2 with migration.

**Weapon loadout.** Six slots (one at start), all firing on one trigger with independent cooldowns
and magazines, staggered duplicates, a projectile budget, per-weapon Set Bonus ladders counted by
`WeaponId`, per-copy tiers upgraded for Scrap, weapon cards in the reward pool and shop, a guaranteed
weapon offer after every boss, an Intermission loadout panel (upgrade, sell, rearrange, resolve a
weapon that arrived with every slot full), a six-tile HUD strip, per-weapon fire-cue de-duplication,
and run save v2 migrating the old single weapon into slot 0.

**Reload loop.** Real magazines (Rapid 24, Spread 8, Beam 12) with reloads, and a top-up after
releasing the trigger.

**Skill tree and Upgrades.** The Vanguard tree (4 branches x 4 key nodes x 4 sub nodes, 212 SP),
Ascension with three rolled offers per rank, eleven permanent upgrade tracks, all read from a run
snapshot frozen at run start. The main menu's Meta Progression route became a tabbed
**Progression** screen (Unlocks, Upgrades, four branches, Ascension), and a rank badge appears in
the menu from Ascension 1.

## Verified (autopilot)

- Stage 3 run: three gates at the three spawn points, all lit before a 3+ enemy wave; build browser
  with all four towers, Support greyed; pause menu with the active card and its tag icon; shop on
  the reward geometry.
- Meta save migrated v1 -> v2 on the owner's profile (5 Core Shards -> 5 Alloy). A failed run paid
  nothing.
- `loadout1`: six slots firing, magazines draining and reloading, 30/60 projectiles live, Set Bonuses
  resolving and upgrading to Aligned, 77+ duplicate fire cues suppressed, a reward weapon going
  pending with a full loadout and being placed through the auto-opened panel, a tier upgrade.
- `progression1`: purchases on every tab; the next run had 2 slots, +10 starting Scrap as computed,
  and a free first tower (Forward Foundations).
- `ascension1`: whole tree bought through the rules (212 SP), an Ascension rank bought, badge shown;
  the next run had 3 slots, +116 Scrap and 2 starting relics.
- `god1` / `god3`: the Volley telegraph drawn along the firing line; the Stage 1 boss paid 3 SP (1 + 2
  first kill) and 16 Alloy, elites 1 each, `boss_stage1` recorded; the post-boss reward offered a
  weapon card as guaranteed; the first stage cleared set the permanent slot-2 flag.
- **The first boss run paid nothing**: WaveManager's `_metaProgressionDefinitionData` was never set
  in SampleScene.unity, so the ledger had no reward values - and neither did the old run-completion
  payout before today, which means no run has ever paid meta currency through that path. The scene
  reference is set now, and the run snapshot no longer caches an empty result when a caller without
  a definition happens to run first.
- The weapon cards' descriptions were blank in the reward screen; their unquoted YAML text contained
  `: `. Quoted.
- Bugs caught by these runs and fixed: the Progression screen rendered no rows after a purchase
  (deferred Destroy + reuse by name); every HUD ammo bar was one object (same name, same parent);
  the gate pulse did not read (tint on dark art); the autopilot photographed panels already closed,
  and wedged in shops with nothing buyable.

The profile save was backed up before the progression tests and restored afterwards; the owner's
meta save is back to 5 Alloy, Spread unlocked, no skill nodes.

## Not verified

- Second Wind, Last Bastion, Overwatch, Executioner, Field Repair in combat; Spread explode/ring and
  Beam slow/ramp tiers; a tower upgrade track and Build Discount in a run; Arsenal (needs six
  distinct weapons, the game has three).
- Anything audible. Fire-cue de-duplication is verified by count only.
- Feel: magazine sizes, reload times, SP and Alloy pace, weapon and tier prices. These need the
  owner to play.

## For the owner

- Assets 85-95 are still missing (all have procedural stand-ins). The lance telegraph and the
  portrait frame have baked-in checkerboards; details in `GRAPHICS_ASSET_PROMPTS.md`.
- Open decision: is respec free every time (current build), or only once per tree version?
- `Docs/SESSION_REPORT_2026-09-16.md`, `-20` and `-21` show as deleted in the working tree. This
  session did not delete them and did not restore them.
