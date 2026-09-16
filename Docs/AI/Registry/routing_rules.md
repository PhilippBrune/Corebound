# COREBOUND Agent Routing Rules

## Purpose

Determine the primary COREBOUND agent role for a task.

---

## Routing Principle

Choose exactly one primary role.

Additional roles may be recommended for later work.

Do not execute additional roles automatically.

---

## Architecture

Use:

**Architect**

When the task involves:

- new system architecture
- major structural changes
- dependencies between systems
- architectural decisions
- significant refactoring
- unclear system boundaries

---

## Gameplay

Use:

**Gameplay Engineer**

When the task involves:

- player mechanics
- combat
- interaction
- gameplay logic
- progression mechanics
- gameplay controllers

---

## Systems

Use:

**Systems Engineer**

When the task involves:

- save/load
- persistence
- settings
- data systems
- reusable infrastructure
- cross-system technical services

---

## UI

Use:

**UI Engineer**

When the task involves:

- main menu
- settings menu
- HUD
- UI navigation
- UI state
- UI presentation
- Unity UI implementation

---

## World

Use:

**World Designer**

When the task primarily involves:

- locations
- world structure
- exploration
- environmental design
- world progression

---

## NPC

Use:

**NPC Designer**

When the task primarily involves:

- NPC behavior
- NPC roles
- dialogue behavior
- NPC interactions

---

## Quest

Use:

**Quest Designer**

When the task primarily involves:

- quest design
- objectives
- quest progression
- quest dependencies
- quest content

---

## Review

Use:

**Reviewer**

When the user asks to:

- review code
- inspect an implementation
- identify architectural issues
- evaluate specification compliance
- perform a code/design review

---

## QA

Use:

**QA**

When the task primarily involves:

- testing
- verification
- bug reproduction
- regression checking
- validation

---

## Documentation

Use:

**Documentation**

When the task primarily involves:

- updating documentation
- recording architectural decisions
- updating specifications
- documenting completed implementation

---

## Multi-Domain Tasks

Some tasks affect multiple areas.

Example:

"Implement the Main Menu with Settings, Save/Load and Quit."

Primary role:

**UI Engineer**

Possible follow-up roles:

1. Systems Engineer — save/load and settings integration
2. QA — verify navigation and behavior

Do not execute those roles automatically.

---

## Routing Output

Before substantial work report:

**Current Role:** `<role>`

**Reason:** `<short explanation>`

**Relevant Workflow:** `<workflow>`

**Relevant Specs:** `<files or FILE NOT FOUND>`

**Potential Next Role:** `<role>`

---

## Unknown Tasks

If no role clearly fits:

1. Use **Architect** for analysis.
2. Explain why routing is ambiguous.
3. Do not invent a role.

---

## Role Handoff

After completing a task, recommend exactly one next agent when further work is useful.

Include:

**Recommended Next Agent:** `<role>`

**Reason:** `<why this role should continue>`

**Next Tasks:**
1. `<task>`
2. `<task>`