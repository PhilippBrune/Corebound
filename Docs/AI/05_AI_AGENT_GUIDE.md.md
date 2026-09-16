---
document: AI_AGENT_GUIDE
version: 1.0
status: Active
priority: Critical
---

# AI Agent Guide

## Goal

Dieses Dokument beschreibt den Arbeitsablauf aller KI-Agenten.

Alle Agenten arbeiten ausschließlich anhand der Spezifikationen.

---

# Read Order

Vor jeder Aufgabe müssen folgende Dokumente gelesen werden:

1. PROJECT_VISION
2. GAME_DESIGN_PRINCIPLES
3. TECHNICAL_ARCHITECTURE
4. PROJECT_RULES
5. CODING_GUIDELINES
6. Relevante Feature Specs

---

# Workflow

Neue Aufgabe

↓

Spezifikation lesen

↓

Fehlende Informationen identifizieren

↓

Rückfragen stellen

↓

Implementieren

↓

Selbstprüfung

↓

Tests

↓

Dokumentation

↓

Abgabe

---

# Responsibilities

Ein Agent darf:

- Features implementieren
- Bugs beheben
- Dokumentation aktualisieren
- Tests schreiben
- Refactorings durchführen

Ein Agent darf NICHT:

- Architektur verändern
- Feature Scope erweitern
- bestehende Systeme grundlegend umschreiben
- mehrere unabhängige Systeme gleichzeitig ändern

---

# Required Behaviour

Agenten müssen:

- deterministisch arbeiten
- kleine Änderungen erzeugen
- bestehende Muster übernehmen
- Code wiederverwenden
- Spezifikationen einhalten

---

# Response Format

Jede Implementierung enthält:

## Summary

Kurze Beschreibung.

---

## Changed Files

Liste aller Dateien.

---

## Explanation

Warum wurde die Änderung vorgenommen?

---

## Risks

Welche Risiken bestehen?

---

## Tests

Welche Tests wurden durchgeführt?

---

## Follow-up

Empfohlene nächste Schritte.

---

# Missing Information

Wenn Informationen fehlen:

NICHT raten.

Stattdessen:

- fehlende Spezifikation benennen
- konkrete Rückfrage stellen

---

# Refactoring

Erlaubt wenn:

- Verhalten identisch bleibt
- Lesbarkeit verbessert wird
- Performance verbessert wird

Nicht erlaubt:

- Gameplay ändern

---

# Documentation

Nach Gameplay Änderungen:

Spec aktualisieren.

Nach Architekturänderungen:

Architecture aktualisieren.

Nach neuen Features:

Changelog ergänzen.

---

# Code Generation Rules

Immer:

- vollständigen Code erzeugen
- kompilierbaren Code erzeugen
- bestehende Patterns verwenden

Nie:

- Pseudocode
- unvollständige Klassen
- TODO-Kommentare
- Platzhaltermethoden

---

# Decision Priority

Bei Konflikten gilt:

1. PROJECT_VISION
2. GAME_DESIGN_PRINCIPLES
3. TECHNICAL_ARCHITECTURE
4. PROJECT_RULES
5. CODING_GUIDELINES
6. Feature Specification

---

# Quality Checklist

Vor jeder Abgabe prüfen:

- Kompiliert der Code?
- Spezifikation erfüllt?
- Architektur eingehalten?
- Tests vorhanden?
- Keine Warnungen?
- Keine offensichtlichen Bugs?
- Keine unnötigen Abhängigkeiten?
- Dokumentation aktuell?

Nur wenn alle Punkte erfüllt sind, gilt die Aufgabe als abgeschlossen.