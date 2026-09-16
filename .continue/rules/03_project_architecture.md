---
name: COREBOUND Architecture
alwaysApply: false
description: Architecture rules for COREBOUND systems and significant structural changes.
---

# Architecture Rules

Before creating a new system:

1. Search the existing project.
2. Identify related classes and systems.
3. Identify existing communication patterns.
4. Check relevant project documentation.

Do not create duplicate systems.

Keep responsibilities separated:

- gameplay
- UI
- presentation
- data
- persistence
- input
- audio
- infrastructure

Prefer explicit dependencies.

Avoid unnecessary:

- global state
- static mutable state
- hidden dependencies
- circular dependencies

Do not make significant architecture changes without explaining:

1. Current architecture
2. Problem
3. Proposed change
4. Affected systems
5. Documentation impact