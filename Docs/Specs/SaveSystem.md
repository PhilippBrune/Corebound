# Save System Specification

## Purpose

Definiert das SaveSystem für persistente Daten des Games.

Das SaveSystem speichert Meta Progression, Settings, Statistiken und fortsetzbare Run-Zustände getrennt und zuverlässig.

## Design Goals

- Hohe Zuverlässigkeit und Integrität der gespeicherten Daten
- Klare Trennung unterschiedlicher Speicher-Domains
- Unterstützung für Continue-Flows ohne exploitbares Save-Scumming im aktiven Combat
- Versionierbarkeit und Migrationsfähigkeit
- Geringe Kopplung zwischen SaveSystem und Gameplay-Systemen

## Gameplay

Das SaveSystem arbeitet im Hintergrund und unterstützt den Player beim Fortsetzen eines Runs, beim Behalten von Meta Progression und beim dauerhaften Speichern von Settings.

Meta-Fortschritt wird nach relevanten Änderungen gesichert.

Settings werden beim Anwenden oder Schließen sauber persistiert.

Run-Zustände werden an sicheren Punkten wie Menü-Übergängen, Stage-Übergängen, Intermissions oder beim geordneten Verlassen des Spiels aktualisiert.

## Rules

- Meta Progression, Settings, Statistiken und Run-Daten werden logisch getrennt gespeichert.
- Schreibvorgänge müssen atomar oder mit Fallback/Backup abgesichert sein.
- Es gibt kein manuelles Quick-Save im aktiven Combat.
- Continue-Daten dürfen nur gültige, vollständige Run-Zustände repräsentieren.
- Versionen und Migrationspfade müssen im Save-Format berücksichtigt werden.
- SaveSystem darf keine Fachlogik für Combat, Economy oder UI enthalten.
- Fehler beim Laden oder Speichern müssen kontrolliert behandelt und dem Player verständlich kommuniziert werden.

## Data Model

Das SaveSystem nutzt serialisierbare Datenmodelle pro Domain.

- SaveSlotMetadata: Version, Zeitstempel, Status und Zusammenfassung des Speicherstands
- MetaProgressionSaveData: persistente Currency, Unlocks und Track-Zustände
- SettingsSaveData: alle anpassbaren Optionen und Binding-Verweise
- RunSaveData: fortsetzbarer Zustand eines aktuellen Runs inklusive Stage-, Build- und Wallet-Zustand
- StatisticsSaveData: aggregierte Daten wie besiegte Enemies, absolvierte Runs oder andere Kennzahlen

## Technical Design

Ein SaveManager koordiniert Laden, Speichern, Versionierung und Fehlerbehandlung.

Serialisierung und Datei-I/O werden von Gameplay-Komponenten getrennt gehalten.

Systeme liefern nur Save-Read-Modelle oder konsumieren geladene Zustände über definierte Schnittstellen.

Events wie OnSaveRequested, OnSaveCompleted, OnLoadCompleted und OnSaveFailed informieren UI und globale Flows.

Der erste Vertical Slice nutzt einen lokalen JSON-basierten Profile-Store mit getrennten Dateien für RunSaveData, SettingsSaveData und MetaProgressionSaveData. Continue im Main Menu liest seinen Status aus dieser Save-Domain statt aus einem serialisierten Scene-Stub.

RunSaveData enthält im ersten Slice bereits Weapon-Referenz, Pending-Reward-Status und Intermission-Status. Ein RunPersistenceController schreibt Checkpoints aus Combat, Reward und Intermission der SampleScene in diese Run-Domain.

Zusätzlich speichert RunSaveData im ersten Slice ausgewählte Card-IDs und aktive Tower-IDs, damit Reward-, Resource- und Tower-Hooks denselben persistierten Run-Zustand fortschreiben.

## Dependencies

- MetaProgression
- Settings
- Player
- Cards
- Resources
- Towers
- Waves
- MainMenu
- UI
- Audio

## UI

Save-bezogene UI zeigt mindestens:

- Continue-Verfügbarkeit
- Speichern- oder Laden-Indikatoren, wenn nötig
- verständliche Fehlermeldungen
- klare Warnungen bei Überschreibung relevanter Daten

## Audio

Speichern und Laden benötigen nur zurückhaltendes Feedback.

Fehlerzustände sollen akustisch unterscheidbar, aber nicht aufdringlich sein.

## Visual

Save-relevante Zustände werden funktional und knapp kommuniziert.

Das Main Menu soll Continue oder fehlerhafte Zustände klar voneinander trennen.

## Balancing

Das SaveSystem soll die Session-Freundlichkeit erhöhen, ohne Risiko aus aktiven Kämpfen zu entfernen.

Run-Fortsetzen dient Komfort und Stabilität, nicht freiem Manipulieren jedes Encounter-Ausgangs.

Persistenzhäufigkeit muss Sicherheit bieten, darf aber Performance oder Dateigröße nicht unnötig belasten.

## Open Questions

- Soll es Cloud-Save-Unterstützung geben? (Auf Steam überwiegend Konfiguration, kein Code.)

Beantwortet 2026-09-22: **drei Profile**, jedes mit eigenem, vollständig getrenntem Speicherstand
(siehe "Drei Profile" unten). Innerhalb eines Profils gibt es weiterhin genau einen fortsetzbaren
Run. Die Granularität von `RunSaveData` (Stage, Wave, Wallet, Cards, Relics, Towers, Loadout,
Pending-Zustände) hat sich über mehrere Slices als ausreichend erwiesen.

## Acceptance Criteria

- SaveSystem speichert Meta, Settings, Statistics und Run-Daten getrennt und versionierbar.
- Continue-Flows sind mit Main Menu und Run-Struktur konsistent.
- Speichervorgänge sind robust gegen unvollständige oder fehlerhafte Zustände abgesichert.
- Gameplay-Systeme bleiben lose gekoppelt und liefern nur benötigte Zustandsdaten.
- Das System verhindert manuelles Quick-Save-Scumming im aktiven Combat.
- MainMenu und Meta- oder Settings-Routen können ihren Status aus echten Save-Domains statt aus Szenen-Stubs ableiten.
- Continue kann im ersten Slice einen tatsächlich geschriebenen Combat-Checkpoint statt nur Bootstrap-Daten laden.

---

## Drei Profile (2026-09-22)

Owner-Entscheidung: Das Spiel hat **drei Profile**. Jedes besitzt einen vollständig eigenen
Speicherstand - Meta-Fortschritt, Settings, Statistiken und den fortsetzbaren Run.

| Regel | Wert |
| --- | --- |
| Anzahl Profile | 3, fest |
| Ordner | `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Corebound/Profile_01 .. _03` |
| Inhalt pro Profil | `run.json`, `settings.json`, `meta.json`, `statistics.json` |
| Aktives Profil | im Hauptmenü gewählt, gespeichert in `profile.json` neben den Profilordnern |
| Wechsel | nur im Hauptmenü, nie während eines Runs |
| Löschen | pro Profil, mit Bestätigung; legt das Profil leer neu an |

Ein Profil, das noch nie benutzt wurde, zeigt "Empty"; sonst Ascension-Rang, freigeschaltete Stages
und die gespielte Zeit, damit die drei unterscheidbar sind.

`LocalProfileSaveManager` hält den Profilnamen nicht mehr als feste `Profile_01`-Konstante, sondern
liest ihn beim Start aus `ProfileSelection` und baut die Pfade darüber auf. Settings sind bewusst
Teil des Profils und nicht global: zwei Personen an einem PC erwarten getrennte Lautstärken und
Tastenbelegungen.

## Lebensdauer von Services (2026-09-22)

Ein Bug, der einen halben Tag gekostet hat (zwei Musikspuren nach dem Tod), kam daher, dass dieses
Dokument nichts über Objektlebensdauer sagte:

- **Genau ein Service überlebt einen Szenenwechsel: der Save-Manager.** Er liegt dafür auf einem
  eigenen, sonst leeren GameObject, niemals auf einem geteilten.
- Jede Szene besitzt ihre eigenen Instanzen von AudioManager, SettingsManager, MusicStateController,
  InputRebindManager und MetaProgressionService. Sie werden nie persistiert.
- Eine zweite Instanz des Save-Managers zerstört sich nicht, sondern **leitet weiter** an die erste.
  So teilen alle Szenen einen Cache statt getrennter Kopien.
- `DontDestroyOnLoad(gameObject)` auf einem GameObject, das noch andere Komponenten trägt, ist
  verboten - es schleppt jede davon mit.
