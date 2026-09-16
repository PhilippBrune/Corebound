# COREBOUND Workflow 03 — Refactoring

## Purpose

Use this workflow when restructuring existing code without intentionally changing its behavior.

## Step 1 — Define Refactoring Goal

Identify:

- current problem
- desired structure
- intended behavior preservation
- affected systems

## Step 2 — Architecture Check

Route first to:

`01_architect`

if the refactoring affects:

- system boundaries
- dependencies
- communication patterns
- shared infrastructure
- major class responsibilities

Small local refactoring may proceed directly with the responsible engineer.

## Step 3 — Select Implementation Agent

Routing:

- Gameplay code → `02_gameplay_engineer`
- Systems/infrastructure → `03_systems_engineer`
- UI code → `04_ui_engineer`

Design-only refactoring is not implementation.

## Step 4 — Inspect

Before changes:

- inspect all affected files
- identify dependencies
- identify consumers
- identify serialized Unity references
- identify potential scene/prefab impact

## Step 5 — Plan

Document:

1. Current structure
2. Problem
3. Target structure
4. Files affected
5. Behavior that must remain unchanged
6. Verification method

## Step 6 — Implement

Make focused changes.

Do not introduce unrelated features.

Do not silently change public behavior.

## Step 7 — Review

Route to:

`08_reviewer`

Reviewer checks:

- architecture
- behavior preservation
- dependency changes
- unnecessary complexity
- Unity serialization risks

## Step 8 — QA

Route to:

`09_qa`

Verify that intended behavior remains intact.

## Step 9 — Documentation

If architecture changed:

`10_documentation`

must update the relevant documentation.

## Step 10 — Handoff

Recommend exactly one next agent and 1–2 next tasks.