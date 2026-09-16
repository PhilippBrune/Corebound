# UI Specification

## Purpose

Definiert die UI-Architektur und die Darstellungsprinzipien für Gameplay, Menüs und Meta-Systeme.

UI soll Informationen lesbar, controller-freundlich und strikt vom Gameplay entkoppelt präsentieren.

## Design Goals

- Readability First in jeder Spielsituation
- Vollständige Bedienbarkeit per Controller
- Klare Trennung von HUD, Menüs, Overlays und Reward-Screens
- Eventgetriebene UI ohne Geschäftslogik
- Modulare Erweiterbarkeit für neue Panels und Systeme

## Gameplay

Während eines Runs zeigt die UI den Zustand von Player, Core, Wave, Resources, Rewards und Interaktionen.

Außerhalb von Combat steuert die UI Card-Auswahl, Shop, Meta Progression, Settings und Main Menu.

UI soll Entscheidungen beschleunigen, nicht verkomplizieren.

Der Player muss jederzeit erkennen können, was wichtig, was optional und was gerade fokussiert ist.

## Rules

- UI reagiert ausschließlich auf Events oder Read-Only-Datenmodelle.
- Keine Schadens-, Spawn-, Save- oder Economy-Logik in UI-Klassen.
- Jede UI-Oberfläche muss per Controller vollständig navigierbar sein.
- Fokuszustände, Bestätigen und Abbrechen folgen überall denselben Mustern.
- Wichtige Gameplay-Informationen haben Vorrang vor dekorativen Elementen.
- Modale Fenster dürfen den Spielfluss nur dann blockieren, wenn eine klare Entscheidung nötig ist.
- Neue Features müssen vorhandene UI-Rahmen erweitern, nicht parallele Sonderlösungen bauen.
- UI-Hierarchien werden in der Unity-Szene von Hand angelegt und im Inspector editierbar gehalten — Presenter-Klassen binden an diese vorhandenen Objekte (`[SerializeField]`-Referenzen), statt ihre Hierarchie zur Laufzeit selbst zu konstruieren. Ein Presenter darf einen rein prozeduralen Fallback besitzen, der eine fehlende Szenen-Hierarchie beim ersten Öffnen im Editor automatisch nachbaut (Self-Healing-Scaffolding), aber dieser Fallback ist ein einmaliges Editor-Werkzeug, kein dauerhafter Laufzeitpfad — sobald die Hierarchie in der Szene existiert, ist sie die alleinige Quelle der Wahrheit für Layout, Farben und Abstände.

## Data Model

UI-Konfiguration wird datengetrieben gehalten.

- HUDLayoutData: Positionierung und Sichtbarkeitsregeln für HUD-Elemente
- PanelDefinitionData: Registrierungen, Prefabs und Routing-Informationen für Panels
- NotificationStyleData: Farben, Animationen und Priorität für Hinweise
- FocusMapData: Controller-Navigationspfade und Fallbacks
- UIThemeData: gemeinsame Typografie-, Icon- und Farbreferenzen

## Technical Design

Ein UIManager koordiniert Panel-Lebenszyklen, Routing und globale Layer.

Einzelne Views besitzen Presenter oder Controller, die auf Events wie OnWaveStarted, OnCardOffered oder OnSettingsOpened reagieren.

Gameplay-Systeme senden keine Befehle an konkrete UI-Objekte, sondern nur Ereignisse oder Datenupdates.

UI-Layer für HUD, Menüs und Blocking-Overlays bleiben organisatorisch getrennt.

Der aktuelle Vertical Slice setzt diese Trennung konkret ueber einen zentralen `UIManager`, `UIManagedPanel`-Registrierung pro Presenter sowie getrennte Layer fuer `main_menu`, `gameplay_hud`, `boss_hud`, `reward_screen`, `settings_panel`, `meta_progression_panel` und `main_menu_route_panel` um.

## Dependencies

- Player
- Combat
- Waves
- Cards
- Towers
- Resources
- Economy
- MainMenu
- Settings
- MetaProgression
- SaveSystem
- Audio
- Input

## UI

Die UI-Struktur umfasst mindestens:

- Gameplay-HUD
- Reward- und Auswahl-Screens
- Shop- und Upgrade-Oberflächen
- Meta- und Settings-Menüs
- Systemhinweise, Fehlermeldungen und Bestätigungen

## Audio

UI-Audio signalisiert Fokus, Bestätigung, Abbruch, Fehler und seltene Reward-Momente.

Die Mischung von UI-Audio darf Combat-Warnungen nicht verdrängen.

## Visual

Die visuelle Sprache priorisiert hohe Lesbarkeit, starke Kontraste und klare Hierarchien.

Animationen sollen Zustandswechsel unterstützen, aber Informationen nicht verzögern.

## Balancing

UI muss die Entscheidungsdichte unterstützen, ohne den Player mit Zahlen oder Panels zu überfordern.

Wichtige Informationen sollten früh sichtbar und sekundäre Details optional sein.

Komplexere Systeme wie Factory oder Meta Progression benötigen progressive Offenlegung statt Vollüberladung.

## Open Questions

- Welche Lokalisierungsanforderungen müssen bereits in der ersten UI-Iteration berücksichtigt werden? (Teil-Lokalisierung existiert bereits, deckt aber nicht alle Oberflächen ab)
- Für Presenter, die aktuell nur einen prozeduralen Konstruktionspfad ohne Szenen-Bindung besitzen (kein `TryBindAuthoredLayout()`-Äquivalent): welche Priorität hat die Migration auf das oben festgelegte „Szene ist Quelle der Wahrheit"-Muster gegenüber neuen Features?

Festgelegte Entscheidungen:

- Sieben Layer-Ebenen sind für den ersten Vertical Slice ausreichend und bereits umgesetzt: `main_menu`, `gameplay_hud`, `boss_hud`, `reward_screen`, `settings_panel`, `meta_progression_panel`, `main_menu_route_panel`.
- Dauerhaft sichtbare HUD-Elemente (Core-/Player-Health, Wave-Status, Resource-Anzeige) bleiben von kontextuellen Overlays (Reward, Shop, Settings, Meta Progression) architektonisch getrennt statt in einem gemeinsamen Panel zu leben.

## Acceptance Criteria

- UI ist strikt eventgetrieben und enthält keine Geschäftslogik.
- Alle UI-Flows sind per Controller vollständig bedienbar.
- HUD, Menüs und Overlays sind modular aufgebaut.
- Informationshierarchie und Lesbarkeit bleiben auch in intensiven Combat-Situationen erhalten.
- Neue Systeme können Panels erweitern, ohne bestehende UI-Grundstrukturen zu brechen.
