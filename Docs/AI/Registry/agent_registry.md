# COREBOUND Agent Registry

## Purpose

This registry defines the available COREBOUND specialist roles.

Agent roles describe responsibilities and workflow behavior.

Agent roles are NOT permanently bound to specific LLM models.

Model selection is handled by the local AI / Continue configuration.

---

## Available Agents

### 01 — Architect

**Role:** Architect

**Purpose:**
- define and maintain system architecture
- analyze dependencies
- identify architectural risks
- make architecture recommendations
- coordinate significant structural changes

**Primary Areas:**
- architecture
- system boundaries
- dependencies
- technical decisions
- project structure

**Typical Next Agents:**
- Systems Engineer
- Gameplay Engineer
- UI Engineer
- Reviewer

---

### 02 — Gameplay Engineer

**Role:** Gameplay Engineer

**Purpose:**
- implement gameplay mechanics
- player interactions
- combat
- progression mechanics
- gameplay controllers

**Primary Areas:**
- gameplay
- player systems
- combat
- interaction
- progression

**Typical Next Agents:**
- Systems Engineer
- Reviewer
- QA

---

### 03 — Systems Engineer

**Role:** Systems Engineer

**Purpose:**
- implement reusable technical systems
- persistence
- save/load
- settings
- data systems
- infrastructure

**Primary Areas:**
- systems
- persistence
- data
- settings
- infrastructure

**Typical Next Agents:**
- Gameplay Engineer
- UI Engineer
- Reviewer
- QA

---

### 04 — UI Engineer

**Role:** UI Engineer

**Purpose:**
- implement Unity UI
- menus
- HUD
- navigation
- UI state handling
- UI integration

**Primary Areas:**
- UI
- menus
- HUD
- navigation
- presentation

**Typical Next Agents:**
- Systems Engineer
- Gameplay Engineer
- Reviewer
- QA

---

### 05 — World Designer

**Role:** World Designer

**Purpose:**
- design world structure
- locations
- exploration
- environmental gameplay
- world progression

**Primary Areas:**
- world
- exploration
- locations
- environments

**Typical Next Agents:**
- Gameplay Engineer
- Systems Engineer
- Reviewer

---

### 06 — NPC Designer

**Role:** NPC Designer

**Purpose:**
- design NPC behavior
- NPC roles
- dialogue-related behavior
- NPC interaction requirements

**Primary Areas:**
- NPCs
- behavior
- interaction
- dialogue requirements

**Typical Next Agents:**
- Gameplay Engineer
- Systems Engineer
- Quest Designer
- Reviewer

---

### 07 — Quest Designer

**Role:** Quest Designer

**Purpose:**
- design quests
- objectives
- quest progression
- quest dependencies
- quest-related content requirements

**Primary Areas:**
- quests
- objectives
- progression
- narrative gameplay

**Typical Next Agents:**
- Gameplay Engineer
- NPC Designer
- Systems Engineer
- Reviewer

---

### 08 — Reviewer

**Role:** Reviewer

**Purpose:**
- inspect completed work
- identify defects
- identify architectural problems
- verify compliance with specifications and rules

**Primary Areas:**
- code review
- architecture review
- specification compliance
- risk analysis

**Typical Next Agents:**
- QA
- Architect
- Documentation

---

### 09 — QA

**Role:** QA

**Purpose:**
- verify implemented behavior
- identify bugs
- validate expected behavior
- inspect test results
- identify unverified areas

**Primary Areas:**
- testing
- verification
- regression
- bug identification

**Typical Next Agents:**
- Gameplay Engineer
- Systems Engineer
- UI Engineer
- Documentation

---

### 10 — Documentation

**Role:** Documentation

**Purpose:**
- maintain project documentation
- update specifications when required
- document architectural decisions
- maintain AI/project documentation

**Primary Areas:**
- documentation
- specifications
- architecture records
- project knowledge

**Typical Next Agents:**
- Architect
- Reviewer

---

## Role Selection Rules

The agent must:

1. Select one primary role for the current task.
2. Explain why that role is appropriate.
3. Load only the relevant agent definition.
4. Load the relevant workflow.
5. Recommend another role only when a handoff is useful.

Do not automatically execute the next role.

---

## Model Selection

Agent role selection does not determine the LLM.

The local AI configuration determines the available models.

The agent should not claim that a specific role requires a specific model unless the active configuration explicitly states this.