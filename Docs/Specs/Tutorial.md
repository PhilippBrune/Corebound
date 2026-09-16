# Tutorial Specification

## Purpose

Definiert das Tutorial als Onboarding für die Kernsysteme des Games.

Das Tutorial soll Movement, Combat, Resource-Sammeln, Tower-Nutzung und Card-Entscheidungen vermitteln, ohne ein separates Spielgefühl zu erzeugen.

## Design Goals

- Schnelles, handlungsbasiertes Lernen statt langer Erklärungstexte
- Vollständige Controller-Führung für Erstspieler
- Integration in bestehende Run-Systeme
- Minimale Unterbrechung des Spielflusses
- Erweiterbarkeit für spätere zusätzliche Lernschritte

## Gameplay

Das Tutorial führt den Player schrittweise durch die ersten Kernaktionen des Games.

Es lehrt durch tatsächliche Interaktion in einer kontrollierten Situation, nicht durch abstrakte Menüseiten allein.

Vermittelt werden mindestens Movement, Weapon-Nutzung, Resource-Sammeln, Tower-Platzierung, Card-Auswahl und das Verständnis von Wave-Druck auf den Core.

Das Tutorial kann als geführter Erstlauf, als optionale Einstiegserfahrung oder als kurzer separater Flow angeboten werden, solange es dieselben Systeme verwendet wie der eigentliche Run.

## Rules

- Tutorial verwendet bestehende Systeme und keine exklusiven Sondermechaniken.
- Jeder Tutorial-Schritt verlangt eine konkrete Spieleraktion.
- Hinweise müssen knapp, controller-freundlich und kontextbezogen sein.
- Das Tutorial darf nach der ersten erfolgreichen Nutzung optional übersprungen oder erneut gestartet werden.
- Tutorial-Schritte dürfen den Player führen, aber nicht mit langen, ununterbrechbaren Textblöcken blockieren.
- Rewards oder Freischaltungen aus dem Tutorial müssen mit Meta Progression konsistent sein.
- Neue Tutorial-Inhalte erweitern vorhandene Step- und Trigger-Modelle.

## Data Model

Tutorial-Daten werden über ScriptableObjects beschrieben.

- TutorialSequenceData: Reihenfolge und Kontext eines Tutorials
- TutorialStepData: einzelne Lernschritte mit Ziel, Text und Erfolgsbedingung
- TutorialTriggerData: Event- oder Zustandsauslöser für Fortschritt
- TutorialPromptData: UI-Text, Glyphs und Priorität eines Hinweises
- TutorialRewardData: optionale Belohnungen oder Abschlussmarker

## Technical Design

Ein TutorialService beobachtet Gameplay-Events und aktiviert den nächsten Schritt, sobald die Bedingung erfüllt ist.

Er kommuniziert mit UI und Input über Events wie OnTutorialStarted, OnTutorialStepChanged, OnTutorialPromptShown und OnTutorialCompleted.

Tutorial darf keine direkte Sonderlogik in Combat, Player oder Tower-Systeme einbauen, sondern nur deren reguläre Events nutzen.

Speicherbare Tutorial-Zustände werden über SaveSystem oder Meta Progression verwaltet.

## Dependencies

- Player
- Input
- Combat
- Resources
- Towers
- Cards
- UI
- Audio
- MainMenu
- SaveSystem

## UI

Tutorial-UI zeigt mindestens:

- aktuellen Lernhinweis
- relevante Controller-Glyphs
- klaren Fortschritt zum Schrittabschluss
- Option zum Überspringen oder Wiederholen, wenn vorgesehen

## Audio

Tutorial-Audio unterstützt Hinweise, Bestätigungen und erfolgreiche Abschlüsse dezent.

Es darf nie konkurrierende Combat-Warnungen überdecken.

## Visual

Tutorial-Hinweise müssen kontextnah eingeblendet werden und den Blick auf die relevante Aktion lenken.

Visuelle Hervorhebungen sollen sparsam und klar sein, damit der Player weiterhin die Arena lesen lernt.

## Balancing

Das Tutorial soll die Hürde senken, ohne erfahrene Spieler unnötig auszubremsen.

Es muss schnell genug sein, um in den regulären Spielfluss zu führen, aber gründlich genug, um die Kernentscheidungen verständlich zu machen.

Tutorial-Belohnungen dürfen nützlich sein, aber kein Pflichtgrind-System erzeugen.

## Open Questions

- Ein dedizierter Playtest-Pass zur Wirksamkeit der einzelnen Schritte (lernt ein echter Erstspieler tatsächlich das Kernsystem pro Schritt?) steht noch aus — reine Code-/Datenprüfung kann das nicht ersetzen.
- Bekommt der Player für den Tutorial-Abschluss einen kleinen persistenten Reward, oder bleibt der Wert rein im gelernten Systemverständnis?

Festgelegte Entscheidungen:

- Das Tutorial ist als geführter Erstlauf umgesetzt, der dieselben Systeme wie ein regulärer Run nutzt (kein separates Minispiel) — das erfüllt Vision's Design Principle „Low Complexity: neue Inhalte durch Kombination vorhandener Systeme".
- `TutorialSequenceData`/`TutorialStepData`/`TutorialService` sind alle Schritte des ersten Slice zwingend (Movement, Weapon, Resource, Tower, Card, Core-Defense-Verständnis); ein optionales Überspringen bereits gesehener Tutorials ist über `HasSeenTutorial` vorgesehen.

## Acceptance Criteria

- Das Tutorial vermittelt die Kernsysteme über echte Spielaktionen.
- Alle Hinweise und Schritte sind controller-freundlich und kontextbezogen.
- Tutorial verwendet dieselben Kernsysteme wie der reguläre Run.
- Fortschritt und optionale Wiederholung sind sauber speicherbar.
- Zusätzliche Tutorial-Schritte lassen sich über Daten ergänzen.
