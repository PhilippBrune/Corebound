# <SPEC NAME>

Version: 1.0
Status: Draft
Owner: AI + Human
Priority: High

---

# Purpose

Beschreibe den Zweck dieser Spezifikation.

Welche Probleme löst dieses System?

Warum existiert es?

---

# Design Goals

Welche Ziele verfolgt dieses System?

Beispiel:

- Einfach verständlich
- Tiefe durch Synergien
- Controller First
- Erweiterbar
- Datengetrieben

---

# Responsibilities

Was gehört zu diesem System?

Was gehört ausdrücklich NICHT dazu?

---

# Gameplay

Wie verhält sich das System aus Sicht des Spielers?

Spielablauf.

Entscheidungen.

Belohnungen.

Risiken.

---

# Technical Design

Wie wird das System technisch umgesetzt?

Beispiel:

- ScriptableObjects
- Prefabs
- Events
- State Machine
- ECS (falls verwendet)

---

# Data Model

Welche Daten besitzt dieses System?

Beispiel

Player

- HP
- Speed
- Damage
- Crit Chance

---

# Rules

Welche festen Regeln gelten?

Beispiele

- Maximal 8 Türme
- Karten können nicht verkauft werden
- Legendäre Karten erscheinen nur nach Bossen

---

# Balancing

Welche Werte können später angepasst werden?

Nicht konkrete Zahlen.

Nur Parameter.

Beispiel

- Damage Multiplier
- Spawn Rate
- Build Cost
- Cooldown

---

# UI

Welche UI benötigt dieses System?

HUD

Icons

Buttons

Tooltips

Animationen

---

# Audio

Welche Sounds werden benötigt?

SFX

Musik

Ambience

Voice

---

# Visuals

Welche Grafiken werden benötigt?

Sprites

VFX

Animationen

Icons

Shader

---

# Dependencies

Welche anderen Systeme werden benötigt?

Welche Systeme verwenden dieses System?

---

# Future Expansion

Wie kann dieses System später erweitert werden?

Neue Karten

Neue Gegner

Neue Ressourcen

Neue Türme

---

# AI Implementation Notes

Hinweise für KI-Agenten.

Code Style

Ordner

Benennung

ScriptableObjects

Events

Tests

Keine Logik außerhalb des Verantwortungsbereichs.

---

# Open Questions

Welche Punkte sind noch ungeklärt?

---

# Acceptance Criteria

Wann gilt diese Spezifikation als vollständig?

Beispiel

- Alle Gameplay-Regeln definiert
- Technische Umsetzung beschrieben
- UI dokumentiert
- Erweiterbarkeit berücksichtigt