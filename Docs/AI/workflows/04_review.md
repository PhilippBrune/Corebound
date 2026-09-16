# COREBOUND Workflow 04 — Review

## Purpose

Use this workflow to review existing work without automatically modifying it.

## Step 1 — Identify Review Target

Determine:

- files
- feature
- system
- specification
- requested review scope

## Step 2 — Select Reviewer

Primary agent:

`08_reviewer`

The Reviewer may route findings to the appropriate specialist.

## Step 3 — Inspect

Review:

- specifications
- implementation
- dependencies
- architecture
- Unity configuration where relevant
- changed files
- relevant scenes/prefabs

## Step 4 — Evaluate

Check:

### Correctness

Does implementation match the requested behavior?

### Architecture

Does it fit the existing architecture?

### Scope

Were unrelated changes introduced?

### C# / Unity

Is the implementation appropriate for the project's Unity/C# environment?

### Maintainability

Is the code understandable and appropriately structured?

### Dependencies

Are dependencies explicit and justified?

### Documentation

Does documentation need updating?

## Step 5 — Findings

Every finding should contain:

- severity
- file
- location
- evidence
- problem
- recommendation

## Step 6 — Routing

Route findings according to domain:

- architecture → `01_architect`
- gameplay → `02_gameplay_engineer`
- systems → `03_systems_engineer`
- UI → `04_ui_engineer`
- world → `05_world_designer`
- NPC → `06_npc_designer`
- quest → `07_quest_designer`

## Step 7 — Verification

If implementation changes are made after review:

Route to:

`09_qa`

## Step 8 — Documentation

If required:

`10_documentation`

## Output

Use:

PASS

PASS WITH WARNINGS

CHANGES REQUIRED

BLOCKED

Never claim testing that was not actually performed.