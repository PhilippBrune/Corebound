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

---

# Second pass — playtest round 2026-09-22

Driven by the owner's second playtest list, the session-report answers, and three requests: more
weapon designs, a completeness review of every spec, and a check of `Docs/AI/Reward_Screen.md`.

## Playtest items

| Item | Root cause / change | Verified |
| --- | --- | --- |
| HUD loses Player %, Core %, Scrap in fullscreen | Six HUD texts had rects 15-21 units tall with best-fit minimum 14 and vertical Truncate; at fullscreen scale the line no longer fitted and Legacy Text draws *nothing*. Rects made taller, minimum 10, Truncate kept (Overflow made them overlap) | screenshots, windowed; fullscreen reasoned only |
| Pause: perk descriptions + stat tabs | Perks tab (grid, hover and d-pad selection, description field) and Player / Towers tabs listing base value and the bonus in brackets, from the same resolvers the game uses (`StatLine`, `CollectStatLines`) | screenshots of all three tabs |
| Tower upgrade info | Build browser shows an upgrade card for a built tower: level, cost, DPS / range / interval before and after | screenshot |
| `wave_banner.png` unused | Banner now drawn from the art (cropped to its plate, preserved aspect) | screenshot |
| Loadout key "I" not visible | Shown in the Intermission line and the build browser hint | screenshot |
| Countdown runs in the weapon menu | `WaveManager.UpdateIntermission` pauses while the loadout panel is open | log |
| Enemies spawn on the player | Spawns keep 3.5 u from the player; blocked spots push outward, then mirror | code + runs, no close spawn seen |
| Run Failed shows no SP / Alloy | Earnings line from `MetaRewardLedger.RunSkillPoints / RunAlloy` | screenshot |
| Two sounds in the main menu after death | **Root cause found** with the new `[AudioProbe]` (polls every 2 s): `LocalProfileSaveManager` put MainMenu's whole shared GameObject into DontDestroyOnLoad, including AudioManager and MusicStateController. Now persists on its own host; scene instances forward | probe: one music source in menu, gameplay and after death |
| Progression footer unreadable; slot unlock info | Footer moved inside the frame; header shows Alloy / SP icons; a slot-sources row explains slots 2-4 | screenshot (earlier pass) |
| Forward Foundations too strong | First tower each stage costs 50% instead of 0 | code |
| Blue frame around the arena in fullscreen | Camera now *covers* the arena; a darkened backdrop fills beyond the walls | screenshot |
| New Run panel closes only on Enter | Left click confirms, right click cancels, for any open route panel | code only - mouse clicks cannot be scripted |
| Crit feedback | Gold spark at 1.8x plus a rising "CRIT n" number | compiles and runs without errors; no screenshot happened to catch one |
| Cooling Array = Rapid Cycler | They were both fire-interval cards. Cooling Array now reloads 25% faster and adds 3 rounds (new modifier targets) | code |
| Weapon upgrades too cheap | Tier cost factor 0.75 -> 1.25 | loadout panel shows 20 Scrap for a 16-Scrap weapon |
| Xbox 360 stick cannot aim | Aim was a Value action, so the mouse position (large magnitude) always won. Now PassThrough, with separate stick and pointer values; the stick claims at 0.25, the mouse must move 4 px to reclaim | code only - needs the controller |
| Respec "once per tree" | First respec per tree version free, then 30 Alloy | code |

## New weapons

Six designs, each a different *shape* of damage: Scorcher (short flame cone, Burn), Cryo Lance
(piercing bolt, Freeze), Railgun (3-round magazine, pierce 4, built-in crit), Nova Pulse (8-shard
ring), Scatter Launcher (bursts on impact, new `ExplodeOnImpact` rule), Twin SMG (two loose barrels,
deep magazine). Each has a five-tier Set Bonus ladder, a card, a shop offer and a HUD short label.
Railgun, Nova and Launcher sit behind Alloy licences (45 / 35 / 40). Nine weapons make Arsenal
reachable. Table and numbers in `Weapons.md`, "Weapon set 2026-09-22".

Verified in autopilot `arsenal3` (new variant: one of each new weapon): all six fire, drain and
reload; Arsenal active (budget 60 -> 68); a shop Scorcher with a full loadout went pending.
**Not verified:** balance, splash totals, Burn/Freeze uptime, any tier 5/6 bonus.

## Spec review

`Docs/SPEC_REVIEW_2026-09-22.md`: 33 findings (15 cross-spec contradictions, 11 gaps, 12 stale
statements, 2 broken references), each with at least two resolution options and a recommendation.
The main-menu layout suggestion (a two-tier layout) is finding B4.

## Reward screen vs `Docs/AI/Reward_Screen.md`

Matches: modal steel-blue frame with amber corner nodes, title plaque, currency left and tower info
right, three equal card columns with rarity frames, a footer banner for the focused card.

Fixed in this pass:
- The footer repeated the card text; it now shows the card's actual numbers ("+22% tower damage"),
  built from its effect data by `CardEffectDescriber`, and a weapon card's stats.
- No mouse: hovering a card selects it, clicking takes it (ignored for 0.5 s after the screen
  opens, so a click aimed at the last enemy cannot pick a card).
- Currency shows Alloy earned this run next to Scrap.
- **Epic and Legendary card titles were invisible**: dark text on the dark plaque. Now light on those
  frames; long names shrink instead of wrapping out of the plaque.

Still different from the document: the tower-information field only counts towers (the document
wants stats of the tower being upgraded); the card body has no artwork or modifier icons; the
currency / tower line sits partly under the header rail. Also seen: the HUD Scrap value and the
reward screen's can differ at the moment the screen opens (68 vs 85 in one shot) - not
investigated.

## Bug found by the autopilot

A weapon from the boss reward with a full loadout stays pending into the next stage, which starts
without an Intermission. The first reward there offered the same weapon card again, which
`TryGrantWeapon` refuses while one is pending - Confirm on it did nothing. Weapon cards are no
longer offered while a weapon is pending. Verified: `loadout1` reproduced the boss-Scorcher case,
the Stage 2 reward resolved, the next Intermission placed it. Open design question in
`10_OPEN_POINTS.md` item 0.

## Process notes

- Recompiling while an autopilot run is still playing reloads the domain mid-play (this Editor
  recompiles and continues) and floods the log with `WeaponHudPresenter` NullReferenceExceptions
  that look like a real bug. Cost one misdiagnosis today.
- Boss kills in *any* autopilot run pay SP and Alloy into the real profile. Today's runs were backed
  up and settled: one Stage 1 boss grant (+1 SP, +6 Alloy) was subtracted; a few Alloy from elite
  kills in that run could not be attributed and may remain.

---

# Third pass — the spec review, implemented

The owner decided every finding of `SPEC_REVIEW_2026-09-22.md` and asked for all of them to be
implemented. That file is now a decision record; the findings are gone because they are solved.

## Code

| Area | What changed |
| --- | --- |
| Interact (A1) | New `InteractRouter`: handlers register with a priority (tower slot > Core repair > Core guard) and one press goes to exactly one of them. A hold whose press was spent no longer raises the guard. |
| Main menu (B4) | Two-tier layout: primary actions stacked, global routes in a wrapping grid row along the bottom. Up/Down steps between the tiers, Left/Right walks the row. |
| Profiles (C3) | `ProfileSelection` owns three profiles, each with its own `run/settings/meta/statistics` files; the choice lives in `profile.json` beside the folders. `LocalProfileSaveManager` builds every path from it and drops its caches on a switch. A Profile route in the main menu lists the three with Ascension, Alloy, SP and whether a run is in progress. |
| Start weapon (B11) | `StartWeaponOptions` decides which weapons a run may start with; the New Run panel lists them and stores the choice in `MetaProgressionSaveData.PreferredStartWeaponId`. A licence now only widens the offer pool. |
| Settings (A12, B6) | New `InputDevice` option (Automatic / Controller / Mouse), honoured by `CoreboundInputReader`. The pause overlay has a fourth tab with volumes, aim assist and the device. |
| Skill tree (C8) | Two systems built so key nodes are rules, not percentages: **Magnetise** (pickup range, `ResourceWalletService`) replaces Scrap Surge, **Wider Draft** (reward reroll on Q / LB, `CardService.TryRerollOffer`) replaces War Chest. |
| Tower art (C11) | `TowerData._spritePath` / `_spriteRect`; the id switch in `TowerRuntimeController` is gone, so a renamed file is visible to the validator. |
| Art validation (B9) | `Corebound > Validate Art Pipeline` now inspects pixels: opaque corners on a cut-out, an opaque centre on a frame, and the baked checkerboard pattern. |
| Autopilot (B10) | Every run ends with a `SUMMARY` line: seconds per stage, damage split weapon vs tower, damage taken, kills, peak projectiles against the budget. |
| Tutorial (B7) | Two more steps: open the loadout panel, repair the Core. |

## Data

- **Waves (A15):** stages 2-5 have their own six `WaveData` assets with 77 new spawn groups, shaped
  by each stage's theme and rising in density (Reactor Depths ranged-heavy, Salvage Fields
  bruiser-heavy, Cryo Vaults mixed, Core Nexus everything).
- **Shops (C5, C6):** `Shop_Stage1` .. `Shop_Stage5`, one visit per stage after wave 3, price
  multiplier 1.0 / 1.1 / 1.2 / 1.3 / 1.45.
- **Factory (C7):** Mortar and Sentinel production nodes, so every tower in the catalog feeds a
  chain.
- **Cards (A9):** the fire-interval slot is one card per rarity now. Hair Trigger (Common) and Rapid
  Cycler (Rare) keep it; Twin Barrel left the pool; Skirmisher's Kit is a projectile-speed card.
- **Weapon icons:** all nine presentations point at cells of the future `weapon_icon_sheet.png`, so
  the HUD picks them up the moment that file exists.

## Specs

Sixteen spec files changed. New: `Docs/Specs/VFX.md` (the file seven specs linked and none had).
Economy.md gained the refund table, the currency table and the precise separation rule; Combat.md
the modifier pipeline; Bosses.md the reward bundle; Input.md the binding table and the device rules;
UI.md the resolution contract; SaveSystem.md the profile and service-lifetime rules.

## Verified

Two full autopilot runs after the changes, no exceptions: the shop opens after wave 3, the pause
tabs and the two-tier menu render, the Profile route lists three profiles, and the summary line
prints (`total=174.7s stage1=112.9s stage2=58.1s damage[weapons=401 (72%) towers=155 (28%)]
kills=40 peakProjectiles=28/60`). That also answers a Combat.md balancing rule for the first time:
the player's own weapons do about three quarters of the damage.

## Not verified

- The new stage 2-5 waves are generated, not playtested. One run died in Stage 2 wave 1 in 17
  seconds; density may be too high.
- Profile switching, the New Run weapon choice, the pause settings tab and the reward reroll are
  wired and compile, but no autopilot path exercises them yet.
- Magnetise and Wider Draft need a profile that has bought those nodes.
- Assets 101-105 (weapon projectiles, icons, impacts) are specified, not created.

---

# Fourth pass — playtest 2026-09-23

## Delivered art, 101-105

101 (projectile sheet) and 102 (weapon icons) are wired: each new weapon draws its own projectile by
pixel rect (the sheet's rows do not share a cell size) and every weapon takes its cell of the 3x3
icon sheet. 105 (nova ring) plays when a ring-firing weapon shoots.

**103 and 104 are defective and not wired:** `vfx_impact_flame_frost.png` and
`vfx_impact_splash_burst.png` are fully opaque - the transparency checkerboard is painted into the
pixels (0 % transparent, corner alpha 255). This is the fourth and fifth time this has happened, and
it is exactly what the new validator check exists for. Both need a re-render.

## Playtest items

| Item | Cause / change |
| --- | --- |
| Settings and Profile show an empty list on the first press | The known deferred-Destroy trap: rows were destroyed and recreated under the same names in one frame, so the new rows attached to the doomed ones. The first press *did* move the selection, which is why the second press looked like it moved two. Fixed in both presenters (the Progression screen already had the fix). |
| Active profile not visible | A profile badge sits in the main menu's top-right corner with the profile number and its Ascension rank, or "empty". |
| Empty profile still showed progression | `MetaProgressionService` cached the save data of whichever profile loaded first. It now drops its cache and reloads when the active profile changes. |
| No way to delete a profile | Q / LB on the Profile screen arms the delete, a second press does it, moving the selection disarms it. |
| Selection not highlighted | The selected row is bold, in the skin's selected colour, with a `>` marker. |
| Settings not tabbed | Settings is one tab per category now, switched with Q / Tab or LB / RB like every other tabbed screen. |
| New Run window unreadable | Rows had a fixed 44 px height while their text wrapped to three lines, so they overlapped. Rows size themselves now, and a weapon row is name plus one stat line instead of its full summary. |
| Fullscreen hides part of the arena | The camera contains the arena again instead of cropping to it. The morning's "blue frame" fix had made it crop; the frame is now filled by backdrop tiles. |
| Shop missing the reward screen's changes | Foundry Exchange got all three: real numbers in the footer line, mouse hover and click, and light titles on the dark Epic/Legendary plaques. |
| Tower sizes uneven | Every tower now carries a cropped sprite rect, so the shared fit size produces the Mortar's on-screen size for all of them. |
| Random Scrap in the Intermission | Factory tower-pulse nodes ran on a wall clock. Production is limited to Combat and BossCombat. |
| Tower level cap 3 | Towers go to level 7. New levels cost 4x to 22x the build cost. |
| Progression not scrollable with the wheel | The ScrollRect never received pointer events (no raycaster on that canvas), so the presenter drives it directly from the mouse wheel when the pointer is over the list. |
| Only branch tabs had icons | Unlocks, Upgrades and Ascension show emblems too (Alloy icon, weapon icon, Ascension badge). |
| Upgrades offered locked weapons | `IsWeaponUnlocked` only matched licences that carry a weapon reference, so Railgun, Nova and Launcher counted as owned. It now shares one rule with the New Run panel. |
| Tutorial back as its own entry | Tutorial is a primary menu line again, beside Continue and New Run. |
| Bosses too easy | Boss health x2.5 (100 / 162 / 225 / 300 / 400) and bosses take 90 % less damage from towers. |
| Tutorial paid meta currency | `MetaRewardLedger` refuses every payout during a tutorial run. |

## Verified

One full autopilot run on profile 1, no exceptions: the shop opens once per stage after wave 3, the
pause menu shows four tabs including Settings, the run summary prints, and the boss survived the
autopilot for the first time (`damage[weapons=301 (84%) towers=56 (16%)]` - the tower share dropped
as intended).

The autopilot needed two fixes of its own: it now dismisses blocking tutorial beats (it sat in front
of the first one until the time cap), and the run summary counts damage by source.

**Found while testing:** the active profile was still Profile 3 from the owner's session, and an
empty profile has not seen the tutorial, so New Run correctly routed into the guided run. That is
the profile system working, not a bug. The owner's profile choice was restored afterwards.

## Not verified

Profile delete, the start-weapon choice, the reward reroll, wheel scrolling and the pause settings
tab all compile and render but have no autopilot path. Tower levels 4-7 and the new stage 2-5 wave
sets are unplayed.
