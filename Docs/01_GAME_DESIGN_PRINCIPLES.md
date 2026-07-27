# Game Design Principles

Version: 1.0

---

# Purpose

Dieses Dokument definiert die grundlegenden Designprinzipien des Spiels.

Jede neue Mechanik, jeder Gegner, jede Karte und jedes Feature muss sich an diesen Prinzipien orientieren.

Falls eine Idee gegen mehrere Prinzipien verstößt, sollte sie überarbeitet oder verworfen werden.

---

# Vision

Ein hochgradig wiederspielbares Action Roguelite, das Deckbuilding, Tower Defense und Factory-Mechaniken verbindet.

Der Fokus liegt auf interessanten Entscheidungen, starken Builds und hoher Lesbarkeit.

---

# Core Design Pillars

## Gameplay First

Gameplay steht immer über Grafik, Story oder Effekten.

Eine Mechanik ist nur dann gut, wenn sie Spaß macht.

---

## Meaningful Decisions

Der Spieler soll ständig Entscheidungen treffen.

Zum Beispiel:

- Neue Karte wählen
- Ressourcen ausgeben oder sparen
- Neuen Turm bauen
- Risiko eingehen
- Legendäre Karte kaufen

Es darf möglichst selten eine offensichtliche beste Wahl geben.

---

## Build Diversity

Jeder Run soll sich unterschiedlich anfühlen.

Neue Builds entstehen durch Kombinationen aus

- Waffen
- Karten
- Türmen
- Ressourcen
- Relikten

Es soll keine dominante Strategie geben.

---

## Simple Rules

Das Spiel besitzt wenige Grundregeln.

Neue Inhalte entstehen hauptsächlich durch Kombinationen vorhandener Systeme.

Nicht durch immer neue Spezialregeln.

---

## High Synergy

Jedes System soll mit anderen Systemen interagieren.

Beispiele:

Karten verbessern Türme.

Türme erzeugen Ressourcen.

Ressourcen kaufen Karten.

Karten verändern Waffen.

---

## Readability First

Auch bei vielen Gegnern und Effekten muss jederzeit erkennbar sein:

- Wo ist der Spieler?
- Wo befinden sich Gegner?
- Welche Angriffe sind gefährlich?
- Welche Türme sind aktiv?

Gameplay darf niemals durch Effekte verdeckt werden.

---

## Controller First

Das komplette Spiel wird primär für Controller entwickelt.

Alle Menüs und Interaktionen müssen ohne Maus funktionieren.

---

## Reward Experimentation

Das Spiel belohnt ungewöhnliche Builds.

Auch vermeintlich schlechte Karten sollen in bestimmten Kombinationen stark werden.

---

## Risk vs Reward

Jede starke Belohnung sollte einen Preis haben.

Zum Beispiel:

- Weniger Leben
- Höhere Gegnerstärke
- Langsamer Angriff
- Höhere Ressourcenkosten

---

## Short Feedback Loop

Spieler sollen regelmäßig belohnt werden.

Zum Beispiel:

- Ressourcen
- Karten
- Neue Türme
- Level Ups
- Shop

Alle 1–3 Minuten sollte eine interessante Entscheidung entstehen.

---

# Difficulty Philosophy

Das Spiel wird schwieriger durch:

- neue Gegnertypen
- bessere Gegnerkombinationen
- mehr Druck
- komplexere Entscheidungen

Nicht durch künstlich erhöhte Lebenspunkte.

---

# Content Philosophy

Neue Inhalte sollen bestehende Systeme erweitern.

Beispiele:

Neue Karte ✔

Neuer Turm ✔

Neue Synergie ✔

Komplett neues Minispiel ✘

---

# Technical Philosophy

Alle Systeme sollen:

- modular
- datengetrieben
- testbar
- erweiterbar

sein.

Gameplaydaten gehören in ScriptableObjects.

---

# AI Development Rules

KI-Agenten sollen:

- bestehende Systeme erweitern
- keine Architektur verändern
- keine globalen Abhängigkeiten erzeugen
- Spezifikationen einhalten
- keine Logik duplizieren

---

# Success Criteria

Eine neue Mechanik ist erfolgreich, wenn:

- sie leicht verständlich ist
- neue Entscheidungen erzeugt
- bestehende Systeme erweitert
- Synergien schafft
- Controller-freundlich ist
- wiederverwendbar ist