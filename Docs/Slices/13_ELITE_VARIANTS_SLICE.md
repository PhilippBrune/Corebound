# Elite Variants Slice

Status: Completed (2026-09-16 audit confirmed: all three named Elite variants — Relentless, Splitshot, Anchor — exist as data assets, are wired into Wave 5/Wave 6/boss-helper spawn groups, and are fully consumed by EnemyRuntimeController)

Priority: High

Depends On: Bosses Stage 1 Slice, Enemies, Waves, Combat, Resources, UI, Audio

---

## Purpose

Dieser Slice fuehrt echte Elite-Varianten und differenziertere Enemy-Pattern ein, nachdem der Stage-1-Boss den Wave-Unterbau bis zum Abschluss nutzt.

Elite-Varianten bleiben Datenvarianten bestehender Enemies. Sie erhoehen Entscheidungsdruck durch Rolle, Timing und Pattern, nicht durch neue Kernregeln.

---

## Linked Specs

- [Enemies Spec](../Specs/Enemies.md)
- [Waves Spec](../Specs/Waves.md)
- [Bosses Spec](../Specs/Bosses.md)
- [Combat Spec](../Specs/Combat.md)
- [Resources Spec](../Specs/Resources.md)
- [UI Spec](../Specs/UI.md)
- [Audio Spec](../Specs/Audio.md)

---

## Existing Runtime Links

- [EnemyRuntimeController](../../Assets/Scripts/Gameplay/Enemies/EnemyRuntimeController.cs)
- [EnemyEliteModifierData](../../Assets/Scripts/Gameplay/Enemies/EnemyEliteModifierData.cs)
- [EnemyData](../../Assets/Scripts/Gameplay/Enemies/EnemyData.cs)
- [EnemyAttackData](../../Assets/Scripts/Gameplay/Enemies/EnemyAttackData.cs)
- [EnemyBehaviourData](../../Assets/Scripts/Gameplay/Enemies/EnemyBehaviourData.cs)
- [SpawnGroupData](../../Assets/Scripts/Gameplay/Waves/SpawnGroupData.cs)
- [WaveManager](../../Assets/Scripts/Gameplay/Waves/WaveManager.cs)

---

## Existing Data Links

- [EnemyElite_Default](../../Assets/ScriptableObjects/Gameplay/Enemies/EnemyElite_Default.asset)
- [SpawnGroup_Wave5Elite](../../Assets/ScriptableObjects/Gameplay/Waves/SpawnGroup_Wave5Elite.asset)
- [SpawnGroup_Wave6RangedElite](../../Assets/ScriptableObjects/Gameplay/Waves/SpawnGroup_Wave6RangedElite.asset)

---

## Scope In

- Drei Stage-1-Elite-Varianten fuer bestehende Rollen: Chaser, Ranged Pressure und Bruiser.
- Pro Elite-Variante ein Stat-Paket plus maximal eine wiederverwendbare Pattern-Variation.
- Elite-Spawn nur ueber bestehende Wave- und SpawnGroupData.
- Elite-Warnung ueber UI und Audio ohne neue Eingabe.
- Resource-Rewards bleiben ueber EnemyRewardData und Elite-Reward-Multiplier abgeleitet.

---

## Scope Out

- Keine neuen Enemy-Familien.
- Keine Elite-exklusive Resource.
- Keine neuen Status Effects nur fuer Elite.
- Keine dauerhaften Arena-Regeln.
- Keine komplexen Boss-Add-Systeme ausser den bereits erlaubten Hilfs-Enemy-Fenstern.

---

## Variant Matrix

| Variant | Base Enemy | Role Test | Allowed Pattern Variation | Player Decision |
| --- | --- | --- | --- | --- |
| Relentless Elite | Chaser | Movement und Target-Prioritaet | Hoeherer MoveSpeed-Multiplier, keine Projectile-Aenderung | Kiten oder frueh fokussieren |
| Splitshot Elite | Ranged Pressure | Dodge-Rhythmus und Abstand | Ein zusaetzliches Projectile im bestehenden Burst | Line of sight brechen oder schnell ausschalten |
| Anchor Elite | Bruiser | Core-Druck und Tower-Position | Hoehere Health, niedrigerer MoveSpeed, staerkerer Contact-Druck | Zum Core zurueckfallen oder Tower-Zone nutzen |

---

## Data Requirements

Neue Assets sollen unter `Assets/ScriptableObjects/Gameplay/Enemies/` liegen:

- `EnemyElite_Relentless.asset`
- `EnemyElite_Splitshot.asset`
- `EnemyElite_Anchor.asset`

Wave-Referenzen sollen unter `Assets/ScriptableObjects/Gameplay/Waves/` liegen:

- `SpawnGroup_Wave5EliteRelentless.asset`
- `SpawnGroup_Wave6EliteSplitshot.asset`
- `SpawnGroup_BossHelperAnchor.asset`

Diese Assets verwenden bestehende ScriptableObject-Typen. Wenn eine Pattern-Variation nicht mit vorhandenen Daten abbildbar ist, muss zuerst begruendet werden, warum `EnemyEliteModifierData` erweitert werden muss.

---

## Controller First

- Elite-Erkennung erfolgt ueber Silhouette, Tint, Spawn-Warnung und Audio, nicht ueber Mouseover.
- Der Player muss Elite-Druck durch Movement, Aim, Tower Slot Cycling und Card-Reward-Entscheidungen beantworten koennen.
- Keine Elite-Interaktion darf Cursor-Zwang erzeugen.

---

## Pattern Rules

- Ein Elite veraendert die Rolle des Base Enemy nicht.
- Ein Elite darf den Base Enemy nicht in einen Boss-Ersatz verwandeln.
- Pattern-Variation muss mit bestehenden Attack- oder Behaviour-Daten erklaerbar sein.
- Mehr Lebenspunkte allein zaehlen nicht als echte Variante.
- Elite-Varianten sollen in Wave-Kompositionen besser werden, nicht isoliert durch Sonderregeln.

---

## Acceptance Criteria

- Stage 1 enthaelt mindestens zwei klar unterscheidbare Elite-Begegnungen.
- Jede Elite-Variante bleibt als Datenvariante eines bestehenden Enemy nachvollziehbar.
- Elite-Warnungen sind ohne Maus lesbar.
- Resource-Rewards bleiben fair und erzeugen keinen Snowball.
- Boss-Hilfs-Enemies koennen eine Elite-Variante nutzen, ohne den Boss zu ueberdecken.
