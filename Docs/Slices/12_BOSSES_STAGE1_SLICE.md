# Bosses Stage 1 Slice

Status: Completed

Priority: Critical

Depends On: Waves, Enemies, Combat, Cards, Resources, Towers, UI, Audio, SaveSystem

---

## Purpose

Dieser Slice zieht den neuen Wave-Unterbau bis zum Stage-Abschluss durch.

Stage 1 endet nach sechs Waves in einer Boss-Begegnung. Der Boss prueft Build, Positioning, Core-Verteidigung und Lesbarkeit mit bestehenden Combat-, Spawn-, Reward- und Save-Regeln.

---

## Linked Specs

- [Bosses Spec](../Specs/Bosses.md)
- [Waves Spec](../Specs/Waves.md)
- [Stages Spec](../Specs/Stages.md)
- [Enemies Spec](../Specs/Enemies.md)
- [Combat Spec](../Specs/Combat.md)
- [Cards Spec](../Specs/Cards.md)
- [Resources Spec](../Specs/Resources.md)
- [Towers Spec](../Specs/Towers.md)
- [UI Spec](../Specs/UI.md)
- [Audio Spec](../Specs/Audio.md)
- [SaveSystem Spec](../Specs/SaveSystem.md)

---

## Existing Runtime Links

- [WaveManager](../../Assets/Scripts/Gameplay/Waves/WaveManager.cs)
- [StageData](../../Assets/Scripts/Gameplay/Waves/StageData.cs)
- [BossRuntimeController](../../Assets/Scripts/Gameplay/Bosses/BossRuntimeController.cs)
- [BossEvents](../../Assets/Scripts/Gameplay/Bosses/BossEvents.cs)
- [BossEventArgs](../../Assets/Scripts/Gameplay/Bosses/BossEventArgs.cs)
- [BossAudioCuePlayer](../../Assets/Scripts/Gameplay/Bosses/BossAudioCuePlayer.cs)
- [BossHudPresenter](../../Assets/Scripts/UI/Gameplay/BossHudPresenter.cs)
- [BossData](../../Assets/Scripts/Gameplay/Bosses/BossData.cs)
- [BossPhaseData](../../Assets/Scripts/Gameplay/Bosses/BossPhaseData.cs)
- [BossAttackPatternData](../../Assets/Scripts/Gameplay/Bosses/BossAttackPatternData.cs)
- [BossRewardData](../../Assets/Scripts/Gameplay/Bosses/BossRewardData.cs)
- [RunPersistenceController](../../Assets/Scripts/SaveSystem/RunPersistenceController.cs)

---

## Existing Data Links

- [Stage_01](../../Assets/ScriptableObjects/Gameplay/Waves/Stage_01.asset)
- [Boss_Stage1](../../Assets/ScriptableObjects/Gameplay/Bosses/Boss_Stage1.asset)
- [BossPhase_01](../../Assets/ScriptableObjects/Gameplay/Bosses/BossPhase_01.asset)
- [BossPhase_02](../../Assets/ScriptableObjects/Gameplay/Bosses/BossPhase_02.asset)
- [BossAttackPattern_Phase1](../../Assets/ScriptableObjects/Gameplay/Bosses/BossAttackPattern_Phase1.asset)
- [BossAttackPattern_Phase2](../../Assets/ScriptableObjects/Gameplay/Bosses/BossAttackPattern_Phase2.asset)
- [BossAudioCue_Default](../../Assets/ScriptableObjects/Gameplay/Bosses/BossAudioCue_Default.asset)
- [BossReward_Stage1](../../Assets/ScriptableObjects/Gameplay/Bosses/BossReward_Stage1.asset)
- [Projectile_BossBurst](../../Assets/ScriptableObjects/Gameplay/Combat/Projectile_BossBurst.asset)
- [CombatTarget_BossStage1](../../Assets/ScriptableObjects/Gameplay/Combat/CombatTarget_BossStage1.asset)

---

## Scope In

- Stage 1 startet nach Wave 6 genau eine Boss-Begegnung.
- Der Boss nutzt Health-Threshold-Phasen.
- Phase 2 darf ein klar begrenztes Hilfs-Enemy-Fenster nutzen.
- Boss-Projectiles nutzen die bestehende Projectile- und Damage-Pipeline.
- Boss-Sieg oeffnet einen hochwertigen Reward-Moment und schliesst danach die Stage ab.
- SaveSystem speichert aktive Boss-Begegnung und Boss-Phase.
- UI zeigt Boss-Name, Boss-Lebenszustand, Boss-Phase und klare Warnungen.
- Audio bekommt Boss-Intro, Phasenwechsel, Grossangriff und Boss-Sieg als Event-Hooks.

---

## Scope Out

- Kein Final Boss.
- Keine alternativen Boss-Pools.
- Keine boss-exklusive Resource.
- Keine neue Arena-Hazard-Kernmechanik.
- Kein aktives Card-Ausspielen.
- Kein neues Input-Konzept fuer Bosses.

---

## Boss Identity

Boss: Corebreaker Warden

Rolle: Stage-1-Abschluss, der Movement, aktive Weapon-Nutzung, Tower-Unterstuetzung und Core-Druck gleichzeitig prueft.

Design-Begruendung: Der Boss fuehrt keine neue Kernmechanik ein. Er kombiniert bestehende Projectiles, Health-Threshold-Phasen, begrenzte Hilfs-Enemy-Spawns und Card-Reward in einem Stage-Finale.

---

## Encounter Flow

1. Wave 6 endet.
2. Intermission oder Reward-Flow schliesst controller-freundlich ab.
3. Boss-Warnung wird passiv angezeigt.
4. Boss spawnt am definierten Boss-Startpunkt.
5. Phase 1 nutzt ein lesbares, breites Projectile-Pattern.
6. Bei Health-Threshold wechselt der Boss in Phase 2.
7. Phase 2 verdichtet Projectile-Druck und oeffnet einmalig ein Hilfs-Enemy-Fenster.
8. Boss-Sieg schreibt Boss-Reward und Stage-Abschluss in den Run-Kontext.
9. Reward-Auswahl nutzt denselben Card-Offer-Flow wie Wave-Rewards.
10. Nach Reward-Auswahl wird Stage 1 als abgeschlossen markiert.

---

## Controller First

- Der Player benoetigt keine neue Boss-Eingabe.
- Warnungen sind lesbar, ohne dass der Player Menues oeffnen muss.
- Der Boss-Reward nutzt Confirm, Cancel und Navigation wie bestehende Card-Angebote.
- Intermission-Early-Start bleibt ueber Confirm erreichbar.
- Tower- und Resource-Entscheidungen bleiben im bestehenden Build- und Intermission-Flow.

---

## Data Requirements

- `StageData` verweist auf sechs `WaveData` Assets und genau ein `BossData` Asset.
- `BossData` verweist auf CombatTargetData, HitReactionData, StatusEffectData, Phasen und Reward.
- Jede `BossPhaseData` verweist auf genau ein `BossAttackPatternData`.
- Hilfs-Enemies werden ueber bestehende `SpawnGroupData` referenziert.
- Boss-Reward verwendet eine eindeutige RewardId fuer SaveSystem (RunPersistenceController, WaveManager Stage-Completion-Gating). **Update 2026-09-16 — korrigiert:** `CardRewardController` konsumiert `RewardId` selbst nicht; er abonniert nur das generische `WaveEvents.RewardPending`-Event und bleibt reward-source-agnostisch. Die RewardId ist ausschliesslich ein SaveSystem-Konzept, kein CardRewardController-Konzept.

---

## Implementation Checklist

- `Stage_01` besitzt sechs Waves und eine BossData-Referenz.
- `WaveManager` wechselt nach Wave 6 in BossCombat.
- Boss-Tod loest Boss-Reward statt normalen Wave-Reward aus.
- Boss-Reward fuehrt nach Auswahl zu StageCompleted.
- Boss-Phase wird bei Save-Checkpoints geschrieben und beim Laden wiederhergestellt.
- UI und Audio reagieren auf BossCombat, Boss-Phase und Boss-Sieg.
- Boss-Hilfs-Enemies schreiben Resource-Rewards wie normale Enemies.
- Boss-Projectiles sind gepoolt und verwenden vorhandene Combat-Daten.

---

## Balance Guardrails

- Der Boss darf kein reiner Health-Sponge sein.
- Phase 1 lehrt Pattern, Phase 2 verdichtet Druck.
- Hilfs-Enemies duerfen nie gefaehrlicher wirken als der Boss selbst.
- Resource- und Card-Belohnung nach Boss-Sieg soll Stage-Abschluss klar markieren.
- Boss muss mit Weapon-fokussiertem, Tower-fokussiertem und Resource-fokussiertem Build schlagbar bleiben.

---

## Acceptance Criteria

- Stage 1 ist von Wave 1 bis Boss-Sieg vollstaendig spielbar.
- Der Boss erscheint nur nach abgeschlossener Wave-Sequenz.
- Phasenwechsel sind lesbar und datengetrieben.
- Boss-Reward und Stage-Abschluss nutzen bestehende Reward- und Save-Flows.
- Controller-Bedienung bleibt unveraendert vollstaendig.
- Keine neue Kernmechanik wurde ohne Begruendung eingefuehrt.
