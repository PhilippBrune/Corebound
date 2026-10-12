# Settings Specification

## Purpose

Definiert das Settings-System für konfigurierbare Spieloptionen.

Settings müssen dauerhaft gespeichert, controller-freundlich bedienbar und sicher auf unterschiedliche Hardware anpassbar sein.

## Design Goals

- Klare, verständliche Optionen mit sicheren Standardwerten
- Vollständige Controller-Bedienung der Settings-Menüs
- Getrennte Persistenz von Settings gegenüber Run- und Meta-Daten
- Unterstützung für Audio, Grafik, Controls und relevante Accessibility-Optionen
- Erweiterbarkeit ohne Umbau des Menüroutings

## Gameplay

Settings können aus dem Main Menu und aus erlaubten In-Game-Menüs geöffnet werden.

Der Player passt dort Audio, Grafik, Steuerung und andere freigegebene Optionen an.

Änderungen sollen nachvollziehbar bestätigt werden und im Fall sensibler Optionen klar zeigen, ob sie sofort oder erst nach Bestätigung wirksam werden.

Das Settings-System unterstützt Komfort und Lesbarkeit, ohne Kernentscheidungen des Spiels zu unterlaufen.

## Rules

- Settings werden getrennt von Run- und Meta-Fortschritt gespeichert.
- Jede Einstellung besitzt einen definierten Standardwert.
- Änderungen müssen per Controller vorgenommen, bestätigt und zurückgesetzt werden können.
- Kritische Änderungen wie Grafikmodus oder Auflösung benötigen sichere Bestätigungsabläufe.
- Gameplay-relevante Komfortoptionen dürfen Lesbarkeit und Zugänglichkeit verbessern, aber keine dominante Vorteilsschleife schaffen.
- Settings-Änderungen kommunizieren ihren aktuellen Status klar im UI.
- Neue Settings werden in vorhandene Kategorien integriert statt als Spezialmenüs verteilt.

## Data Model

Settings-Konfiguration wird datengetrieben beschrieben.

- SettingsDefinitionData: Kategorien, Sichtbarkeit und Default-Werte
- SettingsOptionData: einzelne Optionen, Wertebereiche und Präsentationsregeln
- QualityPresetData: Grafik- oder Performance-Presets
- AccessibilityOptionData: Optionen wie Screenshake-Stärke, Effektintensität oder Zielhilfe-Anteile
- InputSettingsData: Bindings, Deadzones oder Sensitivitätsoptionen in Verbindung mit Input

## Technical Design

Ein SettingsManager lädt, validiert, anwendet und speichert Settings zentral.

Subsysteme wie Audio, Input oder Grafik abonnieren Änderungen über Events wie OnSettingChanged, OnSettingsApplied und OnSettingsReset.

UI liest Definitionsdaten und baut Menüs ohne harte Sonderfälle pro Option auf.

Persistenz und Versionswechsel liegen im SaveSystem.

Der erste MainMenu-Slice nutzt einen echten SettingsManager mit datengetriebenen Optionsdefinitionen, Preview- und Confirm-Flow sowie eine controller-freundliche SettingsRouteTargetController- und Presenter-Kombination innerhalb der MainMenu-UI-Struktur.

AudioManager, InputRebindManager sowie Screen- und QualitySettings werden im ersten Slice bereits direkt durch Settings-Werte angesteuert. Damit sind Audio-Kategorien, Control-Profile, Auflösung, Fullscreen und VSync nicht nur UI-Platzhalter.

**Display Mode (2026-10-12, Grafik).** Vier Modi, gespeichert als `_fullscreenModeIndex` (die Indizes wandern nie): 0 **Windowed** - Fenster mit Titelleiste, nie größer als der Bildschirm (ein zu großes Preset rutscht auf das größte, das mit Titelleiste und Taskleiste passt); 1 **Borderless Fullscreen**; 2 **Exclusive Fullscreen**; 3 **Frameless Window** (angehängt) - ein Fenster in der gewählten Auflösung ohne Titelleiste und Rahmen, mittig auf seinem Monitor. Unity kennt kein rahmenloses *Fenster*, also entfernt `WindowModeController`/`FramelessWindowStyle` nach Unitys Größenänderung den Rahmen per Win32 und prüft danach die echte Fenstergröße; das gibt es nur im Windows-Player, im Editor ist Modus 3 ein normales Fenster. Im Player geprüft: Frameless 1280 × 720 ohne Rahmen, mittig, Unity rendert 1280 × 720; Windowed 1600 × 900 Client mit Titelleiste. Nicht geprüft: das Umschalten zwischen den Modi zur Laufzeit und Exclusive Fullscreen (es hätte den Monitor umgeschaltet).

**Core Alerts (2026-10-12, Accessibility, Standard: an).** Schaltet die Bedrohungslinien zum Kern, die Randmarkierung bei einem Kern außerhalb des Bildes und den Bildschirmrand-Puls bei einem Kerntreffer (`Specs/Core.md`, "Protecting the Core"). Das Glühen des Kerns bleibt immer an. `SettingsOptionKey.ShowCoreAlerts` (angehängt, Index 15), `SettingsSaveData._showCoreAlerts`.

**`SettingsSaveData.Clone()` muss jedes Feld kopieren.** "Übernehmen" speichert einen Klon der bearbeiteten Kopie; ein Feld, das im Klon fehlt, fällt beim Speichern still auf den Standard zurück. Bis 2026-10-12 fehlten `_showRunTimer` und `_inputDeviceIndex` - "Show Run Timer" und "Input Device" ließen sich deshalb nie dauerhaft setzen.

**Offen:** Die Beschriftungen und Beschreibungen der Optionen stehen nur auf Englisch im Asset (`SettingsDefinition_Default`) und erscheinen auch in der deutschen Fassung englisch; nur die Werte ("An"/"Aus") und die Knöpfe sind übersetzt.

## Dependencies

- Audio
- Input
- UI
- SaveSystem
- MainMenu

## UI

Settings-UI zeigt mindestens:

- Kategorienavigation
- aktuellen Wert je Option
- Standardwert oder Reset-Möglichkeit
- eindeutige Bestätigen- und Abbrechen-Flows

## Audio

Änderungen an Lautstärke oder relevanten Audio-Optionen sollten direkt probehörbar sein.

UI-Audio für Navigation und Bestätigung bleibt auch innerhalb der Settings konsistent.

## Visual

Settings-Menüs priorisieren Lesbarkeit, Fokusführung und knappe Erklärtexte.

Lange Listen müssen mit Controller effizient navigierbar bleiben.

## Balancing

Standardwerte sollen auf breiter Hardware und für neue Spieler zuverlässig funktionieren.

Accessibility- und Komfortoptionen sollen Barrieren senken, ohne die Designintention unkenntlich zu machen.

Die Zahl der Optionen im ersten Milestone sollte fokussiert bleiben und nur echte Nutzenfälle abdecken.

## Open Questions

- Welche Accessibility-Optionen sind für den ersten Milestone verpflichtend?
- Wie fein granular sollen Grafikoptionen im Vergleich zu Presets werden?

Beantwortet 2026-09-22:

- *Zielhilfe global, pro Weapon oder pro Gerät?* Global für Controller, gewichtet pro Weapon über das
  Low/Medium/High-Profil in `WeaponData`; Maus und Tastatur bekommen keine Zielhilfe (siehe
  [Combat.md](Combat.md)). Dazu die Option **Eingabegerät: Automatisch / Controller / Maus**, damit die
  Geräteerkennung übersteuerbar ist (siehe [Input.md](Input.md)).
- *Wo sind Settings erreichbar?* Hauptmenü und Pause-Overlay (vierter Tab, mindestens Lautstärken und
  Eingabegerät). Auflösung und Vollbild bleiben dem Hauptmenü vorbehalten.

## Acceptance Criteria

- Settings sind persistent, controller-freundlich und klar kategorisiert.
- Audio-, Grafik-, Control- und ausgewählte Accessibility-Optionen sind sauber integrierbar.
- Änderungen werden zentral verwaltet und eventbasiert an Subsysteme verteilt.
- Kritische Optionen besitzen sichere Bestätigungsabläufe.
- Neue Settings lassen sich ohne Sondermenüs ergänzen.
- Die Settings-Route ist im Main Menu als echtes Optionsziel verdrahtet und nicht nur als statische Overlay-Zusammenfassung vorhanden.
- Audio-, Rebind- und grundlegende Grafikoptionen wirken im ersten Slice bereits direkt auf die Laufzeit.
