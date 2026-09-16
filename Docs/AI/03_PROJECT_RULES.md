---
document: PROJECT_RULES
version: 1.0
status: Active
priority: Critical
---

# Project Rules

## Purpose

Dieses Dokument definiert die verbindlichen Regeln für die Entwicklung.

Alle Entwickler (Mensch oder KI) MÜSSEN diese Regeln einhalten.

Bei Konflikten gilt folgende Priorität:

1. PROJECT_VISION
2. GAME_DESIGN_PRINCIPLES
3. TECHNICAL_ARCHITECTURE
4. PROJECT_RULES
5. CODING_GUIDELINES
6. Feature Specification

---

# General Rules

## MUST

- Jede Änderung benötigt eine bestehende Spezifikation.
- Neue Features dürfen keine bestehenden Features beschädigen.
- Öffentliche APIs müssen stabil bleiben.
- Jede Änderung muss kompilieren.
- Neue Assets müssen logisch einsortiert werden.
- Änderungen müssen minimal sein.
- Jede neue Klasse besitzt genau eine Verantwortung.

---

## MUST NOT

- Keine Architekturänderungen ohne neue Spezifikation.
- Keine doppelte Logik.
- Keine Copy-Paste Implementierungen.
- Keine Magic Numbers.
- Keine Hardcoded Strings.
- Keine versteckten Seiteneffekte.
- Keine direkte Kommunikation zwischen unabhängigen Systemen.

---

# Gameplay Rules

Gameplay darf niemals:

- unnötig komplex werden
- Controller verschlechtern
- Lesbarkeit reduzieren
- dominante Builds erzwingen

Neue Features müssen mindestens eines verbessern:

- Build Diversity
- Replayability
- Player Choice
- Synergies
- Readability

---

# Code Rules

Neue Klassen müssen:

- klein sein
- testbar sein
- dokumentiert sein

Methoden sollen:

- möglichst unter 30 Zeilen bleiben

Dateien sollen:

- möglichst unter 300 Zeilen bleiben

---

# Performance Rules

Keine:

FindObjectOfType()

FindObjectsOfType()

Resources.Load()

im Gameplay Loop.

Keine LINQ innerhalb von Update().

Keine unnötigen Allocations.

Pooling für:

- Gegner
- Projektile
- Effekte

---

# Documentation Rules

Wenn Gameplay geändert wird:

MUSS

- Spec aktualisiert werden
- Changelog ergänzt werden

---

# Review Rules

Vor Abschluss jeder Aufgabe prüfen:

- Kompiliert?
- Keine Warnings?
- Keine NullReference Risiken?
- Event-Leaks?
- Naming korrekt?
- SOLID eingehalten?
- Spezifikation erfüllt?

---

# AI Rules

Ein KI-Agent darf niemals:

- Architektur verändern
- globale Regeln ignorieren
- fremde Systeme umschreiben
- mehrere Features gleichzeitig implementieren

Ein KI-Agent SOLL:

- möglichst kleine Commits erzeugen
- Änderungen erklären
- Unsicherheiten benennen