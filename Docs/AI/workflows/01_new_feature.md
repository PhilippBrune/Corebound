# COREBOUND Workflow 01 — New Feature

## Purpose

Use this workflow when adding a new gameplay, system, UI, world, NPC or quest feature.

## Workflow

### Step 1 — Understand

Identify:

- requested feature
- player-facing goal
- affected systems
- relevant specifications
- dependencies
- acceptance criteria

Do not implement yet.

### Step 2 — Select Primary Agent

Select exactly one primary agent.

Routing:

- Architecture → `01_architect`
- Gameplay → `02_gameplay_engineer`
- Systems / infrastructure → `03_systems_engineer`
- UI → `04_ui_engineer`
- World → `05_world_designer`
- NPC → `06_npc_designer`
- Quest → `07_quest_designer`

If the feature spans multiple domains, choose the agent responsible for the primary design decision.

### Step 3 — Inspect

The selected agent must inspect:

- relevant specifications
- existing implementation
- related scenes
- related prefabs
- dependencies
- existing patterns

Do not create duplicate systems.

### Step 4 — Plan

Create a concise implementation plan containing:

1. Goal
2. Existing systems involved
3. Files likely affected
4. Dependencies
5. Risks
6. Verification approach

### Step 5 — Implement

Implement only the requested feature.

Do not perform unrelated refactoring.

### Step 6 — Review

Route the completed implementation to:

`08_reviewer`

The Reviewer checks:

- specification compliance
- architecture
- scope
- maintainability
- Unity/C# correctness

### Step 7 — Verify

Route to:

`09_qa`

QA verifies actual behavior where possible.

### Step 8 — Document

If the feature changes project knowledge, specifications or architecture:

Route to:

`10_documentation`

### Step 9 — Handoff

The current agent must recommend:

- exactly one next agent
- reason
- 1–2 next tasks

Do not automatically execute those tasks.

## Standard Flow

Designer / Engineer
→ Reviewer
→ QA
→ Documentation

Architectural changes:

Architect
→ relevant implementation agent
→ Reviewer
→ QA
→ Documentation