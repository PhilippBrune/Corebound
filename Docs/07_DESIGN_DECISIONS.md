# Design Decisions

Version: 1.0

Status: Active

Source: Konsolidierung aller Open Questions aus Docs/Specs am 2026-07-28

---

# Purpose

Dieses Dokument überführt die offenen Fragen aus den bestehenden Feature Specs in konkrete Basisentscheidungen.

Die Entscheidungen gelten als Standard für den ersten spielbaren Vertical Slice und für die anschließende Implementierung in Unity 6.

Wenn spätere Playtests eine Entscheidung widerlegen, muss zuerst dieses Dokument aktualisiert und danach die betroffene Feature Spec nachgezogen werden.

---

# Global Baseline

- Der erste spielbare Scope priorisiert einen klaren Vertical Slice statt maximale Content-Breite.
- Controller First bleibt verbindlich für Combat, Menüs, Tower-Interaktion und Reward-Flows.
- Systeme bleiben datengetrieben über ScriptableObjects und kommunizieren eventbasiert.
- Der erste Scope nutzt eine primäre Run Resource und eine getrennte Meta Currency.
- Der erste Scope nutzt einen fortsetzbaren Run, ein lokales Profil und keine Cloud-Save-Abhängigkeit.
- Das Spiel bleibt technisch ein 2D-Unity-Projekt mit orthographischer Top-Down-Ansicht; 2.5D ist nur als visuelle Präsentationsschicht über Sprites, Licht, Schatten, Layering und Faux-Depth erlaubt.

---

# Resolved Decisions By Spec

## Combat

- Standard-Zielhilfe für Controller wird als mittlere Soft-Aim-Unterstützung umgesetzt. Sie arbeitet mit Zielkonus, leichter Magnetisierung und begrenzter Projektilkorrektur. Mouse & Keyboard erhalten keine Zielhilfe.
- Der Player erhält nach bestätigtem Treffer eine kurze Unverwundbarkeitsphase von 0,35 Sekunden, damit Burst-Schaden lesbar und fair bleibt.
- Boss-Begegnungen dürfen pro Phase eigene CombatBalanceData-Overrides verwenden. Erlaubt sind nur Werteanpassungen für Telegraph-Dauer, Cooldown-Rhythmus, Schadensfenster und Spawn-Druck. Es entstehen keine separaten Combat-Regeln.

## Player

- Der Player besitzt eine kleine automatische Resource-Sammelreichweite. Zusätzlich werden verbleibende Resource-Pickups nach Wave-Ende automatisch zum Player eingezogen.
- Interaktionspriorität ist fest definiert: blockierende UI-Bestätigung vor Tower-Interaktion vor Reward- oder Shop-Interaktion vor nicht automatisch eingesammelten Pickups.
- Wenn Player oder Core auf null fallen, endet der aktuelle Run sofort. Ein Revive- oder Downed-State ist nicht Teil des ersten Scopes.

## Weapons

- Standardfeuer ist Hold-to-Fire. Rhythmisches Tapping darf einzelne Weapons stärker machen, ist aber keine Pflichtbedienung.
- Die erste Content-Welle enthält drei Weapon-Archetypen: schnelle Projectile Weapon, Spread Projectile Weapon und kurze Piercing- oder Beam Weapon.
- Aim-Assist-Stärke wird global für Controller konfiguriert, aber pro WeaponData mit Low-, Medium- oder High-Assist-Profil gewichtet.

## Enemies

- Die erste Stage nutzt drei Basisrollen: Chaser, Ranged Pressure und Bruiser. Damit werden Bewegung, Priorisierung und Core-Druck früh gelehrt.
- Elite-Logik besteht im ersten Scope aus einem Stat-Paket plus maximal einer wiederverwendbaren Pattern-Variation.
- Standard-Enemies bewegen sich grundsätzlich in Richtung Core, wechseln aber auf den Player, wenn Archetyp, Nähe oder erhaltener Schaden dies auslösen.

## Waves

- Standard-Intermission beträgt 20 Sekunden und kann per Bestätigung vorzeitig beendet werden.
- Jede Stage verwendet im ersten Scope eine feste Struktur aus sechs Combat Waves und einer Boss-Begegnung. Variable Branches sind kein Startbestandteil.
- Die erste Elite Wave erscheint in Stage 1 nach der Lernphase, konkret ab Wave 5.

## Bosses

- Boss-Phasen werden standardmäßig über Health-Schwellen ausgelöst. Zusätzlich ist ein wiederverwendbares Enrage-Modul als optionaler Zeittrigger erlaubt.
- Hilfs-Enemies sind pro Boss-Phase nur in klar begrenzten Spawn-Fenstern erlaubt und dürfen nie die dominante Gefahr vor dem Boss selbst werden.
- Im ersten Scope besitzt jede Stage genau einen Boss. Alternative Boss-Pools sind eine spätere datengetriebene Erweiterung.

## Towers

- Towers dürfen auch während aktiver Waves gebaut oder verbessert werden, sofern der Player sich an einem gültigen Tower Slot befindet und keine blockierende UI offen ist.
- Jeder Tower besitzt im ersten Scope drei Stufen: Basiszustand plus zwei Upgrades.
- Ein kontrollierter Rückbau oder Refund für Towers ist nicht Teil des ersten spielbaren Umfangs.

## Resources

- Der erste Scope nutzt genau eine primäre Run Resource und eine davon getrennte Meta Currency.
- Die primäre Run Resource besitzt im ersten Scope kein hartes Wallet-Cap.
- Resource-Sammlung kombiniert kurze Auto-Pickup-Reichweite im Combat mit vollständigem Wave-Ende-Vacuuming, damit Tempo und Lesbarkeit erhalten bleiben.

## Economy

- Der Shop arbeitet im ersten Scope mit festen Preis-Kategorien pro Angebotsart und optionalen Stage-Multiplikatoren statt vollständig dynamischer Preisbildung.
- Ein Reroll-System ist nicht Teil des ersten spielbaren Umfangs.
- Refunds für Käufe oder Upgrades existieren im ersten Scope nicht.

## Cards

- Ein Standard-Card-Angebot besteht aus drei Optionen.
- Cards erhalten im ersten Scope kein allgemeines Reroll-System.
- Die erste Card-Tag-Welle nutzt eine kleine kontrollierte Taxonomie: Weapon, Projectile, Tower, Resource, Status Effect, Burn, Freeze und Critical.

## Factory

- Der erste Factory-Umfang nutzt drei Production Triggers: OnEnemyKilled, zeitbasierter Tower-Pulse und OnWaveCompleted.
- Resource Conversion ist nicht Teil des ersten spielbaren Umfangs. Der Startfokus liegt auf Resource-Erzeugung und Multiplikation.
- Snowballing wird durch Soft Caps begrenzt: doppelte Resource-Produzenten verlieren nach den ersten starken Kopien an Effizienz, und Wave-basierte Produktionsboni besitzen harte Datenobergrenzen.

## Meta Progression

- Zulässig sind Content Unlocks, neue Startoptionen und kleine, klar gedeckelte Komfortverbesserungen. Unbegrenzte permanente DPS-, Health- oder Fire-Rate-Skalierung ist ausgeschlossen.
- Meta Progression nutzt eine Hybrid-Struktur aus kurzer linearer Basis und verzweigten Spezialisierungspfaden.
- Zu Run-Beginn freigeschaltet sind zwei Weapons, zwei Towers, eine neutrale Basis-Card-Pool-Auswahl, Stage 1 und das Tutorial. Frühe Ziele schalten eine dritte Weapon, einen dritten Tower, spezialisierte Cards und Stage-2-Inhalte frei.

## Input

- Der erste Scope verwendet vier Action Maps: Gameplay, Build, UI und System.
- Tower-Platzierung erfolgt controller-first über Tower Slot Cycling statt freien Cursor. Ein Radial-Menü ist nur eine mögliche spätere Erweiterung.
- Rebinds sind für alle Kernaktionen des ersten spielbaren Umfangs vorgesehen, inklusive Gameplay, Navigation, Confirm und Cancel.

## Main Menu

- Das Game startet mit einem kurzen Press-Any-Button- oder Press-Start-Einstieg und wechselt danach in das Main Menu.
- Der erste Scope nutzt ein einzelnes lokales Profil und genau einen fortsetzbaren Run statt mehrerer Slots.
- Der erste New-Run-Flow führt einmalig durch eine geführte Tutorial-Stage. Danach bleibt das Tutorial optional über das Main Menu erreichbar.

## Settings

- Pflicht-Accessibility-Optionen im ersten Milestone sind Screen Shake Intensity, Hit- und Effektintensität, HUD- oder Textgröße, Controller Aim Assist Strength und getrennte Lautstärke für Music, SFX und UI.
- Grafikoptionen bleiben im ersten Scope bewusst schlank: Presets, Fullscreen-Modus, Auflösung und VSync.
- Zielhilfe wird als globale Controller-Einstellung exposed. Weapon-spezifische Feinabstimmung bleibt in WeaponData und nicht im Settings-Menü.

## SaveSystem

- Der erste Scope unterstützt genau einen fortsetzbaren Run.
- Cloud Save ist nicht Teil des ersten Milestones.
- RunSaveData speichert mindestens aktuelle Stage, aktuelle Wave, Player-Zustand, Core-Zustand, aktive Build-Bestandteile, Wallet, Tower-Zustand, relevante RNG-Seeds und pending Reward-Kontexte. Gespeichert wird an Intermissions, Stage-Übergängen, Safe Quit und Rückkehr ins Main Menu.

## Tutorial

- Das Tutorial wird als geführte erste Stage umgesetzt und nutzt dieselben Systeme wie der normale Run.
- Zwingende Tutorial-Schritte sind Movement, Weapon-Nutzung, Resource-Sammeln, Tower-Bau, Card-Auswahl und Core-Verteidigung. Spätere Systeme wie Meta Progression oder Detail-Settings bleiben optional.
- Das Tutorial vergibt im ersten Scope keine zusätzliche Meta Currency. Der Abschluss setzt nur einen persistenten Completion-Status für spätere Skip- oder Replay-Flows.

## UI

- Die UI nutzt vier Hauptlayer: World Indicators, Gameplay HUD, Menus and Panels, Overlay and Transitions.
- Dauerhaft sichtbar sind Player-Health, Core-Health, primäre Run Resource und Wave- oder Stage-Status. Kontextuell sichtbar sind Interaktionshinweise, Buffs, Debuffs und Warnungen.
- Die erste UI-Iteration muss lokalisierungsfähig gebaut werden. Deutsche und englische Texte sind Baseline. Text darf nicht in Sprites eingebrannt werden.

## Graphics

- Gameplay bleibt auf einer 2D-XY-Ebene mit orthographischer Kamera; 2.5D wird ausschließlich über gezeichnete Volumenwirkung, Layer-Parallaxen im Hintergrund, Normal-Maps, Light-Response und leichte Höhenstaffelung simuliert.
- Der erste Vertical Slice benötigt vollständige Grafik-Sets für Main Menu, HUD, Tutorial-Hinweise, Player, Core, drei Enemy-Rollen, einen Boss, drei Weapon-Archetypen, Projectile- und Treffer-VFX, Tower-Slots, Tower-Basen, eine primäre Run Resource sowie Stage-1-Arena-Module.
- Alle gameplay-kritischen Objekte erhalten klare Top-Down-Silhouetten und dürfen nicht von dekorativen Effekten verdeckt werden.

## Audio

- Musik arbeitet im ersten Scope mit drei Zuständen: normale Wave, hohe Eskalation oder Elite und Boss.
- Ducking priorisiert zuerst Music und Ambient Layers. Kritische Combat-Warnungen wie Core-Schaden oder Boss-Telegraphs dürfen nicht von UI-Audio verdrängt werden.
- Häufige Kernaktionen wie Weapon-Fire, Treffer und Resource-Pickup erhalten mindestens drei Audio-Variationen. Seltene Ereignisse wie Boss-Intro oder Legendary-Reveal benötigen mindestens eine klare eigene Signatur.

---

# Implementation Consequences

- Es wird kein zweites Ressourcenmodell, kein Reroll-System und kein Refund-System im ersten Vertical Slice gebaut.
- Controller-Flows werden nicht nachträglich ergänzt, sondern von Beginn an als Standard umgesetzt.
- Speichersystem, Input und UI müssen früh strukturiert werden, auch wenn ihre vollständigen Inhalte später ausgebaut werden.
- Alle Content-Systeme müssen mit kleiner initialer Taxonomie und begrenzter Startbreite arbeiten, damit Balancing und Tests früh möglich bleiben.

---

# Remaining Blockers

Aktuell keine offenen Cross-Spec-Blocker.

Weitere offene Fragen dürfen erst wieder eingeführt werden, wenn sie an eine konkrete neue Spec oder eine Playtest-Erkenntnis gebunden sind.