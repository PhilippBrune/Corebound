---
document: GLOSSARY
version: 1.0
status: Active
priority: High
---

# Project Glossary

## Purpose

Dieses Dokument definiert die offiziellen Begriffe des Projekts.

Alle Entwickler und KI-Agenten MÜSSEN diese Begriffe konsistent verwenden.

Wenn ein neuer Begriff eingeführt wird, muss er hier ergänzt werden.

---

# Core Concepts

## Game

Das vollständige Spiel.

Beinhaltet:

- Runs
- Meta Progression
- Menüs
- Einstellungen
- Save System

---

## Run

Ein kompletter Spieldurchlauf.

Start:

- Spieler wählt Startausrüstung
- Run beginnt

Ende:

- Spieler gewinnt
- Spieler verliert

Ein Run dauert ungefähr 60–70 Minuten.

---

## Stage

Eine größere Spieleinheit innerhalb eines Runs.

Eine Stage besteht aus:

- mehreren Waves
- Belohnungen
- eventuell Elite Gegnern
- einem Boss

---

## Wave

Eine einzelne Gegnerphase.

Ablauf:

Start Wave

↓

Gegner erscheinen

↓

Spieler kämpft

↓

Gegner besiegt

↓

Belohnung

↓

Nächste Entscheidung

---

## Boss

Ein besonderer Enemy am Ende einer Stage.

Ein Boss:

- schliesst eine Stage ab
- nutzt bestehende Combat-Regeln
- besitzt klare Phasen und Pattern
- loest nach Sieg einen hochwertigen Reward-Moment aus

---

## Elite

Eine staerkere Variante eines bestehenden Enemy.

Ein Elite:

- bleibt an die Rolle des Base Enemy gebunden
- nutzt ein Stat-Paket
- darf maximal eine wiederverwendbare Pattern-Variation besitzen
- erscheint ueber Wave- oder Boss-Spawn-Daten

---

## Arena

Der aktuelle Spielbereich.

Enthält:

- Spieler
- Core
- Gegner
- Türme
- Ressourcen

---

## Core

Das zentrale Objekt, das verteidigt werden muss.

Wenn der Core zerstört wird:

Run endet.

---

# Player Terms

## Player

Die steuerbare Spielfigur.

Verantwortlich für:

- Bewegung
- Kampf
- Ressourcen sammeln
- Platzieren von Türmen

---

## Build

Die aktuelle Kombination aller aktiven Verbesserungen.

Besteht aus:

- Waffen
- Karten
- Türmen
- Relikten
- Boni

---

## Relic

Eine seltene, passive und run-weite Modifikation.

Relics sind seltener und buildprägender als normale Card-Auswahlen.

Sie erweitern bestehende Systeme wie Weapon, Tower, Resource oder Combat.

---

## Synergy

Eine Kombination verschiedener Systeme, die gemeinsam stärker werden.

Beispiel:

Feuerkarte

+

Explosionsturm

=

Feuerexplosionen

---

# Combat Terms

## Weapon

Die aktive Angriffsart des Spielers.

Beispiele:

- Projektilwaffe
- Nahkampf
- Energieangriff

---

## Projectile

Ein Objekt, das Schaden verursacht und sich durch die Welt bewegt.

Besitzt:

- Schaden
- Geschwindigkeit
- Größe
- Verhalten

---

## Damage

Schadenswert eines Angriffs.

---

## Status Effect

Ein zeitlicher Effekt.

Beispiele:

- Burn
- Freeze
- Poison
- Slow

---

## Pattern

Ein wiederverwendbares Angriffs- oder Bewegungsmuster.

Pattern werden ueber Daten definiert und duerfen keine neuen Kernregeln erzwingen.

---

# Card System

## Card

Eine dauerhafte Modifikation innerhalb eines Runs.

Karten werden nicht aktiv ausgespielt.

Sie verändern Spielsysteme.

---

## Card Rarity

Seltenheitsstufe einer Karte.

Beispiel:

- Common
- Rare
- Epic
- Legendary

---

## Legendary Card

Eine extrem starke Karte.

Hat große Auswirkungen auf Builds.

Soll selten erscheinen.

---

# Tower Defense Terms

## Tower

Eine platzierbare Verteidigungseinheit.

Türme:

- greifen Gegner an
- unterstützen Spieler
- erzeugen Ressourcen

---

## Tower Slot

Ein verfügbarer Platz für einen Turm.

---

## Tower Upgrade

Verbesserung eines vorhandenen Turms.

---

# Factory Terms

## Resource

Eine sammelbare oder erzeugte Ressource.

Verwendung:

- Türme bauen
- Karten kaufen
- Upgrades

---

## Production Chain

Eine Abfolge von Systemen, die Ressourcen erzeugen und verwenden.

Beispiel:

Tower

↓

Resource

↓

Card Purchase

↓

Tower Upgrade

---

## Factory System

Das System aus:

- Ressourcenerzeugung
- Ressourcenverbrauch
- Automatisierung

Factory bedeutet nicht vollständige Automation wie Factorio.

---

# Economy Terms

## Shop

Ein Menü oder Ort zum Kaufen von:

- Karten
- Upgrades
- Verbesserungen

---

## Currency

Eine Ressource mit Kaufwert.

Beispiel:

Gold

---

# Meta Progression

## Meta Progression

Fortschritt außerhalb eines Runs.

Bleibt nach einem Run erhalten.

Beispiele:

- neue Karten freischalten
- neue Waffen
- neue Startoptionen

---

## Unlock

Ein dauerhaft freigeschalteter Inhalt.

---

# Technical Terms

## ScriptableObject

Unity Datencontainer.

Wird verwendet für:

- Karten
- Waffen
- Türme
- Gegner

---

## Component

Ein wiederverwendbares Unity Verhalten.

---

## Event

Eine Nachricht zwischen Systemen.

Beispiele:

OnEnemyKilled

OnWaveStarted

OnCardSelected

---

## Agent

Eine KI-Rolle mit klarer Verantwortung.

Beispiele:

- Gameplay Agent
- Code Agent
- Art Agent
- Testing Agent

---

# Naming Rules

Diese Begriffe dürfen nicht ersetzt werden.

Beispiele:

Nicht:

"Level"

Verwenden:

"Stage"

Nicht:

"Skill Card"

Verwenden:

"Card"

Nicht:

"Base"

Verwenden:

"Core"

---

# New Terms

Neue Begriffe müssen hier ergänzt werden.
