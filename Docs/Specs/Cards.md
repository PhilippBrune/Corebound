# Cards Specification

## Purpose

Definiert das Card-System als primären Run-basierten Modifikator für Builds.

Cards werden nicht aktiv ausgespielt. Sie verändern bestehende Systeme dauerhaft für den aktuellen Run.

## Design Goals

- Meaningful Decisions bei jeder Card-Auswahl
- Hohe Build Diversity über kombinierbare Modifikatoren
- Starke Synergy mit Weapon, Tower, Resource und Combat
- Klare Lesbarkeit von Effekt, Rarity und Build-Richtung
- Datengetriebene Erweiterbarkeit ohne neue Kernmechaniken

## Gameplay

Cards werden an Reward-Momenten wie Wave-Abschlüssen, Boss-Belohnungen, Shop-Angeboten oder anderen klar definierten Entscheidungspunkten angeboten.

Der Player wählt aus einem kleinen Angebot und erhält den Effekt sofort für den restlichen Run.

Cards können offensive, defensive, ökonomische oder Tower-bezogene Eigenschaften verändern, müssen sich dabei aber immer an bestehende Systeme andocken.

Das Card-System ist der wichtigste Treiber für Build-Identität und kurzfristige Richtungswechsel innerhalb eines Runs.

## Rules

- Cards werden niemals aktiv aus der Hand gespielt.
- Cards erzeugen keine eigene Kampf- oder Aktionsressource.
- Jede Card verändert mindestens ein bestehendes System wie Weapon, Tower, Resource, Combat oder Economy.
- Legendary Card ist selten und buildprägend; ihre Seltenheit entsteht über Angebotsgewichte, nicht über gesperrte Angebotsfenster.
- **Jede Card stapelt** (Owner-Entscheidung 2026-09-21): zweimal dieselbe Card wendet ihren Effekt zweimal an. `CardData.AuthoredStackable` bleibt erhalten, falls eine künftige Card einmalig sein soll, wird aber von nichts gelesen.
- Card-Text und Effekte müssen auf Controller genauso verständlich sein wie auf Mouse & Keyboard.
- Neue Cards dürfen neue Synergies schaffen, aber keine neue Grundmechanik ohne separate Spezifikation einführen.

## Data Model

Card-Daten liegen vollständig in ScriptableObjects.

- CardData: Name, Beschreibung, Card Rarity, Tags, Effektliste und Präsentationsdaten
- CardEffectData: ein einzelner Modifikator auf ein Zielsystem mit Wert, Operator und Bedingungen
- CardPoolData: verfügbare Karten pro Progressionsstand, Stage oder Angebotskontext
- CardOfferData: Anzahl der Optionen, Gewichtungen, Sperrregeln und Garantien
Tags sind das Enum `CardTagType` auf `CardData`, kein eigenes Asset.

Runtime-Registrierung aktiver Cards bleibt von den Asset-Daten getrennt.

## Technical Design

Ein Card-Service verwaltet Pools, Angebote und die Anwendung ausgewählter Effekte.

Cards modifizieren Runtime-Systeme über standardisierte Modifier-Schnittstellen statt über direkte Sonderfälle pro Karte.

UI, Audio und Reward-Systeme reagieren auf Events wie OnCardOffered, OnCardSelected, OnCardApplied und OnCardPoolUpdated.

Die Validierung von Stack-Regeln, Sperren und exklusiven Gruppen erfolgt zentral im Card-Service.

Der erste Vertical Slice nutzt einen CardService mit save-basiertem Pending-Reward-Flow, einem deterministischen Drei-Karten-Angebot und direkten Modifier-Hooks für Weapon, Resource und Tower innerhalb der SampleScene.

Card-Reveal-Flash und andere Reward-nahe Effekte verwenden die dokumentierte Familienzuordnung aus der [VFX Specification](./VFX.md).

Dieser Reward-Flow wird im ersten Slice über Wave-Abschluss und Intermission statt über einen isolierten Testgegner getriggert.

## Dependencies

- Combat
- Weapons
- Towers
- Resources
- Economy
- UI
- Audio
- MetaProgression

## Linked Slice Files

- [Wave Boss Balance Slice](../Slices/14_WAVE_BOSS_BALANCE_SLICE.md)
- [Bosses Stage 1 Slice](../Slices/12_BOSSES_STAGE1_SLICE.md)

## UI

Die Card-UI zeigt mindestens:

- Kartenname
- Card Rarity
- kurze Effektbeschreibung
- betroffene Systeme oder Tags
- Bestätigung und Vergleich mit bestehendem Build

Angebote müssen per Controller schnell lesbar und ohne versteckte Zusatzseiten bedienbar sein.

## Audio

Card-Angebote, Reveal, Auswahl und besonders seltene Cards erhalten eindeutige Audio-Cues.

Legendary Card-Auswahl soll akustisch als bedeutender Moment wahrnehmbar sein, ohne die Lesbarkeit des Menüs zu stören.

## Visual

Cards nutzen eine einheitliche visuelle Sprache mit klarer Trennung nach Card Rarity und System-Tags.

Effekte werden im UI konkret beschrieben und nicht nur dekorativ dargestellt.

## Balancing

Cards sollen regelmäßig neue Entscheidungen erzeugen, idealerweise im Abstand von 1 bis 3 Minuten.

Es darf keine kleine Gruppe von Always Pick Cards geben, die fast jeden Build dominieren.

Ökonomische oder Tower-bezogene Cards dürfen stark sein, wenn sie Opportunity Costs oder Build-Verengung erzeugen.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Ein Standard-Card-Angebot besteht aus drei Optionen.
- Ein allgemeines Reroll-System ist nicht Teil des ersten spielbaren Umfangs.
- Die erste Card-Tag-Welle nutzt Weapon, Projectile, Tower, Resource, Status Effect, Burn, Freeze und Critical.
- **2026-09-30:** zweite Tag-Welle Poison, Shock, Support, Summon (NEW_CARD_MECHANICS C4-1; Icons in GRAPHICS_ASSET_PROMPTS Asset 108).
- **2026-09-30 — Tags steuern das Angebot** (Open Topics 4, Owner-Entscheidung (a)): `CardOfferData._tagAffinity` ist der eine Regler. Gewicht = Raritätsgewicht × (1 + Affinität × (2 × Überdeckung − 1)); Überdeckung = Anteil der Karten-Tags, die der Build schon hat, ein Tag zählt ab drei Karten voll. Positiv bevorzugt Synergie, negativ Abwechslung, 0 schaltet Tags aus. Standard +0,35: eine voll passende Karte ist etwa doppelt so wahrscheinlich wie eine ohne Überschneidung. Waffenkarten werden nie nach Tags gewichtet.

**Update 2026-09-09 — Card-Pool 18 → 26 erweitert:** 8 neue Cards ergänzt (`Assets/ScriptableObjects/Gameplay/Cards/`), alle über die bestehenden 4 `CardEffectTarget`-Hebel (WeaponDamageMultiplier, WeaponFireIntervalMultiplier, ResourceGainMultiplier, TowerDamageMultiplier) ohne neue Codepfade: Focused Lens, Rapid Cycler, Harvest Protocol, Reinforced Emitters (je Rare, stackable), Volatile Payload, Twin Turret Array, Endless Supply Line (je Epic, non-stackable), sowie Apex Overload (Legendary, non-stackable, kombiniert Weapon- und Tower-Damage wie das bereits existierende Legendary-Vorbild Reactor Bleed). Alle in `CardPool_Default.asset` registriert.

**Update 2026-09-10 — Card-Pool 26 → 30, Vision-Zielwert erreicht:** Die Early Access Goal-Sektion der Vision fordert explizit „mindestens 30 Karten" — dies war der einzige noch unerfüllte, konkret benannte Zahlenwert im gesamten Dokument. 4 letzte Cards ergänzt, bewusst mit Inspiration aus MTG und Slay the Spire (auf expliziten Wunsch), aber ohne neue Mechaniken einzuführen — beide Konzepte sind reine Neukombinationen der inzwischen 7 bestehenden `CardEffectTarget`-Hebel (siehe Update 2026-09-09 in Relics.md für die 3 neuesten: IncomingDamageTakenMultiplier, ProjectileSpeedMultiplier, PlayerMovementSpeedMultiplier), genau wie Vision's Design Principle „Neue Inhalte entstehen durch Kombination vorhandener Systeme" verlangt:

- **Skirmisher's Kit** (Rare, stackable) — MTG-artiges Zwei-Achsen-Archetyp-Pairing (wie eine Guild/Farbkombination): ProjectileSpeedMultiplier ×1.2 + WeaponFireIntervalMultiplier ×0.92. Erste Card überhaupt, die die neue ProjectileSpeedMultiplier-Achse nutzt (vorher nur auf einem Relic-Evolved-Effekt vorhanden).
- **Overdrive Injector** (Rare, stackable) — Slay-the-Spire-artiger „echter" Trade (vgl. Cards wie „Offering"/„Bloodletting", die Leben für Power abgeben): WeaponDamageMultiplier ×1.3, aber IncomingDamageTakenMultiplier ×1.08. Erste Card, die einen echten Malus trägt — vorher existierte Risk/Reward nur bei Relics (siehe Relics.md „Reckless Circuit" etc.).
- **Sure Footing** (Common, stackable) — PlayerMovementSpeedMultiplier ×1.12. Schließt zusammen mit Brace for Impact die zu dünne Common-Stufe (vorher 6, jetzt 8 von 30 — die anderen Rarities waren bereits stärker vertreten).
- **Brace for Impact** (Common, stackable) — IncomingDamageTakenMultiplier ×0.92. Reines defensives Gegenstück zu Overdrive Injector — jede der neuen Malus-Achsen hat dadurch jetzt sowohl eine Buff- als auch eine Debuff-Card im gewöhnlichen (nicht nur seltenen Relic-)Pool.

Finale Rarity-Verteilung: 8 Common / 12 Rare / 7 Epic / 3 Legendary (30 gesamt).

## Acceptance Criteria

- Cards funktionieren als passive Run-Modifikatoren ohne aktives Ausspielen.
- Card-Effekte können bestehende Systeme datengetrieben erweitern, ohne Sonderlogik pro Einzelkarte zu erzwingen.
- Card-Angebote sind per Controller vollständig navigierbar und klar lesbar.
- Card Rarity und Stack-Regeln sind zentral definiert und konsistent.
- Das System unterstützt neue Cards und Synergies ohne Architekturänderungen.
- Pending Reward und Intermission aus RunSaveData führen im ersten Slice in einen echten controller-freundlichen Card-Offer-Flow.

---

## Stacking is universal (2026-09-21)

Owner decision 2026-09-21: **every card stacks.** Taking the same card twice applies its
effect twice.

`CardData.Stackable` returns true unconditionally. The authored per-card value is preserved as
`AuthoredStackable` so a future card can be made unique again in one line, but nothing reads it
today.

This makes the offer pool simpler to reason about and removes the case where a reward screen
showed a card the player already had and could not take.

---

## Cooling Array re-targeted (2026-09-22)

Playtest 2026-09-22: *"Cooling Array and Rapid Cycler sound the same - are they doing the
same?"* They were. Both were a flat fire-interval multiplier. Cooling Array now works on the
reload loop instead: `WeaponModifier_CoolingArrayReload` (reload time x0.75) and
`WeaponModifier_CoolingArrayMagazine` (+3 rounds), via the new `WeaponModifierTarget`s
`ReloadTimeMultiplier` (15) and `MagazineBonus` (14). The old `CardEffect_FireRateUp` asset is
unreferenced but kept.

Rapid Cycler, Hair Trigger, Twin Barrel and Skirmisher's Kit still share the fire-interval
target. See `Docs/OPEN_TOPICS.md` finding A9 for the options.

The reward screen footer now shows each card's actual numbers, built from its effect data
(`CardEffectDescriber`), e.g. "+22% tower damage". This means two cards that do the same
thing are now visibly the same.
