# Slice Plan Index

Status: Active

Purpose: KI-freundliche Einstiegsseite fuer die naechsten Gameplay-Slices nach dem wiederholbaren Wave-Unterbau.

---

## Required Context

Vor Arbeit an diesen Slices muessen gelesen werden:

- [Project Vision](../00_PROJECT_VISION.md)
- [Game Design Principles](../01_GAME_DESIGN_PRINCIPLES.md)
- [Technical Architecture](../02_TECHNICAL_ARCHITECTURE.md)
- [Project Rules](../AI/03_PROJECT_RULES.md)
- [Coding Guidelines](../AI/04_CODING_GUIDELINES.md)
- [AI Agent Guide](../AI/05_AI_AGENT_GUIDE.md.md)
- [Glossary](../06_GLOSSARY.md)
- [Design Decisions](../07_DESIGN_DECISIONS.md)
- [Implementation Backlog](../08_IMPLEMENTATION_BACKLOG.md)

---

## Ordered Slices

**Update 2026-09-16:** Slices 1-3 sind umgesetzt (siehe [Implementation Backlog](../08_IMPLEMENTATION_BACKLOG.md)); nur Slice 4 (Dynamic Arena) ist noch aktiv in Arbeit.

1. [Bosses Stage 1 Slice](12_BOSSES_STAGE1_SLICE.md) — erledigt
2. [Elite Variants Slice](13_ELITE_VARIANTS_SLICE.md) — erledigt
3. [Wave Boss Balance Slice](14_WAVE_BOSS_BALANCE_SLICE.md) — erledigt
4. [Dynamic Arena Slice](15_DYNAMIC_ARENA_SLICE.md) — in Arbeit

---

## System Specs

- [Stages Spec](../Specs/Stages.md)
- [Waves Spec](../Specs/Waves.md)
- [Enemies Spec](../Specs/Enemies.md)
- [Bosses Spec](../Specs/Bosses.md)
- [Cards Spec](../Specs/Cards.md)
- [Resources Spec](../Specs/Resources.md)
- [Towers Spec](../Specs/Towers.md)
- [Combat Spec](../Specs/Combat.md)
- [UI Spec](../Specs/UI.md)
- [Audio Spec](../Specs/Audio.md)
- [SaveSystem Spec](../Specs/SaveSystem.md)

---

## Slice Rules

- Keine neuen Kernmechaniken ohne dokumentierte Begruendung.
- Bestehende Begriffe aus dem Glossary verwenden.
- Controller First fuer Combat, Intermission, Card-Offer und Tower-Interaktion.
- Inhalte ueber ScriptableObjects definieren, Runtime-Logik klein halten.
- Systeme bleiben eventgetrieben und modular.
- Unity 6 und URP 2D bleiben technische Baseline.
