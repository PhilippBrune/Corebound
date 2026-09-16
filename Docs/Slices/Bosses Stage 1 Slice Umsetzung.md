# Bosses Stage 1 Slice: Umsetzung

## Summary

Der Slice wird als Erweiterung des bestehenden Wave-/Boss-Unterbaus umgesetzt. Keine neue Kernmechanik: Boss nutzt weiter `WaveManager`, `BossRuntimeController`, bestehende Combat-/Projectile-/Reward-/Save-Flows und zusätzliche UI-/Audio-Listener.

## Key Changes

- Boss-Event-Contract ergänzen:
  - Neue Datei `Assets/Scripts/Gameplay/Bosses/BossEvents.cs`.
  - Neue Datei `Assets/Scripts/Gameplay/Bosses/BossEventArgs.cs`.
  - Events exakt als Hooks bereitstellen: `OnBossSpawned`, `OnBossPhaseChanged`, `OnBossDefeated`.
  - Payload: `StageIndex`, `WaveIndex`, `BossId`, `BossName`, `PhaseIndex`, `PhaseLabel`, `HealthNormalized`.

- `WaveManager` anbinden:
  - Nach Wave 6 weiter `StartBossEncounter(0)` verwenden.
  - Bei Boss-Start `BossEvents.RaiseBossSpawned(...)` auslösen.
  - Bei späterem `BossRuntimeController.PhaseChanged` `BossEvents.RaiseBossPhaseChanged(...)` auslösen.
  - Initialen Phase-Callback während `Initialize` unterdrücken, damit Spawn nicht sofort doppelt als Phasewechsel zählt.
  - Bei Boss-Tod vor oder während `CompleteBossEncounter()` `BossEvents.RaiseBossDefeated(...)` auslösen.
  - Bestehende Kette beibehalten: Boss-Tod -> BossReward pending -> CardRewardController öffnet Reward -> Card-Auswahl -> `StageCompleted`.

- Boss-UI ergänzen:
  - Neue Datei `Assets/Scripts/UI/Gameplay/BossHudPresenter.cs`.
  - Reagiert auf BossEvents und liest während `BossCombat` Health aus `WaveManager`.
  - Zeigt Controller-freundlich: Boss-Name, Boss-Health, Phase-Label.
  - Zeigt Warnung:
    - `Boss Incoming` während Intermission vor Boss, wenn `WaveEvents.IntermissionStarted` auf den Boss verweist.
    - `Phase Shift` kurz bei `OnBossPhaseChanged`.
    - `Boss Defeated` kurz bei `OnBossDefeated`.
  - Kein Gameplay-State in UI; UI ist reiner Presenter.

- Boss-Audio ergänzen:
  - Neue Datei `Assets/Scripts/Gameplay/Bosses/BossAudioCueData.cs` als ScriptableObject.
  - Neue Datei `Assets/Scripts/Gameplay/Bosses/BossAudioCuePlayer.cs`.
  - Neues Asset `Assets/ScriptableObjects/Gameplay/Bosses/BossAudioCue_Default.asset`.
  - Audio reagiert auf Boss-Warnung, Spawn, Phasewechsel und Defeat.
  - Nutzt `AudioManager.SfxVolume`; wenn noch keine echten Clips vorhanden sind, sind kurze synthetische Slice-Cues wie beim MainMenu erlaubt.

- Szene und Verlinkung:
  - `Assets/Scenes/SampleScene.unity` um BossHudPresenter und BossAudioCuePlayer ergänzen.
  - Referenzen setzen auf vorhandenen `WaveManager`, `AudioManager` und `BossAudioCue_Default`.
  - Doku aktualisieren: `Docs/Specs/Bosses.md`, `Docs/Slices/12_BOSSES_STAGE1_SLICE.md`, `Docs/08_IMPLEMENTATION_BACKLOG.md`, `Docs/09_CHANGELOG.md`.

## Test Plan

- Unity EditMode Tests oder PlayMode-Test ergänzen, falls vorhandene Teststruktur ohne großen Umbau nutzbar ist:
  - StageData enthält sechs Waves und BossData.
  - Nach Wave 6 startet `BossCombat`.
  - `OnBossSpawned` feuert genau einmal pro Boss-Start.
  - Health-Threshold löst `OnBossPhaseChanged` aus.
  - Boss-Tod löst `OnBossDefeated` und danach RewardPending aus.
  - Nach Card-Auswahl wird `StageCompleted` erreicht.

- Manuelle Unity-Prüfung in `SampleScene`:
  - Wave 1-6 durchlaufen.
  - Intermission vor Boss zeigt Boss-Warnung.
  - Boss-HUD zeigt Name, Health und Phase.
  - Phase 2 aktualisiert UI und spielt Audio-Cue.
  - Boss-Sieg öffnet Boss-Reward.
  - Nach Reward-Auswahl ist Stage abgeschlossen.

- Abschlusscheck:
  - Unity EditMode Tests ausführen.
  - Kompilierung ohne Fehler.
  - Keine direkte UI-Logik in Combat/Boss-Runtime.
  - Keine neuen Core-Regeln, keine neue Resource, kein neues Input-Konzept.

## Assumptions

- `Corebreaker Warden` bleibt der Stage-1-Boss.
- Boss-Warnung vor Boss-Start wird über bestehende Intermission gelöst.
- Phasewechsel-Warnung ist eine kurze UI-/Audio-Rückmeldung beim Health-Threshold, kein zusätzlicher Telegraph-State.
- Bestehende `WaveStatePresenter` darf parallel bestehen bleiben; das neue Boss-HUD übernimmt die spezifische Boss-Darstellung.
