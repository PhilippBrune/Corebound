# Towers Specification

## Purpose

Definiert Towers als platzierbare Unterstützungseinheiten innerhalb des Runs.

Towers erweitern Combat, Resource-Erzeugung und Build-Synergy, ohne den Player als Hauptakteur zu ersetzen.

## Design Goals

- Meaningful Decisions bei Platzierung und Upgrade-Reihenfolge
- Tower als Werkzeuge statt autonomes Hauptsystem
- Gute Synergy mit Cards, Resources, Factory und Weapons
- Klare Controller-Bedienbarkeit beim Bauen und Verbessern
- Datengetriebene Erweiterbarkeit für neue Tower-Rollen

## Gameplay

Der Player baut Towers an verfügbaren Tower Slots gegen Resource-Kosten.

Towers können Schaden verursachen, Gegner kontrollieren, den Player unterstützen oder Resources erzeugen.

Bestehende Towers können über definierte Upgrade-Stufen verbessert werden.

Die Wirkung eines Towers hängt stark von Position, Timing, Wave-Komposition und dem restlichen Build ab.

### Tower-Typ-Auswahl (implementiert 2026-09-09)

Mit einem dritten Tower-Typ (Mortar Tower, zusätzlich zu Ballista und Support) reichte die bisherige Build-Logik nicht mehr aus: `TowerService.ResolveBuildTowerData()` wählte immer den ersten freigeschalteten Eintrag aus `TowerCatalogData` — der Player hatte keine Möglichkeit, zwischen mehreren verfügbaren Tower-Typen zu wählen. Das widersprach Vision's Pillar „Meaningful Decisions" (u. a. explizit „Neuer Turm?" als Beispiel-Entscheidung) und „Build Diversity".

Gelöst über eine zweite, vom bestehenden Tower Slot Cycling (Positions-Auswahl über `PreviousSlot`/`NextSlot`, Schultertasten/Q-E) unabhängige Cycling-Geste für den Tower-**Typ**: neue `PreviousTowerType`/`NextTowerType`-Actions in der Build-Map, gebunden auf Gamepad-Trigger (links/rechts) und Z/X auf der Tastatur — bewusst ein zweites Tastenpaar neben Q/E, nicht dieselben Tasten mit Modifier, um Verwechslungen zu vermeiden. Der Player steht an einem freien Slot, cycled per Input durch alle aktuell freigeschalteten Einträge aus `TowerCatalogData` (`TowerService.CycleTowerType`), `ResolveBuildTowerData()` liefert den Eintrag am aktuellen Index statt immer den ersten. Der Index setzt sich beim Wechsel des Slots automatisch auf den ersten freigeschalteten Eintrag zurück, damit Bauen mit nur einem freigeschalteten Typ weiterhin ohne jede zusätzliche Eingabe funktioniert — mit nur Ballista freigeschaltet ändert sich das Verhalten gegenüber vorher nicht. `TowerCatalog_Default` enthält jetzt alle drei Türme (Ballista, Support, Mortar).

## Rules

- Towers dürfen nur auf gültigen Tower Slots platziert werden.
- Pro Tower Slot ist nur ein aktiver Tower erlaubt.
- Jeder Tower besitzt klar definierte Rollen und Upgrade-Pfade.
- Towers unterstützen den Player, dürfen aber keinen vollständig passiven Sieg ermöglichen.
- Tower-Interaktionen müssen per Controller mit geringer Auswahlkomplexität funktionieren.
- Tower-Effekte nutzen bestehende Combat- und Resource-Regeln.
- Neue Towers erweitern vorhandene Kategorien, statt neue Subsysteme einzuführen.
- Sobald mehr als ein Tower-Typ gleichzeitig freigeschaltet ist, muss der Player den zu bauenden Typ aktiv wählen können — ein automatisches „immer der erste freigeschaltete Katalog-Eintrag" ist nur für genau einen verfügbaren Typ zulässig.
- Die Tower-Typ-Auswahl bleibt controller-first und cursorlos. Erlaubt sind eine Cycling-Geste **oder** ein Build-Browser, solange er am Slot öffnet, per D-Pad navigiert wird und mit Cancel schließt (Owner-Entscheidung 2026-09-22). Umgesetzt ist der Browser (`TowerBuildBrowserPresenter`) mit Roster, Kosten und Upgrade-Karte; die Actions heißen `PreviousTowerType`/`NextTowerType` in `InputSystem_Actions.inputactions` (Build-Map).
- Die Kartengrafik (`tower_info_card_frame.png`) enthält **keinen Text**; "Strong against / Weak against" bzw. "Stark gegen / Schwach gegen" zeichnet der Presenter neben die beiden Kästchen der großen Baukarte (seit 2026-10-07; vorher war der englische Text ins Bild gebacken). Auf der Upgrade-Karte sind die Kästchen Dekor und bleiben unbeschriftet, weil ihre Zeilen Zahlen sind, keine Vor- und Nachteile.

## Data Model

Tower-Daten werden in ScriptableObjects definiert.

- TowerData: Basisdefinition, Rolle, Build-Kosten, Reichweite, Feuerintervall, Sprite-Pfad und Präsentationsdaten
- TowerLevelData: Werte und Freischaltungen pro Upgrade-Stufe
- TowerCatalogData: die baubare Liste, gefiltert über Unlocks

Zielwahl und Effekte liegen in `TowerRuntimeController` und dessen `DamageProfileData`/`ProjectileData`;
Slot-Positionen stehen pro Stage in `StageData`. Eigene Assets dafür gibt es nicht.

## Technical Design

Ein TowerController verbindet Collider, Animation, Reichweiten-Trigger und VFX.

Zielwahl, Attack-Logik, Buff-Verteilung oder Resource-Generierung liegen in separaten Runtime-Klassen.

Tower-Systeme senden Events wie OnTowerBuilt, OnTowerUpgraded, OnTowerActivated und OnTowerDestroyed.

Projectile- und Effektinstanzen von Towers nutzen dieselben Pooling- und Combat-Regeln wie Weapons.

Tower-Aktivierungspulse und Emittor-Ringe greifen im ersten Slice auf die gemeinsame Effektfamilie aus der [VFX Specification](./VFX.md) zurück.

Der erste Vertical Slice nutzt einen TowerService mit save-gebundener Tower-Liste und einem automatisch aufgebauten Runtime-Tower in der SampleScene, damit Tower-bezogene Card- und Meta-Hooks bereits auf ein konkretes Zielsystem wirken können.

Mit dem ersten Wave- und Enemy-Loop greifen Tower-Zielwahl und Tower-Schaden nun gegen wiederkehrende Enemy-Gruppen statt nur gegen einen isolierten Testzustand.

Tower-Typ-Auswahl: `TowerService` hält beim Betreten eines freien Slots einen aktuell vorgeschlagenen Index in die Liste freigeschalteter `TowerCatalogData.Towers`-Einträge. Eine neue semantische Input-Action (`CycleBuildTowerType`, siehe Input.md) verschiebt diesen Index; `ResolveBuildTowerData()` liefert statt „immer erster freigeschalteter Eintrag" den Eintrag am aktuellen Index. Der Index wird pro Slot-Betreten auf den ersten freigeschalteten Eintrag zurückgesetzt, damit Bauen ohne jede Interaktion weiterhin funktioniert (kein Pflichtschritt für Spieler mit nur einem freigeschalteten Typ).

## Dependencies

- Player
- Resources
- Factory
- Combat
- Waves
- Cards
- UI
- Audio

## Linked Slice Files

- [Wave Boss Balance Slice](../Slices/14_WAVE_BOSS_BALANCE_SLICE.md)
- [Bosses Stage 1 Slice](../Slices/12_BOSSES_STAGE1_SLICE.md)
- [Elite Variants Slice](../Slices/13_ELITE_VARIANTS_SLICE.md)

## UI

Tower-UI zeigt mindestens:

- verfügbare Tower Slots
- Build-Kosten
- Reichweitenvorschau
- Upgrade-Optionen
- Status eines ausgewählten Towers
- den aktuell per Tower-Typ-Cycling vorgeschlagenen Tower-Typ (Name, Sprite, Kosten), sobald mehr als ein Typ freigeschaltet ist

Slot-Auswahl, Typ-Auswahl und Bestätigung müssen controller-freundlich ohne Cursor-Zwang funktionieren.

## Audio

Build, Upgrade, Aktivierung und Zerstörung eines Towers besitzen klare Audio-Cues.

Resource-produzierende und offensiv ausgerichtete Towers sollten akustisch unterscheidbar bleiben.

## Visual

Tower-Silhouetten müssen ihre Funktion bereits aus Distanz andeuten.

Reichweiten- oder Support-Effekte werden nur temporär oder kontextuell eingeblendet, um die Arena nicht zu überladen.

## Balancing

Tower-Kosten und Output müssen eine echte Opportunity Cost gegenüber Cards und anderen Investments erzeugen.

Offensive Towers dürfen stark sein, aber ihre Effizienz soll von Platzierung und Synergy abhängen.

Resource-Towers benötigen klare Obergrenzen, damit Factory-Schleifen nicht den gesamten Run entwerten.

## Open Questions

Keine offenen Fragen. Die frühere Frage ("reicht die Textzeile, oder braucht die Typ-Auswahl ein eigenes Overlay?") ist seit 2026-09-22 beantwortet: der Build-Browser ist dieses Overlay — Roster mit Sprite, Kosten, Stärken/Schwächen und Upgrade-Karte.

Festgelegte Entscheidungen:

- Towers werden **nur während einer Intermission** gebaut und verbessert (Owner-Entscheidung 2026-09-22; vorher stand hier "auch während aktiver Waves", was der Code nie so umgesetzt hat). Begründung wie bei der Core-Reparatur: Bauen mitten in der Welle macht aus jeder Bedrohung ein Anhalten-und-Bezahlen statt einer Positionierungsentscheidung.
- Jeder Tower besitzt **sieben Stufen**: Basiszustand plus sechs Upgrades (Owner 2026-09-22; vorher
  drei). Die Kosten steigen steil - Stufe 4 kostet das Vierfache der Baukosten, Stufe 7 das
  Zweiundzwanzigfache -, damit späte Stufen eine echte Investitionsentscheidung gegen Waffen,
  Karten und Core-Reparatur sind.
- Ein kontrollierter Rückbau oder Refund für Towers ist nicht Teil des ersten spielbaren Umfangs.
- Sobald mehr als ein Tower-Typ freigeschaltet ist, braucht der Build-Flow eine explizite Typ-Auswahl (siehe „Tower-Typ-Auswahl" oben) — das ist jetzt mit dem dritten Tower-Typ (Mortar) real geworden und kein rein theoretischer Fall mehr.

**Update 2026-09-10 — vierter Tower-Typ „Sentinel" (Piercing/Crowd-Control):** `TowerData`/`TowerRuntimeController` sind vollständig generisch datengetrieben (Cost, Range, Fire Interval, DamageProfile, Projectile, Upgrade-Levels) — ein neuer Tower-Typ braucht keinen neuen Code, exakt wie schon beim Mortar-Tower. `ProjectileData.PierceCount` existierte bereits im Code (`CombatProjectile` behandelt es generisch: ein Projektil mit Pierce > 0 wird bei einem Treffer nicht sofort freigegeben, sondern durchschlägt weitere Ziele), wurde aber bisher von keinem einzigen Tower/Weapon-Projectile-Asset genutzt. Sentinel Tower füllt diese Lücke: schnelle Feuerrate (0.6s), niedriger Schaden pro Treffer (2, wie `DamageProfile_PlayerPrimary`), aber `_pierceCount: 2` auf `Projectile_TowerSentinel` — durchschlägt bis zu 3 Gegner in einer Linie. Das ist eine strukturell andere Rolle als Ballista (ausbalanciertes Einzelziel), Mortar (langsam/schwer) oder Support (Meta-Progression-Unlock) und damit eine echte Erweiterung von Tower Philosophy's „verändern den Kampf", nicht nur eine weitere Zahlenvariante. In `TowerCatalog_Default.asset` registriert, ohne `_requiredUnlockId` (sofort verfügbar wie Ballista/Mortar) und automatisch über die bestehende Tower-Typ-Auswahl (Cycling) erreichbar, ohne weitere Verdrahtung.

**Nachtrag 2026-09-22:** Der Sentinel hat inzwischen echtes Artwork (`Assets/Art/Towers/tower_sentinel_topdown.png`). Der Pfad stand zunächst fest im Code (`TowerRuntimeController.SentinelSpritePath`) und liegt jetzt wie bei jedem anderen Tower in den Daten (`TowerData._spritePath` mit optionalem Pixel-Rect), damit ein umbenanntes File auffällt und `Corebound > Validate Art Pipeline` es prüfen kann.

## Acceptance Criteria

- Towers sind platzierbare, datengetriebene Unterstützungswerkzeuge auf Tower Slots.
- Tower-Aufbau und Upgrades funktionieren vollständig per Controller.
- Towers können Combat, Support und Resource-Erzeugung über gemeinsame Systeme abbilden.
- Tower-Output bleibt ergänzend zum aktiven Player-Gameplay.
- Neue Towers und Upgrades lassen sich ohne Architekturänderung ergänzen.
- Card- und Meta-Hooks können im ersten Slice bereits auf ein konkretes Tower-Zielsystem wirken.
- Sobald mehr als ein Tower-Typ freigeschaltet ist, kann der Player den zu bauenden Typ per Controller wählen, ohne dass Bauen mit nur einem freigeschalteten Typ zusätzliche Eingaben braucht.

---

## Selection, upgrade cost and permanent upgrades (2026-09-21)

Three changes from the 2026-09-21 playtest and owner decisions:

**Slot selection follows the player.** The highlighted build slot is the one nearest the
player, so position is the control rather than a cycle through an index. Previous/Next still
override it manually, and that override lasts only for the intermission it was made in.

**Upgrade costs roughly doubled.** They were 9 to 22 Scrap against roughly 110 earned by wave 6,
so upgrading was not a decision. Costs now scale off each tower's own build cost: about 1.2x for
the first upgrade and 2.2x for the second.

**Permanent per-tower stat upgrades** are a separate, between-runs system funded by Alloy. See
[Upgrades.md](Upgrades.md). In-run upgrade levels and permanent upgrades compose in the order
base -> permanent upgrades -> in-run level -> card modifiers.

---

## Darstellungsgröße (2026-09-22)

Jeder Tower wird über `DebugSpriteFactory.FitTransformToSprite` auf dieselbe Weltgröße gebracht.
Damit das auch sichtbar stimmt, trägt jeder Tower in seinen Daten einen zugeschnittenen
`_spriteRect`: die Quelltexturen haben unterschiedlich viel transparenten Rand, und ohne Zuschnitt
skaliert dieser Rand mit - der Grund, warum der Mortar deutlich größer wirkte als Ballista und
Support. Ein neuer Tower braucht deshalb ein randloses Sprite oder seinen eigenen Rect.
