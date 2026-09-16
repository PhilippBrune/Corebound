# Enemies Specification

## Purpose

Definiert normale Enemies als Hauptquelle von Druck, Gefahr und Kampfvariation innerhalb von Waves.

Enemies treiben Positioning, Target-Priorität und Build-Prüfung an, ohne die Simple-Rules-Philosophie zu verlassen.

## Design Goals

- Klare Enemy-Rollen mit gut lesbaren Verhaltensmustern
- Schwierigkeit durch Verhalten, Kombinationen und Druck statt reine Lebenspunkt-Erhöhung
- Hohe Kompatibilität mit Wave-, Combat- und Resource-System
- Gute Lesbarkeit auch bei vielen gleichzeitigen Enemies
- Datengetriebene Erweiterbarkeit für neue Archetypen und Varianten

## Gameplay

Enemies erscheinen in Waves und versuchen, den Player unter Druck zu setzen oder den Core zu bedrohen.

Jeder Enemy besitzt eine klar erkennbare Rolle, zum Beispiel direkte Annäherung, Fernkampfdruck, Flächenkontrolle oder Support für andere Enemies.

Spannung entsteht durch Kombinationen mehrerer Enemy-Rollen innerhalb einer Wave und nicht durch versteckte Sonderregeln.

Besiegte Enemies liefern Resource-Belohnungen und können weitere Systeme über Events triggern.

## Rules

- Jeder Enemy nutzt die gemeinsame Combat-Logik für Damage, Status Effect und Trefferreaktionen.
- Enemy-Verhalten muss visuell telegraphiert und ausweichbar oder planbar sein.
- Schwierigkeit wird primär über Spawn-Kombinationen, Tempo und Raumdruck gesteigert.
- Elite- oder Variantenlogik soll möglichst als Modifikator auf bestehende Enemy-Definitionen aufsetzen.
- Enemies dürfen den Core bedrohen, ohne die Position des Players irrelevant zu machen.
- Neue Enemy-Archetypen sollen bestehende Systeme erweitern, nicht neue Kampfformen erfinden.
- Enemy-Logik darf nicht direkt auf UI oder Menu-Systeme zugreifen.

## Data Model

Enemy-Daten werden über ScriptableObjects verwaltet.

- EnemyData: Basisdefinition eines Enemy mit Rollen-Tag, Stats und Präsentation
- EnemyStatData: Health, Movement, Damage, Resistenz- oder Reward-Werte
- EnemyAttackData: Angriffsmuster, Reichweiten, Wind-up und Trigger
- EnemyBehaviourData: Zielprioritäten, Distanzverhalten und Reaktionsmuster
- EnemyRewardData: Resource-Drops, Gewichtungen und Sonderbelohnungen

## Technical Design

Enemy-MonoBehaviours verbinden Navigation, Collider, Animation und Präsentation.

Verhaltenslogik wird in klar getrennten State- oder Strategy-Klassen gehalten.

Spawn, Tod, Zielwechsel und Statusänderungen werden über Events wie OnEnemySpawned, OnEnemyKilled und OnEnemyTargetChanged kommuniziert.

Enemy-Instanzen, Treffer-VFX und Projectiles müssen Pooling verwenden.

Enemy-Hit-Sparks, Death-Bursts sowie Burn- und Freeze-Layer folgen der gemeinsamen Zuordnung in der [VFX Specification](./VFX.md).

Der erste Vertical Slice nutzt drei datengetriebene Enemy-Archetypen bereits konkret in Stage-1-Waves: Chaser, Ranged Pressure und Bruiser. Ihre Reward-Gutschrift, Tower-Zielwahl und Wave-Abschlussbedingungen laufen über denselben Combat-, Resource- und Wave-Unterbau.

## Dependencies

- Combat
- Waves
- Bosses
- Resources
- Player
- UI
- Audio

## Linked Slice Files

- [Elite Variants Slice](../Slices/13_ELITE_VARIANTS_SLICE.md)
- [Bosses Stage 1 Slice](../Slices/12_BOSSES_STAGE1_SLICE.md)
- [Wave Boss Balance Slice](../Slices/14_WAVE_BOSS_BALANCE_SLICE.md)

## UI

Enemy-bezogene UI ist zurückhaltend und priorisiert Lesbarkeit.

Nur relevante Informationen wie Elite-Markierungen, Boss-Vorwarnungen oder gefährliche Cast-Telegraphs werden dauerhaft hervorgehoben.

## Audio

Enemy-Audio signalisiert Spawn, Angriffsvorbereitung, Treffer und Tod klar und ohne Mix-Überladung.

Besonders gefährliche Enemies benötigen priorisierte Warn-Cues.

## Visual

Enemies müssen anhand von Silhouette, Farbe, Bewegung und Telegraph sofort unterscheidbar sein.

Wichtige Gefahrenzustände dürfen nicht allein über kleine UI-Elemente kommuniziert werden.

## Balancing

Enemy-Design soll verschiedene Builds testen, aber keine schmalen Hard-Counter-Strukturen erzwingen.

Frühe Waves lehren Grundmuster, spätere Waves kombinieren diese Muster dichter und aggressiver.

Mehr Lebenspunkte allein sind kein ausreichendes Schwierigkeitswerkzeug.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Die erste Stage nutzt die drei Basisrollen Chaser, Ranged Pressure und Bruiser.
- Elite-Logik besteht im ersten Scope aus einem Stat-Paket plus maximal einer wiederverwendbaren Pattern-Variation.
- Standard-Enemies bewegen sich grundsätzlich in Richtung Core und wechseln auf den Player nur durch Archetyp, Nähe oder erlittenen Schaden.

## Acceptance Criteria

- Enemies basieren auf einem gemeinsamen, datengetriebenen Modell für Stats, Verhalten und Rewards.
- Schwierigkeit entsteht vor allem durch Rollen-Kombinationen und Druck, nicht durch HP-Bloat.
- Enemy-Verhalten bleibt in großen Gruppen klar lesbar.
- Resource- und Combat-Systeme können auf Enemy-Events reagieren, ohne direkte Kopplung.
- Neue Enemy-Archetypen lassen sich ohne Architekturänderungen hinzufügen.
- Reward- und Resource-Gutschrift werden im ersten Slice erst nach echter Enemy-Teilnahme in Waves und nicht mehr nur über einen isolierten Testdummy geprüft.
