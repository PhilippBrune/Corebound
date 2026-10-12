# CLAUDE.md — Corebound

Working rules for AI agents on this project. Everything here is load-bearing: each item exists because getting it wrong already cost a debugging session.

## What this is

COREBOUND — a singleplayer Windows/Steam action roguelite combining direct combat, deckbuilding, tower defense and a focused Factory system. Unity 6.3 LTS (6000.3.10f1), URP 2D, orthographic top-down. Solo developer (German, QWERTZ keyboard) working with AI agents.

**Authoritative documents**, in precedence order:

1. `Docs/00_PROJECT_VISION.md` — the product. Section 19 "Design Boundaries" overrides convenience.
2. `Docs/Specs/*.md` — per-system contracts.
3. `Docs/Slices/*.md` — scoped work packages.
4. `Docs/08_IMPLEMENTATION_BACKLOG.md` — plan and status.
5. `Docs/OPEN_TOPICS.md` — the live open-items register. **One file, since 2026-09-27**: the old
   `10_OPEN_POINTS`, `SESSION_REPORT_*`, `SPEC_REVIEW_*` and `Specs/DIY/*_PROPOSALS` files were
   merged into it and deleted. Do not start a new report file; add to this one.

**Asset prompts and their delivery status** live in two registers, and both carry a per-asset
`**Status:**` line that says whether the file exists and whether anything references it:

- `Docs/AI/GRAPHICS_ASSET_PROMPTS.md` — every sprite, with its target path.
- `Docs/AI/AUDIO_ASSET_PROMPTS.md` — every music loop and SFX cue, same format.

Keep both current when art or audio is added, replaced or wired. A prompt with no status line, or a
status that disagrees with the disk, is how assets end up finished and unused — the 2026-09-27 audit
found 16 finished sprites and 4 finished boss loops that nothing referenced.

**Treat these docs as claims, not facts.** They have repeatedly drifted from the code in *both* directions — a 2026-09-17 audit found seven "still open" items that were fully implemented, and the vision still states targets the build has since passed or missed. Verify against code before acting on any status claim, and correct the doc when it is wrong.

## Testing

- **Test through `Assets/Scenes/MainMenu.unity` → New Run.** Opening `SampleScene.unity` directly hides a whole class of bug, because the Editor has already loaded the additive canvas scenes before Play starts. Several defects reproduced *only* via MainMenu.
- **Debug stage-select:** F9 / F10 in the Main Menu cycle which stage a New Run starts on. An on-screen label shows the current setting. Use it to reach Stage 2+ without playing through.
- **Agents can playtest now.** `Assets/Editor/DebugAutopilot.cs` plays a real run end to end. It is
  **off by default**: tick `Corebound > Autopilot > Enabled`, or write `enable` into
  `AutopilotRequest.txt` in the project root. Writing a stage number into that file starts a run on
  that stage within a second; `disable` turns it back off. Progress goes to `Editor.log` behind
  `[Autopilot]`, including a `->` line for every wave-state transition, a stall report when nothing
  changes for 12s, and per-sample HUD, shop, arena and enemy-position state.
- **The autopilot only ever plays Profile 2.** The owner plays Profile 3. Since 2026-09-30 the
  autopilot switches to Profile 2 before it starts and switches back when Play Mode ends; before
  that, one god run paid into the owner's real save and had to be reconstructed by hand. Do not
  remove that guard, and never test on Profile 1 or 3. Profile 2 is a scratch profile: back up
  `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Corebound/Profile_02/` before a variant that wipes
  it (`campaign`).
- **Autopilot variants** (write into `AutopilotRequest.txt`; each starts stage N afterwards and
  combines with the others, e.g. `class custodian god 2`): `loadoutN` fills all six weapon slots
  (3 Rapid, 2 Spread, 1 Beam), `arsenalN` six different ones; `progressionN` grants test currency
  and buys a scripted set of nodes, upgrades and a slot unlock through the Progression screen;
  `ascensionN` fills the tree and buys an Ascension rank; `godN` turns on god mode; `bossN` is god
  mode plus a skip to every stage's boss, logging each phase (`BOSS END ... in Ns`); `cardsN` applies
  the ten NEW_CARD_MECHANICS cards and logs what they did per wave; `recordsN` photographs the
  Records tabs; `padN` plays through a virtual gamepad; `class <id> N` plays that class, unlocked or
  not, and logs a `CLASS` line per wave state; `campaignK` wipes Profile 2 and plays K real runs
  from Stage 1 back to back, one `CAMPAIGN` line per run (XP, Alloy, SP, boss times, weapon
  rarities offered). Without `class`, a run plays the Vanguard. Writing `stop` ends a run that is
  already playing. `tutorialN` plays the guided run with a shot per beat; `rollcheck` (Edit Mode)
  rolls the weapon rarity 200,000 times through the game's own function; `spendtest` grants SP and
  Alloy and runs the campaign's between-run spending, then stops. `steamN` and `trailerN` are
  capture sessions: the Game view is fixed at 1920 × 1080, debug overlays are hidden, the 420 s run
  cap is lifted, and the view is restored when Play ends; `steam` adds `steam_*` combat shots,
  `trailer` records 30 fps JPEG clips of combat and every boss under `Temp/Trailer/<time>/`.
  `coreN` probes the Core-protection layer: a `CORE` line per wave state (hits, hit sounds, edge
  pulses, threat lines, danger state, hurt lines, guard charge and pulses, intercepts,
  untouched-wave bonus and streak) and `core_*` screenshots of each first; it raises the Guard in
  waves 2-4, pretends the Core is off-screen in waves 3-4, and lowers the Core to 48 % / 27 % in
  waves 4-5 (a restore, not a hit). With `god` the Stage 1 boss sets it to 74 % for the carry banner. Screenshots of gates, reward, shop, build browser, loadout panel, pause, boss
  telegraphs, premise and ending go to `Temp/AutopilotShots/` at the moment each state is reached;
  `ScreenCapture` keeps only the last capture of a frame, so two shots in one frame lose the first.
- **Windows player build without a dialog:** write `windows` into `BuildRequest.txt` (or
  *Corebound > Build > Windows Player*); it syncs the art mirror, builds `Builds/Windows/` and logs
  `[Build] Succeeded/Failed`. Anything behind `#if UNITY_STANDALONE_WIN && !UNITY_EDITOR` (the
  frameless window) only compiles and runs there. The player reads the **active profile** from
  `LocalLow/DefaultCompany/Corebound/profile.json` like the Editor does - set it to 2 before
  launching a test build and restore it afterwards; its log is `Player.log` in the same folder.
- **Validate Art Pipeline without a dialog:** write `validate` (or `sync`) into
  `ArtPipelineRequest.txt` in the project root; the report goes to `Editor.log` behind
  `[ArtPipeline]`. It checks the StreamingAssets mirror, every `StageThemeData` path, every
  `SpriteSheetData` cell against its image, and flags frames with an opaque centre.
- **Three rules the autopilot had to learn the hard way.** It must call `DebugSetUsingGamepad(true)`,
  because `PlayerAimResolver` reads `Aim` as a *screen position* in mouse mode, so a world direction
  aims every shot at a screen corner and looks exactly like "projectiles do no damage". It must bind
  to the `CoreboundInputReader` in the *player's* scene, since MainMenu and SampleScene each carry
  one. And periodic sampling is not enough to prove a UI state never happened: Reward and
  Intermission are routinely shorter than the sample interval, so transitions must be logged.
- **Watch for single-frame event consumption.** `ConfirmPressed` has multiple subscribers, and a
  handler that *opens* something synchronously can have that same press consumed by the thing it just
  opened. This has now caused three separate bugs (intermission gate, shop never showing, build
  intermission skipped). Anything opened from inside an input-event handler needs a
  `Time.frameCount` guard.
- **`Assets/Editor` compiles into `Assembly-CSharp-Editor.dll`**, not `Assembly-CSharp.dll`. When
  verifying a build by DLL timestamp, check the one that actually contains your file.
- **A second Unity project may be open on this machine** (`sudoku_rouge_like`). Anything that
  focuses "the Unity window" to refresh or click must match the window title `Corebound*`, or the
  keystroke lands in the other Editor.
- **The asset watcher ignores same-size rewrites.** A refresh that logs
  `RefreshV2(NoUpdateAssetOptions)` picked up nothing. Changing the file's *length* (not just its
  mtime) reliably wakes it; `SetForegroundWindow` alone is not enough, and Windows' foreground lock
  needs `AttachThreadInput` to beat while the user is active.
- **Synthetic *OS* input does not reach Unity's Input System, but a virtual device does.**
  `SendInput`/`keybd_event` drive Editor UI (Play button, menus, Hierarchy) and never reach
  `Keyboard.current`. A **virtual gamepad** does: `Corebound > Virtual Gamepad > Add` creates a real
  device via `InputSystem.AddDevice<Gamepad>()`, and `QueueStateEvent` feeds it state that runs the
  whole binding path - actions, control schemes, device detection. Verified 2026-09-25 by
  `Corebound > Virtual Gamepad > Self Test` (Confirm, Cancel, Previous, Next all fired,
  `IsUsingGamepad` flipped) and by the autopilot variant `padN`, which plays a whole run with
  movement, aim and fire coming from the virtual stick instead of the reader's debug surface.
  Write `test` into `VirtualPadRequest.txt` to run the self test while already in Play Mode.
  Keyboard input still has no synthetic path; anything needing a *key* press needs the user.
- Agents cannot hear audio. Do not guess at audio bugs; ask which sounds.

## Engine gotchas that have already caused bugs

- **`SceneManager.LoadScene(..., Additive)` is not synchronous.** Unity defers it to end of frame, so the new scene's objects do not exist when the call returns — and not during the requesting frame's `Start()` either. Canvas presenters must bind from `AdditiveCanvasSceneLoader.CanvasSceneReady`, never from `Start()` alone.
- **`Destroy()` is deferred; the object stays findable by name for the rest of the frame.** Rebuilding anything resolved via `GameObject.Find` must rename/deactivate the old object first, or the new content gets parented under the doomed one. Edit Mode uses `DestroyImmediate` and will not reproduce this.
- **Runtime art is read as raw bytes from `Assets/StreamingAssets/Art/` first**, falling back to `Assets/Art/` *only inside the Editor*. An unmirrored or stale file therefore looks perfect in the Editor and ships broken. After adding or replacing any art, run **`Corebound > Validate Art Pipeline`**.
- **`StageThemeData` stores texture paths as plain strings.** A renamed file is a silent null, not a broken reference the Editor flags. The same validator checks these.
- **Canvas layout is scene-file-driven.** `RewardCanvas`/`ShopCanvas`/`HUDCanvas`/`OverlayCanvas` (boss HUD, notifications, relic reveal - since 2026-09-30) own their own positions; presenters must only *find* elements and repair missing sprites, never reassert anchors, positions or sizes. Several HUD Images intentionally carry no sprite in the scene and receive one at runtime — a partial re-init that skips `CreateRuntimeSprites()` leaves them rendering plain white.
- **`Time.timeScale` survives the end of Play Mode in the Editor.** Stopping Play while a pause,
  reward or tutorial beat holds the freeze used to start the next session - main menu included - at
  0. `GameplayTimeScaleGuard` now resets it at every session start; anything new that freezes time
  must go through that guard.
- **Never `GetComponent<T>() ?? AddComponent<T>()` on a Unity object.** In the Editor a missing
  component is a fake-null that `??` treats as present, so nothing is added; it broke a stage change
  on 2026-09-30. Use `gameObject.GetOrAdd<T>()` (`ComponentUtility`), which compares with `!= null`.
- **Avoid `[`/`]` and other layout-dependent keys.** `Keyboard.leftBracketKey` addresses a US *physical* position, which on the QWERTZ keyboard in use is `ü`. Function keys are safe everywhere.

## Code conventions

- Gameplay data lives in ScriptableObjects; systems talk through events and stay loosely coupled.
- **Single compilation unit** — the project has no `.asmdef`. Everything under `Assets/Scripts` is `Assembly-CSharp`. Introducing assembly definitions is a deliberate restructuring, not a side effect.
- Controller-first is binding for gameplay, menus and build flows.
- Localization: add a `_xDe` sibling field and resolve via `LocalizationService.Resolve(en, de)`. Put real German in the **C# field default**, not only in the asset — fields added after an asset was last saved deserialize empty and silently fall back to English.
- Comments explain *why*, especially non-obvious constraints. Do not narrate what the code already says.
- Prefer fixing the root cause over adding a workaround, and say plainly when something is unverified.

## Workflow

- Commit messages: what changed and **why**, in prose. Reference the playtest report or spec that motivated it.
- Log substantive sessions in `Docs/OPEN_TOPICS.md`; keep `Docs/OPEN_TOPICS.md` current.
- Do not delete the user's files (including `be_*` art backups) without asking.
- Never claim something is verified if it was only reasoned about. Distinguish "compiles" from "runs" from "plays correctly".
