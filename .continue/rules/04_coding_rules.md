---
name: COREBOUND Coding Rules
globs:
  - "**/*.cs"
alwaysApply: false
description: C# coding standards for COREBOUND.
---

# Coding Rules

Write readable, maintainable C#.

Use meaningful names.

Avoid meaningless names such as:

- temp
- data2
- manager2
- newSystem

Keep methods focused.

Keep classes focused.

Do not create giant classes containing unrelated systems.

Do not silently swallow exceptions.

Do not invent APIs, classes or namespaces.

Do not use pseudocode as finished implementation.

Do not refactor unrelated code during feature work.

Avoid unnecessary allocations in frequently executed loops such as Update.

Do not claim code was tested unless it was actually tested.