# Combat Specification

## Purpose

Definiert das zentrale Echtzeit-Kampfsystem des Runs.

Combat verbindet Player, Weapon, Projectile, Tower, Enemy, Core und Card zu einem gemeinsamen Schadens- und Effektmodell.

Der Schwerpunkt liegt auf aktivem Spielerverhalten. Tower unterstützen den Kampf, ersetzen den Player aber nicht.

## Design Goals

- Action First durch direkte, reaktionsschnelle Kämpfe in jeder Wave
- Hohe Lesbarkeit trotz vieler Enemy-, Projectile- und Tower-Interaktionen
- Starke Synergy zwischen Weapon, Card, Tower und Resource-System
- Einheitliche Regeln für Damage, Status Effect und Trefferauflösung
- Controller-first-Zielerfassung und Kampfinteraktionen

## Gameplay

Während einer Wave bekämpft der Player Enemies aktiv innerhalb der Arena.

Weapon und Projectile erzeugen den primären Schaden des Builds.

Tower liefern zusätzlichen Schaden, Kontrolleffekte oder Resource-Unterstützung.

Card-Effekte modifizieren bestehende Kampfparameter wie Damage, Angriffstempo, Projectile-Verhalten, Status Effect oder Tower-Effizienz.

Combat erzeugt in kurzen Abständen Feedback durch Treffer, Kills, Resource-Drops, Status-Anwendungen und eskalierende Enemy-Gruppen.

## Rules

- Combat läuft ausschließlich während aktiver Waves und Boss-Begegnungen.
- Player, Tower und Enemy verwenden dieselben Grundbegriffe für Damage und Status Effect, auch wenn ihre Werte unterschiedlich sind.
- Core-Schaden ist immer klar lesbar und hat höhere Priorität als Effekt-Overlays.
- Tower dürfen allein keine dominante Strategie bilden; der höchste Schadensanteil eines erfolgreichen Builds soll vom Player beeinflusst werden.
- Status Effects stapeln nur nach klaren Datenregeln und nicht über versteckte Sonderfälle.
- Friendly Fire zwischen Player und Tower ist nicht Teil des Kernspezifikationsumfangs.
- Neue Combat-Inhalte erweitern bestehende Modifier und Reaktionsmuster statt neue Grundregeln einzuführen.

## Data Model

Combat-Daten werden datengetrieben über ScriptableObjects definiert.

- CombatBalanceData: globale Kampfparameter wie Hit-Stop-Grenzen, Drop-Frequenzen und Obergrenzen für Effektdichte
- DamageProfileData: Schadenstyp, Basisschaden, kritische Multiplikatoren, Durchschlag oder Flächeneigenschaften
- StatusEffectData: Dauer, Stapelregel, Tickrate, VFX- und Audio-Referenzen
- HitReactionData: Trefferfeedback, Knockback-Grenzen und Telemetrie-Flags

Runtime-Zustand bleibt in isolierten Komponenten oder Services und nicht in ScriptableObjects.

## Technical Design

MonoBehaviours empfangen Kollisionen, Animation Events und Scene-Bezüge.

Schadensberechnung, Statusauflösung und Modifikator-Ketten liegen in normalen C# Klassen.

Combat-Systeme kommunizieren über Events wie OnDamageDealt, OnDamageTaken, OnEnemyKilled, OnCoreDamaged und OnStatusApplied.

Card-, Tower- und Weapon-Modifikatoren registrieren sich über klar definierte Schnittstellen an derselben Combat-Pipeline, damit keine doppelte Logik entsteht.

Projectile-, Enemy- und Effekt-Instanzen müssen Pooling verwenden.

Der erste Vertical Slice setzt diese Struktur über einen PlayerCombatController am Player, CombatProjectile mit ProjectilePool für das Hold-to-Fire-Basisfeuer, CombatHitReceiver für Ziele sowie HealthRuntime und StatusEffectRuntime für Schaden, Tod und Burn- oder Freeze-Auflösung um.

Die konkrete Zuordnung des Shared-Atlas fuer Treffer-, Status- und Reward-nahe Effekte steht in der [VFX Specification](./VFX.md).

Bis der Weapons-Slice aktiv ist, kapselt PlayerCombatData die minimale Feuerkonfiguration des Players, ohne die gemeinsame Damage- und Status-Pipeline zu umgehen.

## Dependencies

- Player
- Weapons
- Enemies
- Waves
- Bosses
- Towers
- Cards
- Resources
- UI
- Audio

## UI

Combat-UI zeigt mindestens:

- Player-Lebenszustand
- Core-Lebenszustand
- aktuelle Wave-Gefahr
- temporäre Status-Hinweise
- klare Treffer- und Warnindikatoren

UI reagiert nur auf Combat-Events und enthält keine Schadenslogik.

## Audio

Jeder wichtige Kampffall besitzt eindeutiges Audio-Feedback:

- Weapon-Feuer
- Treffer
- Enemy-Tod
- Core-Schaden
- Boss-Warnung
- Status-Anwendung

Viele gleichzeitige Treffer werden über Priorisierung und Voice-Limits gemischt.

## Visual

Visuelle Combat-Signale müssen auch bei hoher Dichte lesbar bleiben.

Projectile-Farben, Enemy-Telegraphs und Core-Gefahr dürfen sich nicht gegenseitig verdecken.

Status Effects nutzen konsistente Farb- und Formsprachen statt individueller Spezialeffekte pro Inhalt.

## Balancing

Combat-Schwierigkeit steigt primär über Enemy-Komposition, Druck und Positionierungsanforderungen.

Zeit bis zur ersten relevanten Kampfentscheidung soll innerhalb der ersten Minute eines Runs liegen.

Card- und Tower-Synergien dürfen starke Power-Spikes erzeugen, aber keinen vollständig passiven Sieg ermöglichen.

HP-Bloat ohne neue Verhaltensanforderungen ist zu vermeiden.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Controller nutzt eine mittlere Soft-Aim-Unterstützung mit Zielkonus und leichter Magnetisierung; Mouse & Keyboard nutzt keine Zielhilfe.
- Der Player erhält nach bestätigtem Treffer eine kurze Unverwundbarkeitsphase für faire Lesbarkeit gegen Burst-Schaden.
- Boss-Begegnungen dürfen pro Phase CombatBalanceData-Overrides für Telegraph, Cooldown, Schaden und Druck verwenden, aber keine eigenen Combat-Regeln einführen.

## Acceptance Criteria

- Combat verwendet ein einheitliches Damage- und Status-Framework für Player, Enemy und Core-Interaktionen.
- Weapon-, Card- und Tower-Modifikationen können ohne Sonderlogik an dieselbe Combat-Pipeline andocken.
- Combat bleibt bei vielen Projectiles und Enemies lesbar und controller-freundlich.
- UI, Audio und VFX reagieren eventbasiert auf Combat-Ereignisse.
- Neue Combat-Inhalte können überwiegend datengetrieben ergänzt werden.

---

## Modifier-Pipeline (2026-09-22)

Die einzige Quelle für die Reihenfolge, in der Modifikatoren komponiert werden. SkillTree.md,
Upgrades.md, Towers.md und WeaponLoadout.md verweisen hierher und definieren keine eigene Reihenfolge
mehr (vorher standen dort fünf einander widersprechende Varianten).

**Weapons**, pro ausgerüsteter Kopie:

`Basiswert (WeaponData) → In-Run-Tier → permanente Alloy-Upgrades → Skill Tree → Innate Effects + Set Bonuses → Cards und Relics`

**Towers**, pro gebautem Tower:

`Basiswert (TowerData) → permanente Alloy-Upgrades → In-Run-Upgrade-Level → Skill Tree → Cards und Relics`

Regeln, die diese Reihenfolge überhaupt erst bedeutsam machen:

- Multiplikative Ziele (Damage, Fire Interval, Projectile Speed, Crit Damage, Spread, Reload) werden
  multipliziert; ihre Reihenfolge ist mathematisch egal und nur zur Lesbarkeit festgeschrieben.
- Additive Ziele (Additional Projectiles, Additional Pierce, Magazine, Crit Chance, Status Chance)
  werden addiert. Hier entscheidet die Reihenfolge nur, welche Obergrenze zuerst greift.
- Obergrenzen aus [Upgrades.md](Upgrades.md) gelten **nur** für permanente Upgrades. Cards, Relics
  und Set Bonuses dürfen darüber hinausgehen; das ist die beabsichtigte Beziehung zwischen
  dauerhaftem Fortschritt und einem einzelnen Run.
- Regeländernde Ziele (Full Ring, Explode on Impact/Expire, Slow on Hit, Critical Every Nth) sind
  Schalter: einmal gesetzt, bleiben sie gesetzt, unabhängig von der Quelle.
