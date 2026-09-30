# Stages Specification

## Purpose

Definiert Stages als größere Progressions- und Themenblöcke innerhalb eines Runs.

Eine Stage bündelt Waves, Rewards, Elite-Druck, Boss-Finale und den thematischen Rahmen eines Encounter-Abschnitts.

## Design Goals

- Klarer Makro-Fortschritt innerhalb eines Runs
- Saubere Struktur oberhalb einzelner Waves und unterhalb des Gesamt-Runs
- Gute Kombination aus thematischer Identität, Difficulty-Pacing und Build-Entscheidungen
- Modulare Erweiterbarkeit für neue Stage-Themen, Enemy-Pools und Bosses
- Datengetriebene Definition ohne Stage-spezifische Sonderarchitektur

## Gameplay

Ein Run besteht aus einer Folge von Stages — laut Project Vision's Gameplay Loop: `Kleine Arena → Gegner → Ressourcen → Türme → Karten → Build → Elite → Boss → Neue Stage → Repeat → Final Boss → Run beendet`. Der `Neue Stage`-Schritt wiederholt sich mehrfach pro Run, bevor der Run in den `Final Boss` mündet — ein Run ohne mindestens eine Stage-Progression über Stage 1 hinaus erfüllt diesen Loop nicht vollständig.

Jede Stage besitzt eine feste Zusammenstellung aus Waves, Reward-Momenten, potenziellen Elite-Begegnungen und einem abschließenden Boss.

Stages geben dem Run Struktur, erzeugen Fortschrittsgefühl und erhalten über Arena-Thema, Enemy-Pool und Reward-Druck eine eigene Identität — genau die Art von Build-Kontext-Wechsel, die Vision's Pillar „Build Diversity" von einem Run zum nächsten fordert.

Nach Abschluss einer Stage wechselt der Run in die nächste größere Einheit oder endet beim finalen Sieg.

### Stage-Progression (mehrere Stages)

Der erste Vertical Slice liefert nur Stage 1. Die Spec beschreibt hier bewusst bereits die Struktur für Stage 2+, damit neue Stages ausschließlich durch neue Daten entstehen (`StageData`/`StageThemeData`/stage-eigene `WaveData`-Assets), nicht durch neue Architektur — passend zu Vision's Technical Vision „Neue Inhalte entstehen durch Kombination vorhandener Systeme, nicht durch ständig neue Mechaniken."

**Update 2026-09-14 — 5-Stage-Konzept ausgearbeitet:** Playtest-Feedback fragte nach einem vollständigen Konzept für bis zu 5 Stages (Visual Design, Enemies, Maps, Musik). Die vorherige Tabelle war ein grober Platzhalter für Stage 2/3 und hatte keine Stage 4/5; die Tabelle unten ersetzt sie durch ein vollständig ausgearbeitetes 5-Stage-Konzept. Stage 1 und 2 haben bereits Daten-Assets (Stage 2 noch ohne dedizierte Kunst); Stage 3-5 sind neu. Passende Asset-Prompts stehen in [GRAPHICS_ASSET_PROMPTS.md](../AI/GRAPHICS_ASSET_PROMPTS.md) (Asset 47-58) und [AUDIO_ASSET_PROMPTS.md](../AI/AUDIO_ASSET_PROMPTS.md) (Audio 20-23). Karten müssen laut Feedback nicht symmetrisch sein — Stage 2-5 nutzen deshalb bewusst asymmetrische Arena-Layouts statt Stage 1's symmetrischem Maze-Raster, was `StageArenaMazeGenerator`/`StageArenaArtPresenter` an einer Stelle erweiterbar machen muss (siehe Technical Design unten), aber pro Stage weiterhin nur über Daten (`StageThemeData`), nicht über neue Architektur.

| Stage | Arbeitstitel | Thematischer Rahmen | Palette | Enemy-Pool-Akzent | Map-Charakter | Boss-Rolle | Musik |
|---|---|---|---|---|---|---|---|
| 1 | Foundry Perimeter | Industrielle Ritual-Plattform (bereits implementiert) | warmes Stahlblau + Amber | Chaser/Ranged/Bruiser-Grundrollen, ausgeglichen | symmetrisches Maze-Raster (bereits implementiert) | Einführungs-Boss, lehrt Phasenwechsel | bestehende Calm/Combat/Boss-Loops |
| 2 | Reactor Depths | Vertikale Reaktorkammer, freiliegende Leitungen, pulsierende Containment-Felder (Daten existieren als `Stage_02`, Kunst fehlt noch) | kühles Blau-Violett + Cyan | Ranged-Pressure-Schwerpunkt, höhere Elite-Dichte (bereits in `StageEnemyPool_Stage2`) | asymmetrisch: Reaktorkern liegt außermittig, diagonale Kühlrohr-Korridore statt Raster, unregelmäßige Sichtlinien begünstigen die Ranged-Gegner | "Reactor Sentinel" (bereits benannt in `Boss_Stage2`): Area-Denial-Phase mit sich ausbreitenden Containment-Breach-Zonen | Combat-Loop unverändert, Boss-Loop bekommt zusätzliches Alarm-/Containment-Motiv als Layer |
| 3 | Salvage Fields | Offenes Schrottfeld mit havarierten Mech-Wracks und freiliegender Bewehrung | rostiges Orange + Verdigris-Grün | Bruiser-lastig, mehrere Elite-Varianten gleichzeitig aktiv | asymmetrisch: offenes Feld mit wenigen großen Wrackhaufen statt Maze-Zellen, breite Anlaufwege passend zum Bruiser-Schwerpunkt | Boss mit Summon-/Add-Phase, ruft Schrott-Drohnen aus den Wrackhaufen | griffigere, perkussivere Combat-Variante über dem bestehenden Combat-Bett |
| 4 | Cryo Vaults | Gefrorene Kühlmittel-/Kryostase-Anlage mit vereisten Leitungen und rissigen Eisbodenplatten | eisiges Weiß-Blau + blasses Cyan | nutzt bestehende Rollen, aber mit verstärktem "Frost"-Elite-Modifier-Akzent (aktiviert das bereits vorhandene, bisher ungenutzte `vfx_freeze_status_pulse.png`) | asymmetrisch: enge Eiskorridore wechseln mit offenen aufgetauten Kammern, erzwingt Rhythmuswechsel zwischen eng und offen | "Cryo Warden": Frost-Puls-AOE-Phase, die kurz verlangsamt/festhält statt reinen Schaden zu machen | kälterer, spärlicherer Combat-Loop; Boss-Loop mit hoher, gläserner Leitmotiv-Ebene |
| 5 (letzte) | Core Nexus | Finale, hochkontrastige Kernkammer; Architektur zitiert alle vier vorherigen Stage-Paletten als "Best-of"-Abschluss | Weiß-Gold gegen tiefes Schwarz | volle Mischung aller bisherigen Rollen und Elite-Varianten gleichzeitig | asymmetrisch, größte Arena des Runs: offener zentraler Kern-Dais umgeben von Terrain-Fragmenten der vorherigen vier Stages | Final Boss, höchste Phasenzahl, kombiniert Area-Denial (Stage 2) + Summon (Stage 3) + Frost-AOE (Stage 4) in einem Kampf | intensivste Boss-Loop-Variante, löst die Leitmotive aus Stage 2-4 musikalisch auf |

Jede weitere Stage folgt demselben Muster: eigenes `StageThemeData` (Arena-Look), eigene `WaveData`-Assets (Zusammensetzung und Tempo aus bestehenden Enemy-Rollen, keine neuen Rollen zwingend nötig; `StageEnemyPoolData` wurde 2026-09-30 gelöscht), ein eigenes `BossData`, und optional ein eigenes `ShopOfferTableData` für Stage-passende Angebote. Die Reihenfolge und Freischaltung mehrerer Stages wird über `StageProgressionData` verwaltet (siehe Data Model) — `StageData.NextStageData` verkettet Stages bereits heute linear, das reicht für eine feste Reihenfolge ohne Branching.

**Update 2026-09-08:** `Stage_02` ("Reactor Depths") existiert jetzt als vollständig autoriertes Daten-Beispiel für dieses Muster — eigenes `StageTheme_Stage2` (dieselbe Stage-1-Geometrie, aber kühl blau-violett statt warm stahlblau getönt, da noch keine dedizierte Stage-2-Kunst existiert), eigenes `StageEnemyPool_Stage2` (Ranged-Pressure-Schwerpunkt statt Chaser-Schwerpunkt), eigener `Boss_Stage2` ("Reactor Sentinel", höhere Health/Speed als Boss_Stage1) und Verkettung über `StageProgression_VerticalSlice`. Die 6 Waves sind bewusst mit Stage 1 identisch (dieselben `WaveData`-Assets), um den Scope auf die Stage-Ebene zu begrenzen. **Update 2026-09-09 — implementiert:** `WaveManager.CompleteStage()` schaltet jetzt tatsächlich auf `StageData.NextStageData` um, statt es nur für einen Anzeigetext zu lesen: neue `AdvanceToNextStage()`-Methode weist `_stageData` neu zu, setzt Wave-Index und Combat-Zustand zurück, ruft `StageArenaArtPresenter.RebuildForNewStage()` (neue Methode — zerstört die alte Arena-Geometrie sauber und baut sie mit dem neuen Stage-Theme neu auf, statt Tiles zu duplizieren) und startet die erste Wave der neuen Stage. `Stage_01._stageRewardData.EndRunOnCompletion` steht jetzt auf `false`, `Stage_02` bleibt `true` (letzte Stage der Kette). Stage 1 → Stage 2 ist damit innerhalb einer laufenden Session vollständig spielbar.

**Update 2026-09-16 — behoben:** Die vormalige "Bekannte Grenze" (ein frischer App-Start mitten in Stage 2+ fiel auf die im Szenen-Component fest hinterlegte `StageData` zurück) ist behoben, ohne eine neue Registry-Struktur einzuführen: `WaveManager.ResolveStageDataForSave()` löst `RunSaveData.StageIndex` gegen `StageProgressionData.Stages` auf - dieselbe geordnete Liste, die `AdvanceToNextStage()` zur Laufzeit bereits durchläuft. `InitializeFromSave()` ruft das jetzt vor jeder anderen Save-Verarbeitung auf und stößt bei Bedarf `StageArenaArtPresenter.RebuildForNewStage()` an, da die Arena-Visuals (`Awake()`) sonst vor der Stage-Auflösung (`OnEnable()`) bauen würden. Ein Beenden-und-Fortsetzen mitten in Stage 2+ funktioniert damit jetzt wie ein normaler Session-Übergang.

## Rules

- Eine Stage besteht aus mehreren Waves, Rewards, optionalen Elite-Druckmomenten und einem Boss.
- Stages nutzen bestehende Systeme wie Waves, Enemies, Bosses, Cards, Towers und Resources.
- Der erste Vertical Slice nutzt eine feste Stage-Struktur statt verzweigter Stage-Branches.
- Jede Stage besitzt im ersten Scope genau einen Boss.
- Eine Stage darf Schwierigkeit und Thema verdichten, aber keine neue Kernmechanik exklusiv für sich einführen.
- Stage-Übergänge müssen für den Player klar erkennbar und ausreichend belohnend sein.
- Stage-Daten bleiben vollständig vom UI- und Combat-Code entkoppelt.

## Data Model

Stage-Daten werden über ScriptableObjects definiert.

- StageData: Identität, Reihenfolge der Waves, Boss-Referenz und Meta-Daten
- StageThemeData: Arena-Thema, Farbwelt, Props, Hintergrund und Präsentation
- StageRewardData: Stage-Abschlussbelohnungen und Übergangslogik
- ~~StageEnemyPoolData~~ (2026-09-30 gelöscht; die stage-eigenen `WaveData`-Assets legen die Gegner fest)
- StageProgressionData: Stage-Reihenfolge, Freischaltung und Skalierungsregeln

## Technical Design

Ein Stage-Service oder WaveManager auf höherer Ebene verwaltet aktive Stage-Daten, Stage-Start und Stage-Abschluss.

Stages koordinieren vorhandene Wave-, Enemy- und Boss-Systeme über Daten und Events statt über separate Sonderlogik.

Im ersten Vertical Slice übernimmt der WaveManager diese Rolle direkt und emittiert StageStarted, StageCompleted und StageTransitioned für UI, Audio, SaveSystem und Reward-Flows.

**Asymmetrische Maps (2026-09-14):** Stage 1 ist die einzige Stage mit fest verdrahteter, symmetrischer Maze-Generierung (`StageArenaMazeGenerator`, `StageArenaArtPresenter.CreatePropClusters`/`Stage1PropPlacements`). Das 5-Stage-Konzept oben braucht pro Stage ein eigenes, teils asymmetrisches Layout (Stage 2: diagonale Korridore, Stage 3: offenes Feld mit wenigen großen Wrackhaufen, Stage 4: enge/offene Wechsel, Stage 5: zentraler Dais mit Terrain-Fragmenten). Das erfordert einen einmaligen Architektur-Schritt (kein Pro-Stage-Sonderweg): `StageThemeData` um ein austauschbares Layout-Datenfeld erweitern (z. B. eine pro-Stage-Platzierungsliste analog zu `Stage1PropPlacements`, oder ein einfacher Layout-Algorithmus-Enum), das `StageArenaArtPresenter` statt der heute hart codierten Stage-1-Geometrie liest. Bis dahin fallen neue Stages beim Rebuild weiterhin auf Stage 1's Geometrie zurück (nur Textur/Tint wechseln bereits über `StageThemeData`).

## Dependencies

- Waves
- Enemies
- Bosses
- Cards
- Resources
- Economy
- UI
- Audio
- SaveSystem
- MetaProgression

## Linked Slice Files

- [Bosses Stage 1 Slice](../Slices/12_BOSSES_STAGE1_SLICE.md)
- [Elite Variants Slice](../Slices/13_ELITE_VARIANTS_SLICE.md)
- [Wave Boss Balance Slice](../Slices/14_WAVE_BOSS_BALANCE_SLICE.md)

## UI

Die UI zeigt aktuelle Stage, Stage-Wechsel und Stage-Abschlussmomente klar an.

Stage-Kommunikation soll Fortschrittsgefühl erzeugen, ohne das HUD zu überladen.

## Audio

Stages können eigene Musik- oder Stimmungswechsel auslösen, bleiben aber im selben Audio-Regelwerk wie Waves und Bosses.

Stage-Übergänge und Abschlüsse brauchen klar erkennbare, nicht überlange Signale.

## Visual

Jede Stage sollte eine thematische visuelle Identität besitzen, die Arena, Props, Farbklima und Reward-Momente beeinflusst.

Die aktuelle Stage-1-Implementation nutzt dazu den modular beschriebenen Ground-Atlas aus [VFX_stage1_ground_tileset.md](./VFX_stage1_ground_tileset.md) sowie den Prop-Atlas aus [VFX_stage1_prop_set.md](./VFX_stage1_prop_set.md).

Stage-Reward- und Übergangsmomente greifen auf die dokumentierte Effektfamilie aus der [VFX Specification](./VFX.md) zurück.

Die 2D-Top-Down-Lesbarkeit bleibt dabei immer wichtiger als dekorative Dichte.

## Balancing

Stages sind das zentrale Werkzeug für Run-Pacing oberhalb einzelner Waves.

Sie sollen Schwierigkeit, Belohnungswert und Build-Prüfung eskalieren lassen, ohne über reine Stat-Erhöhungen zu arbeiten.

## Open Questions

- ~~Wie viele Stages soll ein vollständiger Run vor dem Final Boss durchlaufen?~~ Beantwortet 2026-09-14: 5 Stages (siehe Tabelle oben), Stage 5 „Core Nexus" trägt `IsFinalStage`.
- Skaliert jede weitere Stage rein über Enemy-Pool-Gewichtung und Boss-Stats, oder führt eine spätere Stage auch neue Enemy-Rollen ein? Für das 5-Stage-Konzept bewusst Letzteres vermieden — Stage 4's "Frost"-Akzent nutzt den bestehenden Elite-Modifier statt einer neuen Rolle, passend zu „keine neue Kernmechanik exklusiv für sich" (Rules oben).
- ~~Konkreter, priorisierter nächster Schritt: `WaveManager.CompleteStage()` muss bei `!ShouldEndRunOnCompletion()` tatsächlich auf `_stageData.NextStageData` umschalten...~~ Umgesetzt 2026-09-09 (siehe „Stage-Progression" oben).
- Arena-*Geometrie* (Wände, begehbare Maze-Zellen) bleibt weiterhin 100% `StageArenaMazeGenerator`s Stage-1-only-Algorithmus - nur Arena-*Größe* sowie Tower-Slots und Enemy-Spawn-Punkte sind inzwischen pro Stage datengetrieben (siehe [Dynamic Arena Slice](../Slices/15_DYNAMIC_ARENA_SLICE.md)). Eine größere/anders geformte Stage 2+ Arena rendert also weiterhin mit Stage 1's Maze-Layout gestreckt/zugeschnitten. Bewusst nicht Teil dieses Passes - siehe Slice-Dokument für den vorgeschlagenen Strategie-Enum-Ansatz.

Festgelegte Entscheidungen:

- Der erste Scope verwendet feste Stage-Strukturen statt Branching — das gilt auch für mehrere Stages: eine lineare Kette über `StageData.NextStageData`, kein Verzweigen.
- Stage 1 besitzt sechs Combat Waves, eine Elite Wave im späteren Verlauf und genau einen Boss.
- Weitere Stages entstehen ausschließlich datengetrieben (neues `StageData`/`StageThemeData`/`WaveData`-Set/`BossData` pro Stage) und thematisch modular — keine neue Code-Architektur pro Stage, siehe „Stage-Progression" oben.
- Die letzte Stage einer Kette markiert sich über `StageData.IsFinalStage`; ihr Boss ist der Final Boss aus Vision's Gameplay Loop.

## Acceptance Criteria

- Eine Stage ist als klar definierte Spieleinheit zwischen Run und Wave dokumentiert.
- Stage-Daten koordinieren Waves, Enemies, Rewards und Bosses ohne Sonderarchitektur.
- Stage-Übergänge sind für UI, Audio und SaveSystem anschlussfähig.
- Der Stage-1-Loop bleibt im 2D-Top-Down-Spiel konsistent lesbar.
- Zusätzliche Stages lassen sich datengetrieben ergänzen, ohne `WaveManager`/`StageArenaArtPresenter`/UI-Code zu ändern.
- Ein Run kann über `StageData.NextStageData` mehrere Stages linear verketten und über `IsFinalStage` erkennen, wann der Final Boss ansteht.
