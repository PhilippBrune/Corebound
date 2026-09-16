# COREBOUND — GLOBAL AI RULES

## 1. Engine

COREBOUND is developed with Unity and C#.

Use:

- Unity
- Unity Editor
- C#
- Unity APIs
- Unity-compatible packages and tooling

Never use:

- Godot
- GDScript
- Godot project structures

Never create Godot project files for COREBOUND.

---

## 2. Evidence Before Assumptions

Inspect actual workspace files before making project-specific claims.

Do not infer file contents or implementation state solely from filenames.

If something has not been verified, state:

`Nicht verifiziert.`

Never invent:

- files
- folders
- classes
- scenes
- systems
- APIs
- namespaces
- specifications
- test results

---

## 3. Unity Project Verification

When validating the Unity project, inspect relevant project files.

Useful evidence includes:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`

Do not declare the project valid merely because `Assets/` exists.

---

## 4. Generated Unity Directories

Treat these as Unity-managed/generated:

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`

Do not modify them during normal development.

---

## 5. Specification First

Before implementing a non-trivial feature:

1. Find the relevant specification.
2. Read it.
3. Inspect existing implementation.
4. Identify dependencies.
5. Check architecture impact.
6. Implement only after the above is understood.

If no relevant specification exists:

- state that clearly
- do not invent one
- recommend whether one should be created

---

## 6. Minimal Changes

Do not:

- refactor unrelated code
- rename unrelated files
- rewrite working systems without justification
- introduce packages without justification
- modify generated Unity folders
- alter unrelated scenes or assets

Keep changes targeted and reversible.

---

## 7. Tool Usage

Use workspace tools to inspect project state.

For investigation:

- prefer read-only operations
- inspect tool results before deciding the next operation
- use the simplest suitable search method

Do not use complex regex when a literal search is sufficient.

Tool results are evidence.

Model assumptions are not.

---

## 8. Code Standards

Use:

- C#
- meaningful English identifiers
- focused classes
- explicit responsibilities
- appropriate Unity lifecycle usage
- minimal coupling
- testable logic where practical

Avoid:

- giant MonoBehaviours
- unnecessary global state
- hidden dependencies
- duplicated systems
- meaningless filenames
- pseudocode presented as finished implementation

---

## 9. Editing Rules

Before editing:

1. Read the target file.
2. Understand its role.
3. Check relevant specifications.
4. Identify dependencies.
5. Make the smallest appropriate change.

After editing:

1. Inspect the resulting file.
2. Check obvious compile issues.
3. Verify references and paths.
4. Report what was actually verified.

Never claim Unity compilation or runtime success unless it was actually verified.

---

## 10. Documentation

If implementation changes:

- architecture
- public behavior
- workflows
- important technical decisions

update the relevant documentation.

Do not create duplicate documentation when an existing document is the correct source of truth.

---

## 11. Role Selection

Before substantial work identify:

**Current Role:**  
**Reason:**  
**Relevant Specs:**  
**Task:**

When the task is complete recommend 1–2 next steps.

For each:

- Recommended Role
- Objective
- Reason

Do not automatically execute those next steps.

---

## 12. Language

Communication may be in German.

Project specifications may be in German.

C# identifiers, class names, methods and technical code should normally use English.

---

## 13. User Approval

Request user approval before:

- significant architecture changes
- destructive operations
- deleting project files
- changing core specifications
- introducing major dependencies
- changing established project conventions