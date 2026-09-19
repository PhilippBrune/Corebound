# CLAUDE.md — Corebound

Working rules for AI agents on this project. Everything here is load-bearing: each item exists because getting it wrong already cost a debugging session.

## What this is

COREBOUND — a singleplayer Windows/Steam action roguelite combining direct combat, deckbuilding, tower defense and a focused Factory system. Unity 6.3 LTS (6000.3.10f1), URP 2D, orthographic top-down. Solo developer (German, QWERTZ keyboard) working with AI agents.

**Authoritative documents**, in precedence order:

1. `Docs/00_PROJECT_VISION.md` — the product. Section 19 "Design Boundaries" overrides convenience.
2. `Docs/Specs/*.md` — per-system contracts.
3. `Docs/Slices/*.md` — scoped work packages.
4. `Docs/08_IMPLEMENTATION_BACKLOG.md` — plan and status.
5. `Docs/10_OPEN_POINTS.md` — the live open-items register.

**Treat these docs as claims, not facts.** They have repeatedly drifted from the code in *both* directions — a 2026-09-17 audit found seven "still open" items that were fully implemented, and the vision still states targets the build has since passed or missed. Verify against code before acting on any status claim, and correct the doc when it is wrong.

## Testing

- **Test through `Assets/Scenes/MainMenu.unity` → New Run.** Opening `SampleScene.unity` directly hides a whole class of bug, because the Editor has already loaded the additive canvas scenes before Play starts. Several defects reproduced *only* via MainMenu.
- **Debug stage-select:** F9 / F10 in the Main Menu cycle which stage a New Run starts on. An on-screen label shows the current setting. Use it to reach Stage 2+ without playing through.
- **Synthetic input does not reach Unity's Input System.** `SendInput`/`keybd_event` drive Editor UI (Play button, menus, Hierarchy) but never `Keyboard.current`, so gameplay input cannot be scripted. `InputSystem.QueueStateEvent` updates device state but does not fire bound `InputAction` callbacks either, and `InputTestFixture` needs its own assembly, which this project's asmdef-less layout cannot provide. See the header of `Assets/Editor/InputSimulatorWindow.cs` before re-attempting. **Anything requiring a keypress needs the user to press it.**
- Agents cannot hear audio. Do not guess at audio bugs; ask which sounds.

## Engine gotchas that have already caused bugs

- **`SceneManager.LoadScene(..., Additive)` is not synchronous.** Unity defers it to end of frame, so the new scene's objects do not exist when the call returns — and not during the requesting frame's `Start()` either. Canvas presenters must bind from `AdditiveCanvasSceneLoader.CanvasSceneReady`, never from `Start()` alone.
- **`Destroy()` is deferred; the object stays findable by name for the rest of the frame.** Rebuilding anything resolved via `GameObject.Find` must rename/deactivate the old object first, or the new content gets parented under the doomed one. Edit Mode uses `DestroyImmediate` and will not reproduce this.
- **Runtime art is read as raw bytes from `Assets/StreamingAssets/Art/` first**, falling back to `Assets/Art/` *only inside the Editor*. An unmirrored or stale file therefore looks perfect in the Editor and ships broken. After adding or replacing any art, run **`Corebound > Validate Art Pipeline`**.
- **`StageThemeData` stores texture paths as plain strings.** A renamed file is a silent null, not a broken reference the Editor flags. The same validator checks these.
- **Canvas layout is scene-file-driven.** `RewardCanvas`/`ShopCanvas`/`HUDCanvas` own their own positions; presenters must only *find* elements and repair missing sprites, never reassert anchors, positions or sizes. Several HUD Images intentionally carry no sprite in the scene and receive one at runtime — a partial re-init that skips `CreateRuntimeSprites()` leaves them rendering plain white.
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
- Log substantive sessions in `Docs/SESSION_REPORT_*.md`; keep `Docs/10_OPEN_POINTS.md` current.
- Do not delete the user's files (including `be_*` art backups) without asking.
- Never claim something is verified if it was only reasoned about. Distinguish "compiles" from "runs" from "plays correctly".
