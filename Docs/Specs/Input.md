# Input Specification

## Purpose

Definiert das Input-System für Gameplay, Menüs und Build-Interaktionen.

Input ist die Grundlage für Controller First und muss alle Kernaktionen ohne Mauspflicht zugänglich machen.

Project Vision benennt Input explizit als eigenen Design Pillar (Pillar 5: „Controller First — Alle Aktionen funktionieren perfekt mit Controller. Keine Mauspflicht.") und wiederholt es in den Design Principles sowie der Technical Vision. Dieser Spec ist damit nicht nur ein technisches Feature-Dokument, sondern die Umsetzung eines Kern-Pillars — jede Abweichung von voller Controller-Parität ist ein Pillar-Verstoß, kein Detail.

## Design Goals

- Vollständige Controller-Nutzbarkeit des gesamten Spiels
- Parität zwischen Controller und Mouse & Keyboard
- Klare Trennung von Gameplay-, UI- und Meta-Kontexten
- Gute Erweiterbarkeit für Rebinds und neue Eingabegeräte
- Semantische Input-Events statt direkter Gerätekopplung in Fachlogik

## Gameplay

Im aktiven Run steuert Input Movement, Angreifen, Interaktionen, Tower-Bau und Bestätigungen.

In Menüs steuert Input Navigation, Fokuswechsel, Bestätigen, Abbrechen und Kontextaktionen.

Input muss flüssig zwischen Combat, Reward-Auswahl und Menüs wechseln, ohne widersprüchliche Zustände zu erzeugen.

Der Player soll jederzeit verstehen, welche Aktion aktuell auf welchem Eingabekanal verfügbar ist.

## Rules

- Alle Kernaktionen müssen vollständig per Controller ausführbar sein.
- Input verwendet das Unity Input System.
- Gameplay- und UI-Kontexte werden über getrennte Action Maps oder gleichwertige Kontexte modelliert.
- Fachlogik darf nie direkt Hardware-spezifische Tasten oder Achsen abfragen.
- Kontextwechsel müssen deterministisch und visuell nachvollziehbar sein.
- Kein Feature darf exklusiv Mauspräzision voraussetzen.
- Rebinds sind architektonisch vorgesehen, auch wenn ihr finaler Umfang separat priorisiert wird.

## Data Model

Input nutzt Asset-basierte Konfiguration.

- InputSystem_Actions.inputactions: zentrale Definition aller Actions, Bindings und Action Maps. Die Build-Map hat `PreviousSlot`/`NextSlot` (Tower-Slot) und `PreviousTowerType`/`NextTowerType` (Tower-Typ). Die Bindungstabelle unten ist die lesbare Fassung dieses Assets
- InputContextData: erlaubte Kontexte und deren Prioritäten für Gameplay, UI und Menüs
- ControllerGlyphData: Zuordnung von Eingaben zu Icons oder Textdarstellung
- RebindProfileData: gespeicherte oder auswählbare Binding-Varianten
- InputAssistData: Daten für Zielhilfe, Deadzones oder Hold-Dauern

## Technical Design

Ein InputReader oder InputRouter liest Action-Callbacks und übersetzt sie in semantische Signale für Player, UI und Systeme.

MonoBehaviours verarbeiten nur die Unity-seitige Verdrahtung des Input Systems.

Fachsysteme reagieren auf Events oder Interfaces wie OnMoveInput, OnPrimaryAction, OnConfirm, OnCancel und OnNavigate.

Kontextfreigaben werden zentral verwaltet, damit Combat, UI und Build-Modi nicht gleichzeitig widersprüchliche Eingaben konsumieren.

Der MainMenu-Slice leitet rohe 2D-Navigation zusätzlich an aktive Route-Targets weiter, damit Settings- und Meta-Overlays ohne Sonder-Hardwareabfragen dieselbe semantische Eingabeschicht nutzen.

Der erste Settings-Slice bindet RebindProfileData bereits über einen InputRebindManager an die Laufzeit an, sodass Control-Profile semantische Actions statt Hardwarecodes umlegen.

## Dependencies

- Player
- UI
- MainMenu
- Settings
- Tutorial
- Towers

## UI

Input-bezogene UI zeigt:

- aktuelle Controller-Glyphs oder Tastaturhinweise
- Fokuszustände
- Kontextaktionen
- Rebind- oder Konflikthinweise

Der erste Vertical Slice zeigt MainMenu-Hinweise über ControllerGlyphData mit Gamepad- und Tastatur-Textdarstellung in derselben UI-Skin-Struktur wie Fokusmarker und Button-Frames.

## Audio

Bestätigen, Abbrechen, Fehlbedienung und Fokuswechsel erhalten klare UI-Audio-Cues.

Input-Hinweise dürfen nicht allein über Audio vermittelt werden, sondern nur unterstützend wirken.

## Visual

Aktive Fokus- und Interaktionszustände müssen im UI und in der Arena klar sichtbar sein.

Controller-spezifische Hinweise sollen kontextnah und knapp eingeblendet werden.

## Balancing

Input-Latenz und Deadzones müssen reaktionsschnelles Combat ermöglichen.

Hold-Dauern, Radial-Auswahlen oder Mehrfachbestätigungen dürfen den Spielfluss nicht ausbremsen.

Zielhilfen dürfen Controller unterstützen, aber keine dominante Präzisionsvorteile gegenüber Mouse & Keyboard erzeugen.

## Open Questions

- Rebind-Profile decken aktuell 5 von ~11 eindeutigen Actions ab (PrimaryFire, Confirm, Cancel, Move, Aim — Move/Aim seit 2026-09-08 über ein neues `RebindProfile_Southpaw`, das die Gamepad-Stick-Zuordnung tauscht). Welche der übrigen Actions (Interact, Navigate, Previous/NextSlot) brauchen als Nächstes ein alternatives Profil, um „Rebinds für alle Kernaktionen" (siehe Festgelegte Entscheidungen) tatsächlich zu erfüllen?

Festgelegte Entscheidungen:

- Der erste Scope verwendet die vier Action Maps Gameplay, Build, UI und System.
- Tower-Platzierung erfolgt controller-first über Tower Slot Cycling und nicht über freie Cursor-Navigation.
- Rebinds sind für alle Kernaktionen des ersten spielbaren Umfangs vorgesehen, inklusive Navigation, Confirm und Cancel — das ist ein direkter Ausdruck des Controller-First-Pillars, nicht optionale Politur, und bleibt bis zur vollen Abdeckung ein offener Punkt (siehe Open Questions).

## Acceptance Criteria

- Alle Kernaktionen sind per Controller und Mouse & Keyboard bedienbar.
- Fachlogik arbeitet mit semantischen Input-Signalen statt Hardwareabfragen.
- Kontexte für Gameplay und UI wechseln konsistent und nachvollziehbar.
- Rebinds und Eingabehints sind architektonisch vorgesehen.
- Input unterstützt controller-freundliche Tower- und Menüinteraktion ohne Mauszwang.
- Menüs können geräteabhängige Glyph- oder Text-Hinweise aus ControllerGlyphData ohne direkte Hardwareabfrage rendern.

---

## Bindungen (2026-09-22)

Die lesbare Fassung von `InputSystem_Actions.inputactions`. Bei Abweichung gilt das Asset; diese
Tabelle ist dann falsch und gehört korrigiert.

| Map | Action | Gamepad | Tastatur / Maus |
| --- | --- | --- | --- |
| Gameplay | Move | linker Stick | WASD |
| Gameplay | Aim | rechter Stick | Mausposition |
| Gameplay | PrimaryFire | RT | linke Maustaste |
| Gameplay | Interact | A | E |
| Build | PreviousSlot / NextSlot | LB / RB | Q / E |
| Build | PreviousTowerType / NextTowerType | LT / RT | Z / X |
| UI | Navigate | D-Pad, linker Stick | Pfeiltasten |
| UI | Confirm | A | Enter |
| UI | Cancel | B | Escape |
| UI | Previous / Next (Tabs) | LB / RB | Q / Tab |
| System | Pause | Start | Escape |
| System | Loadout | Select / Back | I |

**Regel Tastaturlayout:** Keine Bindung auf eine layoutabhängige Taste. `Keyboard.leftBracketKey`
und Nachbarn adressieren eine *physische* US-Position, die auf der hier verwendeten QWERTZ-Tastatur
woanders liegt. Buchstaben, Ziffern und Funktionstasten sind sicher.

**Debug-Tasten** (nur im Editor): F9 / F10 wechseln im Hauptmenü die Start-Stage.

## Aktives Eingabegerät (2026-09-22)

Wer zuletzt wirklich etwas getan hat, bestimmt das aktive Gerät - vorher gewann immer die Maus, weil
`Aim` als Value-Action die Kontrolle mit der größten Magnitude nahm und eine Mausposition in Pixeln
jeden Stickwert überbietet. Ein Xbox-Controller konnte dadurch nicht zielen.

- `Aim` ist eine **PassThrough**-Action; Stick- und Zeigerwert werden getrennt geführt.
- Der Stick übernimmt ab einer Auslenkung von **0.25**.
- Die Maus übernimmt erst nach einer Bewegung von **4 Pixeln**, damit eine ruhende Maus den
  Controller nicht verdrängt.
- Im Gamepad-Modus ist `Aim` eine Weltrichtung, im Maus-Modus eine Bildschirmposition. Jeder
  Konsument muss `IsUsingGamepad` beachten.
- **Settings-Option "Eingabegerät": Automatisch / Controller / Maus** (Owner-Entscheidung
  2026-09-22). "Automatisch" benutzt die Regeln oben; die beiden anderen Werte frieren das Gerät
  fest, was bei einem angeschlossenen, driftenden Controller hilft.
