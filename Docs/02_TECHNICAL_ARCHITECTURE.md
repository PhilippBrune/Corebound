# Technical Architecture

Version: 1.0

---

# Goal

Dieses Dokument beschreibt die technische Architektur des Projekts.

Alle Implementierungen müssen sich an dieser Struktur orientieren.

---

# Engine

Unity 6

Universal Render Pipeline (URP)

2D

---

# Programming Language

C#

.NET Standard gemäß Unity-Version

---

# Programming Principles

- SOLID
- Composition over Inheritance
- Single Responsibility
- Dependency Injection (wo sinnvoll)
- Event Driven
- Data Driven

---

# Folder Structure

Assets/

    Art/

        Animations/

        Audio/

        Fonts/

        Materials/

        Prefabs/

        Sprites/

        UI/

        VFX/

    Gameplay/

        Cards/

        Combat/

        Enemies/

        Factory/

        Player/

        Resources/

        Towers/

        Weapons/

        Waves/

    Managers/

    ScriptableObjects/

    Scenes/

    Scripts/

    Settings/

    Tests/

---

# Gameplay Structure

Gameplay besteht aus unabhängigen Systemen.

Player

↓

Combat

↓

Weapons

↓

Cards

↓

Resources

↓

Towers

↓

Enemies

↓

Wave System

↓

Boss System

---

# Script Philosophy

MonoBehaviours enthalten möglichst wenig Logik.

Komplexe Berechnungen gehören in normale C# Klassen.

---

# Data Driven Design

Spielinhalte werden über ScriptableObjects definiert.

Zum Beispiel:

- Karten
- Waffen
- Gegner
- Türme
- Bosse

Balancing soll möglichst ohne Codeänderungen möglich sein.

---

# Event System

Systeme kommunizieren über Events.

Nicht direkt miteinander.

Beispiele:

OnEnemyKilled

OnWaveStarted

OnCardSelected

OnTowerBuilt

OnBossSpawned

---

# Managers

Manager koordinieren Systeme.

Sie enthalten keine Spiellogik.

Beispiele:

GameManager

WaveManager

SaveManager

AudioManager

UIManager

---

# Save System

Getrennte Speicherung für:

Meta Progression

Spielstände

Einstellungen

Statistiken

---

# UI Architecture

Gameplay und UI dürfen nicht voneinander abhängen.

UI reagiert ausschließlich auf Events.

---

# Input System

Unity Input System

Controller First

Mouse & Keyboard vollständig unterstützt

Rebinds geplant

---

# Testing

Neue Systeme sollen möglichst:

- Unit Tests besitzen
- leicht isoliert testbar sein
- wenige Abhängigkeiten haben

---

# Performance Goals

Ziel:

- 60 FPS Minimum
- 120 FPS empfohlen

Auch bei:

- vielen Gegnern
- vielen Projektilen
- vielen Türmen

---

# Asset Rules

Assets werden logisch gruppiert.

Keine doppelten Prefabs.

Keine ungenutzten Dateien.

Klare Benennung.

---

# Naming Convention

Classes

PascalCase

Methods

PascalCase

Private Fields

_camelCase

Properties

PascalCase

ScriptableObjects

<Name>Data

Interfaces

I<Name>

Enums

PascalCase

---

# Logging

Debug.Log ausschließlich während Entwicklung.

Für spätere Builds eigenes Logging-System.

---

# AI Development Rules

KI-Agenten dürfen:

✔ Neue Features hinzufügen

✔ ScriptableObjects erweitern

✔ Tests ergänzen

✔ Dokumentation aktualisieren

Nicht erlaubt:

✘ Architektur ändern

✘ Bestehende APIs ohne Spezifikation verändern

✘ Zirkuläre Abhängigkeiten erzeugen

✘ Geschäftslogik in UI verschieben

---

# Scalability

Das Projekt soll problemlos erweitert werden können um:

- neue Karten
- neue Türme
- neue Waffen
- neue Gegner
- neue Bosse
- neue Ressourcen
- neue Spielmodi

ohne bestehende Systeme grundlegend umzubauen.

---

# Long Term Goal

Nach Abschluss der Early-Access-Version sollen neue Inhalte überwiegend datengetrieben ergänzt werden können, ohne Kernsysteme ändern zu müssen.