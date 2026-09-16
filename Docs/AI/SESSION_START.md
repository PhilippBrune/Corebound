# COREBOUND — SESSION START

## Purpose

Initialize the COREBOUND AI development environment at the beginning of a session.

---

## Initialization

Read:

1. `Docs/AI/MASTER_PROMPT.md`
2. `Docs/AI/system.md`
3. `Docs/AI/rules.md`
4. `Docs/AI/context.md`

Then determine the current task.

Do not automatically load every specification or agent document.

Load only what is relevant to the current task.

---

## Task Classification

Classify the request as one or more of:

- New Feature
- Bug Fix
- Refactoring
- Architecture
- Documentation
- Review
- QA
- World Design
- NPC Design
- Quest Design
- UI
- Gameplay

---

## Agent Selection

Use:

`Docs/AI/Registry/routing_rules.md`

Then load the appropriate role from:

`Docs/AI/agents/`

Before substantial work state:

**Current Role:**  
**Reason:**  
**Relevant Specs:**  
**Task:**

---

## Workflow Selection

Select the applicable workflow from:

`Docs/AI/workflows/`

Follow the workflow before implementing.

---

## Workspace Verification

Before making project-specific claims:

- inspect the actual workspace
- inspect relevant files
- verify paths
- verify existing implementation

Documentation alone does not prove that an implementation exists.

---

## Implementation

If the user has authorized implementation:

1. Inspect relevant code.
2. Check dependencies.
3. Follow the selected workflow.
4. Make the smallest appropriate change.
5. Inspect changed files.
6. Verify what can actually be verified.

If implementation was not requested, do not modify files.

---

## Completion

At the end of the task:

1. Report what was actually done.
2. List changed files.
3. Report actual verification.
4. Report unresolved issues.
5. Recommend exactly one next agent.
6. Recommend 1–2 next tasks.
7. State whether user approval is required.

Do not automatically execute those next tasks.