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

---

## Weapon set 2026-09-22

Playtest request 2026-09-22: *"create more different weapon designs"*. Six weapons were added
to the three archetypes above, all built from existing rules. Each one differs in **shape**
(where and how its damage lands), not only in its numbers. Nine distinct weapons also make
WeaponLoadout.md's Arsenal bonus reachable for the first time.

| Weapon | Id | Shape | Base (damage · interval · magazine / reload) | Innate | Gate |
| --- | --- | --- | --- | --- | --- |
| Scorcher | `weapon_scorcher` | 2-jet flame cone, 2.7 u reach, pierce 1 | 0.5 · 0.09 s · 36 / 1.6 s | every hit Burns | none |
| Cryo Lance | `weapon_cryo` | slow piercing bolt | 2.8 · 0.48 s · 10 / 1.4 s | every hit Freezes (slow) | none |
| Railgun | `weapon_railgun` | arena-crossing slug, pierce 4 | 9 · 1.05 s · 3 / 1.9 s | +15% crit chance | 45 Alloy licence |
| Nova Pulse | `weapon_nova` | 8-shard ring, 3.5 u reach | 1.3 · 0.75 s · 6 / 1.5 s | full ring | 35 Alloy licence |
| Scatter Launcher | `weapon_launcher` | shell, bursts for 75% splash in 1.1 u | 3.4 · 0.85 s · 4 / 1.7 s | explode on impact (new rule `ExplodeOnImpact`) | 40 Alloy licence |
| Twin SMG | `weapon_smg` | two loose barrels | 0.65 · 0.11 s · 48 / 2.0 s | none | none |

Sustained single-target DPS including reload, for comparison: Rapid ~9.5, Spread ~7.1, Beam
~6.4, Scorcher ~7.4 + Burn, SMG ~8.6, Nova ~8.8 if every shard hits (realistically a quarter
of that), Railgun ~6.1 across a whole line, Cryo ~4.5 + slow, Launcher ~2.7 + splash. These
are first-pass numbers, not playtested.

Every weapon has its own five-tier Set Bonus ladder, authored on its `WeaponData`. Tier 6 is
the archetype's defining rule: a Scorcher ring of fire, a Cryo or Railgun guaranteed crit
cycle, Nova shards that burst when they miss, a Launcher that fires three shells, and an SMG
with +10% crit.

### Data added

- `WeaponData._innateEffects`: `(target, value)` pairs, the same type as Set Bonus effects,
  applied to every copy from the first. This is how Nova's ring and the Launcher's burst are
  expressed without new combat code.
- `WeaponData._shortLabel`: the 1-2 character HUD tile label. Nine weapons made first letters
  ambiguous (Spread / Scorcher / Scatter Launcher).
- `WeaponData._displayNameDe` / `_summaryDe`.
- `WeaponModifierTarget.ExplodeOnImpact` = 16 (appended): every hit bursts, reusing
  `CombatProjectile.Explode`. The struck target is not hit twice.
- Per weapon: pattern, damage profile, projectile (tinted cells of `projectile_sheet_core.png`),
  presentation (its own synth fire frequency), card, shop offer, and entries in
  `WeaponCatalog_Default`, `CardPool_Default` and `ShopOfferTable_Stage1`.

### Not done

- **Art.** Projectiles reuse the three existing sheet rows, tinted; the weapons have no icons
  (the HUD shows the short label). Prompts for a dedicated sheet belong in
  GRAPHICS_ASSET_PROMPTS.md.
- **Balance.** Verified only that all six fire, drain and reload in a real run (autopilot
  `arsenal3`, Stage 3), and that a Scorcher offered by the shop went pending with a full
  loadout. Not verified: splash damage totals, Burn/Freeze uptime, and any tier-5/6 Set Bonus
  in play.
