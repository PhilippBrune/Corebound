---
document: CODING_GUIDELINES
version: 1.0
status: Active
priority: Critical
---

# Coding Guidelines

## Goal

Alle KI-Agenten erzeugen konsistenten Code.

Lesbarkeit hat Vorrang.

---

# Language

C#

Unity 6

---

# General Principles

MUST

- SOLID
- Composition over Inheritance
- DRY
- KISS

SHOULD

- YAGNI
- Clean Code

---

# Naming

Classes

PascalCase

Interfaces

IInterface

Enums

PascalCase

Methods

PascalCase

Properties

PascalCase

Private Fields

_camelCase

Constants

UPPER_CASE

Namespaces

Company.Project.System

---

# File Rules

Eine Datei enthält:

- eine Klasse

Ausnahmen:

- kleine Enums
- kleine Interfaces

---

# Class Rules

Klassen sollen:

- klein
- fokussiert
- testbar

sein.

Keine God Objects.

---

# MonoBehaviour

MonoBehaviours sollen:

- Eingaben empfangen
- Unity Events behandeln
- Komponenten verbinden

Geschäftslogik gehört NICHT in MonoBehaviours.

---

# ScriptableObjects

ScriptableObjects speichern ausschließlich Daten.

Beispiele:

- Karten
- Waffen
- Türme
- Gegner
- Audio
- Balancing

---

# Events

Kommunikation zwischen Systemen erfolgt über Events.

Direkte Referenzen vermeiden.

---

# Update()

Update() nur wenn notwendig.

Ansonsten:

- Coroutines
- Events
- Timer

---

# Nullability

Immer prüfen:

null

bevor Objekte verwendet werden.

---

# Comments

Kommentare erklären:

WARUM

nicht

WAS

XML Kommentare für:

- public Klassen
- public Methoden

---

# Logging

Debug.Log()

nur während Entwicklung.

Keine Debug Logs im Release.

---

# Exceptions

Exceptions nur für:

unerwartete Fehler.

Nicht als Kontrollfluss.

---

# Inspector

Alle serialisierten Felder:

private

[SerializeField]

Keine public Felder ohne Begründung.

---

# Tests

Neue Kernlogik:

SHOULD

Unit Tests besitzen.

---

# Folder Rules

Scripts niemals direkt im Root.

Gameplay logisch gruppieren.

---

# Dependencies

Lose Kopplung.

Abhängigkeiten injizieren.

Singletons nur für:

- GameManager
- AudioManager
- SaveManager

---

# AI Output

Neue Dateien:

vollständig

kompilierbar

ohne Platzhalter

ohne TODO

ohne FIXME

Wenn Informationen fehlen:

Fragen stellen

nicht raten.