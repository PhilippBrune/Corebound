# Dynamic Arena Slice

Status: Partially Implemented (2026-09-15 - see Implementation Note below)

Priority: High

Depends On: Stages, Waves, Towers, Enemies

---

## Purpose

Playtest-Feedback (2026-09-15): "Ich dislike aktuell den statischen Enemy Spawn, die statische Position der Towers, die statische Arena Size." Gleichzeitig laesst [00_PROJECT_VISION.md](../00_PROJECT_VISION.md) (v2.0) in Section 20 "Open Design Questions" genau diese drei Punkte ausdruecklich offen:

- "Wie werden bebaubare Tower-Standorte bestimmt?"
- "Ist Bewegung frei im Arena, raumbasiert oder explorationsbasiert?"
- "Wie werden Ressourcen gesammelt?"
- "Wie sind Stages und Waves strukturiert?"

Dieser Slice beantwortet die ersten drei Fragen fuer Tower-Placement, Arena-Groesse und Enemy-Spawn konkret und macht alle drei zu Daten statt zu fest verdrahteten Werten - ohne die Vision-Regel zu brechen, dass neue Tiefe durch Kombination bestehender Systeme entsteht, nicht durch neue Mechaniken.

Ziel ist **kein** freies Sandbox-Building (das wuerde die Core-Identitaet "Tower Defense mit Struktur" aufloesen) und **keine** Exploration im Metroidvania-Sinn - das Arena-Gefuehl (ein abgegrenzter Kampfraum pro Wave/Stage, siehe [01_GAME_DESIGN_PRINCIPLES.md](../01_GAME_DESIGN_PRINCIPLES.md)) bleibt. Was sich aendert: Groesse, Form, Spawn-Richtungen und Baufelder werden pro Stage (perspektivisch auch pro Wave-Encounter) aus Daten erzeugt statt aus einer einzigen, fuer alle Stages wiederverwendeten Stage-1-Geometrie.

---

## Implementation Note (2026-09-15)

The first implementation pass deviated from the `StageArenaLayoutData` asset design below in favor of something smaller and lower-risk, after discovering two things the original architecture section didn't account for:

1. `StageThemeData.ArenaSize` already existed (added in an earlier round) and `StageArenaArtPresenter` already reads it - arena *size* was already a per-stage data value, just not yet paired with matching tower/spawn placement. Only tower placement and enemy spawn were genuinely still hardcoded.
2. `StageData` (not `StageThemeData`) already owned `EnemySpawnOrigin`/`SpawnFanRadius`/`SpawnFanAngleStepDegrees` directly - extending those in place was less invasive than introducing a new cross-referenced asset type.

What actually shipped: `StageData` gained two new optional arrays, both empty by default -

- `SpawnPointDefinition[] SpawnPoints` (`Position`, `FanRadius`, `FanAngleStepDegrees` per point) - `WaveManager.ResolveSpawnPosition()` round-robins spawn bursts across these by spawn index when the array is non-empty, and falls back to the original single `EnemySpawnOrigin`/`SpawnFanRadius` fields unchanged when it's empty.
- `Vector2[] TowerSlotPositions` - `TowerService.ResolveActiveSlotPositions()` prefers this when the active stage has authored it, falling back to `TowerService`'s own serialized `_slotPositions` (today's Stage 1 layout) otherwise.

Both fall back to today's exact behavior when unauthored, so Stage 1 needed zero asset changes and has zero regression risk. `Stage_02.asset` (`Assets/ScriptableObjects/Gameplay/Waves/Stage_02.asset`) was given two spawn points (opposite diagonal corners, matching Stages.md's "diagonale Kühlrohr-Korridore" concept) and a 4-point tower layout shifted off-center (matching "Reaktorkern liegt außermittig") as the proof-of-variation this slice's Acceptance Criteria calls for.

**Explicitly not done in this pass** (still open, still matches the architecture/problem statement below):

- Arena *geometry* (walls, walkable maze cells) is still 100% `StageArenaMazeGenerator`'s Stage-1-only algorithm - only its size number is data-driven, not its shape. A larger/differently-shaped Stage 2+ arena still renders with Stage 1's maze layout stretched or cropped to fit.
- Prop placements (`Stage1PropPlacements`) are still Stage-1-only and untouched.
- Tower slots do not yet rebuild when `WaveManager.AdvanceToNextStage()` transitions to a new stage mid-run - `TowerService.CreateSlotRuntimes()` only runs once from `Awake()`. A player who reaches Stage 2 in the same run session still sees Stage 1's slot layout until this is wired up (needs care around what happens to towers already built on slots that move/disappear - deliberately deferred rather than rushed).
- No `BuildableZones`/shape-constrained placement - `TowerSlotPositions` is still a flat point list, same shape as before, just per-stage now.

---

## Linked Specs

- [Stages Spec](../Specs/Stages.md)
- [Waves Spec](../Specs/Waves.md)
- [Towers Spec](../Specs/Towers.md)
- [Enemies Spec](../Specs/Enemies.md)
- [Graphics Spec](../Specs/Graphics.md)

---

## Existing Runtime Links

- [WaveManager](../../Assets/Scripts/Gameplay/Waves/WaveManager.cs) - `ResolveSpawnPosition()` (Zeile ~700) spawnt aktuell aus **einem** `StageData.EnemySpawnOrigin`-Punkt in einem Fan (`SpawnFanRadius`, `SpawnFanAngleStepDegrees`). Bereits datengetrieben, aber nur ein einziger Ursprung pro Stage.
- [StageData](../../Assets/Scripts/Gameplay/Waves/StageData.cs) - haelt `EnemySpawnOrigin`, `BossSpawnPosition`, `SpawnFanRadius`, `SpawnFanAngleStepDegrees`.
- [StageThemeData](../../Assets/Scripts/Gameplay/Waves/StageThemeData.cs) - haelt Ground/Prop/Boundary-Texturen und den `SpawnGateTexture`/`SpawnGateLocalPosition` (ein einzelnes, optisches Gate - keine Gameplay-Spawnlogik).
- [StageArenaArtPresenter](../../Assets/Scripts/Gameplay/Shared/StageArenaArtPresenter.cs) - erzeugt die Arena-Geometrie. `ArenaSize`-Property existiert bereits (2026-09-14 ergaenzt), liest aber aus Stage-1-only-Feldern (`StageArenaMazeGenerator`, `Stage1PropPlacements`, `MazeBlockerCells`). Laut Stages.md's eigenem Technical-Design-Abschnitt bereits als bekannte Luecke dokumentiert: neue Stages fallen beim Rebuild auf Stage 1's Geometrie zurueck, nur Textur/Tint wechseln schon ueber `StageThemeData`.
- [TowerService](../../Assets/Scripts/Gameplay/Towers/TowerService.cs) - `_slotPositions` (`Vector2[]`) treibt `CreateSlotRuntimes()`, das die Slot-GameObjects zur Laufzeit generiert. Der Erzeugungsmechanismus ist bereits array-basiert und datengetrieben; die Luecke ist nur, dass `_slotPositions` ein einzelnes, im Scene-Component fest hinterlegtes Array ist statt aus Stage-Daten zu kommen.
- [CameraFollowController](../../Assets/Scripts/Gameplay/Player/CameraFollowController.cs) - liest `StageArenaArtPresenter.ArenaSize` fuer Kamera-Bounds; muss bei variabler Arena-Groesse weiterhin korrekt funktionieren (tut es bereits, da es die Property live liest statt einen Wert zu cachen).

---

## Data Links

- [Waves Data Folder](../../Assets/ScriptableObjects/Gameplay/Waves) (`StageData`, `StageThemeData`, `StageTheme_Stage2` etc.)
- [Towers Data Folder](../../Assets/ScriptableObjects/Gameplay/Towers)

---

## Problem Statement

Alle drei genannten Punkte haben dieselbe Grundursache: **Stage 1's Geometrie ist die einzige Geometrie.** Stage 2-5 (Assets 47-62, siehe [GRAPHICS_ASSET_PROMPTS.md](../AI/GRAPHICS_ASSET_PROMPTS.md)) haben bereits eigene Texturen und eigene thematische Layout-Konzepte in Stages.md (diagonale Korridore, offenes Schrottfeld, eng/offen-Wechsel, zentraler Dais) - aber keine davon wird aktuell tatsaechlich als andere Geometrie gebaut, weil:

1. **Arena-Groesse**: `StageArenaArtPresenter` generiert die Arena ueber `StageArenaMazeGenerator`, das nur Stage 1's symmetrisches Maze-Raster kennt. Groesse und Form sind Code, nicht Daten.
2. **Tower-Placement**: `TowerService._slotPositions` ist ein festes 4-Element-Array, unabhaengig von Stage oder Arena-Groesse. Eine groessere oder anders geformte Arena bekommt trotzdem dieselben vier Punkte.
3. **Enemy-Spawn**: `WaveManager.ResolveSpawnPosition()` faechert von **einem** `EnemySpawnOrigin` aus. Eine Stage mit "diagonalen Kuehlrohr-Korridoren" (Stage 2) oder "mehreren grossen Wrackhaufen" (Stage 3) sollte Gegner aus mehreren, thematisch passenden Richtungen schicken koennen, nicht aus einem Punkt.

Alle drei Systeme sind bereits so gebaut, dass sie **Werte aus Daten lesen** - keines davon ist hart im Code verdrahtet. Die Luecke ist ausschliesslich, dass die Datenquelle pro Stage nicht existiert bzw. auf Stage 1 fixiert ist. Das ist eine gute Ausgangslage: dieser Slice erweitert bestehende Datenpfade, er ersetzt keine Laufzeit-Architektur.

---

## Proposed Architecture

Ein neues ScriptableObject **`StageArenaLayoutData`**, referenziert von `StageThemeData` (analog zu `GroundTexture`/`PropTexture`/`BoundaryTexture`), buendelt die drei zusammengehoerigen Layout-Aspekte an einer Stelle statt sie in drei unabhaengigen Systemen zu duplizieren:

```
StageArenaLayoutData
├── ArenaSize: Vector2                          // ersetzt Stage-1-only Maze-Bounds
├── BuildableZones: BuildableZoneDefinition[]    // ersetzt TowerService._slotPositions als feste Quelle
│     ├── Shape: Rect oder Polygon (einfachstes MVP: Rect-Liste)
│     └── MaxSlotCount / SlotSpacing
├── SpawnPoints: SpawnPointDefinition[]          // ersetzt den einzelnen EnemySpawnOrigin
│     ├── Position: Vector2
│     ├── Tag: SpawnPointTag (Primary, Flank, Rear - fuer spaetere Rollen-Praeferenz)
│     └── FanRadius / FanAngleStepDegrees (pro Punkt, nicht mehr global)
└── PropPlacements: PropPlacement[]              // Generalisierung von Stage1PropPlacements
```

Verbrauchende Systeme aendern sich minimal:

- `StageArenaArtPresenter` liest `ArenaSize`/`PropPlacements` aus `StageThemeData.LayoutData` statt aus den Stage-1-only-Feldern. `ArenaSize`-Property bleibt bestehen, nur die interne Quelle wechselt.
- `TowerService.CreateSlotRuntimes()` bleibt strukturell gleich (Array rein, Slots raus), bekommt sein `_slotPositions`-Array aber aus `StageThemeData.LayoutData.BuildableZones` statt aus einem im Scene-Component fest hinterlegten Inspector-Array. Fuer den MVP reicht eine einfache Platzierungsregel (z. B. gleichmaessig entlang der Buildable-Zone-Raender verteilt), kein vollstaendiger Constraint-Solver.
- `WaveManager.ResolveSpawnPosition(spawnIndex, isBossHelper)` waehlt statt eines einzelnen `EnemySpawnOrigin` einen `SpawnPointDefinition` aus `StageData`/`StageThemeData.LayoutData.SpawnPoints` (z. B. rotierend oder gewichtet nach `SpawnGroupData`-Rolle) und faechert von dort aus - die bestehende Fan-Logik (`Mathf.Cos`/`Mathf.Sin` um einen Ursprung) bleibt exakt erhalten, sie bekommt nur mehrere moegliche Ursprungspunkte statt eines fixen.

Das ist bewusst **keine** prozedurale Arena-Generierung zur Laufzeit (kein Rogue-like-Room-Generator) - die Layout-Daten werden weiterhin pro Stage von Hand autoriert (wie `Stage1PropPlacements` heute schon), nur eben pro Stage unterschiedlich statt stage-uebergreifend identisch. Das passt zur Vision-Regel "keine neue Kernmechanik exklusiv fuer eine Stage" (Stages.md Rules) - jede Stage bekommt eigene Werte fuer dieselbe Mechanik, nicht eine eigene Mechanik.

---

## Scope In

- `StageArenaLayoutData`-Asset-Typ plus ein Asset pro existierender Stage (`Stage1`, `Stage2` - Stage 3-5 folgen mit ihrer jeweiligen Kunst, siehe Stages.md).
- `StageArenaArtPresenter` liest Arena-Groesse und Prop-Placements aus dem neuen Asset statt aus Stage-1-only-Feldern; `StageArenaMazeGenerator` wird zur Stage-1-spezifischen Implementierung einer austauschbaren Layout-Quelle, nicht mehr der einzigen.
- `TowerService` generiert Slot-Positionen aus `BuildableZones` statt aus einem fixen Inspector-Array; Slot-Anzahl darf pro Stage variieren (mehr Platz auf einer groesseren Arena).
- `WaveManager.ResolveSpawnPosition()` waehlt aus mehreren `SpawnPoints` statt einem `EnemySpawnOrigin`; bestehende Fan-Streuung pro Punkt bleibt erhalten.
- Stage 1 wird 1:1 als erstes Layout-Asset migriert (gleiche Werte wie heute), damit sich am aktuell gespielten Verhalten nichts aendert, solange kein neues Layout-Asset fuer eine andere Stage autoriert wird.
- Mindestens ein zweites, wirklich unterschiedliches Layout (Stage 2, passend zu "diagonale Kuehlrohr-Korridore, Reaktorkern aussermittig") als Nachweis, dass das System echte Variation traegt, nicht nur Stage 1 mit anderen Zahlen.

## Scope Out

- Kein Laufzeit-generiertes Layout (kein prozeduraler Generator, keine Zufalls-Arena pro Run).
- Keine freie Tower-Platzierung ausserhalb definierter Buildable Zones (bleibt naeher an TD-Identitaet als an Factory-Sandbox).
- Kein Per-Wave-Layout-Wechsel innerhalb einer Stage (Layout ist stage-scoped, nicht wave-scoped) - falls spaeter gewuenscht, ist das ein eigener Folge-Slice.
- Keine Aenderung an Spieler-Bewegungsfreiheit (Vision-Frage "frei/raumbasiert/explorationsbasiert" bleibt fuer diesen Slice bei "frei innerhalb der Arena", wie heute).
- Keine Aenderung an Resource-Sammel-Mechanik (separate offene Vision-Frage, nicht Teil dieses Slices).

---

## Migration Steps

1. `StageArenaLayoutData`-ScriptableObject-Klasse anlegen (Felder wie oben skizziert, MVP mit Rect-basierten Buildable Zones statt Polygonen).
2. `StageThemeData` um eine `LayoutData`-Referenz erweitern.
3. `Stage1_ArenaLayout.asset` mit den heutigen Stage-1-Werten befuellen (aus `StageArenaMazeGenerator`/`Stage1PropPlacements`/aktuellem `EnemySpawnOrigin`/`_slotPositions` 1:1 uebernommen) - reine Datenmigration, kein Verhaltenswechsel.
4. `StageArenaArtPresenter.ResolveStageThemeData()`-Nachbar-Methoden auf `LayoutData` umstellen; `ArenaSize`-Property-Rueckgabewert bleibt API-kompatibel.
5. `TowerService.CreateSlotRuntimes()` auf `LayoutData.BuildableZones` umstellen; Stage-1-Asset erzeugt bewusst dieselben vier Punkte wie heute.
6. `WaveManager.ResolveSpawnPosition()` auf `LayoutData.SpawnPoints` umstellen; Stage-1-Asset erhaelt genau einen SpawnPoint mit den heutigen `EnemySpawnOrigin`/`SpawnFanRadius`/`SpawnFanAngleStepDegrees`-Werten.
7. Regressionscheck: Stage 1 spielt sich identisch zu vorher (gleiche Tower-Slots, gleicher Spawn-Fan, gleiche Arena-Groesse).
8. Erst danach: `Stage2_ArenaLayout.asset` mit echter Abweichung autorieren (groessere/asymmetrische Arena, 2-3 Spawn Points fuer die diagonalen Korridore, Buildable Zones die zur aussermittigen Reaktorkern-Lage passen) - das ist der eigentliche Nachweis, dass "statisch" durchbrochen ist.

---

## Open Questions

- Reicht ein Rect-basiertes `BuildableZones`-MVP fuer alle fuenf Stages, oder braucht Stage 5 (Terrain-Fragmente um einen zentralen Dais) ein Polygon von Anfang an? Vorschlag: mit Rects starten, auf Polygone erweitern, sobald ein konkretes Stage-Layout das erzwingt - vermeidet vorzeitige Komplexitaet.
- Sollen `SpawnPoints` eine Rollen-Praeferenz bekommen (z. B. Ranged-Pressure-lastige Stage 2 bevorzugt Spawn Points mit freier Sichtlinie), oder reicht rotierende/gewichtete Zufallsauswahl fuer den ersten Durchgang? Stages.md's Enemy-Pool-Akzent-Spalte legt nahe, dass das frueher oder spaeter relevant wird, muss aber nicht Teil des MVP sein.
- Wird `StageArenaMazeGenerator` langfristig komplett durch datengetriebene Prop/Wall-Placements ersetzt, oder bleibt es als eine von mehreren austauschbaren Layout-Strategien (z. B. "Maze" fuer Stage 1, "OpenField" fuer Stage 3) mit einem Strategie-Enum in `StageArenaLayoutData`? Letzteres vermeidet, Stage 1's bereits funktionierendes System neu schreiben zu muessen.

---

## Acceptance Criteria

- Stage 1 verhaelt sich nach der Migration exakt wie vorher (keine Regression) - Arena-Groesse, Tower-Slot-Positionen und Spawn-Fan sind bitgleich zu heute.
- Mindestens eine zweite Stage hat eine tatsaechlich andere Arena-Groesse, andere Buildable-Zone-Anordnung und mehr als einen Spawn Point.
- Kein neuer Code-Pfad in `WaveManager`, `TowerService` oder `StageArenaArtPresenter` ist stage-spezifisch verzweigt (`if (stageId == "stage_2")` o. ae.) - jede Abweichung kommt ausschliesslich aus dem jeweiligen `StageArenaLayoutData`-Asset.
- `CameraFollowController` und alle bestehenden Tests/Checks, die `StageArenaArtPresenter.ArenaSize` konsumieren, funktionieren unveraendert mit variabler Arena-Groesse.
