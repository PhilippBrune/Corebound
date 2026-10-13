# Corebound in Steamworks — step by step

Version 1.2 · 2026-10-13 (changes applied in Steamworks) · App **5420920** · Store item **1372998**

- App admin: <https://partner.steamgames.com/apps/view/5420920>
- Store page: <https://partner.steamgames.com/admin/game/edit/1372998>

Steamworks moves and renames its tabs now and then; the names here are the ones in use in 2026.
If a field is somewhere else, the Steamworks search box finds it by name.

---

## Status — 2026-10-13, after the changes

The owner signed in to Steamworks in a separate browser window and authorised the changes; the agent
made them there and re-read every page after saving. Result: Valve's **store checklist is complete**
("Checkliste abgeschlossen"); the build checklist still lacks a price and a build.

**Done and verified on the live pages:**

| Where | What |
| --- | --- |
| Release date | **20 Jan 2027, 19:00 CET** (10:00 PST, a Wednesday), shown as "Coming soon" |
| Languages | English and German: interface only (full audio unticked) |
| Genres / features | **Early Access** ticked; *Steam Achievements* unticked until the game unlocks them |
| System requirements | OS "Windows 10 64-bit" (leading space gone), memory 4 GB, DirectX 11; German OS and graphics lines added |
| Controller questionnaire | Full controller support, Xbox controllers. PlayStation: no (the game shows Xbox glyphs A/B/X/RT). Steam Input API: no |
| Accessibility questionnaire | Camera comfort (screen shake to 0), custom volume controls (music/SFX/UI), playable without quick-time events. Not claimed: adjustable text size (HUD Scale covers the HUD only), directional audio (all audio is 2D) |
| Support information | Support website `https://steamcommunity.com/app/5420920/discussions/` — no e-mail entered (see below) |
| Description | "About This Game" EN was pasted into the visual editor as literal BBCode (it would have shown `[h2]` tags); replaced with clean BBCode in raw mode. German "About" and both short descriptions (EN 247, DE 265 characters) added — the German store localization item is now done |
| Early Access tab | All six answers in EN and DE (texts in 2.4) |
| Library assets | Library header (920 × 430) uploaded; library logo position saved (Steam's default, lower left) |
| Content survey | Completed and **ratings published**: Brazil 10, Germany USK 12, both for violence. Answers: cartoon/fantasy violence, fighting without blood with weapons; nothing else. AI disclosure EN (yours) + DE added |
| Client images | App icon 184 × 184 JPG and shortcut icon ICO uploaded |
| Launch option | `Corebound.exe`, Launch (default), Windows, 64-bit only |
| Depot | Main languages English **and German** |
| Steam Cloud | 10 MB / 200 files; Auto-Cloud `WinAppDataLocalLow` / `DefaultCompany/Corebound` / `*.json`, recursive, Windows. Steam set **"developer only"** on saving — leave it until tested (below) |

**Waiting for you:**

1. **Publish the app-admin changes** — apps/publishing/5420920: *Auf Veröffentlichung vorbereiten* →
   *Auf Steam veröffentlichen*. The agent's click was stopped by its own safety rule as a production
   change. Until this is done, the launch option, icons, depot languages and cloud are not in
   effect, and **no build can be uploaded** (Steam refuses content for a depot whose configuration
   was never published).
2. **Upload the build.** The zip is ready: `Builds/Steam/corebound_depot_5420921.zip` (177 MB,
   318 files, the 2026-10-12 build without `*.pdb` and the Burst debug folder). SteamPipe →
   *Web-Uploads* (apps/depotuploads/5420920) → depot 5420921 → *Standard* → *Hochladen*. Then
   SteamPipe → Builds: set it live on a password `beta` branch to test, on `default` for the review.
   SteamCMD (3.3) works as well.
3. **Price** for package "Corebound - 1871267" (Pricing → propose) — yours to choose.
4. **Submit the store page for review** (store page → *Veröffentlichen*). Valve needs 2–3 working
   days per round, and the page must be public as "Coming soon" for 14 days before release — for
   20 January, submit by **mid-December** at the latest.
5. **Support e-mail** (optional — the discussions URL already satisfies the field). Your personal
   address was deliberately not entered on a public page.
6. **Steam Cloud:** after the build is on a beta branch, play once on this PC, check the cloud icon
   in the library, then untick *Cloud-Unterstützung nur für Entwickler aktivieren* and tick
   *Steam Cloud* among the store page features. If you ever change the Unity company name
   (section 1), change the Auto-Cloud subdirectory with it.
7. **Worth a second look:** the violence answers (the survey's *Tötungen* item was left unticked —
   enemies are machines and fantasy creatures), and whether HUD Scale should count as adjustable
   text size.

---

## Review of the live pages — 2026-10-13 (before the changes; all items handled above)

Read in a separate browser window you signed into; nothing was clicked or saved. Steamworks shows
its labels in German for this account, so they are given as you see them.

**Correct already:** app type Game, name Corebound, Windows + 64-bit only; developer and publisher
Kratossos; genres Action, Indie, Strategy; 20 tags; feature Single-player; install folder
`Corebound`; depot **5420921 "Corebound Content"** attached to the packages; all eight store and
library images uploaded at the right sizes; 5+ screenshots and the trailer (Valve's checklist ticks
both); release date 24 Dec 2026, 19:00 CET, shown as "Bald verfügbar"; Steam Input left alone
(correct — the game handles controllers itself).

**Wrong — change these:**
1. **Languages** (Allgemeine Informationen → Unterstützte Sprachen): English and German are ticked
   as **Volle Audiounterstützung**. The game has no voice-over — untick it; tick only **Oberfläche**
   for both. (Untertitel also stays off: there is no spoken dialogue to subtitle. Section 2.1 of this
   guide said otherwise and is corrected.)
2. **Early Access is not set.** The genre list has "Early Access" unticked, so the page would sell
   a full release. Tick **Early Access** among the genres; the Early Access tab then needs the six
   answers from 2.4.
3. **"Steam-Errungenschaften" is ticked as a feature** while there are 0 achievements and the game
   cannot unlock any yet. Either untick it until they work (recommended — it also clears the
   "Errungenschaft konfiguriert" item), or keep it and have Steamworks.NET + the achievements built
   before release (section 6).
4. **System requirements:** the OS field reads " Windows 10 64-bit" with a leading space; DirectX
   is "N/A" — choose **11**; memory is empty — **4 GB** is a safe estimate for this 2D game until
   measured (2.6). The checklist already accepts what is there; these make it accurate.

**Missing — required before "Coming soon":**
5. **Descriptions** (Beschreibung): the short description and "Über dieses Produkt" are empty —
   paste 2.2 and 2.3.
6. **Support information** (Beschreibung tab, above "Über dieses Produkt"): enter a **Support-E-Mail**
   (a support website URL works too). Required.
7. **Controller support** (Allgemeine Informationen → Controllerunterstützung): answer the short
   questionnaire — Xbox controllers: full support; every menu navigable by pad.
8. **Content survey** (Inhaltsbefragung und Alterskennzeichen): it is an *unpublished draft* —
   finish it, including the AI disclosure from 2.5, and submit it.
9. **Library header** (Grafische Elemente → Bibliothekskopfzeile, 920 × 430): this is a slot of its
   own, separate from the store header capsule. Upload `header_capsule_920x430.png` there as well.
   Then open the **Positionierungswerkzeug** for the library logo once and save a position.
10. **App icon and shortcut icon** (Steamworks-Einstellungen → Clientbilder): both empty and both
    required. `Docs/Steam/store_art/app_icon_184x184.jpg` for the *Anwendungssymbol*,
    `shortcut_icon.ico` (or `shortcut_icon_256x256.png`) for the *Verknüpfungssymbol*.

**Missing — required before release:**
11. **Launch option** (Installation → Allgemeine Installation → Startoptionen): none. Add
    `Corebound.exe`, Windows, 64-bit.
12. **Main languages of the depot** (SteamPipe → Depots → Verwaltung der Hauptsprachen): only English
    is ticked; tick **German** too — the build carries both.
13. **A build** (SteamPipe → Builds): none uploaded yet — section 3.3.
14. **Price** (package "Corebound - 1871267", hidden, no price): propose one (3.6) and get it approved.

**Recommended:** Steam Cloud (quota is 0 / 0 files — 3.4 has the values), German store localization
(the Lokalisierung tab: translate the short description and "Über dieses Produkt").

**After every change in the app admin:** Veröffentlichen → Auf Veröffentlichung vorbereiten →
Veröffentlichen (3.8). Store-page edits are saved per tab and reviewed when you submit the page.

**Worth a second thought:** 24 December falls inside Steam's Winter Sale, when new releases are
easily buried. *(Resolved: moved to 20 January 2027.)*

---

## 0 · What is ready and what is not

| Item | State | Where |
| --- | --- | --- |
| Store text (short description, about, tags) | ready to paste | section 2 below |
| Screenshots (10, 1920 × 1080) | ready | `Docs/Steam/screenshots/` |
| Trailer (43 s rough cut) | ready, but replace before launch | `Builds/Steam/corebound_trailer_roughcut.mp4` |
| Capsules and library art | **drafts** from the menu art | `Docs/Steam/store_art/` (`Tools/build_store_art.py`) |
| Achievements (40) | specified, no icons | `Docs/STEAM_PAGE.md` §1 |
| Windows build | ready to upload | `Builds/Windows/` (Corebound > Build > Windows Player) |
| Achievements unlocking in game | **not built** | needs Steamworks.NET, section 6 |
| System requirements | **not measured** | section 2.6 |
| Price, release date | **yours** (developer and publisher: Kratossos) | sections 2.1, 3.6 |
| Content survey, AI disclosure | **yours to answer** (drafts below) | section 2.5 |

The order that works: store page first (it needs Valve's review and must be public as "Coming
soon" for at least two weeks before release), the build in parallel, achievements once the game can
unlock them.

---

## 1 · Before the first upload: three project settings

Open **Edit > Project Settings > Player** in Unity.

1. **Version** (`bundleVersion`, now `1.0`): set `0.1.0` for the first Early Access build and raise
   it with every upload, so a bug report names its build.
2. **Company Name** (now `DefaultCompany`): the save folder is
   `%USERPROFILE%/AppData/LocalLow/<Company Name>/Corebound`. Change it *before* anyone else plays,
   and **copy your own `DefaultCompany/Corebound` folder to the new name first**, or your Profile 3
   will look empty.
3. **Icon**: the exe still has Unity's icon. Asset 116 (game icon) in the art register is the
   prompt; set it under *Icon* when it exists.

Then build: **Corebound > Build > Windows Player** (or write `windows` into `BuildRequest.txt`).
It syncs the art mirror first and writes `Builds/Windows/` (about 280 MB). Never upload the folder
`Corebound_BurstDebugInformation_DoNotShip` — the script in section 3.3 excludes it.

---

## 2 · Store page — <https://partner.steamgames.com/admin/game/edit/1372998>

### 2.1 Basic info

- **Game name:** Corebound
- **Developer:** Kratossos · **Publisher:** Kratossos (owner, 2026-10-12).
- **Supported operating systems:** Windows (64-bit only — the build is x86_64).
- **Genres:** Action, Strategy, Indie, and **Early Access** (it is a genre box on Steam, not a tab
  setting).
- **Languages** (the table): English — Interface ✔ only · German — Interface ✔ only. No Full Audio
  (there is no voice-over) and no Subtitles (Steam means subtitles for spoken dialogue, and there is
  none).
- **Features:** Single-player. Tick *Steam Achievements* only once the game unlocks them (section 6),
  *Steam Cloud* once 3.4 is set up.
- **Support information** (on the Description tab): a support e-mail address or website — required.
- **Controller support:** Full controller support (Xbox controllers; every menu is navigable). Do
  not claim PlayStation glyphs — the game shows generic pad glyphs.
- **Website:** leave empty unless you have one.

### 2.2 Short description (paste; 247 of 300 characters)

```
Fight, build and engineer your way through five stages to protect a living energy Core. Corebound mixes twin-stick combat with deckbuilding, tower defence and a production network — every card, tower and weapon changes what the other three can do.
```

### 2.3 About This Game (paste; Steam's markup)

```
[h2]Protect the Core[/h2]
The makers built the Works to rebuild a dead world and put its last seed in a single Core. Then they were gone, and the machines forgot what they were for. You are a Binder, bound to the Core: if it breaks, it brings you back - and you try again. Fight with up to six weapons at once, build towers around it, and turn your defence into a production network that pays for the next fight.

[h2]Fight[/h2]
[list]
[*]Direct, twin-stick combat - nine weapons, each with its own rhythm, and Set Bonuses for running copies of one.
[*]Elite enemies, arena objectives and five bosses with their own telegraphs and phases.
[/list]

[h2]Build[/h2]
[list]
[*]Four tower types, placed and upgraded between waves.
[*]A Factory chain: towers feed production that pays Scrap and grows in tiers.
[*]Guard the Core yourself - block hits, charge a counter-pulse, intercept what gets close.
[/list]

[h2]Draft[/h2]
[list]
[*]42 cards to collect, plus ten class cards - each changes weapons, towers, the Core or each other.
[*]Every run is a different build.
[/list]

[h2]Grow[/h2]
[list]
[*]Five classes with their own signatures and skill trees.
[*]Account levels, Paragon and Ascension for the long run.
[/list]

Controller-first, with full mouse and keyboard. English and German.
```

### 2.4 Early Access questions (as submitted 2026-10-13; the German versions are on the page)

Phrased as plans, not promises — Valve's own hint on the tab asks for that. No Discord is named
because none exists yet; add it to the community answer once it does.

**Why Early Access?** "Corebound is built by one developer. The core loop - combat, towers,
cards, the Factory and five stages with bosses - is complete and playable, and the next steps
(balance, difficulty and more content) are best shaped by the people playing it."

**How long?** "About 6 to 12 months, depending on what players report."

**How will the full version differ?** "The plan is more stages, enemies and bosses; deeper class
skill trees; higher difficulty tiers; Steam achievements and Steam Cloud saves; Steam Deck support;
and balance shaped by player feedback."

**Current state?** "Five stages with five bosses, five playable classes, nine weapons, four tower
types, 52 cards, a Factory production chain, account progression with Paragon and Ascension, a
guided first run, English and German."

**Price during and after?** "The price is expected to rise moderately when the full version
releases."

**Community?** "Through the Steam discussions and reviews. Every update comes with patch notes, and
reported bugs and balance problems go first into the next update."

### 2.5 Content survey and AI disclosure (answer yourself)

Answer the survey from what is in the game. As far as the project shows: stylised sci-fi combat
against machines and creatures, no realistic violence, no sexual content, no gambling, no real-world
money in the game, no user-generated content, no online features. Check each against your own
knowledge of the game before ticking.

**AI generation disclosure — draft for "pre-generated content":**

```
The game's 2D artwork (sprites, backgrounds, interface frames and icons), its music and sound effects, and parts of its text were made with the help of generative AI tools, then selected, edited and assembled by the developer. The program code was written with the assistance of AI coding agents and reviewed by the developer.
```

**Live-generated content:** none — the game does not generate anything with AI while it runs.

Steam takes this disclosure seriously: an AI-made game that does not declare it can be removed.

### 2.6 System requirements (measure before filling in)

Not measured yet. Run `Builds/Windows/Corebound.exe` on the weakest PC you can get, play Stage 5
with six weapons, and note CPU, GPU, RAM and whether it holds 60 fps. Until then use only what is
certain:

- **OS:** Windows 10 64-bit
- **Memory:** 4 GB (an estimate for a 2D game of this size — replace it once measured)
- **Graphics:** DirectX 11 compatible; **DirectX version:** 11
- **Storage:** 1 GB available space (the build is 281 MB)

### 2.7 Tags (Steam takes up to 20; the first five weigh most)

Action Roguelike, Tower Defense, Deckbuilding, Top-Down Shooter, Automation, Roguelite, Twin Stick
Shooter, Base Building, Sci-fi, 2D, Singleplayer, Replay Value, Strategy, Action, Controller, Early
Access, Stylized, Difficult, Character Customization, Indie.

### 2.8 Graphical assets (Store page → Graphical Assets, then Library assets)

Upload from `Docs/Steam/store_art/` — every file is already at Steam's size:

| Slot | File | Note |
| --- | --- | --- |
| Header capsule | `header_capsule_920x430.png` | draft |
| Small capsule | `small_capsule_462x174.png` | draft; check the logo reads at this size |
| Main capsule | `main_capsule_1232x706.png` | draft |
| Vertical capsule | `vertical_capsule_748x896.png` | draft |
| Page background | `page_background_1438x810.png` | optional |
| Library capsule | `library_capsule_600x900.png` | draft |
| Library header (*Bibliothekskopfzeile*) | `header_capsule_920x430.png` | a slot of its own, same file as the store header |
| Library hero | `library_hero_3840x1240.png` | draft, upscaled ~3× — soft; no logo by Steam's rule |
| Library logo | `library_logo_1280x720.png` | transparent PNG; save a position with the positioning tool |

The app icon and the shortcut icon go into the app admin, not the store page — section 3.7.

They are built from the main-menu art. That art carries a small translucent star in its
lower-right corner — an image generator's mark; the script paints it out, and a faint smudge
remains in the main capsule's corner. For launch, generate the key art (Assets 114 and 115 in the
art register) and rebuild: `python Tools/build_store_art.py <landscape> <portrait>`.

Steam's rule for capsules: artwork, the game's name and nothing else — no review quotes, awards,
discount text or "Early Access" banners; Steam marks Early Access itself.

### 2.9 Screenshots (Store page → Screenshots)

Upload all ten from `Docs/Steam/screenshots/` in their number order — the first few are what
search and hover previews show. `10_class_select.jpg` shows "Profile 2 · Paragon 115" in a corner;
crop it or leave it out. All are god-mode captures (the HUD reads 100%); a human run with the Core
in danger would sell better (see `STEAM_PAGE.md` §2).

### 2.10 Trailer (Store page → Trailers)

Upload `Builds/Steam/corebound_trailer_roughcut.mp4` (H.264, 1920 × 1080, 30 fps, AAC). Steam
transcodes it and autoplays it **muted** — the cards in the cut are written for that. Treat it as a
placeholder: nobody has heard its music against the picture yet. `STEAM_PAGE.md` §3 has the plan
for a human-played trailer.

### 2.11 Submit for review

When every required field is green, **Submit for review** on the store page. Valve reviews within a
few business days; after approval you choose when the "Coming soon" page goes public. Wishlists only
count from then.

---

## 3 · App admin — <https://partner.steamgames.com/apps/view/5420920>

App settings changes only take effect after **Publish** (section 3.8).

### 3.1 Installation → General

- **Install folder:** `Corebound`
- **Launch option 1:** executable `Corebound.exe`, operating system Windows, CPU architecture 64-bit.
  No arguments.

### 3.2 SteamPipe → Depots

The default depot is **5420921 "Corebound Content"** (checked 2026-10-13). Operating system:
Windows; language: all languages. Under *Verwaltung der Hauptsprachen* tick **English and German** —
the build carries both, and Steam tells the game which one the player uses.

### 3.3 Upload the build

**Without SteamCMD (simplest, up to 2 GB zipped):** SteamPipe → *Web-Uploads*
(apps/depotuploads/5420920), depot 5420921, option *Standard*, choose the zip, *Hochladen*. The zip
must hold the contents of `Builds/Windows/` at its root (`Corebound.exe` at the top level), without
`*.pdb` and without `Corebound_BurstDebugInformation_DoNotShip`;
`Builds/Steam/corebound_depot_5420921.zip` is exactly that, made 2026-10-13. The depot configuration
must have been published once (3.8) before the page accepts a file.

**With SteamCMD:**

1. Download the **Steamworks SDK** (Steamworks → *Downloads*) and unpack it, e.g. to `C:\steamworks_sdk`.
2. In `C:\steamworks_sdk\tools\ContentBuilder\scripts\` create `app_build_5420920.vdf`:

```
"AppBuild"
{
	"AppID" "5420920"
	"Desc" "Corebound 0.1.0"
	"ContentRoot" "C:\Users\Philipp\Documents\Corebound\Builds\Windows\"
	"BuildOutput" "..\output\"
	"Depots"
	{
		"5420921"
		{
			"FileMapping"
			{
				"LocalPath" "*"
				"DepotPath" "."
				"recursive" "1"
			}
			"FileExclusion" "*.pdb"
			"FileExclusion" "Corebound_BurstDebugInformation_DoNotShip\*"
		}
	}
}
```

3. Build the game (section 1), then in a terminal:

```
cd C:\steamworks_sdk\tools\ContentBuilder\builder
steamcmd.exe +login <your Steamworks account> +run_app_build ..\scripts\app_build_5420920.vdf +quit
```

   The first login asks for your password and the Steam Guard code; SteamCMD remembers the login
   afterwards. Use the account that has upload rights for the app.
4. In **SteamPipe → Builds** the new build appears with its description. Set it live on a branch:
   a private `beta` branch with a password for your own testing, the `default` branch once you are
   ready for release (Valve checks the default build in the release review).
5. Test it from Steam: add the beta password under the game's *Properties → Betas*, install, play.

### 3.4 Steam Cloud (recommended)

**Steam Cloud → Steam Cloud settings:** byte quota 10 MB, files 200. **Auto-Cloud → Root
overrides / paths:** root `WinAppDataLocalLow`, subdirectory `DefaultCompany/Corebound` (or the new
company name from section 1), pattern `*.json`, recursive. That carries profiles, settings and runs
between PCs without any code.

### 3.5 Stats & Achievements

Add the 40 achievements from `Docs/STEAM_PAGE.md` §1: for each, **API name** exactly as listed
(e.g. `ACH_BOSS_1`), display name and description in English, then *Localize* for German, the
**Hidden** flag where the table says so, and two 256 × 256 icons (achieved / unachieved grey). The
icons do not exist yet (Asset 117 in the art register is the prompt).

The game cannot unlock any of them yet — that needs section 6. Achievements can be set up first;
nothing is visible to players before release.

### 3.6 Pricing and release date

- **Pricing:** the app's base package → *Propose pricing*; Steam suggests regional prices from your
  US price. Early Access prices are usually a little below the planned full price.
- **Release date:** on the store page's *Release* section. While unsure, a vague date ("Q2 2027") is
  allowed for "Coming soon"; a real date is needed to release.

### 3.7 Client images (Steamworks-Einstellungen → Clientbilder)

Both required for the community checklist:
- **Anwendungssymbol** (app icon, also the community icon): `Docs/Steam/store_art/app_icon_184x184.jpg`
  (184 × 184 JPG).
- **Verknüpfungssymbol** (shortcut icon): `Docs/Steam/store_art/shortcut_icon.ico` (256 px plus the
  small sizes) or `shortcut_icon_256x256.png`.

Both are cut from the logo's crystal until Asset 116 (game icon) exists.

### 3.8 Publish

**Publish → Prepare for publishing → Publish to Steam.** Every app-admin change above (install,
depots, cloud, achievements) waits until this is done.

---

## 4 · Release checklist

1. Store page approved and public as "Coming soon" for at least 14 days.
2. A build on the `default` branch, tested from Steam on a clean PC.
3. **Release** checklist in Steamworks all green → *Request build review*; Valve launches the
   build and checks the store page matches it (a few business days).
4. Press **Release** on the release day.

---

## 5 · Files you will need, in one place

| Need | File |
| --- | --- |
| Short description, about, EA answers, AI disclosure | this guide, section 2 |
| Screenshots | `Docs/Steam/screenshots/01…10_*.jpg` |
| Trailer | `Builds/Steam/corebound_trailer_roughcut.mp4` |
| Capsules, library art, app and shortcut icons | `Docs/Steam/store_art/` |
| Build | `Builds/Windows/` (Corebound > Build > Windows Player) |
| Upload script | section 3.3 |
| Achievement list | `Docs/STEAM_PAGE.md` §1 |
| Art still to generate | `Docs/AI/GRAPHICS_ASSET_PROMPTS.md`, "Steam store and library" (Assets 114–117) |

---

## 6 · What the game itself still needs for Steam

- **Steamworks.NET** (MIT): add it via Unity's Package Manager from its Git URL
  (`https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net`), put
  `steam_appid.txt` containing `5420920` next to the exe **for local testing only** (not in the
  upload), and add one `SteamAchievementService` that turns the game's existing events into
  achievements (design in `STEAM_PAGE.md` §1, "Wiring").
- **Restart through Steam:** `SteamAPI.RestartAppIfNecessary(5420920)` at startup, so a copy started
  outside Steam relaunches through it.
- **Steam Deck:** request a compatibility review once the HUD scale reads at 1280 × 800.

Ask and these can be built in the project the same way as everything else; only the uploads and
the answers in sections 2–3 need you.
