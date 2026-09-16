# COREBOUND --- AI BOOTSTRAP

## Objective

Initialize the local AI development workflow for COREBOUND.

## Required Environment

-   Unity
-   Visual Studio Code
-   Continue
-   Ollama

## Startup

1.  Open the COREBOUND project root in VS Code.
2.  Confirm `project.godot` is NOT being used as the engine definition.
3.  Confirm Unity project files are present.
4.  Start the local Ollama service.
5.  Open Continue Agent mode.
6.  Use the configured local agent model.
7.  Load project guidance according to `SESSION_START.md`.

## First Agent Check

The agent must inspect the workspace before proposing implementation.

Use a read-only request such as:

"Inspect the COREBOUND Unity project. Do not modify files. Report the
verified project structure and relevant documentation."

## Expected Behavior

The agent should: - use workspace tools - inspect actual files -
distinguish verified facts from assumptions - identify the appropriate
role - recommend 1--2 next steps

## Important

COREBOUND uses Unity.

Do not create or maintain Godot project files for COREBOUND.
