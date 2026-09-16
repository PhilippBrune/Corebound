# Player Specification

## Purpose

Definiert den Player als steuerbare Figur des Runs.

Der Player ist die zentrale aktive Einheit in der Arena und verbindet Movement, Combat, Resource-Sammlung und Tower-Interaktion.

## Design Goals

- Sofort reaktionsfähige Steuerung auf Controller und Mouse & Keyboard
- Klare Priorität des Player-Handelns gegenüber passiven Systemen
- Gute Lesbarkeit der Position, Ausrichtung und Trefferlage
- Saubere Erweiterbarkeit über Card-, Weapon- und Meta-Progression-Boni
- Möglichst geringe Logik im Unity-seitigen Input- und Präsentationslayer

## Gameplay

Der Player bewegt sich frei innerhalb der Arena, bekämpft Enemies aktiv und sammelt verfügbare Resources ein.

Zusätzlich platziert oder verbessert der Player Towers an gültigen Tower Slots, sofern die aktuelle Combat-Situation dies zulässt.

Zwischen Waves trifft der Player Build-Entscheidungen über Card-Auswahl, Shop-Nutzung und andere erlaubte Progressionsschritte.

Der Player bleibt der primäre Auslöser für Tempo, Risiko und Positioning innerhalb eines Runs.

## Rules

- Der Player ist immer eindeutig sichtbar und darf nicht von Effekten verdeckt werden.
- Der Player besitzt einen klar definierten Zustandsraum für Movement, Combat, Schaden und Interaktion.
- Core-Zerstörung beendet den Run immer.
- Player-Niederlage erzeugt einen klar definierten Verlustzustand, der mit Combat und SaveSystem konsistent umgesetzt wird.
- Tower-Platzierung und andere Build-Aktionen dürfen den Player nicht in verschachtelte Maus-Workflows zwingen.
- Der Player darf nie durch Card- oder Tower-Builds vollständig automatisiert werden.
- Alle Player-Modifikationen müssen aus bestehenden Systemen stammen, nicht aus separaten Sonderregeln.

## Data Model

Player-Daten werden über ScriptableObjects konfiguriert.

- PlayerData: Basiswerte für Movement, Health, Interaktionsreichweite und Startzustand
- PlayerStatProgressionData: additive und multiplikative Stat-Gruppen für Run-Boni
- PlayerFeedbackData: Trefferfeedback, Blinkzustände, Audio- und VFX-Referenzen
- PlayerInteractionData: Reichweiten, Cooldowns und Regeln für Sammeln, Bauen und Bestätigen

Temporäre Laufzeitwerte wie aktuelle Lebenspunkte, aktive Boni und Zustände werden getrennt von den Asset-Daten gehalten.

## Technical Design

Ein PlayerController liest Input und delegiert an spezialisierte Runtime-Komponenten für Movement, Combat und Interaktion.

MonoBehaviours koordinieren Unity-spezifische Aspekte wie Rigidbody, Animation, Collider und Kamera-Anbindung.

Geschäftslogik für Beschleunigung, Zielrichtung, Schadensaufnahme und Interaktionsfreigaben liegt in testbaren C# Klassen.

Der Player sendet Events wie OnPlayerMoved, OnPlayerDamaged, OnResourceCollected, OnTowerPlacementStarted und OnTowerPlaced.

## Dependencies

- Input
- Combat
- Weapons
- Resources
- Towers
- Cards
- UI
- Audio
- SaveSystem
- MetaProgression

## UI

Der Player benötigt unmittelbar lesbare UI-Rückmeldungen für:

- Lebenszustand
- temporäre Buffs oder Debuffs
- aktive Interaktionsaufforderungen
- Tower-Platzierungsstatus
- Richtungs- oder Zielhinweise bei Controller-Bedienung

## Audio

Player-Audio deckt Bewegung, Treffer, Resource-Sammeln, Interaktion und bestätigte Build-Aktionen ab.

Schaden und kritische Zustände müssen sich akustisch klar von normalen Angriffs- oder Sammelgeräuschen unterscheiden.

## Visual

Der Player nutzt eine klare Silhouette, starke Kontrastwerte und priorisierte Layer-Reihenfolge.

Wichtige Zustände wie Treffer, Unverwundbarkeit oder Interaktionsbereitschaft werden über kurze, gut lesbare Signale dargestellt.

## Balancing

Der Player startet jeden Run mit ausreichender Handlungsfähigkeit, ohne bereits vollständig ausgebaut zu sein.

Movement und Überlebensfähigkeit sollen Druck erzeugen, aber keine übermäßige Frustration in frühen Waves verursachen.

Build-Verbesserungen dürfen die Effektivität des Players deutlich erhöhen, müssen aber an Entscheidungen, Opportunity Costs oder Ressourcen gebunden bleiben.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Der Player nutzt eine kleine automatische Resource-Sammelreichweite; verbliebene Pickups werden nach Wave-Ende automatisch eingezogen.
- Die Interaktionspriorität lautet: blockierende UI vor Tower-Interaktion vor Reward- oder Shop-Interaktion vor nicht automatisch eingesammelten Pickups.
- Wenn Player oder Core auf null fallen, endet der aktuelle Run sofort; ein Revive-State ist nicht Teil des ersten Scopes.

## Acceptance Criteria

- Der Player kann Movement, Combat, Resource-Sammeln und Tower-Interaktion vollständig per Controller ausführen.
- Player-Logik ist in testbare Runtime-Komponenten getrennt und nicht in einem einzelnen God Object gebündelt.
- Run-Boni aus Card, Weapon und Meta Progression können den Player datengetrieben modifizieren.
- Player-Zustände und Rückmeldungen bleiben in intensiven Combat-Situationen klar lesbar.
- Player und Core besitzen eindeutige Verlustzustände ohne versteckte Sonderfälle.
