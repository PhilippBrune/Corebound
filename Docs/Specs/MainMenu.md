# Main Menu Specification

## Purpose

Definiert das Main Menu als zentralen Einstiegspunkt ins Game.

Das Main Menu verbindet Run-Start, Fortsetzen, Meta Progression, Settings und andere globale Flows.

## Design Goals

- Schneller Weg vom Startbildschirm in einen Run
- Klare Struktur für Continue, New Run, Meta Progression und Settings
- Vollständige Controller-Navigation ohne Mauspflicht
- Saubere Integration mit SaveSystem und Tutorial-Einstieg
- Erweiterbarkeit für weitere globale Menüpunkte ohne Neudesign

## Gameplay

Vom Main Menu aus kann der Player einen neuen Run starten, einen gültigen gespeicherten Run fortsetzen, Meta Progression öffnen oder Settings ändern.

Das Main Menu dient außerdem als sicherer Ort für Entscheidungen außerhalb des aktiven Combat.

Es soll die nächste sinnvolle Aktion klar priorisieren und keine unnötigen Zwischenschritte verlangen.

Menü-Flows müssen sowohl für Erstnutzer als auch für wiederkehrende Spieler verständlich sein.

## Rules

- Continue wird nur angeboten, wenn ein gültiger fortsetzbarer Spielstand existiert.
- New Run darf bestehende fortsetzbare Run-Daten nicht ohne klare Bestätigung überschreiben.
- Jeder Main-Menu-Eintrag muss per Controller erreichbar sein.
- Das Main Menu enthält keine Spiellogik des Runs, sondern nur Flow-Steuerung.
- Meta Progression, Settings und Tutorial-Zugänge werden von hier aus konsistent erreichbar gemacht.
- Das Main Menu darf keine Maus-Hover-Pflicht oder versteckten Fokuszustände haben.
- Neue globale Menüpunkte sollen in dasselbe Routing-System integriert werden.

## Data Model

Main-Menu-Daten werden asset- oder konfigurationsgetrieben beschrieben.

- MainMenuFlowData: verfügbare Routen, Reihenfolgen und Sichtbarkeitsregeln
- MenuEntryData: Label, Zielscreen, Freischalt- oder Sichtbarkeitsbedingungen
- ContinueStateData: Statusdarstellung für fortsetzbare Inhalte
- IntroFlowData: Splash-, Press-Start- oder Erstnutzer-Einstiege
- TutorialEntryData: Regeln für Tutorial-Hinweise oder direkten Einstieg

## Technical Design

Das Main Menu basiert auf UI-Panel-Routing und einem leichten Zustandsmodell.

SaveSystem liefert Continue- und Profilstatus, MetaProgression liefert Unlock-Status für relevante Einträge.

Ereignisse wie OnMainMenuOpened, OnContinueSelected, OnNewRunConfirmed und OnGlobalMenuRouteChanged koordinieren andere Systeme.

Das Main Menu bleibt von aktiven Gameplay-Szenen und Combat-Komponenten getrennt.

Der erste Vertical Slice nutzt einen MainMenuCanvasPresenter mit MainMenuSkinData, das importierte Background-, Logo-, Button-Frame- und Fokusmarker-Assets referenziert. Die Eingabeschicht bleibt controller-first und treibt ausschließlich Routing und Fokuszustand, nicht die UI-Art-Logik.

Dieselbe Skin-Struktur referenziert zusätzlich ControllerGlyphData für Gerätehinweise und MainMenuAudioCueData für UI-Cues, damit visuelle und akustische Frontend-Rückmeldung im selben datengetriebenen Slice konfiguriert bleibt.

Die Routen Continue, Settings und Meta Progression werden im ersten Vertical Slice über MainMenuRouteExecutionData, MainMenuRouteTargetController und save- oder servicegestützte Route-Targets ausgeführt. Continue liest seinen Zielzustand aus SaveSystem, Settings öffnet eine echte Options-UI mit SettingsManager, und Meta Progression öffnet eine persistente Unlock- und Currency-Ansicht über MetaProgressionService.

## Dependencies

- SaveSystem
- MetaProgression
- Settings
- Tutorial
- UI
- Audio
- Input

## UI

Das Main Menu zeigt mindestens:

- Continue oder New Run
- Zugang zu Meta Progression
- Zugang zu Settings
- Quit
- klare Fokus- und Statushinweise
- kontextnahe Confirm-, Cancel- und Navigate-Hinweise aus derselben Skin-Struktur

## Audio

Main-Menu-Audio umfasst Fokus, Bestätigung, Abbruch und optional eine eigenständige Title-Atmosphäre.

Wichtige Übergänge wie Continue oder New Run sollen akustisch eindeutig bestätigt werden.

Der erste spielbare Slice bindet diese UI-Cues lokal über MainMenuAudioCueData und semantische MainMenu-Input-Events an. Bis finale UI-SFX importiert sind, darf der Vertical Slice einfache synthetisierte Confirm-, Cancel-, Navigate- und Open-Cues verwenden.

## Visual

Das Main Menu sollte die Identität des Games transportieren, ohne die Bedienung zu erschweren.

Große Buttons, klare Fokuszustände und saubere Informationshierarchie haben Vorrang vor dekorativer Komplexität.

Der erste spielbare Slice verwendet einen importierten MainMenu-Hintergrund, ein dediziertes Corebound-Logo, modulare Button-Frames und einen klaren Fokusmarker. Text bleibt editierbar und liegt auf separaten UI-Komponenten statt eingebrannt in die Art-Assets.

## Balancing

Das Main Menu soll den Weg in den nächsten Run beschleunigen und keine Friktion erzeugen.

Erstnutzer benötigen klare Orientierung, wiederkehrende Spieler schnelle Direktpfade.

Zu viele globale Optionen im ersten Bildschirm sind zu vermeiden.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Das Game startet mit einem kurzen Press-Start-Einstieg vor dem eigentlichen Main Menu.
- Der erste Scope nutzt ein lokales Profil und genau einen fortsetzbaren Run statt mehrerer Slots.
- Der erste New-Run-Flow führt einmalig durch die geführte Tutorial-Stage; danach bleibt das Tutorial optional im Main Menu erreichbar.

## Acceptance Criteria

- Das Main Menu bietet die Kernpfade Continue, New Run, Meta Progression und Settings controller-freundlich an.
- Continue- und Überschreiblogik sind mit SaveSystem konsistent.
- Main-Menu-Routing bleibt von Gameplay-Logik getrennt.
- Erstnutzer- und Wiederkehrer-Flows sind klar verständlich.
- Neue globale Menüpunkte lassen sich modular ergänzen.
- Die erste MainMenu-UI verwendet importierte Art-Assets für Hintergrund, Logo, Button-Frames und Fokusmarker statt reiner Debug-Textdarstellung.
- Controller-Glyph-Hinweise und UI-Cues werden über dieselbe Skin- und Presenter-Struktur verdrahtet.
- Continue, Settings und Meta Progression besitzen im ersten Slice echte Route-Ziele innerhalb des bestehenden MainMenu-Routings.

---

## Layout (2026-09-22)

Das Hauptmenü war eine einzelne vertikale Liste. Mit sichtbarem "Continue" passte sie im Vollbild
nicht mehr auf den Bildschirm. Owner-Entscheidung 2026-09-22: **zweistufiges Layout**.

```
            C O R E B O U N D
         ┌─────────────────────┐
         │      CONTINUE       │   primäre Aktion, groß
         └─────────────────────┘
         ┌─────────────────────┐
         │       NEW RUN       │
         └─────────────────────┘

  [Progression]  [Tutorial]  [Settings]  [Quit]      sekundäre Reihe
```

- **Primäre Stufe:** die ein bis zwei Aktionen, die einen Run starten oder fortsetzen. Groß, mittig,
  volle Breite.
- **Sekundäre Stufe:** alle globalen Routen als eine Reihe gleich breiter Schaltflächen am unteren
  Rand.
- **Navigation:** Hoch/Runter wechselt zwischen den Stufen und innerhalb der primären Einträge,
  Links/Rechts innerhalb der sekundären Reihe. Der Fokus merkt sich je Stufe seine letzte Position.
- Die sekundäre Reihe bricht auf zwei Zeilen um, wenn die Breite pro Eintrag unter 150 px fiele.
- Das Profil (siehe [SaveSystem.md](SaveSystem.md), "Drei Profile") steht als Kopfzeile über der
  primären Stufe und wird über eine eigene Route gewechselt.
