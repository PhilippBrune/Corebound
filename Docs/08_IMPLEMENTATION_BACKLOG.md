# Unity 6 Implementation Backlog

Version: 1.4

Status: Active

Basis: Projektvision, Technical Architecture, geschlossene Specs, Graphics Spec, 07_DESIGN_DECISIONS.md und Docs/Slices

Update 2026-09-07: Gegen den "Corebound Repository Audit" (Repository-Analyse · 2026-09-07) abgeglichen und aufgefrischt. Der vorherige Stand (Version 1.2, 2026-07-28) war in beide Richtungen veraltet — u.a. wurden MainMenu-Quit und Grafik-Asset-Produktion fälschlich als offen geführt, während die Statistics-Save-Domain und Tutorial korrekt als offen erkannt waren. Seither wurden die vollständigen R1–R22-Rework-Pläne aus dem Audit (vier Prioritäts-Durchläufe P0–P3), ein eigener Technical-Debt-Pass und das Factory-System umgesetzt.

Update 2026-09-17 (Version 1.4): Jede "Noch offen"-Aussage in diesem Dokument gegen den tatsächlichen Code-/Asset-Stand geprüft (Lesen der jeweiligen Klassen, GUID-Verfolgung durch die ScriptableObject-Referenzketten). Sieben Einträge waren stale und wurden korrigiert: Shop-Tower-Upgrades (Towers, Shop), Card-Stack-/Duplikat-Regeln (Cards), UIThemeData.Font-Anbindung (UI), Beam-Weapon-Pierce-Verhalten (Weapons), Tower-Typ-Anzahl (Towers), importierte Audiodateien und zentrales Musikzustands-System (Audio, jetzt Completed). Details und Belege in den jeweiligen Abschnitten sowie in der Recommended-Next-Slice-Sektion.

---

# Purpose

Dieses Dokument zeigt den aktuellen Implementierungsplan und den sichtbaren Status jedes Kernbereichs.

Die Reihenfolge folgt der vorgegebenen Systemkette:

MainMenu -> Input -> Player -> Combat -> Weapons -> Cards -> Towers -> Resources -> Factory -> Waves -> Enemies -> Bosses

---

# Status Legend

- Completed: fertig umgesetzt und ohne bekannte lokale Blocker in diesem Slice
- Partially Completed: Grundgerüst oder Teilmenge umgesetzt, Integration oder Folgearbeit offen
- Open: noch nicht umgesetzt

---

# Current Snapshot

- Completed
- Graphics Spec und Graphics Asset Prompt Pack erstellt; 41/42 Bildassets produziert und importiert
- ScriptableObject- und Art-Ordnerstruktur für den Vertical Slice angelegt
- Laufzeit-Grafikladung ist build-sicher (Assets/StreamingAssets/Art/ statt Editor-only Assets/-Zugriff)
- Kompletter Kernloop (MainMenu, Input, Player, Combat, Weapons, Enemies, Waves, Bosses, Cards, Towers, Shop, Settings, SaveSystem, MetaProgression) ist echter, datengetriebener Code und in MainMenu.unity sowie SampleScene.unity tatsächlich verdrahtet
- MainMenu-Routing inklusive Quit ist vollständig implementiert (Quit war in Version 1.2 fälschlich als offen geführt)
- Core besitzt ein eigenes Datenmodell (CombatTargetData-Asset statt Null-Fallback) und feuert reguläre Combat-Events
- Tower-Bau/-Upgrade ist während Combat, BossCombat und Intermission möglich (nicht mehr nur Intermission)
- Player empfängt Status Effects über denselben Pfad wie Enemy/Boss/Core
- Elite-Varianten sind drei eigenständige, unterschiedlich wirkende Presets (Relentless/Splitshot/Anchor) statt eines geteilten Default-Assets
- Card-Rarity beeinflusst Angebots-Gewichtung inklusive Legendary-Mindestfenster
- SaveSystem ist gehärtet: atomare Writes (.tmp -> File.Replace -> .bak), eigene Statistics-Domain, Save-Format-Versions-Warnungen, Save-Fehler-Events mit UI-Feedback (NotificationPresenter)
- Tutorial hat echten Inhalt (TutorialSequenceData/TutorialStepData) statt eines reinen Routing-Stubs
- Audio: echte importierte Audiodateien (23 .mp3 unter Assets/Audio/) belegen alle Clip-Slots der Wave-, Boss-, MainMenu-, Factory- und Combat-Cue-Player; MusicStateController/MusicState bilden ein zentrales, in SampleScene verdrahtetes Musikzustands-System (Calm/Combat/Boss, inkl. per-Stage-Boss-Musik) — Update 2026-09-17, Version 1.3 führte dies noch unter Partially Completed/Open

- Partially Completed
- Factory-System (FactoryService, drei Production Triggers, ProductionChainData/-NodeData/-ModifierData, Soft-Cap-Snowball-Schutz) neu aufgesetzt und in SampleScene verdrahtet, aber nur eine Beispiel-Chain autoriert
- UI-Lokalisierung: LocalizationService existiert und wird von einem Teil der Oberflächen genutzt (u.a. NotificationPresenter, BossHudPresenter, MainMenu-Route-Panels), aber längst nicht von allen Gameplay-/UI-Texten
- Bekannte, bewusst zurückgestellte Technical-Debt-Punkte (siehe Docs/09_CHANGELOG.md bzw. den Repository Audit, Abschnitt 08): drei große "God-Object"-Klassen (WaveManager, WaveStatePresenter, StageArenaArtPresenter) sowie mehrfach duplizierte Presenter-UI-Bausteine

- Open
- Relics-System (spec-seitig bewusst für diesen Vertical Slice zurückgestellt, kein Defekt)
- Weitere Factory-Production-Chains und echte Balance-Iteration über die eine Beispiel-Chain hinaus
- Volle UI-Lokalisierungsabdeckung, God-Object-Refactors der drei oben genannten Klassen

---

# Planning Constraints

- Das Spiel bleibt ein Unity-6-2D-Projekt mit orthographischer Top-Down-Ansicht.
- 2.5D ist nur als visuelle Präsentationsschicht über Sprites, Licht, Schatten, Layering und Faux-Depth erlaubt.
- Controller First ist für Gameplay, Menüs und Build-Flows verbindlich.
- Gameplay-Daten werden bevorzugt über ScriptableObjects verwaltet.
- Systeme kommunizieren eventgetrieben und bleiben lose gekoppelt.
- Wenn die Reihenfolge fachliche Vorgriffe erzwingt, sind nur kleine temporäre Hooks erlaubt.

---

# Shared Contracts

## Runtime Interfaces

- IMainMenuStateProvider
- IInputReader
- ICombatHitReceiver
- ICombatModifierProvider
- ICardOfferService
- ITowerSlotService
- IResourceWallet
- IProductionService
- IWaveStateReader
- IEnemySpawnService

## Canonical Events

- OnMainMenuOpened
- OnNewRunSelected
- OnContinueSelected
- OnInputContextChanged
- OnPlayerDamaged
- OnCoreDamaged
- OnDamageDealt
- OnEnemyKilled
- OnCardOffered
- OnCardSelected
- OnTowerBuilt
- OnTowerUpgraded
- OnResourceCollected
- OnProductionTriggered
- OnWaveStarted
- OnWaveCompleted
- OnBossSpawned
- OnBossPhaseChanged

---

# Ordered Core Backlog

## 1. MainMenu

Status: Completed

Bereits umgesetzt:

- MainMenuRoute, MainMenuEntryAvailability, MainMenuState und IMainMenuStateProvider
- MainMenuRuntimeModel und MainMenuController
- SaveBackedMainMenuStateProvider mit LocalProfileSaveManager, SettingsManager und MetaProgressionService
- MainMenuInputHandler für die Anbindung an die Eingabeschicht
- MainMenu Szene mit Controller- und State-Provider-Objekten
- MainMenu Szene mit InputReader-Objekt und verdrahtetem MainMenu-Input-Kontext
- MainMenuFlow_Default und sechs MenuEntry Assets
- ContinueState_Default, IntroFlow_Default und TutorialEntry_Default
- MainMenu Szene in EditorBuildSettings eingetragen
- Runtime-Canvas mit importiertem Background, Logo, Button-Frames, Fokusmarker, Prompt-Panel und Status-Panel wird in der Szene erzeugt
- Glyph-Hint-Bar und prozedurale UI-Cues für Navigate, Confirm, Cancel und Menu-Open über dieselbe Presenter-Skin-Struktur verdrahtet
- Continue lädt im aktuellen Slice die gespeicherte Gameplay-Szene aus RunSaveData, und Settings sowie Meta Progression öffnen echte servicegestützte Route-Ziele im MainMenu
- Quit ist über ein echtes Bestätigungspanel vollständig implementiert (MainMenuRouteTargetController) — in Version 1.2 fälschlich als offen geführt
- New-Run-Flow wendet die freigeschaltete Start-Loadout-Auswahl über MetaProgressionService.GetUnlockedStartLoadouts an, statt immer den statischen Bootstrap zu nutzen

Noch offen:

- weitere Frontend-Polish-Schritte wie zusätzliche Modal-Layouts und finale UI-SFX-Assets statt des lokalen Cue-Players (weiterhin prozedurale Tongenerierung, keine importierten Audiodateien)
- vollständige Lokalisierungsabdeckung über alle MainMenu-Texte hinweg (Teilabdeckung über LocalizationService vorhanden)

## 2. Input

Status: Completed

Bereits umgesetzt:

- InputSystem_Actions auf Gameplay, Build, UI und System umgestellt
- IInputReader und CoreboundInputReader angelegt
- InputContextData, ControllerGlyphData, InputAssistData und RebindProfileData angelegt
- InputContext_MainMenu, InputContext_Gameplay und InputContext_Build erstellt
- MainMenu an Confirm-, Cancel- und Navigate-Signale angebunden
- semantische MainMenu-Input-Events für erfolgreiche Navigate-, Confirm- und Cancel-Aktionen exponiert
- Gameplay- und Build-Consumer außerhalb des MainMenu vorhanden (PlayerController, TowerService, CardRewardController, ShopService u.a.) — in Version 1.2 fälschlich als offen geführt
- InputRebindManager wendet Rebind-Overrides tatsächlich zur Laufzeit an (ApplyBindingOverride)
- CoreboundInputReader-Hook/Unhook auf eine gemeinsame Bindungstabelle konsolidiert statt 20+ Zeilen manuell gepaarter +=/-=

Lieferobjekte:

- vier Action Maps: Gameplay, Build, UI und System
- IInputReader als semantische Eingabeschicht
- Menü-Navigation für Press Start, Confirm, Cancel und Fokuswechsel
- echte Rebind-Anwendung zur Laufzeit

Noch offen:

- Rebind-Content deckt weiterhin nur einen kleinen Teil der ~12 Actions ab
- Feinschliff für Eingabewiederholung, Deadzones und spätere Glyph-Nutzung

## 3. Player

Status: Completed

Bereits umgesetzt:

- PlayerData, PlayerController und PlayerMovementMotor angelegt
- PlayerInteractionPriorityResolver und Interaction-Typen angelegt
- SampleScene mit PlayerRoot und Gameplay-InputReader verdrahtet
- 2D-Bewegung auf Basis der Eingabeschicht, echte Art statt Debug-Visual
- Combat-Anbindung ueber separaten PlayerCombatController auf dem PlayerRoot ergaenzt
- Player-Tod bzw. Core-Zerstörung setzt WaveState.Failed über WaveManager.OnCombatantDied (Verlustzustand ist eine echte Run-State-Transition)
- Player empfängt Status Effects (Burn/Freeze) über denselben Pfad wie Enemy/Boss/Core statt gar nicht
- Trefferfeedback über CameraShake (Screen-Shake-Intensity-Option) und Hit-Flash am Core/Player

Lieferobjekte:

- PlayerController mit Movement und Aim
- Interaktionspriorität für UI, Tower Slots, Rewards und Pickups
- Verlustzustand für Player und Core
- Auto-Pickup-Hook für Resources-Anbindung, inkl. Wave-Ende-Vacuuming

Noch offen:

- weiteres visuelles/Audio-Polish für Trefferfeedback über die bestehende Basis hinaus

## 4. Combat

Status: Completed

Bereits umgesetzt:

- DamageContext, DamageKind und DamageSourceType als gemeinsame Damage-Grundlage angelegt
- CombatBalanceData, DamageProfileData, ProjectileData, StatusEffectData, HitReactionData und CombatTargetData erstellt
- HealthRuntime und StatusEffectRuntime für Damage-, Death- und Status-Auflösung angelegt
- PlayerCombatController mit Hold-to-Fire, Controller-Soft-Aim (non-alloc Physics2D-Query) und Player-I-Frames umgesetzt
- CombatProjectile plus ProjectilePool als Pooling-Basis umgesetzt
- CombatHitReceiver, Burn- und Freeze-Status-Assets sowie TargetDummy in SampleScene verdrahtet
- Enemy-, Boss-, Core- und Tower-Seite laufen über denselben CombatHitReceiver/CombatEvents-Pfad wie der Player
- Combat-Events sind in CameraShake, Hit-Flash und die jeweiligen Audio-Cue-Player eingespeist
- Enemy-/Boss-Angriffe (Bruiser-Charge, Boss-Projectile-Bursts) haben ein sichtbares, datengetriebenes Telegraph/Windup statt sofortigem Angriff
- gemeinsamer StatusEffectData.Find()-Helper statt zweifach dupliziertem Lookup in CombatHitReceiver und PlayerCombatController

Lieferobjekte:

- DamageContext, Health Runtime und Death Handling
- Player-I-Frames nach Treffer
- Burn- und Freeze-Status, jetzt auch am Player wirksam
- Combat Events und Pooling-Basis für Projectiles
- geteilte Hit-Reception-Pipeline für Player, Enemy, Boss und Core

Noch offen:

- weiteres VFX-Polish (siehe Graphics/VFX-Sheets, teilweise ungenutzte Atlanten)

## 5. Weapons

Status: Partially Completed

Bereits umgesetzt:

- WeaponData, WeaponAttackPatternData und WeaponCatalogData angelegt
- Rapid-, Spread- und Beam-Starter-Assets verdrahtet
- PlayerCombatController liest aktive Weapon aus WeaponCatalog, RunSaveData und MetaProgressionSaveData
- erster Weapon-Unlock aus Meta Progression besitzt einen direkten Gameplay-Pfad
- "Beam" unterscheidet sich von "Rapid" tatsächlich mechanisch, nicht nur parametrisch: ProjectileData._pierceCount (Projectile_PlayerBeam.asset: 2) wird von CombatProjectile.OnTriggerEnter2D echt ausgewertet (_remainingPierceCount überlebt bis zu zwei zusätzliche Treffer, statt beim ersten Treffer zu released) — Version 1.3 führte dies noch als offen ("kein echtes Pierce-Verhalten"), war aber bereits umgesetzt

Lieferobjekte:

- WeaponData, WeaponAttackPatternData und ProjectileData
- Hold-to-Fire-Flow
- drei Starter-Archetypen
- globale Controller-Aim-Assist-Stärke plus Weapon-Profile
- gemeinsamer CombatMath.Rotate()-Helper statt vierfach dupliziertem Trigonometrie-Code (Boss/Enemy/Weapon)
- echtes Pierce-Verhalten für die Beam-Weapon über ProjectileData.PierceCount

Noch offen:

- WeaponPresentationData und differenziertere Audio- oder Visual-Profile
- Weapon-Modifier-Pipeline für spätere Card- und Tower-Hooks (aktuell einzelne Float-Properties auf CardService statt eines eigenen Asset-Typs — siehe Weapons.md's Audit-Notiz vom 2026-09-16)

## 6. Cards

Status: Partially Completed

Bereits umgesetzt:

- CardData, CardEffectData, CardPoolData und CardOfferData angelegt
- save-basierter Pending-Reward- und Intermission-Flow über CardRewardController und CardRewardPresenter umgesetzt
- drei sofort verfügbare und eine meta-gebundene Card-Linie verdrahtet
- Card-Modifikatoren greifen bereits auf Weapon-, Resource- und Tower-Multiplikatoren
- CardService gewichtet Angebote jetzt nach Rarity inklusive Legendary-Mindestfenster statt rein deterministischer Modulo-Rotation
- Unlock-Auflösung über den gemeinsamen MetaProgressionService.IsUnlockSatisfied()-Helper statt dreifach dupliziertem Fallback-Code (Cards/Towers)
- Stack-/Duplikat-Regeln sind umgesetzt: CardData._stackable ist pro Karte autoriert, CardService.CanOfferCard() und TryApplyCard() blocken beide nicht-stackable Duplikate über denselben ContainsSelected()-Check — Version 1.3 führte dies noch als offen, war aber bereits umgesetzt

Lieferobjekte:

- CardData, CardEffectData, CardPoolData und CardOfferData
- drei Card-Optionen pro Reward-Moment
- Card-Tag-Basis für Weapon, Projectile, Tower, Resource, Burn, Freeze und Critical
- Modifier-Anbindung für Combat und Weapons
- gewichtete, Rarity-sensitive Angebotslogik
- Stack-/Duplikat-Regeln über CardData.Stackable

Noch offen:

- tiefere Card-Effekte für Status, Economy und spätere Factory- oder Tower-Interaktionen
- ~~CardData.Tags wird von keinem Code gelesen~~ — 2026-09-30: Tags gewichten das Kartenangebot über einen Regler (`CardOfferData._tagAffinity`, Open Topics 4 (a)); vier neue Tags Poison/Shock/Support/Summon

## 7. Towers

Status: Partially Completed

Bereits umgesetzt:

- TowerData und TowerCatalogData angelegt
- TowerService hält save-gebundene aktive Tower-Liste
- vier eigenständige Tower-Typen (Support, Ballista, Mortar, Sentinel — je eigene TowerData mit eigenem Range/Cost/FireInterval/DamageProfile/Projectile) sind in TowerCatalog_Default katalogisiert und nutzen dieselbe Projectile- und Combat-Pipeline wie Weapons — Version 1.3 führte hier noch "ein Runtime-Tower"
- Meta-Unlocks und Card-Modifikatoren können bereits auf Tower-Zustand wirken
- Tower-Bau und -Upgrade sind während Combat, BossCombat und Intermission möglich (IsBuildableWaveState), nicht mehr nur während Intermission — vorher ein Widerspruch zur dokumentierten Design-Entscheidung
- fehlgeschlagene Tower-Upgrades nach bereits erfolgter Zahlung werden jetzt erstattet (TowerService.RefundPurchase), vorher ein potenzieller stiller Resource-Verlust
- CountBuiltTowersById(string) für produzentenabhängige Systeme (u.a. Factory) ergänzt
- Tower Upgrades als Shop-Angebot sind verkaufbar (ShopService löst ShopOfferType.TowerUpgrade über TowerService.CanUpgradeSelectedTowerForShop()/TryApplyUpgradeFromShop() vollständig auf, ShopOffer_TowerUpgrade.asset ist in ShopOfferTable_Stage1 autoriert und über Stage_01 erreichbar) — Version 1.3 führte dies noch als offen, war aber bereits umgesetzt

Lieferobjekte:

- TowerData, TowerLevelData und TowerTargetingData
- Tower Slot Cycling für Controller
- Mid-Wave- und Intermission-Building und Upgrades
- drei Stufen pro Tower
- Tower-Upgrade-Kauf über den Shop

Noch offen:

- keine bekannten offenen Kernpunkte für den ersten Vertical Slice (TowerLevelData ist bereits pro Tower autoriert — siehe "drei Stufen pro Tower" oben — und vier Tower-Typen decken die von der Vision geforderten "ungefähr drei" bereits ab)

## 8. Resources

Status: Completed

Bereits umgesetzt:

- primäre Run Resource als ResourceWalletService und ResourceData angelegt
- ResourcePickupPool/ResourcePickupRuntime: echte Arena-Pickups mit kurzer Auto-Pickup-Reichweite im Combat und vollständigem Wave-Ende-Vacuuming statt rein eventgetriebener Gutschrift
- Card-Modifikatoren verstärken Resource-Gewinne
- Wallet schreibt nicht mehr synchron bei jedem einzelnen Pickup auf die Festplatte, sondern sammelt Änderungen (dirty flag) und flusht einmal pro Frame plus beim Deaktivieren/Beenden — vorher ein Disk-Write pro Pickup

Lieferobjekte:

- eine primäre Run Resource
- Pickup Runtime mit Auto-Pickup und Wave-Ende-Vacuuming
- Wallet, Spend-Validation und Event-Ausgabe
- HUD-Anbindung
- Ausgaben für Tower-Building/-Upgrades und Shop-Käufe über EconomyService

Noch offen:

- keine bekannten offenen Kernpunkte für den ersten Vertical Slice

## 9. Factory

Status: Partially Completed

Bereits umgesetzt (2026-09-07 neu aufgesetzt, vorher 0% umgesetzt trotz explizitem Erst-Scope-Anspruch laut Design Decisions):

- Assets/Scripts/Gameplay/Factory mit ProductionTriggerType, ProductionNodeData, ProductionModifierData, ProductionChainData, FactoryEvents/-EventArgs und FactoryService angelegt
- FactoryService wertet registrierte Producer zentral aus und hört auf CombatEvents.CombatantDied (gefiltert auf EnemyRuntimeController) und WaveEvents.WaveCompleted; Tower-Pulse-Producer laufen über einen eigenen Timer pro Node
- Soft-Cap gegen Snowballing über ProductionModifierData.ResolveEffectiveProducerWeight (geometrische Abschwächung ab der zweiten identischen Tower-Kopie, harte Obergrenze für gezählte Kopien)
- harte Datenobergrenze pro Trigger über ProductionNodeData.MaxAmountPerTrigger (v.a. für Wave-Completion-Boni)
- FactoryAudioCueData/-CuePlayer mit Rate-Limiting gegen Audio-Spam bei häufigen Kill-Triggern
- FactoryNoticePresenter nutzt den bestehenden NotificationPresenter als "unterstützendes, nicht dominierendes" Signal statt einer neuen UI-Hierarchie
- eine Beispiel-Chain (ProductionChain_Default: Enemy-Kill-Trickle, Support-Tower-Pulse, Wave-Clear-Bonus) autoriert und in SampleScene.unity auf CombatRuntime verdrahtet
- TowerService.CountBuiltTowersById(string) als neue, minimale öffentliche API für produzentenabhängige Systeme ergänzt

Lieferobjekte:

- FactoryService (statt eines separaten ProductionService-Interfaces — als konkrete Klasse umgesetzt)
- Production Triggers für Kill, Tower-Pulse und Wave-Ende (exakt die drei laut Design Decisions für den ersten Scope vorgesehenen — Resource Conversion ist bewusst nicht Teil dieses Slice)
- Soft Caps gegen Snowballing
- eine erste Resource-Produktionskette über einen Tower-Hook (Support Tower) und Kill-/Wave-Hooks

Noch offen:

- weitere Production Chains über die eine Beispiel-Chain hinaus, insbesondere mit echten Card-Synergien (OnCardApplied ist in Factory.md als möglicher künftiger Trigger genannt, aber nicht Teil des Erst-Scope)
- Balancing-Pass für die tatsächlichen Erzeugungsraten im Playtest
- UI über den einfachen Notice-Toast hinaus (aktive Produktionsquellen, Multiplikatoren als dauerhaft sichtbare Anzeige) — bewusst zurückgestellt, siehe Factory.md's eigene Warnung vor einer zu komplexen Oberfläche

## 10. Waves

Status: Partially Completed

Bereits umgesetzt:

- StageData, WaveData, SpawnGroupData und WaveRewardData für einen ersten Stage-1-Loop angelegt
- WaveManager steuert Combat, Reward, Intermission, BossCombat und Stage-Abschluss in SampleScene
- Reward-Pending wird erst bei abgeschlossenem Wave-Clear statt bei einem Einzelkill gesetzt
- Stage_01 enthaelt jetzt sechs Combat Waves plus Boss-Referenz
- WaveModifierData.IsEliteEncounter ist kein totes Feld mehr: WaveProgressEventArgs trägt es jetzt bis zu Audio-/HUD-Konsumenten weiter, statt dass diese "Elite" per Substring-Match im Anzeigenamen erraten
- Spawn-Geometrie (Fächer-Radius, Winkel-Schritt, Boss-Spawnhöhe) von hartkodierten Zahlen in WaveManager auf echte StageData-Felder verschoben, damit sie pro Stage tunbar sind
- WaveManager re-abonniert Boss-PhaseChanged jetzt korrekt in OnEnable (vorher ein Bug: nach einem Disable/Enable während eines Boss-Kampfs blieben Phasenwechsel-Events dauerhaft stumm)
- drei eigenständige Elite-Varianten (Relentless/Splitshot/Anchor) statt eines geteilten Default-Presets

Lieferobjekte:

- StageData und WaveData mit fixer Stage-1-Struktur
- WaveManager mit sechs Combat Waves, einer Elite Wave und Boss-Übergang
- 20-Sekunden-Intermission mit Early Start
- Reward-Hooks für Cards
- drei unterscheidbare Elite-Varianten

Noch offen:

- ~~StageEnemyPoolData war autoriert, aber von keinem Code gelesen~~ — 2026-09-30 samt der fünf Assets gelöscht (Owner-Entscheidung, Open Topics 4 (a)); die stage-eigenen `WaveData`-Assets haben die Rolle übernommen
- WaveManager selbst ist mit ~15 Verantwortlichkeiten (State Machine, Statistik-Persistenz, Core-Konstruktion, Label-Auflösung) ein bekannter "God Object"-Kandidat für einen künftigen Split, siehe Code-Kommentar in WaveManager.cs

## 11. Enemies

Status: Partially Completed

Bereits umgesetzt:

- Chaser, Ranged Pressure und Bruiser als datengetriebene Enemy-Rollen angelegt
- EnemyRuntimeController verbindet Bewegung, Core- oder Player-Druck, Contact-Damage und Reward-Auslösung
- Enemy-Spawns und Tower-Zielwahl laufen im wiederholbaren Wave-Loop der SampleScene
- Core-Zielwahl besitzt jetzt einen echten Damage-Pfad statt nur Retargeting
- Elite-Modifier-Paket mit Health-, Damage-, Reward- und Projectile-Boni vorhanden
- Bruiser-Charge hat jetzt ein sichtbares Windup/Telegraph statt sofortiger 2.6x-Geschwindigkeit ohne Vorwarnung (folgt demselben Muster wie der bereits vorhandene Ranged-Pressure-Telegraph)
- drei eigenständige, unterschiedlich wirkende Elite-Varianten statt eines einzigen geteilten Presets

Lieferobjekte:

- Chaser, Ranged Pressure und Bruiser
- Elite-Paket aus Stat-Modifier plus drei eigenständigen Varianten (Relentless/Splitshot/Anchor)
- Core-orientiertes Retargeting zum Player
- Resource-Rewards über OnEnemyKilled (jetzt zusätzlich an Factory-Production-Trigger angebunden)
- Telegraph vor gefährlichen Angriffen (Bruiser-Charge, Ranged Pressure)

Noch offen:

- EnemyRuntimeController und BossRuntimeController bleiben dicke MonoBehaviours (~660 bzw. ~440 Zeilen) mit teils dupliziertem Windup-/Telegraph- und Rigidbody-Setup-Code über die gemeinsame CombatMath.Rotate()-Konsolidierung hinaus

## 12. Bosses

Status: Partially Completed

Lieferobjekte:

- ein Stage-1-Boss
- Health-Threshold-Phasen und optionales Enrage-Modul
- begrenzte Hilfs-Enemy-Fenster
- Boss-spezifische Telegraph- und Reward-Integration

Bereits umgesetzt:

- BossData, BossPhaseData, BossAttackPatternData, BossRewardData und BossRuntimeController angelegt
- Boss_Stage1 mit zwei Phasen, BossReward_Stage1 und BossBurst-Projectile-Daten vorhanden
- Stage_01 verweist nach sechs Waves auf Boss_Stage1
- WaveManager kann nach der Wave-Sequenz in BossCombat wechseln und Boss-Reward ausloesen
- RunPersistenceController speichert aktive Boss-Begegnung und Boss-Phase
- BossEvents, BossHudPresenter und BossAudioCuePlayer fuer Boss-Warnung, Spawn, Phasewechsel und Defeat verdrahtet; seit 2026-09-30 liegt das Boss-HUD (mit Notification und Relic-Reveal) in der autorierten Szene `OverlayCanvas.unity`, und die Autopilot-Variante `bossN` prüft die Phasenwechsel aller fünf Bosse
- Continue-/Restore-Pfad ueberschreibt beim Laden keinen bestehenden Run-Save mehr sofort
- BossAttackPatternData besitzt jetzt ein Telegraph-/Windup-Feld; Boss-Projectile-Bursts feuern nicht mehr sofort bei Cooldown-Ablauf ohne Vorwarnung
- WaveManager re-abonniert BossRuntimeController.PhaseChanged jetzt auch in OnEnable, nicht mehr nur beim ersten Encounter-Start

Noch offen:

- weitere Boss-Polish-Schritte fuer Timing und Playtest-Fairness
- Boss-Reward gegen Cards, Resources und Towers balancen
- Lesbarkeit von Boss-Projectiles, Hilfs-Enemies und Phase-2-Druck im Playtest pruefen

Slice-Handoff:

- [Bosses Stage 1 Slice](Slices/12_BOSSES_STAGE1_SLICE.md)
- [Elite Variants Slice](Slices/13_ELITE_VARIANTS_SLICE.md)
- [Wave Boss Balance Slice](Slices/14_WAVE_BOSS_BALANCE_SLICE.md)

---

# Secondary Tracks

## SaveSystem

Status: Completed

Bereits umgesetzt:

- LocalProfileSaveManager mit getrennten JSON-Domains für Run, Settings, Meta Progression und Statistics
- RunSaveBootstrap_Default als erster echter Continue-Datensatz
- SaveBackedMainMenuStateProvider für Continue- und Tutorial-Status im Main Menu
- RunPersistenceController mit Combat-, Reward- und Intermission-Checkpoints in SampleScene
- RunSaveData speichert nun auch aktive Card-IDs und Tower-IDs für denselben Run
- WaveContext wird nun save-basiert mit Stage-, Wave-, Reward- und Intermission-Zustand fortgeschrieben
- StatisticsSaveData als eigene Domain ergänzt
- JsonFileSaveStore schreibt atomar (.tmp -> File.Replace -> .bak, alter Stand bleibt bei Absturz mid-write erhalten)
- Save-/Load-Fehler feuern Events, die NotificationPresenter als sichtbares, lokalisiertes UI-Feedback anzeigt (vorher nur Debug.LogError)
- SaveFormatVersion wird beim Laden gegen die aktuell erwartete Version geprüft und bei Abweichung gewarnt (kein Migrations-Framework, da bisher kein Formatwechsel stattgefunden hat)
- LocalProfileSaveManager.Instance wird bei Zerstörung korrekt zurückgesetzt (vorher: Duplicate-Instanzen zerstörten nur die Komponente statt des ganzen GameObjects, und Instance konnte auf ein zerstörtes Objekt zeigen bleiben)
- die zuvor 12-fach duplizierte "_saveManagerSource as ISaveManager ?? LocalProfileSaveManager.Instance"-Fallback-Kette (dabei an drei Stellen ohne Fallback, was Speichern/Laden dort still fehlschlagen ließ) ist jetzt ein einziger SaveManagerResolver.Resolve()-Aufruf

Noch offen:

- keine bekannten offenen Kernpunkte für den ersten Vertical Slice

## Settings

Status: Completed

Bereits umgesetzt:

- SettingsDefinition_Default mit Audio-, Grafik-, Accessibility- und Control-Optionen
- SettingsManager mit Preview-, Apply- und Persistenz-Flow
- controller-freundliche Settings-Route-UI im Main Menu
- AudioManager-, RebindProfile- und zusätzliche Grafikoptionen wie Auflösung direkt an den Settings-Flow angebunden
- Screen-Shake-Intensity (CameraShake), Effect-Intensity und HUD-Scale (WaveStatePresenter) haben jetzt echte Konsumenten statt toter Datenfelder
- InputAssistData.DefaultMenuNavigateThreshold als einzige Quelle für den zuvor fünffach duplizierten 0.5f-Navigations-Schwellwert-Fallback

Noch offen:

- zusätzliche Grafikoptionen jenseits der ersten Presets und Window-Resolution-Pakete

## Tutorial

Status: Partially Completed

Bereits umgesetzt:

- TutorialSequenceData, TutorialStepData und TutorialStepType als echter, datengetriebener Inhalt statt eines reinen Menü-Routing-Stubs
- TutorialService setzt HasSeenTutorial nach Abschluss aller Pflichtschritte (vorher strukturell nie erreichbar)

Noch offen:

- Playtest-Pass, ob die aktuellen Schritte als Einführung wirklich ausreichen, bzw. weitere Schritte über die erste Abdeckung hinaus

## UI

Status: Partially Completed

Bereits umgesetzt:

- MainMenu-, HUD-, Reward- und Boss-Overlay-Canvas-Hierarchien sind vorhanden und in beiden Szenen verdrahtet (nicht mehr fehlend wie in Version 1.2 behauptet)
- Reward-Screen bindet jetzt zuverlässig an die handautorierte Szenen-Hierarchie (_authoredCanvasGroup war zuvor null, wodurch eine doppelte Fallback-UI mit Platzhaltertext parallel lief)
- LocalizationService (EN/DE) existiert und wird von NotificationPresenter, BossHudPresenter, ContinueStateData, IntroFlowData, den MainMenu-Route-Panels, CardData und jetzt auch ShopPresentationData/ShopService/ShopPresenter genutzt
- UIThemeData.Font wird jetzt über den gemeinsamen UIFontResolver.Resolve()-Helper von 11 Presenter-Klassen gelesen (vorher: toter Datenpfad) — Version 1.3 führte dies pauschal als ungenutzt, das galt aber nur noch für Farben/Sprite, nicht mehr für Font
- Update 2026-09-17: gefundene Lücke geschlossen, bevor sie zur echten Lokalisierungsabdeckung gezählt werden konnte - die vier MainMenuRoutePanelData-Assets (Settings/MetaProgression/NewRunConfirm/QuitConfirm) hatten ihre `_xDe`-Felder zwar im Code, aber nie befüllte Werte in der gespeicherten YAML (leeres `_de`-Feld → Resolve() fällt immer auf Englisch zurück), da die Assets seit Hinzufügen der Felder nie im Editor neu gespeichert wurden - direkt in der YAML nachgetragen. CardData bekam dieselbe Behandlung neu hinzugefügt (`_displayNameDe`/`_descriptionDe`, alle 30 Card-Assets übersetzt) - deckt automatisch sowohl CardRewardPresenter als auch ShopPresenter ab, da beide über `CardData.DisplayName`/`.Description` lesen.
- Update 2026-09-17 (zweiter Durchgang): WaveStatePresenter (die HUD-Statuszeile, alle Banner, Pause-Overlay inkl. Active-Effects-Liste, Failed-Overlay), WaveManager.FailureCauseLabel und TowerService.ResolveSelectedSlotSummary()/SelectedTowerUpgradeTitle()/Description() (die Tower-Placement/-Upgrade-Zeile im HUD während Intermission) sind jetzt vollständig über LocalizationService abgedeckt - das ist die während des Spielens am häufigsten sichtbare Oberfläche im gesamten Spiel. Bewusst nicht übersetzt: `occupant.name`/`TowerData.DisplayName`/`occupant.UpgradeLabel` (Turmname/-Levelbezeichnung) bleiben englisch, da das eine eigene TowerData/TowerLevelData-Übersetzung erfordern würde (siehe unten) - die umgebende Satzstruktur ist aber vollständig deutsch, ein in der Spielelokalisierung übliches Muster (Eigennamen bleiben, UI-Text wird übersetzt).

Noch offen:

- Lokalisierung deckt weiterhin nur einen Teil der Oberflächen ab: CardRewardPresenter (eigene Strings jenseits der jetzt lokalisierten CardData-Felder), RelicRevealPresenter, MetaProgressionRouteTargetPresenter, SettingsRouteTargetPresenter, MainMenuCanvasPresenter bleiben unlokalisiert
- TowerData.DisplayName und TowerLevelData.Label (Turmnamen und Upgrade-Stufenbeschreibungen, u.a. in den jetzt lokalisierten HUD-Sätzen eingebettet) sind nicht übersetzt - eigener, in sich abgeschlossener Folgeschritt (4 TowerData- plus ~12 TowerLevelData-Assets), analog zum bereits erledigten CardData-Durchgang
- UIThemeData.PrimaryTextColor/SecondaryTextColor/AccentColor/PanelFrameSprite sind weiterhin von keinem Presenter gelesen (nur .Font ist inzwischen über UIFontResolver angebunden) — jeder Presenter hardcodet weiterhin eigene Farben
- prozedurale UI-Bau-Bausteine (CreateText/CreateImage/ResolveFont u.ä.) sind über mindestens 6 Presenter-Klassen dupliziert und teils bereits leicht auseinandergedriftet; ein gemeinsamer Builder wäre wertvoll, wurde aber als zu riskant ohne Unity-Editor-Zugriff zur Verifikation zurückgestellt
- WaveStatePresenter bleibt ein "God Object" aus vier separaten Verantwortlichkeiten (Runtime-UI-Bau, Editor-Szenen-Tooling, Daten-Binding, Pause-/Szenen-Flow-Steuerung)

## Audio

Status: Completed

Bereits umgesetzt:

- ereignisgetriebene, datengetriebene Audio-Cue-Player für Wave-, Boss-, MainMenu- und jetzt auch Factory-Events, jeweils mit eigenem *AudioCueData-Asset
- ProceduralToneFactory als gemeinsame Tongenerierung statt dreifach dupliziertem Sinuswellen-Code
- Rate-Limiting gegen Audio-Spam bei hochfrequenten Events (Factory-Produktionstrigger)
- echte importierte Audiodateien (23 .mp3 unter Assets/Audio/) sind alle 14 Clip-Slots der vier *AudioCueData-Assets plus AudioCue_CombatHit zugewiesen; ProceduralToneFactory dient jetzt nur noch als automatischer Fallback für künftig unbefüllte Felder — Version 1.3 führte dies noch als offen
- MusicStateController/MusicState bilden bereits ein zentrales Musikzustands-System (Calm/Combat/Boss inkl. per-Stage-Boss-Clip-Auflösung) und sind in SampleScene verdrahtet — Version 1.3 führte dies noch als offen

Noch offen:

- keine bekannten offenen Kernpunkte für den ersten Vertical Slice

## Meta Progression

Status: Completed

Bereits umgesetzt:

- MetaProgressionDefinition_Default mit Core-Shards-Currency und erstem Unlock-Katalog
- MetaProgressionService mit persistenter Currency- und Unlock-Speicherung, jetzt inklusive korrektem SaveManager-Fallback (vorher: ohne im Inspector zugewiesene Referenz schlugen Käufe und das Tutorial-Seen-Flag still fehl, ohne persistiert zu werden)
- controller-freundliche Meta-Route-UI für Kauf und Statusanzeige im Main Menu
- Weapon-Unlocks beeinflussen bereits die Weapon-Auflösung für neue oder ungefixte Runs
- New-Run-Flow wendet die freigeschaltete Start-Loadout-Auswahl tatsächlich an (MainMenuRouteTargetController.ApplyUnlockedStartLoadout)
- IsUnlocked/IsUnlockPurchased-Doppelbenennung auf eine einzige Methode konsolidiert
- 2026-09-30/10-01: fünf spielbare Klassen (Vanguard, Engineer, Salvager, Custodian, Catalyst) mit Signatur, eigenem Skill-Tree, Kit, Karten-Neigung und zehn Klassenkarten; Auswahl im New-Run-Panel, freie Freischaltung über bestehende Quests (`Specs/DIY/CHARACTER_CLASSES_2026-09-30.md`, "As built")
- 2026-09-30: Story - die Stimme des Kerns, Prämisse, Ende, Core Logs und Credits in Records (`Specs/DIY/STORY_2026-09-30.md`, "As built")

Noch offen:

- Belohnungszufuhr aus echten Post-Run- und Gameplay-Ergebnissen über die bestehenden Reward-Hooks hinaus weiter ausbauen

## Graphics Asset Production

Status: Completed

Bereits umgesetzt:

- Graphics Spec, Asset-Prompts und Unity-Ordnerstruktur vorhanden
- 41 von 42 spezifizierten Bildassets produziert und importiert (nur glyph_hint_capsule.png fehlt, laut Prompts-Dokument bewusst mit einem generischen Button-Frame überbrückt) - Stand 2026-10-01: alle geliefert und verdrahtet, die Glyph-Kapsel eingeschlossen; Status pro Asset in `AI/GRAPHICS_ASSET_PROMPTS.md`
- Laufzeit-Ladepfad ist build-sicher: Assets/StreamingAssets/Art/ wird zuerst versucht, der alte Application.dataPath-Zugriff ist jetzt Editor-only (vorher: ein echter Standalone-Build wäre praktisch grafiklos gewesen, da Assets/ im gepackten Build nicht existiert)

Noch offen:

- ~~sechs Multi-Slot-Sprite-Sheets umgehen das Datenmodell~~ — 2026-09-30: Raster und benannte Zellen liegen in `SpriteSheetData`-Assets (`Assets/ScriptableObjects/Art/`: beide Projektil-Sheets, Waffen-Icons, Tag-Icons, beide Impact-Sheets); `combat_vfx_sheet_essentials.png` ist durch Einzeldateien ersetzt und nur noch in Kommentaren genannt. Boden-/Prop-Atlanten der Stages folgten 2026-10-01 (`SpriteSheet_StageGround1-5`, `SpriteSheet_StageProps1-5`)
- ~~die vier Klassen-Sprites (Assets 110-113)~~ — 2026-10-07 geliefert und verdrahtet, zusammen mit einem neu gezeichneten Vanguard (Asset 14): alle fünf über einen Ladepfad, auf die Figur zugeschnitten, gleich groß, mit Mipmaps (`Specs/DIY/CHARACTER_CLASSES_2026-09-30.md`, "Sprites")
- 2026-10-07: Steam-Seite vorbereitet — Achievement-Vorschläge, zehn Screenshots, Rohschnitt-Trailer (`STEAM_PAGE.md`); Capsule-Art und Achievement-Icons fehlen noch

---

# Recommended Next Slice

Bosses, Elite Variants und Wave Boss Balance (vormals hier als nächster Schnitt gelistet) sind inzwischen umgesetzt; alle drei zugehörigen Slice-Dateien sind erledigt. Der komplette Kernloop von MainMenu bis Bosses ist jetzt ein durchgängig spielbarer Vertical Slice. Relics bleibt spec-seitig bewusst zurückgestellt (kein Defekt).

Konkrete Reihenfolge für den nächsten Arbeitsblock, nach Aufwand/Nutzen sortiert:

1. Weitere Factory Production Chains autorieren und im Playtest balancieren — aktuell existiert nur eine Beispiel-Chain (Enemy-Kill-Trickle, zwei Tower-Pulse-Knoten für Ballista/Support, Wave-Clear-Bonus). Echtes Balancing braucht Playtest; reine Content-Erweiterung ist ohne neue Trigger-Typen oder Tower-Rollen aber kaum noch sinnvoll möglich, da alle drei zulässigen Trigger-Typen (Kill/TowerPulse/WaveEnd) und beide Tower-Typen mit Pulse-Knoten bereits von der einen Kette abgedeckt sind — eine zweite, rein additive Kette würde ungeprüft auf die bestehende aufstacken (Chains laufen parallel, nicht alternativ), was gegen Factory.md's eigene Snowballing-Warnung liefe.
2. (Begonnen: `ProceduralUIBuilder` liest `UIThemeData`; Skill-Graph, Factory-Kettenanzeige, Records und seit 2026-10-07 das Route-Panel nutzen ihn - elf Presenter bauen noch über `ProceduralUIFactory`, als nächstes das Loadout-Panel; ein Presenter pro Sitzung, siehe Open Topics 3.1.) UIThemeData.PrimaryTextColor/SecondaryTextColor/AccentColor/PanelFrameSprite tatsächlich an die Presenter anbinden statt der aktuell pro Presenter hartcodierten Farben (Font läuft bereits über UIFontResolver, siehe UI-Abschnitt oben), idealerweise zusammen mit einem gemeinsamen procedural-UI-Builder, der die inzwischen leicht auseinandergedrifteten CreateText/CreateImage-Duplikate ersetzt.
3. God-Object-Splits von WaveManager, WaveStatePresenter und StageArenaArtPresenter — größtenteils bewusst zurückgestellt, da diese Refactors Unity-Editor-Zugriff zum Kompilieren und Verifizieren brauchen (siehe Code-Kommentare in den jeweiligen Klassen). Update 2026-09-09: die rein lesende, seiteneffektfreie Label-/Elite-Flag-Auflösung von WaveManager (Wave-/Boss-/Stage-Beschriftungen) wurde ohne Editor gefahrlos nach `WaveEncounterLabelResolver` extrahiert (1035 → 905 Zeilen), da diese Methoden nur Felder lesen statt State zu mutieren und pro Aufrufstelle 1:1 durch explizite Parameter ersetzbar waren. Die verbleibenden, zustandsverändernden Anteile von WaveManager sowie WaveStatePresenter und StageArenaArtPresenter bleiben zurückgestellt. Update 2026-10-01: die vier vereinbarten Schnitte sind erledigt - `GameplayTimeScaleGuard` (Pause-Invariante), `StageTransitionCoordinator` (Stage-Wechsel aus WaveManager), `SpawnGateVisuals` (aus StageArenaArtPresenter) und `HudHealthBars` (aus WaveStatePresenter).
4. Vollständige UI-Lokalisierungsabdeckung über die bereits vorhandene LocalizationService-Basis hinaus. Update 2026-09-17: CardData (alle 30 Card-Assets übersetzt, deckt CardReward- und Shop-Angebote ab), ShopPresentationData/ShopService/ShopPresenter, die vier MainMenuRoutePanelData-Assets (hatten `_xDe`-Felder im Code, aber nie befüllte YAML-Werte - fielen in der Praxis immer auf Englisch zurück) sowie in einem zweiten Durchgang WaveStatePresenter/WaveManager.FailureCauseLabel/TowerService (die komplette HUD-Statuszeile, alle Banner, Pause- und Failed-Overlays, die Tower-Placement/-Upgrade-Zeile) ergänzt - damit ist die während des Spielens am häufigsten sichtbare Oberfläche vollständig abgedeckt. Weiterhin offen: CardRewardPresenter (eigene Strings), RelicRevealPresenter, MetaProgressionRouteTargetPresenter, SettingsRouteTargetPresenter, MainMenuCanvasPresenter, sowie TowerData.DisplayName sowie TowerLevelData.Label (Eigennamen, bewusst nicht übersetzt, siehe UI-Abschnitt oben).

Update 2026-09-17: Ein vollständiger Abgleich jeder "Noch offen"-Aussage in diesem Dokument gegen den tatsächlichen Code-/Asset-Stand ergab sieben stale Einträge (Shop-Tower-Upgrades, Card-Stacking, UIThemeData.Font, Beam-Pierce, Tower-Typ-Anzahl, importierte Audiodateien, zentrales Musikzustands-System) — alle waren bereits umgesetzt, das Dokument aber nicht nachgeführt worden. Alle sieben wurden in den jeweiligen Abschnitten oben korrigiert und aus dieser Liste entfernt; die verbleibenden vier Punkte wurden dabei gegengeprüft und sind tatsächlich noch offen. Lehre für zukünftige Durchläufe: Einträge in diesem Dokument vor Wiederverwendung gegen den Code prüfen statt blind zu übernehmen.

Slice-Handoff-Historie:

- [Bosses Stage 1 Slice](Slices/12_BOSSES_STAGE1_SLICE.md) — erledigt.
- [Elite Variants Slice](Slices/13_ELITE_VARIANTS_SLICE.md) — erledigt.
- [Wave Boss Balance Slice](Slices/14_WAVE_BOSS_BALANCE_SLICE.md) — erledigt.
