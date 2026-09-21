# Weapons Specification

## Purpose

Definiert Weapons als aktive Angriffsquelle des Players.

Weapons bilden zusammen mit Combat und Cards den wichtigsten unmittelbaren Ausdruck eines Builds.

## Design Goals

- Spürbar unterschiedliche Weapon-Archetypen bei gemeinsamen Grundregeln
- Action-First-Gefühl durch direktes, reaktives Angreifen
- Gute Controller-Bedienbarkeit für Aim, Fire und Feedback
- Klare Synergy mit Card-, Tower- und Resource-Systemen
- Erweiterbarkeit über Daten statt harte Sonderimplementierungen

## Gameplay

Der Player startet einen Run mit einer aktiven Weapon-Konfiguration.

Während Combat erzeugt die Weapon Angriffe, Projectiles oder Nahbereichseffekte basierend auf Input, Ausrichtung und Weapon-Daten.

Cards und andere Build-Systeme verändern Weapon-Werte, Feuerverhalten, Projectile-Eigenschaften, Status Effects oder Zusatztrigger.

Weapon-Identität entsteht über Reichweite, Rhythmus, Trefferbild und Synergy-Möglichkeiten statt über völlig eigene Regelwerke.

## Rules

- Jede Weapon muss vollständig per Controller nutzbar sein.
- Weapons verwenden die gemeinsame Combat-Pipeline für Damage, Status Effect und Trefferauflösung.
- Weapon-Unterschiede entstehen über Daten für Pattern, Timing, Projectile und Modifikatoren.
- Der Kernspezifikationsumfang geht von einer aktiven Weapon zur gleichen Zeit aus.
- Zusätzliche Weapon-Slots oder Swap-Systeme benötigen eine separate Erweiterung, dürfen aber das Datenmodell nicht brechen.
- Keine Weapon darf den Player zu Mauspräzision zwingen, die auf Controller nicht erreichbar ist.
- Weapon-Verhalten darf neue Power-Spikes erzeugen, aber keine dominante Einheitsstrategie erzwingen.

## Data Model

Weapon-Daten werden in ScriptableObjects definiert.

- WeaponData: Basiswerte, Beschreibung, Präsentation und Verweise auf Pattern-Daten
- WeaponAttackPatternData: Schussfolge, Salvenstruktur, Streuung, Reichweite und Trigger-Bedingungen
- ProjectileData: Schaden, Geschwindigkeit, Größe, Verhalten, Lebensdauer und VFX
- WeaponModifierData: klar definierte Hooks für Fire Rate, Damage, Crit, Multishot oder Status Effect
- WeaponPresentationData: Audio, Animation, Sprite, Muzzle- oder Impact-Referenzen

**Update 2026-09-20 — implementiert.** Alle fuenf Asset-Typen existieren jetzt.
`WeaponModifierData` (Target, Value, optionale `RequiredWeaponId`) deckt die hier genannten
Hooks Fire Rate, Damage, Crit, Multishot und Status Effect ab; `WeaponPresentationData` haelt
Icon, Accent-Farbe, Muzzle-/Impact-Farben und die Fire-/Critical-Audio-Cues. `CardData` und
`RelicData` tragen beide ein optionales `WeaponModifierData[]`, `CardService` aggregiert sie,
und `WeaponModifierState` loest sie zusammen mit den drei bisherigen Float-Multiplikatoren zu
den effektiven Weapon-Werten auf. Die alten `CardEffectTarget`-Multiplikatoren bleiben gueltig
und werden als Basiswerte eingerechnet, damit bestehende Cards unveraendert funktionieren.

Die urspruengliche Audit-Notiz vom 2026-09-16 lautete: `WeaponModifierData` und
`WeaponPresentationData` existieren als eigene Asset-Typen noch nicht; Modifikatoren laufen als
einzelne Float-Properties direkt auf `CardService`.

## Technical Design

Ein Weapon-Controller verarbeitet Fire-Anfragen und delegiert Berechnungen an testbare Runtime-Klassen.

Projectile-Erzeugung nutzt Pooling und liest ausschließlich Daten-Assets und aktuelle Modifier-Zustände.

Weapon-Systeme senden Events wie OnWeaponFired, OnProjectileSpawned, OnWeaponModified und OnWeaponStateChanged.

**Update 2026-09-20 — implementiert.** `WeaponEvents` sendet alle vier Signale. Die Namen
tragen kein `On`-Praefix (`WeaponFired`, `ProjectileSpawned`, `WeaponModified`,
`WeaponStateChanged`), weil `CombatEvents`, `FactoryEvents`, `WaveEvents` und `ShopEvents` im
Projekt alle so heissen — inhaltlich entsprechen sie eins zu eins der Liste oben.
`PlayerCombatController.WeaponFired` bleibt zusaetzlich als Instanz-Event bestehen, weil
`TutorialService` es bereits konsumiert und ein Tutorial nicht auf fremde Schuesse reagieren
darf. Konsumenten heute: `WeaponAudioCuePlayer` (Fire- und Critical-Cue pro Weapon) und
`WeaponHudPresenter` (Identitaet, Bereitschaft, aktive Modifikatoren).

Weapon-Logik bleibt unabhängig von UI und speichert keine Persistenzdaten direkt.

Der erste Vertical Slice nutzt WeaponData, WeaponAttackPatternData und WeaponCatalogData bereits direkt im PlayerCombatController. RunSaveData kann die aktuell ausgerüstete Weapon referenzieren, und MetaProgression-Weapon-Unlocks können diese Auswahl für neue oder ungefixte Runs auflösen.

## Dependencies

- Player
- Combat
- Cards
- Towers
- Resources
- UI
- Audio
- MetaProgression

## UI

Weapon-bezogene UI zeigt bei Bedarf:

- aktuelle Weapon-Identität
- relevante Bereitschafts- oder Cooldown-Zustände
- aktive Weapon-Modifikatoren
- Zielhilfe- oder Richtungsfeedback für Controller

## Audio

Jede Weapon besitzt eindeutige Feuer-, Treffer- und kritische Feedback-Cues.

Unterschiedliche Weapon-Archetypen müssen akustisch unterscheidbar bleiben, auch wenn mehrere Tower gleichzeitig feuern.

## Visual

Weapons benötigen klare Fire- und Impact-Signale mit hoher Lesbarkeit im Top-Down-Kampfbild.

Projectile-Form und -Farbe sollen Weapon-Charakter transportieren, dürfen Enemy-Gefahren aber nicht überdecken.

## Balancing

Weapons sollen unterschiedliche Build-Pfade eröffnen, ohne dass eine Grundwaffe fast immer besser ist als alle anderen.

Card-Synergies dürfen starke Power-Spikes auslösen, müssen aber Opportunity Costs oder klare Spezialisierungen mitbringen.

Frühe Weapons müssen bereits Spaß machen und dürfen nicht erst nach vielen Cards funktionieren.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Standardfeuer ist Hold-to-Fire; rhythmisches Tapping kann situativ nützlich sein, ist aber keine Pflicht.
- Die erste Content-Welle enthält drei Weapon-Archetypen: schnelle Projectile Weapon, Spread Projectile Weapon und kurze Piercing- oder Beam Weapon.
- Aim Assist wird global für Controller geregelt und pro WeaponData nur über Low-, Medium- oder High-Assist-Profile gewichtet.

## Acceptance Criteria

- Weapons nutzen eine gemeinsame Combat-Pipeline und ein gemeinsames Datenmodell.
- Unterschiedliche Weapon-Archetypen lassen sich primär über ScriptableObjects definieren.
- Controller-Bedienung für Weapon-Nutzung ist gleichwertig zur Tastatur-Maus-Steuerung.
- Card- und Tower-Effekte können Weapons ohne Sonderfälle modifizieren.
- Projectile- und Feedback-Handling bleibt performant und lesbar.
- Weapon-Unlocks aus Meta Progression besitzen im ersten Slice einen direkten Pfad in die laufende Combat-Auswahl.

---

## Superseded in part by WeaponLoadout.md (2026-09-21)

This spec was written around **one** active weapon, and says so in its Rules: *"Der
Kernspezifikationsumfang geht von einer aktiven Weapon zur gleichen Zeit aus."* That rule no
longer holds.

The owner decided on 2026-09-21 that the player carries **up to six weapons at once**, all
firing automatically, with escalating set bonuses for carrying duplicates. The requirements
live in [WeaponLoadout.md](WeaponLoadout.md).

**What this document still owns:** what a single weapon *is*. `WeaponData`,
`WeaponAttackPatternData`, `ProjectileData`, `WeaponModifierData` and `WeaponPresentationData`
are all unchanged, as is the shared combat pipeline, aim assist, and the modifier hook model.

**What it no longer owns:** how many weapons are equipped, how they are acquired mid-run, and
what happens when several of the same kind are carried.

**Concrete conflicts to resolve when WeaponLoadout is implemented:**

- Rules: "one active weapon at a time" is superseded. The follow-on sentence about additional
  slots needing "eine separate Erweiterung" is exactly what WeaponLoadout.md is.
- Technical Design: `PlayerCombatController._activeWeapon` becomes a loadout rather than a
  single field.
- `RunSaveData.EquippedWeaponId` becomes `EquippedWeaponIds`, which is a save format change
  and needs a migration that promotes the old single id into slot 0.
- The UI section's single-weapon HUD widget is replaced by a six-slot strip.
- Audio: the de-duplication rule in WeaponLoadout.md is what keeps this spec's "akustisch
  unterscheidbar" requirement true once six weapons fire at once.
