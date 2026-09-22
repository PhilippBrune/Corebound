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
- **Szene gegen Prozedur (präzisiert 2026-09-22).** Feste Rahmen, Panel-Positionen und Hintergründe gehören in die Szene; ein Presenter darf sie nur finden und fehlende Sprites nachrüsten, niemals Anker oder Größen neu setzen. Das gilt bindend für `HUDCanvas`, `RewardCanvas` und `ShopCanvas`. **Prozedural erzeugt werden dürfen** Inhalte, deren Anzahl erst zur Laufzeit feststeht oder die einem Weltobjekt folgen: Listenzeilen und Grids (Loadout-Panel, Pause-Tabs, Progression-Zeilen, Tower-Roster), Tooltips und Karten am Objekt (Upgrade-Karte), Weltraum-Prompts (Core-Reparatur) und Schadenszahlen. Ein Presenter, der eine Szenen-Hierarchie vorfindet, benutzt sie; ein prozeduraler Fallback bleibt erlaubt, damit eine fehlende Hierarchie im Editor nicht in einen Absturz läuft.

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

- Layer-Ebenen (Stand 2026-09-22): `main_menu`, `main_menu_route_panel`, `gameplay_hud`, `boss_hud`, `reward_screen`, `shop_panel`, `weapon_loadout_panel`, `tower_build_browser`, `pause_overlay`, `settings_panel`, `meta_progression_panel`.
- Dauerhaft sichtbare HUD-Elemente (Core-/Player-Health, Wave-Status, Resource-Anzeige) bleiben von kontextuellen Overlays (Reward, Shop, Settings, Meta Progression) architektonisch getrennt statt in einem gemeinsamen Panel zu leben.

## Acceptance Criteria

- UI ist strikt eventgetrieben und enthält keine Geschäftslogik.
- Alle UI-Flows sind per Controller vollständig bedienbar.
- HUD, Menüs und Overlays sind modular aufgebaut.
- Informationshierarchie und Lesbarkeit bleiben auch in intensiven Combat-Situationen erhalten.
- Neue Systeme können Panels erweitern, ohne bestehende UI-Grundstrukturen zu brechen.

---

## Auflösung und Seitenverhältnis (2026-09-22)

Drei Bugs entstanden daraus, dass dieses Dokument keine Auflösungsregeln hatte: HUD-Werte
verschwanden im Vollbild, die Hauptmenü-Einträge passten mit sichtbarem "Continue" nicht mehr in den
Bildschirm, und um die Arena lag ein blauer Rahmen.

- **Referenzauflösung 1920x1080**, `CanvasScaler` im Modus *Scale With Screen Size*, Match 0.5. Jeder
  Canvas benutzt dieselben Werte.
- Unterstützt werden 16:9, 16:10 und 21:9 zwischen 1280x720 und 3840x2160. Nichts darf davon abhängen,
  dass exakt 16:9 vorliegt.
- **Best-Fit-Text**: Ein Text mit `resizeTextForBestFit` braucht eine Rect-Höhe von mindestens
  1.3 x seiner Mindestgröße und `verticalOverflow = Truncate`. Mit `Overflow` schrumpft Legacy Text
  nicht, sondern zeichnet über seine Nachbarn; mit zu kleiner Rect zeichnet er gar nichts.
  Mindestgrößen liegen nicht unter 10.
- **Listen, die wachsen können** (Hauptmenü, Progression, Loadout) definieren eine Mindesthöhe pro
  Eintrag und ein Verhalten, wenn die Summe nicht passt. Sie dürfen nie stillschweigend überlaufen.
- **Die Kamera füllt die Arena immer aus** (Cover, nicht Fit): sie skaliert auf das Maximum aus
  Höhen- und Breitenbedarf und wird an den Arenagrenzen plus Wandzuschlag geklemmt. Außerhalb der
  Wände liegen abgedunkelte Backdrop-Kacheln, damit in keinem Seitenverhältnis die leere
  Hintergrundfarbe zu sehen ist.
- Autopilot-Screenshots entstehen in der aktuellen Editor-Auflösung; eine Prüfung im echten Vollbild
  bleibt ein manueller Schritt.
