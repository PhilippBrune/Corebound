---
name: Unity CSharp
globs:
  - "**/*.cs"
  - "**/*.unity"
  - "**/*.prefab"
  - "**/*.asset"
alwaysApply: false
description: Unity and C# development rules for COREBOUND.
---

# Unity C# Rules

COREBOUND uses Unity and C#.

Never generate Godot code.

Never create .gd files.

Use appropriate Unity concepts:

- Scenes
- GameObjects
- Components
- MonoBehaviour
- ScriptableObject
- Prefabs
- SerializedField

Use existing project conventions.

Before creating a class, search for existing related implementations.

Prefer:

- focused classes
- clear responsibilities
- composition
- explicit dependencies
- meaningful names

Avoid:

- giant MonoBehaviours
- unnecessary inheritance
- hidden dependencies
- unnecessary global state
- duplicated systems

C# files use PascalCase names.

Example:

MainMenuController.cs
InventorySystem.cs
QuestDefinition.cs