# COREBOUND Workflow 05 — Design Decision

## Purpose

Use this workflow when a significant design or architecture decision must be made before implementation.

## Step 1 — Define Decision

Document:

- decision required
- current situation
- problem
- constraints
- affected systems
- relevant specifications

Do not implement yet.

## Step 2 — Determine Domain

Route to the appropriate specialist:

- Architecture → `01_architect`
- Gameplay → `02_gameplay_engineer`
- Systems → `03_systems_engineer`
- UI → `04_ui_engineer`
- World → `05_world_designer`
- NPC → `06_npc_designer`
- Quest → `07_quest_designer`

If multiple systems are affected, `01_architect` coordinates the decision.

## Step 3 — Inspect

Inspect:

- existing implementation
- existing specifications
- dependencies
- related systems
- Unity constraints

Do not invent missing project information.

## Step 4 — Produce Options

For significant decisions provide:

### Option A

Description:

Advantages:

Disadvantages:

Risks:

### Option B

Description:

Advantages:

Disadvantages:

Risks:

If only one valid option exists, explicitly explain why.

## Step 5 — Recommendation

Provide:

- recommended option
- reasoning
- affected systems
- implementation implications
- documentation implications

Do not implement the decision automatically if user approval is required.

## Step 6 — Review

For significant technical decisions:

`08_reviewer`

reviews the proposal.

For architectural decisions:

`01_architect`

has final architectural responsibility.

## Step 7 — Documentation

After approval:

`10_documentation`

records the decision in the appropriate documentation.

## Step 8 — Implementation

Only after approval:

Route to the responsible implementation agent.

Possible agents:

- `02_gameplay_engineer`
- `03_systems_engineer`
- `04_ui_engineer`

## Step 9 — Verification

After implementation:

`08_reviewer`
→
`09_qa`

## Step 10 — Handoff

Report:

- decision
- selected option
- reason
- affected systems
- implementation status
- documentation status
- recommended next agent
- 1–2 next tasks

Do not automatically start the next task.