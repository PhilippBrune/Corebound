# COREBOUND Workflow 02 — Bugfix

## Purpose

Use this workflow when correcting an existing defect.

## Step 1 — Reproduce / Inspect

Determine:

- reported problem
- expected behavior
- actual behavior
- reproduction conditions
- affected files
- affected systems

Do not assume the cause.

## Step 2 — Identify Owner

Routing:

- Architecture problem → `01_architect`
- Gameplay bug → `02_gameplay_engineer`
- System / persistence bug → `03_systems_engineer`
- UI bug → `04_ui_engineer`
- World problem → `05_world_designer`
- NPC behavior → `06_npc_designer`
- Quest behavior → `07_quest_designer`

If the problem is unclear, use:

`08_reviewer`

for diagnosis before implementation.

## Step 3 — Inspect Existing Implementation

Inspect the relevant implementation before editing.

Determine:

- root cause
- affected dependencies
- possible side effects
- whether the issue is isolated or architectural

## Step 4 — Fix

Implement the smallest appropriate correction.

Do not:

- rewrite unrelated systems
- perform opportunistic refactoring
- change architecture without justification

## Step 5 — Review

Route the fix to:

`08_reviewer`

Reviewer verifies:

- root cause addressed
- no unnecessary changes
- architecture remains valid
- no obvious regression

## Step 6 — QA

Route to:

`09_qa`

QA verifies:

- original bug
- expected behavior
- relevant regression cases

## Step 7 — Documentation

If the bug exposed or changed a documented behavior:

Route to:

`10_documentation`

## Step 8 — Handoff

Report:

- root cause
- fix
- files changed
- verification
- remaining risks
- recommended next agent
- 1–2 next tasks

Do not automatically continue.