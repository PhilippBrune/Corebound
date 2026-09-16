# Audio Specification

## Purpose

Definiert das Audio-System und die Audio-Richtlinien des Games.

Audio unterstützt Combat-Lesbarkeit, Belohnungsgefühl, Menüführung und die Identität von Runs, Stages und Bosses.

Project Vision's Audio Direction gibt die Zielrichtung wörtlich vor: „Treibender Soundtrack. Satte Treffergeräusche. Klare Audiohinweise. Keine überladenen Soundlandschaften." Jede Entscheidung in diesem Spec (Musikzustände, Voice-Limits, Mix-Priorität) leitet sich aus dieser Vorgabe ab, nicht nur aus allgemeinen Audio-Best-Practices.

## Design Goals

- Klar priorisierte Audio-Information für Gameplay-relevante Ereignisse
- Starke Rückmeldung für Combat, Rewards und Menüs
- Gute Unterstützung der Lesbarkeit statt reines Spektakel
- Saubere Steuerbarkeit über Settings
- Ereignisgetriebene und modular erweiterbare Audio-Architektur

## Gameplay

Im Run reagiert Audio auf Weapon-Feuer, Treffer, Enemy-Gefahr, Core-Schaden, Resource-Sammeln, Wave-Übergänge und Boss-Momente.

Außerhalb des Runs unterstützt Audio Navigation, Bestätigung, Meta Progression und Main Menu.

Musik und Soundeffekte sollen Spannung, Eskalation und Belohnung transportieren, ohne wichtige Informationen zu verdecken.

Audio ist ein aktiver Informationsträger und kein rein dekoratives Layer.

## Rules

- Audio wird eventbasiert ausgelöst und nicht über verstreute Direktaufrufe ohne zentrale Regeln.
- Wichtige Gefahren- und Schaden-Signale haben Mix-Priorität vor dekorativen Sounds.
- Häufige Ereignisse wie Treffer oder Pickups benötigen Voice-Limits, Cooldowns oder Gruppierung.
- Audio-Kategorien wie Music, SFX und UI müssen getrennt regelbar sein.
- Jede Systemfamilie nutzt konsistente Audio-Identitäten.
- Audio darf Controller-Bedienung unterstützen, aber nie als alleinige Informationsquelle voraussetzen.
- Neue Audio-Inhalte werden in bestehende Kategorien und Eventpfade integriert.

## Data Model

Audio-Daten werden über ScriptableObjects und Unity-Audio-Assets organisiert.

- AudioCueData: einzelne Clips, Variationen, Lautstärke- und Pitch-Bereiche
- AudioEventData: Zuordnung eines Gameplay- oder UI-Ereignisses zu AudioCues
- MusicStateData: definierte Musikzustände für Main Menu, Wave, Boss oder Reward
- MixerBusData: Routing und Kategorisierung in Audio-Mixer-Gruppen
- AudioPriorityData: Regeln für Priorität, Ducking und Voice-Limits

## Technical Design

Ein AudioManager empfängt Events aus Gameplay- und UI-Systemen und löst passende AudioEvents aus.

Musikzustände und Snapshot-Wechsel werden zentral verwaltet.

Subsysteme referenzieren Audio-Daten oder senden semantische Audio-Ereignisse, statt selbst Quellen zu verwalten.

Audio muss mit Settings, Pause-Zuständen und Scene-Wechseln konsistent umgehen.

Der erste MainMenu-Slice darf UI-Cues lokal über MainMenuAudioCueData und semantische Input-Events anstoßen, solange dieser Pfad sauber gekapselt bleibt und ohne Architekturbruch in einen späteren AudioManager überführt werden kann.

Der erste Settings-Slice bindet Lautstärke-Werte bereits an einen AudioManager, und MainMenuAudioCuePlayer skaliert UI-Cues direkt über die aktuelle UI-Volume-Kategorie.

## Dependencies

- Combat
- Waves
- Bosses
- UI
- MainMenu
- Settings
- Tutorial

## UI

Audio-bezogene UI umfasst:

- Lautstärke- und Mix-Einstellungen
- akustische Bestätigung für Fokus, Bestätigen und Abbrechen
- verständliche Rückmeldung auf Fehler oder gesperrte Aktionen

## Audio

Das Audio-System deckt mindestens folgende Kategorien ab:

- Music
- Combat SFX
- Enemy und Boss SFX
- Resource und Reward SFX
- UI SFX
- Ambiente oder atmosphärische Layer

## Visual

Wichtige Audio-Signale müssen visuell unterstützt werden, besonders bei Gefahr, Boss-Angriffen oder Core-Schaden.

Audio und Visual sollen gemeinsam dieselbe Prioritätensprache sprechen.

## Balancing

Audio-Mix muss klare Prioritäten zwischen Information, Feedback und Atmosphäre setzen.

Wichtige Ereignisse dürfen nie im Lärm vieler kleiner Effekte untergehen.

Musikwechsel und Reward-Sounds sollen Höhepunkte verstärken, ohne Ermüdung zu erzeugen.

## Open Questions

- Welche Ducking-Regeln sind zwischen UI, Music und Combat-SFX nötig? (`MixerBusData`/`AudioPriorityData` aus dem Data Model sind noch nicht als eigene Klassen angelegt)
- Wie viele Audio-Variationen braucht jede Kernaktion für ausreichende Langzeitwirkung, sobald echte Audiodateien statt Variations-Pitching auf einem Clip vorliegen?

Festgelegte Entscheidungen:

- Musik eskaliert über drei feste Zustände statt kontinuierlicher Layer-Überblendung: Calm (zwischen Waves), Combat (aktive Wave) und Boss (Boss-Encounter), mit Crossfade zwischen den Zuständen statt hartem Schnitt — das erfüllt „Treibender Soundtrack" ohne die Komplexität eines vollständigen adaptiven Musiksystems.
- `AudioCueData.FrequencyVariations` verlangt mindestens 3 Variationen für häufige Kernaktionen (aktuell durchgesetzt für Combat-Hit/-Death), damit „Satte Treffergeräusche" nicht durch Wiederholung ermüden.

## Acceptance Criteria

- Audio wird zentral, eventbasiert und kategorisiert verwaltet.
- Wichtige Gameplay-Signale bleiben im Mix priorisiert und lesbar.
- Settings können relevante Audio-Kategorien sauber steuern.
- Neue Systeme können AudioEvents ohne Sonderarchitektur ergänzen.
- Audio unterstützt Combat, Menüs und Rewards konsistent.
