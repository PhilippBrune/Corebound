# Waves Specification

## Purpose

Definiert das Wave-System als strukturierende Taktung eines Runs.

Waves erzeugen Druck, geben dem Combat einen Rhythmus und liefern die wichtigsten Reward-Momente zwischen Kämpfen.

## Design Goals

- Klarer Spannungsbogen innerhalb jeder Stage
- Short Feedback Loop mit regelmäßigen Entscheidungen
- Gute Verknüpfung von Combat, Rewards, Build-Planung und Boss-Pacing
- Hohe Lesbarkeit von Phase, Gefahr und Fortschritt
- Modulare, datengetriebene Erstellung neuer Stages und Waves

## Gameplay

Eine Stage besteht aus einer geordneten Folge von Waves und endet in einer Boss-Begegnung.

Jede Wave startet, spawnt definierte Enemy-Gruppen, endet nach dem Besiegen aller zugewiesenen Gegner und führt anschließend in einen Reward- oder Intermission-Moment.

Zwischen Waves trifft der Player Build-Entscheidungen wie Card-Auswahl, Tower-Planung oder Economy-Nutzung.

Das Wave-System steuert Tempo und Schwierigkeit, ohne selbst Combat- oder Reward-Logik zu enthalten.

## Rules

- Wave-Ablauf folgt stets derselben Grundstruktur: Start, aktiver Kampf, Abschluss, Belohnung, nächste Entscheidung.
- Spawn-Positionen müssen fair und lesbar sein.
- Schwierigkeitssteigerung erfolgt über Enemy-Komposition, Spawn-Rhythmus und Raumdruck.
- Elite-Waves und Boss-Vorbereitungen sind Datenvarianten des bestehenden Wave-Rahmens.
- Eine Wave darf den Player fordern, aber nicht ohne lesbare Ursache überrollen.
- Zwischenphasen müssen controller-freundlich navigierbar bleiben.
- Das Wave-System koordiniert andere Systeme, enthält aber keine direkte Spiellogik dieser Systeme.

## Data Model

Wave-Daten werden in ScriptableObjects definiert.

- StageData: Reihenfolge von Waves, Stage-Metadaten und Boss-Referenz
- WaveData: Spawnlisten, Abschlussbedingungen, Reward-Kontext und Modifikatoren
- SpawnGroupData: Enemy-Typen, Mengen, Staffelung und Prioritäten
- WaveModifierData: optionale Daten-Overrides für Tempo, Dichte oder Encounter-Regeln
- WaveRewardData: definierte Reward-Momente nach Wave-Abschluss
- StageEnemyPoolData: bewusst zurückgestelltes Datenfeld für eine spätere gewichtete
  Zufallsauswahl von Enemy-Typen pro Stage (statt fest kuratierter SpawnGroupData-Listen).
  Aktuell von keinem Code gelesen; geplant für Stage 2+, sobald Zufallsvarianz innerhalb
  einer Wave gewünscht ist.

## Technical Design

Ein WaveManager verwaltet Sequenz, Timer, Spawn-Freigaben und Übergänge zwischen Zuständen.

Enemy-Spawns erfolgen über Spawn-Services und Daten, nicht über hartkodierte Abläufe.

Andere Systeme werden über Events wie OnWaveStarted, OnWaveCompleted, OnIntermissionStarted und OnStageCompleted informiert.

Der WaveManager darf keine Combat-Berechnungen, Card-Anwendung oder UI-Logik enthalten.

Der erste Vertical Slice nutzt StageData, WaveData und SpawnGroupData bereits direkt in einer SampleScene-WaveManager-Instanz. Reward-Pending und Intermission werden erst nach vollständigem Wave-Abschluss statt nach Einzelkills ausgelöst.

## Dependencies

- Enemies
- Bosses
- Combat
- Resources
- Cards
- Economy
- Towers
- UI
- Audio

## Linked Slice Files

- [Bosses Stage 1 Slice](../Slices/12_BOSSES_STAGE1_SLICE.md)
- [Elite Variants Slice](../Slices/13_ELITE_VARIANTS_SLICE.md)
- [Wave Boss Balance Slice](../Slices/14_WAVE_BOSS_BALANCE_SLICE.md)

## UI

Wave-UI zeigt mindestens:

- aktuelle Wave oder Stage
- Start- und Abschlusszustand
- relevante Warnhinweise
- verbleibende Kerninformation zum Fortschritt

Zwischen-Wave-Oberflächen müssen ohne Mauspflicht erreichbar sein.

## Audio

Wave-Start, Eskalation, Abschluss und Boss-Übergang benötigen klare Audio-Signale.

Audio darf Spannung aufbauen, ohne wichtige Combat-Warnungen zu verdecken.

## Visual

Wave-Übergänge werden durch kurze, gut lesbare visuelle Marker oder Arena-Zustände unterstützt.

Boss- oder Elite-Vorbereitungen müssen vor Kampfbeginn verständlich erkennbar sein.

## Balancing

Zwischen zwei wichtigen Entscheidungen sollen möglichst nicht mehr als wenige Minuten liegen.

Frühe Waves lehren Grundmuster, mittlere Waves prüfen Build-Richtung, späte Waves verdichten Gefahr und Tempo.

Stage-Länge und Wave-Anzahl müssen zum Ziel eines etwa 60 bis 70 Minuten langen Runs passen.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Standard-Intermission beträgt 20 Sekunden und kann per Bestätigung vorzeitig beendet werden.
- Jede Stage nutzt im ersten Scope eine feste Struktur aus sechs Combat Waves und einer Boss-Begegnung.
- Die erste Elite Wave erscheint in Stage 1 ab Wave 5.

## Acceptance Criteria

- Waves strukturieren den Run in wiederholbare, klar lesbare Kampf- und Entscheidungsphasen.
- Stage- und Wave-Inhalte sind überwiegend datengetrieben definierbar.
- Andere Systeme reagieren eventbasiert auf Wave-Übergänge.
- Schwierigkeit eskaliert über Encounter-Design und nicht primär über reine Stat-Erhöhung.
- Controller-Navigation zwischen Combat und Reward-Momenten bleibt konsistent.
- Der erste Slice führt Reward und Intermission über einen echten wiederholbaren Wave-Loop statt über einen einzelnen Testgegner aus.
