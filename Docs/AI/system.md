# COREBOUND — AI SYSTEM

## Purpose

Defines the architecture and operating model of the COREBOUND local AI development environment.

---

## Environment

- Unity
- C#
- Visual Studio Code
- Continue
- Ollama
- Local LLMs

---

## AI Architecture

The configured local LLM acts as the primary development agent and orchestrator.

The agent uses workspace tools to:

- inspect files
- search code
- inspect project structure
- read specifications
- reason about architecture
- make authorized changes
- verify changes

---

## Specialist Roles

Specialist roles are defined under:

`Docs/AI/agents/`

Available roles:

1. Architect
2. Gameplay Engineer
3. Systems Engineer
4. UI Engineer
5. World Designer
6. NPC Designer
7. Quest Designer
8. Reviewer
9. QA
10. Documentation

These are workflow roles.

They are NOT separate AI instances.

---

## Model vs Role

Model selection and agent-role selection are separate concepts.

### Model

Determined by the local AI configuration.

### Role

Determined by the COREBOUND task and routing rules.

Never assume that changing the role requires changing the LLM.

---

## Operating Pipeline

```text
User Request
     ↓
Understand
     ↓
Inspect Workspace
     ↓
Identify Specification
     ↓
Select Role
     ↓
Select Workflow
     ↓
Plan
     ↓
Implement
     ↓
Verify
     ↓
Report
     ↓
Recommend Next Role