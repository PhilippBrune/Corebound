# Open Topics — Corebound

Version 2.5 · 2026-10-12 · **The single live list.** Closed topics are deleted, not archived — git
history holds what was fixed and why. Do not start a new report file; add here.

`08_IMPLEMENTATION_BACKLOG.md` is the plan. This is the punch list. Every open point carries a
suggestion; points that need an owner decision are marked **Decide**.

**Closed 2026-10-12:**
- **Protecting the Core (old 1.1), decided P1-1, P2-1, P3-1, P4-1 and built:** threat lines, the
  Core's danger glow, an off-screen edge alert, a hit sound, a screen-edge pulse, two hurt lines,
  a Guard that charges a counter-pulse, intercept and untouched-wave Scrap, and the Core's health on
  the next stage's first banner — with a **Core Alerts** setting. Rules in `Specs/Core.md`,
  "Protecting the Core"; what was verified in `Specs/DIY/CORE_PROTECTION_FEEL_2026-10-07.md`,
  "As built".
- **Settings that never saved:** `SettingsSaveData.Clone()` skipped `_showRunTimer` and
  `_inputDeviceIndex`, so "Show Run Timer" and "Input Device" reverted on every Apply. Fixed.
- **Banners that wrapped under the plate:** a message longer than the plate's one line (German
  ones, "Core untouched +25 XP") now shrinks its font to fit instead of wrapping.
- **Window modes:** Settings → Display Mode now has Windowed (never larger than the screen),
  Borderless Fullscreen, Exclusive Fullscreen and a new **Frameless Window** (no title bar, centred,
  at the chosen resolution). Measured in a Windows player build (Settings.md).
- **Steam:** the Steamworks pages need your sign-in, so `Docs/Steam/STEAMWORKS_GUIDE.md` walks
  through both (App 5420920, store item 1372998) with paste-ready text and the SteamPipe script;
  draft capsules at every size are in `Docs/Steam/store_art/`; *Corebound > Build > Windows Player*
  builds what gets uploaded.

**Closed 2026-10-07** (third session; details in the commits and the linked docs):
- **Assets 110–113 and a redrawn Asset 14** delivered and wired: all five classes wear their own
  sprite through one path (trimmed to the figure, mipmapped, the same on-screen size and hitbox).
  The art register is grouped by category, and Asset 14's prompt is rewritten in the class style.
- **1.1, as suggested:** a Paragon level costs **6,500 XP** (a); every weapon-rarity roll is logged
  (b, below); boss health and Stage 1 wait for a human run.
- **Legendary weapons fire.** `rollcheck` (200,000 rolls through the game's own function): Common
  61.84%, Rare 26.13%, Epic 10.03%, Legendary 1.99% against the configured 62/26/10/2. In play,
  every roll is logged (`ROLL` lines; `rolls[...]` in SUMMARY and CAMPAIGN): three runs rolled 15,
  18 and 31 times with **1, 2 and 0 Legendary** — 3 in 64. The earlier 0 in 44 had a 41% chance (0.98^44).
- **2.2:** the tower card's baked English is gone from both card sizes (the original is kept as
  `be_tower_info_card_frame.png`); the build card draws "Strong against / Weak against" itself, in
  English and German. The validator treats the five filled panel frames as panels.
- **3.1:** the route panel (`MainMenuRoutePanelPresenter`) is on `UIThemeData` — four presenters now.
- **Section 4, closed by counters and screenshots:** the Salvager's magnet (5.8) and chain floor
  (tier 1 at the first Reward with no tower built — only the floor gives that); Overclock Beacon
  (6 → 13 tower boosts over three waves); Core Aegis (regenerated 0.014 → 0.071, Core 88% → 96%);
  Target Painter stacking (mark bonus 0.15 → 0.30 with a second copy); health bars drain and flash
  after a probe hit; all seven tutorial beats photographed in their panel; German checked on screen.
- **Tutorial beats no longer cover the reward or shop.** The probe showed beats 6 and 7 opening on
  top of the card reward; a beat now waits until the reward and shop are closed. Re-run: both opened
  over the arena in the intermission.
- **German on screen:** the check found and fixed the main menu buttons, Factory node names, the
  reward title, "Kern" in the HUD and run-end text, three weapon names, "Schrott" for Scrap, and 46
  words spelled with ae/oe/ue instead of umlauts across 18 files (the menu footer read
  "Bestaetigen"; four were in code comments quoting the specs). Boss phases had internal ids on the bar ("ReactorSentinel P1"); all ten now have
  names in both languages (English seen on the bar in the trailer run: "Opening Volley",
  "Containment Sweep", "Reclamation", "Cold Watch").
- **Section 5, all (a):** the loadout HUD shows "x3 (+1)"; the skill-tree version is stored per
  class; Records shows the ten class cards in their own row, counted apart from the collection; the
  campaign spends Skill Points and Alloy between runs (greedy: cheapest node, then cheapest upgrade —
  `spendtest` bought 26 node ranks and 48 upgrades from 32 SP and 400 Alloy); Profile 3 keeps its
  repaired save.
- **Steam:** 40 achievement proposals, store text, the capsule list, ten 1920 × 1080 screenshots
  (`Docs/Steam/screenshots/`) and a 43-second rough-cut trailer (`Builds/Steam/`, not in git;
  `Tools/build_trailer.py` rebuilds it) — `Docs/STEAM_PAGE.md`.

---

## 1 · Decide

### 1.1 Balance still waiting for a human — **Decide when you have played**

- **Boss health from Stage 3 on.** Bosses die in 6–9 s with a collected build (5.0–8.0 s for Stages
  3–5 in the two 2026-10-07 god runs, six weapons each). (a) ×2 health for Stages 3–5. (b) Scale with filled weapon slots.
- **Stage 1 for a new player.** The autopilot never beat the Stage 1 boss without god mode. (a) Wait
  for your first run. (b) Soften waves 4–7 and the boss now.
**Suggestion:** both wait for one human full run; note the boss times you see.

### 1.2 Class trees are a first pass — **Decide**

Each class tree is the Vanguard's tree with one class keystone inserted; the concept's bigger nodes
(a fifth tower slot, reroll interest, "Shock arcs to poisoned enemies first") are not built.
(a) Play the classes first and decide per class what its tree is missing. (b) Build the three now.
**Suggestion:** (a).

### 1.3 Files to delete — **Decide** (nothing is deleted without you)

- **`Docs/AI/` agent scaffold, 36 files:** `agents/`, `prompts/`, `Registry/`, `workflows/`,
  `MASTER_PROMPT.md`, `BOOTSTRAP.md`, `SESSION_START.md`, `system.md`, `rules.md`, `context.md`,
  `CREATE_SPEC_PROMPT`, `README.md`, `03_PROJECT_RULES.md`, `04_CODING_GUIDELINES.md`,
  `05_AI_AGENT_GUIDE.md.md`, `AGENT_ROLE_SELECTION.md`, `AGENT_WORKFLOW.md`. They describe a
  ten-agent setup from August (last touched 2026-09-16) that `CLAUDE.md` has replaced. The two asset
  registers, `ART_PIPELINE_AUTOMATION.md` and `Reward_Screen.md` stay.
- **`.continue/`** (Continue extension config, last touched 2026-08-02) and the untracked
  `.ollama/` folder, if you no longer use them.
- **`Profile_01`**, the old test profile (last written 2026-09-29).
**Suggestion:** delete the scaffold; your call on the other two.

---

## 2 · Art and audio

- `stage2/3/4/5_ground_tileset.png` are JPEG data behind a `.png` name. **Suggestion:** leave until
  one needs transparency, then re-export as real PNG.
- **Steam capsules and achievement icons** do not exist (sizes in `STEAM_PAGE.md`). **Suggestion:**
  prompts when you start the store page.
- **Audio 29, the Core-hit sound** (`AUDIO_ASSET_PROMPTS`): wired as a procedural low tone until the
  file exists. **Suggestion:** generate it; add the clip to `AudioCue_CoreHit`'s Clip Variations.
- **Core crack overlays** (Core protection A3, "cracks a little more"): no art, so the danger state
  is the glow alone. **Suggestion:** only if the glow reads too weak in your playtest.
- **The main-menu background carries a watermark:** a small translucent four-point star (an image
  generator's mark) at about (1256, 648) of `mainmenu_background.png`. In the menu it sits under the
  bottom-right buttons; the store-art script paints it out of the capsules. **Decide:** clean the
  game asset too (original kept as `be_mainmenu_background.png`), or regenerate it.
- **Steam key art, game icon, achievement icons** (Assets 114–117): prompts written; the capsules are
  drafts until they exist.

---

## 3 · Agreed work still open

### 3.0 Three layout flaws the captures show

- **Shop:** the hint line "Confirm or click buys. Cancel or right click skips the shop" runs across
  the top of the three cards and clips the first card's title (`09_foundry_exchange.jpg`).
- **Card reward:** the selected card's stat line ("Damage 0.5 · 2x every 0.09 s · …") sits on the
  frame's lower edge instead of inside the bottom plate (`05_card_reward.jpg`).
- **Wave banner under the notification strip:** the top notification line ("+3 · Salvage Trickle")
  covers the upper half of the wave banner plate whenever both show (every `core_*` probe shot).
**Suggestion:** move the three in the scene files (`ShopCanvas`, `RewardCanvas`, `HUDCanvas` /
`OverlayCanvas` — layout is scene-driven), one screenshot each to confirm.

### 3.0b German still missing

- **Wave plates** read "Wave 4", "Wave 5 Elite" in the German build: the encounter labels come from
  the wave assets with no German field.
- **The Settings screen's option names and descriptions** are English-only in
  `SettingsDefinition_Default` (only "An"/"Aus" and the buttons are translated).
**Suggestion:** a `_labelDe` beside each, as for the menu entries.

### 3.1 UI theme unification — **four presenters done**

`ProceduralUIBuilder` resolves `UIThemeData`; the skill graph, the Factory chain indicator, Records
and the route panel are on it. Eleven presenters still build through `ProceduralUIFactory`.
**Suggestion:** the weapon loadout panel next — in the Steam captures it is the one screen that is
still a plain dark box.

---

## 4 · Verification debt — built, compiled, not proven

- **Class cards being offered** (Rare/Epic; none came up in the probes) and the class unlock
  announcement on a profile that really meets a condition. **Needs your first playtest of a class.**
- **The mark ring and poison tint** on screen: at capture scale they cannot be told from the impacts
  around them. **Needs your eyes.**
- **The victory sting and credits loop** by ear. **Needs your ears.**
- **Hardware cursor:** screenshots cannot capture the OS cursor. **Needs your eyes.**
- **The build card's "Strong against / Weak against" labels** are drawn but were not photographed
  in the build browser (the probe photographed the upgrade card, which hides them on purpose).
- **Salvager chain tier at wave start:** the floor is proven, but the autopilot's first Combat line
  is sampled before wave 1 starts, so "tier 1 from the first second" is reasoned, not seen.
- **Display modes:** switching between them while the game runs (Frameless → Windowed gives the
  frame back first) and Exclusive Fullscreen were not tried in the player — the test set each mode
  in the settings file before launch. **Needs one pass through Settings in a build.**
- **Core protection:** the hit sound by ear (procedural until Audio 29), the Core Alerts toggle
  turning the lines, marker and pulse off, and the second hurt line on screen (spoken per the
  counter, not photographed). The edge alert is proven only with the probe pretending the Core
  was off-screen — the arena camera never loses it. **Needs your ears and one Settings toggle.**

---

## 5 · Unplayed

Verified to compile and, where an autopilot run reaches it, to run — but not played by a human:
- **2026-10-12:** the whole Core-protection layer — whether the lines and the pulse help or clutter,
  whether 3/6 Scrap per untouched wave and +1 per intercept are worth chasing, and whether a Guard
  that charges a pulse makes guarding feel like a counter-attack.
- **2026-10-07:** the five class sprites; the "x3 (+1)" loadout readout; Records' class row; the
  per-class tree version (and its migration from the old single version); the localized boss phases.
- **2026-09-30 / 10-01:** all five classes and their ten cards; the New Run class picker and the
  per-class Progression screen; the Core's voice, the premise, the ending, Core Logs and credits; the
  relic reveal held behind the boss notice; the tutorial beat panel; the new main-menu frames.
- **2026-09-30 (overnight):** the ten new cards; the stage crossfade; tag-weighted offers and rolled
  weapon rarities; the canvases in `OverlayCanvas`; the sheet-driven projectiles and icons.
- **2026-09-29:** Alloy quests; the quest scroll fix; XP per wave; Core placement away from gates;
  the aim-jump fixes and the hardware cursor.
- **2026-09-27/28:** the enemy stuck-watchdog, tower projectiles, the 5-second spawn delay, the
  reward layout, relic text fitting, HUD scale in the pause menu, and the second attempt at the
  profile-swap Continue fix — worth a deliberate check, because the first attempt looked correct
  and was not.

---

## Session log

**2026-10-12 (later).** Window modes (Frameless Window added, Windowed fitted to the screen),
verified in the first Windows player build on Profile 2 (the build's own profile switch restored
to 3 afterwards). The Steamworks pages could not be opened — sign-in required — so the upload is a
step-by-step guide plus draft store art and a build tool.

**2026-10-12.** Built the Core-protection decisions (P1-1, P2-1, P3-1, P4-1) and verified them with
the new autopilot variant `core N` on Profile 2: four runs with and without god mode, a `CORE` line
per wave state and a screenshot of every first. Two existing bugs found on the way (the settings
clone, the wrapping banner). The owner asked to use a newly installed "ECC" library; none was
found on this machine (not under `~/.claude/plugins`, `~/.claude/skills`, the project's `.claude/`
or `~/.codex`), so the work used the usual tools.

**2026-10-07 (third session).** Wired Assets 110–113 and the new Asset 14, regrouped the art
register, logged the weapon-rarity roll, implemented every v2.3 suggestion (Sections 1.1, 2.2, 3.1,
4 and 5), wrote the Core-protection concept and the Steam page, and cleaned the repo
(`Corebound.slnx` and `VirtualPadRequest.txt` untracked, the umlaut sweep, the validator's panel
list). Verified on Profile 2 with new autopilot variants: `rollcheck` (Edit Mode distribution),
`spendtest` (the campaign's between-run spending), `steam N` (Game view at 1920 × 1080, debug
overlays hidden, `steam_*` combat shots) and `trailer N` (30 fps JPEG clips of combat and every boss
under `Temp/Trailer/`). Capture sessions run without the 420 s cap.

Worth knowing:
- **A second Unity project is open on this machine** (`sudoku_rouge_like`). Scripts that focus
  "the Unity window" must match the title `Corebound*`, or a refresh lands in the other Editor.
- **`ScreenCapture` captures the Game view's render size**, not the window: setting the view to a
  fixed 1920 × 1080 is what makes the captures Steam-sized, and it is restored when Play ends.

**2026-09-30 → 10-01 (second session).** Built the five classes with their trees and ten class
cards, the story, Assets 108/109, the 1.3 deletions, all six Section 5 decisions, all five Section
3 items, the four Section 2.1 UI files, and the open Section 4 probes; rewrote this file (v2.3).
New autopilot variants `class`, `campaign`, `tutorial`, `records` and `stop`.

Three things went wrong and are worth knowing:
- **One god run played on the owner's Profile 3** (2026-09-30, `boss1`). Profile 3 was rebuilt by
  hand; two values are estimates (Scorcher kills 25, playtime ≈ 584 s). Both saves are in
  `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Corebound/Backups_2026-09-30/`. The autopilot now
  always switches to Profile 2 and restores the owner's profile when Play ends.
- **`Time.timeScale` outlives Play Mode in the Editor.** Every session now starts unfrozen.
- **The fifth real campaign run wedged** in front of a Relay Warden in a corner (the autopilot's
  wall push). Fixed.
