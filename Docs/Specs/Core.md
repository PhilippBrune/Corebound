# Core Specification

## Purpose

Definiert den Core als zentrales Verteidigungsobjekt jedes Runs.

Der Core ist der gemeinsame strategische Bezugspunkt für Player, Enemy, Tower, Waves und Bosses.

## Design Goals

- Sofort verständliches Primärziel jedes Runs
- Hohe visuelle und spielerische Lesbarkeit des Verlustdrucks
- Starke Verknüpfung mit Positioning, Tower-Platzierung und Enemy-Priorisierung
- Einfache Regeln ohne Sonderfälle oder alternative Kernziele im ersten Scope
- Saubere technische Trennung zwischen Core-Zustand, UI und Combat-Reaktionen

## Gameplay

Der Player verteidigt den Core innerhalb der Arena gegen angreifende Enemies und Bosses.

Der Core erzeugt ständigen Entscheidungsdruck, weil der Player nicht nur eigenes Überleben, sondern auch Wegführung, Tower-Position und Target Priority beachten muss.

Wenn Enemies den Core erreichen oder ihn gezielt angreifen, sinkt dessen Lebenszustand und der Run kann verloren gehen.

Der Core ist kein passives Dekoelement, sondern die feste Niederlagenbedingung des Verteidigungsloops.

## Rules

- Jeder Run besitzt genau einen aktiven Core.
- Wenn der Core zerstört wird, endet der Run sofort.
- Der Core bewegt sich im ersten Scope nicht und wird nicht umplatziert.
- Core-Schaden muss immer klar lesbar und akustisch eindeutig sein.
- Enemies dürfen den Core bedrohen, ohne die Position des Players irrelevant zu machen.
- Der Core verwendet dieselbe grundlegende Damage-Logik wie andere Combat-Ziele, kann aber eigene Resistenz- oder Regelwerte besitzen.
- Zusätzliche Core-Varianten oder alternative Missionsziele sind nicht Teil des ersten Vertical Slice.

## Data Model

Core-Daten werden über ein ScriptableObject definiert: **CoreData** (`Assets/Scripts/Gameplay/Combat/CoreData.cs`, Asset: `CoreData_Default.asset`).

- `_displayName`: Anzeigename des Core.
- `_combatTargetData`: Referenz auf ein `CombatTargetData`-Asset (`CombatTarget_Core.asset`) — trägt Basis-Health, Alive/Dead-Farben und Kollisionsgröße. Diese Werte werden bewusst nicht in CoreData dupliziert, da `CombatTargetData` bereits identisch für Enemies, Bosse und den Core verwendet wird.
- `_criticalHealthThreshold`: Normalisierter Health-Anteil (Standard 0.35), unterhalb dessen `OnCoreCritical` feuert.

**Update 2026-09-16 — Umgesetzt:** Die ursprünglich geplanten separaten `CoreDefenseData`/`CoreFeedbackData`/`CorePlacementData`-Assets wurden bewusst nicht als eigene leere ScriptableObjects gebaut, da ihr Inhalt bereits an anderer Stelle real existiert und eine reine Platzhalter-Klasse dem Scope-Prinzip ("kleineres, poliertes Feature-Set") widersprechen würde:

- Schadensreaktion/"Shield"-Werte: bereits als `CoreGuardController` implementiert (siehe unten).
- Feedback (Treffer-Flash, Zerstörungsfarbe): bereits über `CombatHitReceiver`s generischen Flash/Dead-Color-Mechanismus abgedeckt, plus die gemeinsame VFX-Eskalationszuordnung.
- Placement: der Core ist im ersten Vertical Slice ein fest auf `Vector3.zero` verankertes GameObject (`CoreAnchor`, siehe `WaveManager.EnsureCoreTarget()`) — es gibt noch keine variable Platzierungslogik, die eigene Daten rechtfertigen würde.

Sollte künftig echte Platzierungs- oder Verteidigungsvarianz nötig werden (z. B. bewegliche Core-Varianten, siehe Open Design Questions in der Vision), sollten diese Daten dann als eigene Assets ergänzt werden.

## Technical Design

**CoreController** (`Assets/Scripts/Gameplay/Combat/CoreController.cs`) sitzt auf demselben `CoreAnchor`-GameObject wie der Core's `CombatHitReceiver` und wird von `WaveManager.EnsureCoreTarget()` konfiguriert (`Configure(CoreData, CombatHitReceiver)`). Er hält die Referenz auf das CoreData-Asset und abonniert die gemeinsamen `CombatEvents` (gefiltert auf das Core-GameObject), um die drei dokumentierten Events erneut auszulösen:

- `OnCoreDamaged` (bei jedem eingehenden Treffer)
- `OnCoreCritical` (einmalig beim Unterschreiten von `_criticalHealthThreshold`, erneut auslösbar nach Erholung über die Schwelle)
- `OnCoreDestroyed` (beim Tod des Core)

Core-Schaden wird über die gemeinsame Combat-Pipeline verarbeitet (`CombatHitReceiver` + `CombatEvents`, identisch zu Enemies/Bossen).

Die visuelle Eskalation fuer Core-Schaden und kritische Core-Momente nutzt die gemeinsame Zuordnung aus der [VFX Specification](./VFX.md).

Der Core besitzt keine direkte Abhängigkeit zur UI und startet keine Menülogik selbst.

### CoreGuardController (bisher undokumentiert, jetzt nachgetragen)

`Assets/Scripts/Gameplay/Combat/CoreGuardController.cs` implementiert eine aktive Verteidigungsoption: Hält der Spieler die Interact-Taste, während er sich innerhalb von 2.2 Einheiten um den Core befindet, wird ein Guard-Schild aktiviert, der eingehenden Core-Schaden auf 15 % reduziert (`GuardDamageMultiplier = 0.15f`) und einen sichtbaren Ring um den Core zeichnet.

Dies ist die konkrete Umsetzung von Core.md's Pillar-Bezug zu "Action Comes First" (siehe [Vision](../00_PROJECT_VISION.md) 5.1): Der Core ist ohne diesen Mechanismus rein passiv und wird nur von Towers/Positionierung indirekt geschützt. `CoreGuardController` gibt dem Spieler eine direkte, momentane Entscheidung: weiter angreifen oder aktiv den Core abschirmen.

Ein einmaliger Hinweis-Toast ("Hold Interact near the Core to raise a Guard shield") wird beim ersten Betreten der Guard-Reichweite gezeigt, sofern die Taste noch nicht gehalten wird — Discoverability für einen Mechanismus ohne sonstige On-Screen-Anzeige.

## Dependencies

- Player
- Combat
- Towers
- Enemies
- Bosses
- Waves
- UI
- Audio
- SaveSystem

## UI

Die UI zeigt permanent den Core-Lebenszustand und kritische Warnungen bei drohendem Verlust.

Core-bezogene Hinweise müssen auch im dichten Combat klar erkennbar bleiben.

## Audio

Core-Schaden, kritischer Zustand und Zerstörung besitzen priorisierte Audio-Signale.

Diese Signale dürfen von Reward- oder UI-Sounds nicht verdrängt werden.

## Visual

Der Core braucht eine starke, sofort lesbare Top-Down-Silhouette und eine klare Gefahrenkommunikation bei Schaden.

Beschädigungszustände sollen ohne übermäßigen Effektlärm sichtbar eskalieren.

## Balancing

Der Core muss Druck erzeugen, darf aber nicht so fragil sein, dass einzelne unklare Treffer den Run unfair beenden.

Core-Health, Enemy-Druck und Stage-Pacing müssen so abgestimmt werden, dass Positioning und Priorisierung relevant bleiben.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Der Core ist die feste zentrale Niederlagenbedingung jedes Runs.
- Der Core bleibt stationär in der Arena verankert.
- Core-Schaden wird über die allgemeine Combat-Pipeline verarbeitet und über priorisierte UI- und Audio-Signale kommuniziert.

## Acceptance Criteria

- Jeder Run besitzt genau einen klar lesbaren Core.
- Core-Zerstörung beendet den Run eindeutig und ohne Sonderfälle.
- Core-Schaden ist in Combat, UI und Audio sauber integriert.
- Enemies und Bosses können den Core über dieselbe Combat-Architektur bedrohen.
- Der Core stärkt Positioning und Defensiventscheidungen, ohne neue Kernmechaniken einzuführen.

---

## Repair (2026-09-21)

Settled 2026-09-21: the Core is **stationary and cannot be upgraded**, but it **can be
repaired**.

During an Intermission the player walks to the Core and presses Interact - the same input
that builds a tower when standing on a slot - to spend Scrap and restore a fraction of the
Core's maximum health. Each repair within the same run costs more than the last.

Repair is deliberately not an upgrade. It restores what was lost and never raises a ceiling,
so it competes with towers and weapons for Scrap without becoming a power-creep sink. A
player who has taken no Core damage has nothing to buy here, which is the correct incentive.

`CoreRepairService` owns this. Proximity is the discovery mechanism: reusing one input by
position keeps the control scheme from growing a key per verb, and gives the Core a physical
place the player has to go to.
