# Corebound on Steam — achievements, screenshots, trailer

Version 1.0 · 2026-10-07 · Owner request: *"I would like to publish the game later on Steam. Make
suggestions for achievements, create screenshots for the Steam page. Are you able to create a
trailer for the game? Put the results in an own .md file."*

Everything here is a proposal except the screenshots and the rough-cut trailer, which exist. Nothing
is wired to Steam yet: the project has no Steamworks package (see "Wiring" below).

---

## 1 · Achievements

**Principles.** Every achievement names a thing the player already sees in the game — a boss, a
class, a quest, a Records count — so it reads as recognition, not homework. Most ride on trackers
that already exist (the quest ledger, `statistics.json`, Records); the "Source" column says which,
and **new** marks the eleven that need a counter first. Hidden ones are the story beat and the
close calls, where knowing in advance spoils the moment.

40 achievements: 9 story, 10 classes, 9 build, 7 mastery, 5 challenge. A full clear on the Vanguard
earns about eight; the last ones take the 100-hour player the vision aims at (Vision §11).

### Story and stages

| API name | Name (EN / DE) | Unlock | Source | Hidden |
| --- | --- | --- | --- | --- |
| `ACH_TUTORIAL` | Power-On Self Test / Selbsttest bestanden | Finish the guided first run | tutorial completion | |
| `ACH_BOSS_1` | Foundry Fire / Feuer in der Gießerei | Defeat the Corebreaker Warden | quest `boss_stage_1` | |
| `ACH_BOSS_2` | Meltdown Averted / Kernschmelze verhindert | Defeat the Reactor Sentinel | quest `boss_stage_2` | |
| `ACH_BOSS_3` | Scrap the Overseer / Aufseher verschrottet | Defeat the Salvage Overseer | quest `boss_stage_3` | |
| `ACH_BOSS_4` | Thaw / Tauwetter | Defeat the Cryo Warden | quest `boss_stage_4` | |
| `ACH_BOSS_5` | Corebound / Corebound | Defeat the Core Nexus Warden and finish a run | quest `boss_stage_5` | |
| `ACH_ENDING` | What the Core Remembers / Was der Kern erinnert | See the ending through to the credits | story ending shown | yes |
| `ACH_LOGS_ALL` | Black Box / Flugschreiber | Unlock all 13 Core Logs | Records → Core Logs 13/13 | |
| `ACH_FIRST_RELIC` | Spoils of War / Kriegsbeute | Take a boss relic | relic granted | |

### Classes

| API name | Name (EN / DE) | Unlock | Source | Hidden |
| --- | --- | --- | --- | --- |
| `ACH_UNLOCK_ENGINEER` | Torque Spec / Drehmoment nach Vorschrift | Unlock the Engineer (a tower at its highest level in one run) | class unlock | |
| `ACH_UNLOCK_SALVAGER` | One Man's Trash / Des einen Schrott | Unlock the Salvager (Factory chain tier 3 in one run) | class unlock | |
| `ACH_UNLOCK_CUSTODIAN` | Not a Scratch / Kein Kratzer | Unlock the Custodian (a stage with the Core untouched) | class unlock | |
| `ACH_UNLOCK_CATALYST` | Reaction Started / Reaktion eingeleitet | Unlock the Catalyst (defeat the Stage 3 boss) | class unlock | |
| `ACH_CLEAR_VANGUARD` | Vanguard: Held the Line / Vorhut: Linie gehalten | Finish a run as the Vanguard | **new**: class id in the run history entry | |
| `ACH_CLEAR_ENGINEER` | Engineer: Built to Last / Ingenieur: Für die Ewigkeit gebaut | Finish a run as the Engineer | **new** (same field) | |
| `ACH_CLEAR_SALVAGER` | Salvager: Nothing Wasted / Verwerter: Nichts verschwendet | Finish a run as the Salvager | **new** (same field) | |
| `ACH_CLEAR_CUSTODIAN` | Custodian: Never Left Its Side / Hüter: Nie von seiner Seite | Finish a run as the Custodian | **new** (same field) | |
| `ACH_CLEAR_CATALYST` | Catalyst: Chain Reaction / Katalysator: Kettenreaktion | Finish a run as the Catalyst | **new** (same field) | |
| `ACH_CLASS_CARDS` | Signature Moves / Markenzeichen | Collect all ten class cards | Records → class cards 10/10 | |

### Builds

| API name | Name (EN / DE) | Unlock | Source | Hidden |
| --- | --- | --- | --- | --- |
| `ACH_FULL_ARSENAL` | Full Arsenal / Volles Arsenal | Six different weapons equipped at once | Full Arsenal set bonus active | |
| `ACH_SET_TIER3` | Matched Set / Komplettes Set | Reach the highest tier of any Set Bonus | `ActiveSetBonus` tier 3 | |
| `ACH_LEGENDARY_WEAPON` | Legend in Hand / Legende in der Hand | Take a Legendary weapon card | **new**: rolled rarity on a taken weapon card | |
| `ACH_CHAIN_TIER4` | Assembly Line / Fließband | Factory chain tier 4 in one run | quest `factory_tier4` | |
| `ACH_FOUR_TOWER_TYPES` | Mixed Defence / Gemischte Verteidigung | All four tower types built in one run | **new**: distinct tower ids built this run | |
| `ACH_TOWER_MAX_ALL` | Chief Engineer / Chefingenieur | Every tower type upgraded to its highest level (over any runs) | quests `tower_max_*` (4) | |
| `ACH_STATUS_TRIO` | Bad Day / Schlechter Tag | Poison, Shock and a Mark on one enemy at the same time | **new**: status check on hit | |
| `ACH_SCRAP_HOARD` | Scrap Baron / Schrottbaron | Hold 500 Scrap at once | **new**: wallet high-water mark | |
| `ACH_RELICS_FIVE` | Trophy Wall / Trophäenwand | Hold five relics in one run | relic count in run state | |

### Mastery (long-term)

| API name | Name (EN / DE) | Unlock | Source | Hidden |
| --- | --- | --- | --- | --- |
| `ACH_WEAPON_1000` | Trusted Tool / Bewährtes Werkzeug | 1,000 kills with one weapon | any quest `kills_*` | |
| `ACH_ALL_CARDS` | Full Deck / Volles Deck | Collect every card | quest `cards_all` | |
| `ACH_KILLS_10K` | Exterminator / Kammerjäger | 10,000 enemies defeated | `statistics.json` `_totalEnemyKills` | |
| `ACH_ELITE_50` | Elite Hunter / Elitejäger | Complete any elite-kill quest (50 of one affix) | any quest `elite_kills_*` | |
| `ACH_LEVEL_70` | Fully Calibrated / Voll kalibriert | Reach account level 70 | meta save level | |
| `ACH_TREE_FULL` | Every Branch / Jeder Zweig | Fill one class's skill tree | `SkillTreeRules` (all nodes at max rank) | |
| `ACH_ASCENSION_1` | Beyond the Limit / Über die Grenze | Buy the first Ascension rank | meta save Ascension rank | |

### Challenges

| API name | Name (EN / DE) | Unlock | Source | Hidden |
| --- | --- | --- | --- | --- |
| `ACH_UNTOUCHED_ALL` | Flawless Guardian / Makelloser Wächter | Every stage cleared with the Core untouched (over any runs) | quests `stage_core_untouched_*` (5) | |
| `ACH_ONE_WEAPON_ALL` | Specialist / Spezialist | Every stage cleared with a single weapon type (over any runs) | quests `stage_one_weapon_*` (5) | |
| `ACH_NO_TOWERS` | Hands-On / Selbst ist der Kern | Clear a stage without building a tower | **new**: towers built per stage | |
| `ACH_CORE_CLOSE` | Hairline Crack / Haarriss | Defeat a boss with the Core below 5% | **new**: Core health at boss defeat | yes |
| `ACH_FAST_CLEAR` | Overclocked / Übertaktet | Finish a run in under 20 minutes of active time | history `_elapsedActiveSeconds` | yes |

**Numbers to tune after a human playtest:** `ACH_FAST_CLEAR` (god-mode clears took 11–13 min, a real
run will be longer), `ACH_SCRAP_HOARD` (the Salvager's hoard makes 500 easy for one class only —
that is fine) and `ACH_CORE_CLOSE` (5% is about one hit at Stage 5).

### Wiring

Steam needs three things the project does not have yet:

1. **A Steamworks package.** [Steamworks.NET](https://steamworks.github.io/) is the usual choice for
   Unity (MIT, wraps the C++ API one-to-one). It needs the App ID; until then `steam_appid.txt` with
   480 (Spacewar) works for testing.
2. **One `SteamAchievementService`** in the persistent scene, the only class that references
   Steamworks. It listens to events the game already raises — `QuestLedger.QuestCompleted`,
   `CombatEvents`, the class unlock announcement, the story ending — and calls `SetAchievement`
   then `StoreStats`. Everything stays inside `#if !DISABLESTEAMWORKS`, so a build without Steam
   (and every Editor session) runs exactly as now. Single compilation unit: no `.asmdef` needed.
3. **The eleven new counters**, each a small field in the run or meta save, written where the
   event already happens.

On first start with Steam, unlock everything the save already earned (the quest ledger and the
statistics know it), so players who start before achievements exist lose nothing.

**Icons:** 256 × 256 px each, an *unlocked* and a *locked* version (Steam shows the locked one
grey only if you upload it grey). The art style of the tag icons (Assets 108) fits; one prompt per
group in `Docs/AI/GRAPHICS_ASSET_PROMPTS.md` when the list is final.

---

## 2 · Screenshots

Ten shots, in the suggested upload order, in `Docs/Steam/screenshots/` (1920 × 1080 JPEG, quality
92). The lossless PNGs are in `Builds/Steam/screenshots/`, which git ignores.

They are real gameplay, captured by the autopilot on Profile 2 in two capture sessions — `steam
class engineer arsenal god 2` and `trailer class catalyst arsenal god 1` — with the Game view fixed
at 1920 × 1080 and the debug overlays hidden. Nothing is composited or retouched; number 8 is a
frame from the trailer footage, the rest are screen captures.

| # | File | Shows | Why it is in |
| --- | --- | --- | --- |
| 1 | `01_salvage_overseer_boss.jpg` | Salvage Fields: the Salvage Overseer's telegraph, the Core's voice line, a tower pulse | The hook in one frame: a boss, a defended Core, a story |
| 2 | `02_reactor_depths_towers.jpg` | Reactor Depths: Factory pulses ("+5 · Sentinel Sweep Pulse ×1.25"), shockwave rings, six weapons | Tower defence and production, not only shooting |
| 3 | `03_cryo_warden_boss.jpg` | Cryo Vaults: the Cryo Warden inside its ring telegraph, towers around it | A second biome, a second boss |
| 4 | `04_core_nexus_overcharge.jpg` | Core Nexus: the moment the Engineer overcharges a tower, the Core speaking | The final stage, a class ability |
| 5 | `05_card_reward.jpg` | The card reward: three cards, two of them weapons, one Epic | The deckbuilding choice |
| 6 | `06_salvage_fields_combat.jpg` | Busy combat, a critical hit, two tower auras | Readable chaos |
| 7 | `07_reactor_sentinel_boss.jpg` | The Reactor Sentinel's ring telegraph | Boss variety |
| 8 | `08_foundry_corebreaker_warden.jpg` | Stage 1: the Corebreaker Warden at 74%, towers around the Core | The first stage players see |
| 9 | `09_foundry_exchange.jpg` | The shop (Foundry Exchange) with an Epic offer | The economy |
| 10 | `10_class_select.jpg` | The New Run panel: class, signature, start weapon | Five classes, six start weapons |

**Before they go on the page:**
- Every shot is a **god-mode run**, so the HUD reads "Player 100% · Core 100%" throughout. A store
  page sells tension; one human run with the Core at 40% and a crowded screen would beat 2 and 6.
- Profile 2's **HUD scale is 0.8**, so the HUD text is small at 1080p. Capture at 1.0.
- `10_class_select` shows "Profile 2 · Paragon 115" in the corner. Crop it or retake on a fresh
  profile.
- Missing, and worth one capture each: the skill tree (`progression` variant), the Factory chain
  readout with tier 3, and the Records screen.
- Steam asks for at least five screenshots of the game itself, 1920 × 1080 recommended; menus are
  allowed but should not lead.

---

## 3 · Trailer

**Can I make one? Partly — a rough cut, yes; the trailer you publish, not alone.**

What exists: **`Builds/Steam/corebound_trailer_roughcut.mp4`** — 43 s, 1920 × 1080, 30 fps, H.264
High at about 8 Mbit/s, AAC stereo, 44 MB. It is cut from real gameplay that the autopilot recorded
(`trailer class catalyst arsenal god 1`, a full five-stage clear with 13 clips), with the game's own
logo, menu art and music (`music_boss_stage5_loop.mp3`, from 2:04, where the track is loudest).
`Builds/` is ignored by git, so the file lives only on this machine; `Tools/build_trailer.py`
rebuilds it from any `trailer` recording in about two minutes.

| Time | Shot | Card |
| --- | --- | --- |
| 0:00 | Cold open: the Core Nexus Warden fight | — |
| 0:03 | Logo over the main-menu art | "Defend a living Core. Fight, build, draft, engineer." |
| 0:06 | Foundry combat, two clips | **FIGHT** — up to six weapons at once |
| 0:11 | Reactor Depths combat, two clips | **BUILD** — towers that guard the Core |
| 0:16 | The card reward (Epic Scorcher) | **DRAFT** — a build, one card at a time |
| 0:18 | Salvage Fields combat, Factory pulses | **ENGINEER** — a production chain that pays back |
| 0:21 | Cryo Vaults, Core Nexus | **FIVE STAGES** |
| 0:26 | The New Run class picker | **FIVE CLASSES** |
| 0:28 | Four bosses, two seconds each | **FIVE BOSSES** |
| 0:35 | The final boss | **PROTECT THE CORE** |
| 0:38 | Logo | **EARLY ACCESS** — Wishlist on Steam |

Each clip is cut to its busiest stretch, measured by frame-to-frame motion; the cuts are 8-frame
crossfades. The cards are readable without sound, which matters because Steam starts the trailer
muted.

**What a rough cut cannot do, and why the published trailer needs you:**
- **I cannot hear it.** The music was chosen by measured loudness, not by ear; whether the cut sits
  on the beat is unknown. Listen before anything else.
- **The autopilot plays like a machine, in god mode.** Movement is mechanical, nobody takes damage,
  and nothing is ever close. A trailer sells near-misses and comebacks.
- **The camera shows the whole arena**, so every sprite is small. A trailer wants close moments: a
  capture-only camera zoom (a debug key that halves the orthographic size around the player) would
  give them without touching gameplay.
- **No moments of decision on screen** beyond one card reward: no tower being placed, no Factory
  tier announcement, no class unlock.

**For the published trailer** (Steam recommends gameplay from the first seconds; 30–60 s works best
for a first trailer):
1. Play three or four real runs with OBS recording at 1080p60 (or 1440p), HUD scale 1.0, one class
   per run, and note the timestamps of good moments: a boss phase shift, a Core saved at 10%, a
   six-weapon screen, a chain-tier announcement.
2. Keep this cut's structure — cold open, four verbs, five bosses, the Core line — and replace the
   footage. The verbs are the pitch; they also work as the Steam short description in motion.
3. Add one beat the rough cut lacks: the Core taking a hit and the player turning back to save it
   (see `Specs/DIY/CORE_PROTECTION_FEEL_2026-10-07.md` — the threat lines and the Core's hurt lines
   would make this moment read on video).
4. Export H.264, 1920 × 1080, 5,000 kbit/s or more, stereo AAC; upload the MP4 to Steamworks.

---

## 4 · The rest of the store page

### Text

**Short description** (Steam allows 300 characters; this is 247):

> Fight, build and engineer your way through five stages to protect a living energy Core. Corebound
> mixes twin-stick combat with deckbuilding, tower defence and a production network — every card,
> tower and weapon changes what the other three can do.

**About this game** — lead with the decision, not the genre list:

- *You are the Core's last defender.* Fight directly with up to six weapons at once.
- *Build the defence around you.* Four tower types, upgraded between waves.
- *Draft a build.* Cards change weapons, towers, the Core and each other — 42 to collect, plus ten
  class cards.
- *Turn defence into production.* Towers feed a Factory chain that pays Scrap and grows in tiers.
- *Five classes, five stages, five bosses*, a skill tree per class, Paragon and Ascension for the
  long run.
- Controller-first, full mouse and keyboard; English and German.

**Tags** (Steam lets you set 20; the first five carry the most weight): Action Roguelike, Tower
Defense, Deckbuilding, Top-Down Shooter, Automation, Roguelite, Twin Stick Shooter, Base Building,
Sci-fi, 2D, Singleplayer, Replay Value, Strategy, Action, Controller, Early Access, Stylized,
Difficult, Character Customization, Indie.

### Things Steam will ask for

- **AI-generated content disclosure.** Steam's content survey asks whether the game contains
  pre-generated AI content and how it was reviewed. Corebound does: the art, the music and sound
  were generated from the prompts in `Docs/AI/`, and the code was written with AI agents. Say so
  plainly and describe the review (every asset checked and wired by hand, the art pipeline
  validator). Undisclosed AI content is grounds for removal.
- **Early Access questionnaire** (Vision §1: Early Access): why Early Access, how long, what the
  full version adds, current state, price plans. The Early Access roadmap in
  `08_IMPLEMENTATION_BACKLOG.md` is the source.
- **System requirements.** Not measured yet. Take them from a release build on the lowest machine
  you can borrow; until then do not publish numbers.
- **Languages:** English and German, interface and subtitles; no voice.
- **Controller support:** "Full controller support" is accurate (controller-first, every menu
  navigable). Steam Deck: unverified; the 1280 × 800 screen needs a HUD-scale check before applying.
- **Capsule art** - drafts at every size since 2026-10-12 in `Docs/Steam/store_art/`, built by

  | Asset | Size (px) |
  | --- | --- |
  | Header capsule | 920 × 430 |
  | Small capsule | 462 × 174 |
  | Main capsule | 1232 × 706 |
  | Vertical capsule | 748 × 896 |
  | Page background (optional) | 1438 × 810 |
  | Library capsule | 600 × 900 |
  | Library hero | 3840 × 1240 |
  | Library logo (transparent) | 1280 × 720 |
  | App icon (also the community icon) | 184 × 184 JPG |
  | Shortcut icon | 256 × 256 PNG or ICO |

  Capsules must show the game's name legibly at the small size. The upload steps for all of it are
  in **Docs/Steam/STEAMWORKS_GUIDE.md** (App 5420920, store item 1372998).

### Order of work

1. Steamworks partner account and the Steam Direct fee; app created → App ID **5420920** (done).
2. "Coming soon" page with capsules, these screenshots and a trailer — Steam wants it live at least
   two weeks before release, and wishlists only start counting once it is.
3. Steamworks.NET + `SteamAchievementService` + the eleven counters; achievement icons.
4. A human-played trailer (section 3), replacing the rough cut.
5. Steam Next Fest with a demo (Stage 1 and 2, one class) if the timing fits.
