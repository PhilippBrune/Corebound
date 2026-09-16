# Changelog

## 2026-07-28

### Added

- Docs/Slices/README.md als Index fuer die naechsten Gameplay-Slices angelegt.
- Docs/Slices/12_BOSSES_STAGE1_SLICE.md als Bosses-Handoff fuer den Stage-1-Abschluss angelegt.
- Docs/Slices/13_ELITE_VARIANTS_SLICE.md als Handoff fuer echte Elite-Varianten und differenziertere Enemy-Pattern angelegt.
- Docs/Slices/14_WAVE_BOSS_BALANCE_SLICE.md als Balance-Handoff fuer Cards, Resources und Towers unter Wave- und Boss-Druck angelegt.
- Erster Combat-Slice unter Assets/Scripts/Gameplay/Combat mit gemeinsamer Damage- und Status-Pipeline, Player-I-Frames, Projectile-Pooling und Controller-Soft-Aim.
- Combat-Default-Assets unter Assets/ScriptableObjects/Gameplay/Combat fuer CombatBalance, DamageProfile, Projectile, Burn, Freeze, HitReaction, TargetDummy und PlayerCombat.
- Combat-Verdrahtung in Assets/Scenes/SampleScene.unity ueber PlayerCombatController, Projectile-Prototyp und TargetDummy.

### Updated

- Docs/06_GLOSSARY.md um Boss, Elite und Pattern ergaenzt.
- Docs/Specs/Bosses.md, Enemies.md, Waves.md, Stages.md, Cards.md, Resources.md und Towers.md mit Links auf die relevanten Slice-Handoff-Dateien erweitert.
- Docs/08_IMPLEMENTATION_BACKLOG.md auf die neue Bosses-, Elite- und Balance-Reihenfolge sowie vorhandene Bosses-Basis aktualisiert.
- Assets/Scripts/Input erweitert um die Information, ob der letzte Input von einem Gamepad stammt.
- Docs/Specs/Combat.md um die konkrete First-Slice-Architektur erweitert.
- Docs/08_IMPLEMENTATION_BACKLOG.md auf Partially Completed fuer Combat und die neue SampleScene-Verdrahtung aktualisiert.
- Assets/Scripts/UI/MainMenu/MainMenuCanvasPresenter.cs von Debug-Textlisten auf eine art-basierte MainMenu-UI mit Background, Logo, Button-Frames und Fokusmarker umgestellt.
- Assets/Scenes/MainMenu.unity und Assets/ScriptableObjects/UI/MainMenu/MainMenuSkin_Default.asset an die importierten MainMenu-Art-Assets verdrahtet.
- Docs/Specs/MainMenu.md und Docs/08_IMPLEMENTATION_BACKLOG.md um den umgesetzten Art-UI-Slice ergänzt.
- Assets/Scripts/UI/MainMenu/MainMenuAudioCuePlayer.cs, Assets/Scripts/UI/MainMenu/MainMenuGlyphHintBar.cs und Assets/Scripts/UI/MainMenu/MainMenuAudioCueData.cs ergänzt, um UI-Cues und Controller-Hinweise in dieselbe MainMenu-Skin-Struktur zu ziehen.
- Assets/ScriptableObjects/UI/MainMenu/MainMenuAudioCue_Default.asset, Assets/ScriptableObjects/UI/MainMenu/MainMenuSkin_Default.asset und Assets/ScriptableObjects/Input/ControllerGlyph_Default.asset für MainMenu-Cues und Gerätehinweise verdrahtet.
- Docs/Specs/Input.md und Docs/Specs/Audio.md um die First-Slice-Anbindung von Gerätehinweisen und MainMenu-UI-Cues ergänzt.
- Assets/Scripts/UI/MainMenu/MainMenuRouteTargetController.cs, MainMenuRouteExecutionData.cs, MainMenuRoutePanelData.cs und MainMenuRoutePanelPresenter.cs ergänzt, um Continue, Settings und Meta Progression an echte Route-Ziele im MainMenu anzuschließen.
- Assets/ScriptableObjects/UI/MainMenu/MainMenuRouteExecution_Default.asset, MainMenuRoutePanel_Settings.asset und MainMenuRoutePanel_MetaProgression.asset ergänzt und in Assets/Scenes/MainMenu.unity verdrahtet.
- Assets/Scripts/SaveSystem/LocalProfileSaveManager.cs plus getrennte Run-, Settings- und Meta-Save-Domains ergänzt und über SaveBackedMainMenuStateProvider in das MainMenu eingebunden.
- Assets/Scripts/Settings/SettingsManager.cs, SettingsDefinitionData.cs, SettingsRouteTargetController.cs und SettingsRouteTargetPresenter.cs ergänzt, um die Settings-Route auf eine echte Options-UI mit Persistenz und Preview-Flow umzustellen.
- Assets/Scripts/MetaProgression/MetaProgressionService.cs, MetaProgressionDefinitionData.cs, MetaProgressionRouteTargetController.cs und MetaProgressionRouteTargetPresenter.cs ergänzt, um Meta Currency und Unlocks persistent im MainMenu verfügbar zu machen.
- Assets/ScriptableObjects/SaveSystem/RunSaveBootstrap_Default.asset, Assets/ScriptableObjects/Settings/SettingsDefinition_Default.asset und Assets/ScriptableObjects/MetaProgression/MetaProgressionDefinition_Default.asset ergänzt und in Assets/Scenes/MainMenu.unity verdrahtet.
- Assets/Scripts/Gameplay/Weapons/WeaponData.cs, WeaponAttackPatternData.cs, WeaponCatalogData.cs und WeaponPatternResolver.cs ergänzt und in PlayerCombatController an die bestehende Combat-Pipeline sowie Meta-Progression-Auflösung angeschlossen.
- Assets/Scripts/SaveSystem/RunPersistenceController.cs und RunProgressionCheckpointData.cs ergänzt, um Combat-, Reward- und Intermission-Checkpoints in RunSaveData zu schreiben.
- Assets/Scripts/Audio/AudioManager.cs und Assets/Scripts/Input/InputRebindManager.cs ergänzt und über SettingsManager an Lautstärke-, Rebind- und Grafikoptionen gebunden.
- Assets/ScriptableObjects/Gameplay/Weapons und zusätzliche Combat-/Save-/Input-Assets für drei Starter-Weapons, Rebind-Profil und Run-Checkpoint ergänzt sowie in Assets/Scenes/MainMenu.unity und Assets/Scenes/SampleScene.unity verdrahtet.
- Assets/Scripts/Gameplay/Cards/CardService.cs, CardRewardController.cs und CardRewardPresenter.cs ergänzt, um Pending Reward und Intermission in einen echten Drei-Karten-Reward-Flow zu überführen.
- Assets/Scripts/Gameplay/Resources/ResourceWalletService.cs sowie Assets/Scripts/Gameplay/Towers/TowerService.cs und TowerRuntimeController.cs ergänzt, damit Resource- und Tower-Hooks denselben Save- und Reward-Unterbau nutzen.
- Assets/Scripts/MetaProgression/StartLoadoutData.cs und erweiterte Meta-Definitionen ergänzt, um Unlock-Pfade über Weapons hinaus auf Cards, Towers und Startoptionen auszuweiten.
- Assets/ScriptableObjects/Gameplay/Cards, Assets/ScriptableObjects/Gameplay/Resources, Assets/ScriptableObjects/Gameplay/Towers sowie InputContext_Reward.asset ergänzt und in Assets/Scenes/SampleScene.unity verdrahtet.
- Assets/Scripts/Gameplay/Waves/WaveManager.cs, StageData.cs, WaveData.cs, SpawnGroupData.cs und WaveStatePresenter.cs ergänzt, damit Combat, Reward und Intermission über einen echten wiederholbaren Wave-Loop laufen.
- Assets/Scripts/Gameplay/Enemies/EnemyData.cs, EnemyBehaviourData.cs und EnemyRuntimeController.cs ergänzt, damit Chaser, Ranged Pressure und Bruiser an Reward-Auslösung, Resource-Gutschrift und Tower-Zielwahl angeschlossen sind.
- Assets/ScriptableObjects/Gameplay/Enemies und Assets/ScriptableObjects/Gameplay/Waves für Stage-1-Enemy- und Wave-Daten ergänzt und in Assets/Scenes/SampleScene.unity verdrahtet.
- Assets/Scripts/Gameplay/Bosses/BossEvents.cs, BossEventArgs.cs, BossAudioCuePlayer.cs und Assets/Scripts/UI/Gameplay/BossHudPresenter.cs ergänzt, damit Spawn-, Phase- und Defeat-Hooks jetzt direkt über die SampleScene laufen.
- Assets/ScriptableObjects/Gameplay/Bosses inklusive BossAudioCue_Default.asset ergänzt und Stage_01 auf sechs Waves plus Corebreaker Warden mit zwei Phasen, Helper-Spawns und Boss-Reward verdrahtet.
- Restore-Hooks in CombatHitReceiver, PlayerCombatController, WaveManager und RunPersistenceController ergänzt, damit Core-Druck echten Schaden verursacht und bestehende Run-Saves beim Laden nicht mehr durch Checkpoint-Defaults überschrieben werden.

## 2026-09-07

### Added

- Vollständiger "Corebound Repository Audit" (29 Specs gegen Code/Assets abgeglichen) als Artifact erstellt; 22 Rework-Pläne (R1–R22) identifiziert und in vier Prioritäts-Durchläufen (P0–P3) vollständig umgesetzt, u.a. build-sichere Laufzeit-Grafikladung über Assets/StreamingAssets/Art/, Reward-Screen-Bindungsfix, CombatTargetData für den Core, Tower-Bau während aktiver Waves, Player-Status-Effect-Empfang, Bruiser-/Boss-Telegraphs, drei eigenständige Elite-Varianten, gewichtete Card-Rarity-Angebote, echte MetaProgression-Loadout-/Reward-Anbindung, gehärtetes SaveSystem (atomare Writes, Statistics-Domain), Settings-/Input-Konsumenten für zuvor tote Datenfelder, UI-Datenmodell-Verdrahtung und Tutorial-Inhalt.
- Separater, vier-teiliger Technical-Debt-Pass über den gesamten Assets/Scripts-Baum durchgeführt: drei bis dahin unentdeckte echte Bugs behoben (fehlende SaveManager-Fallbacks in drei Klassen, ein Shop-Kauf-Bug mit hartkodiertem Stage-Index, eine nie neu abonnierte Boss-Phasen-Subscription), mehrfach duplizierte Logik konsolidiert (Rotate()-Trigonometrie, Unlock-Auflösung, Navigations-Schwellwerte, SaveManager-Resolver), tote Felder/Events entfernt oder als bewusst zurückgestellt dokumentiert.
- Assets/Scripts/Gameplay/Factory als neues System aufgesetzt: ProductionTriggerType, ProductionNodeData, ProductionModifierData, ProductionChainData, FactoryEvents/-EventArgs, FactoryService (Enemy-Kill-, Tower-Pulse- und Wave-Completion-Trigger inkl. Soft-Cap gegen doppelte Produzenten), FactoryAudioCueData/-CuePlayer und FactoryNoticePresenter (nutzt den bestehenden NotificationPresenter statt einer neuen UI-Hierarchie). Assets/ScriptableObjects/Gameplay/Factory mit einer ersten Beispiel-Chain (Enemy-Kill-Trickle, Support-Tower-Pulse, Wave-Clear-Bonus) befüllt und in Assets/Scenes/SampleScene.unity auf CombatRuntime verdrahtet.

### Updated

- Docs/08_IMPLEMENTATION_BACKLOG.md vollständig gegen den tatsächlichen Code-/Asset-Stand aufgefrischt (vorheriger Stand vom 2026-07-28 war in beide Richtungen veraltet).
- Assets/Scripts/Gameplay/Towers/TowerService.cs um CountBuiltTowersById(string) ergänzt, das FactoryService für die Tower-Pulse-Produzentenzählung nutzt.
