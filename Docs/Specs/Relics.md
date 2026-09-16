# Relics Specification

## Purpose

Definiert Relics als seltene, buildprägende Run-Modifikatoren.

Relics erweitern bestehende Systeme stärker als gewöhnliche Cards, ohne ein eigenes aktives Subsystem zu erzeugen.

## Design Goals

- Hohe Build Identity mit wenigen, bedeutenden Funden
- Starke Synergy mit Card, Weapon, Tower, Resource und Combat
- Seltene, aber leicht verständliche Power-Spikes
- Klare Trennung zu normalen Cards durch Seltenheit und Effektgewicht
- Datengetriebene Erweiterbarkeit ohne neue Grundregeln

## Gameplay

Relics erscheinen selten an hochwertigen Reward-Momenten wie Boss-Abschlüssen, besonderen Stage-Belohnungen oder anderen streng kontrollierten Angebotsfenstern.

Eine erhaltene Relic verändert den aktuellen Build spürbar für den restlichen Run.

Relics sind kein aktives Item, werden nicht manuell ausgelöst und erzeugen keine zusätzliche Eingabekomplexität.

Sie dienen dazu, seltene, stark synergetische Build-Achsen zu eröffnen, ohne das Spiel in ein separates Inventarsystem zu verwandeln.

Relics sind laut Project Vision explizit Teil von zwei Kern-Pillars, nicht nur eine spätere Erweiterung: Vision führt sie unter „Build Diversity" (Synergien entstehen durch Waffen, Türme, Karten, **Relikte**, Ressourcen) und unter „Progression → Innerhalb eines Runs" (Karten, Türme, Waffen, Ressourcen, **Relikte**) explizit gleichrangig neben Cards. Relics sind damit ein vorgesehener, benannter Baustein des Zielspiels — ihr Fehlen im ersten Vertical Slice ist eine bewusste Scope-Entscheidung (siehe „Festgelegte Entscheidungen"), keine Streichung aus der Vision.

### Beispiel-Relics

Damit die Abgrenzung zu normalen Cards konkret bleibt (Vision's Card-Beispiele sind kleine, additive Boni; Relics sind größer und build-definierend), einige illustrative Beispiele im selben Stil wie Vision's Card-Beispielliste:

- **Overclocked Core** — jeder Tower-Treffer hat eine kleine Chance, sofort erneut zu feuern (starke, seltene Synergy mit jedem Damage-Tower-Build).
- **Scavenger's Contract** — jeder besiegte Elite-Gegner droppt garantiert eine zusätzliche Card-Wahl statt nur Resources (verbindet Combat-Risiko direkt mit Build-Wachstum).
- **Twin Chamber** — die aktuelle Waffe feuert einen zusätzlichen, leicht abgeschwächten Parallelschuss (verändert einen ganzen Weapon-Build strukturell, nicht nur numerisch).
- **Foundry Bond** — jede gebaute Tower-Instanz erhöht den Ressourcenertrag aller Factory-Produktionsknoten dauerhaft (direkte Relic-zu-Factory-Synergy, siehe Factory Philosophy in der Vision).

## Rules

- Relics sind passiv und run-weit aktiv.
- Relics werden deutlich seltener angeboten als Cards.
- Relics greifen ausschließlich auf bestehende Systeme wie Weapon, Tower, Resource, Combat oder Economy zu.
- Relics dürfen buildprägend sein, aber keine dominante Einheitsstrategie erzwingen.
- Relics sind nicht Teil eines aktiven Equipment-Managements im ersten Scope.
- Relics und Cards dürfen sich über gemeinsame Modifier-Schnittstellen ergänzen.
- Der erste Vertical Slice behandelt Relics als spätere Erweiterung und nicht als verpflichtenden Kern des Stage-1-Loops.

## Data Model

Relic-Daten werden über ScriptableObjects beschrieben.

- RelicData: Name, Beschreibung, Präsentation und Effektliste
- RelicEffectData: starke, klar definierte Modifier auf bestehende Systeme
- RelicPoolData: kontrollierte Seltenheits- und Angebotsfenster
- RelicTagData: thematische oder synergetische Gruppierung

## Technical Design

Relics nutzen dieselbe Modifier- und Event-Architektur wie Cards, aber mit eigener Pool- und Reward-Steuerung.

Ein Relic-Service kann später auf Card-Offer- und Reward-Systeme aufsetzen, ohne diese Architektur zu brechen.

UI und Audio reagieren auf Events wie OnRelicOffered, OnRelicSelected und OnRelicApplied.

## Dependencies

- Cards
- Combat
- Weapons
- Towers
- Resources
- Economy
- UI
- Audio
- MetaProgression

## UI

Relics benötigen eine deutlich wertigere Präsentation als normale Cards.

Beschreibung, Build-Relevanz und Systembezug müssen auf einen Blick erkennbar sein.

## Audio

Relic-Reveal und Auswahl brauchen seltene, bedeutungsvolle Audio-Signale.

Sie sollen sich klar von normalen Card-Rewards unterscheiden.

## Visual

Relics nutzen eine eigenständige, hochwertige visuelle Sprache innerhalb derselben Stilfamilie wie Cards.

Seltenheit und Build-Gewicht müssen bereits visuell erkennbar sein.

## Balancing

Relics sollen seltener als Cards, aber spürbarer in ihrer Wirkung sein.

Sie dürfen starke Synergies eröffnen, müssen aber durch Angebotsknappheit und Build-Spezialisierung kontrolliert bleiben.

## Open Questions

- Braucht jede Stage ihren eigenen, thematisch passenden Relic-Pool (analog zu `StageEnemyPoolData`), oder bleibt der Relic-Pool run-weit einheitlich?
- Wann bekommt der Reveal-Moment die in „Visual" geforderte eigenständige, hochwertige Präsentation (siehe „Update 2026-09-09" unten für den aktuellen, bewusst einfacheren Zwischenstand)?

**Update 2026-09-09 — v1 implementiert, mit bewusster Scope-Reduktion:** `RelicData`/`RelicPoolData` existieren als echte ScriptableObjects (`Assets/Scripts/Gameplay/Relics/`), `_effects` nutzt bewusst denselben `CardEffectData`-Typ wie Cards (siehe Rules: „gemeinsame Modifier-Schnittstellen"), läuft über dieselbe `CardModifierSnapshot`-Pipeline in `CardService`. 3 Relics ausgeliefert (Overcharged Core, Foundry Bond, Adrenal Surge), je zwei kombinierte Stat-Effekte statt eines einzelnen wie bei den meisten Cards — spürbar stärker und „build-defining" im Sinne der Balancing-Vorgabe. Ausgelöst über `WaveManager.CompleteBossEncounter()` → `CardService.TryGrantRandomRelic()`, exakt am in Rules vorgesehenen Moment (Boss-Abschluss).

Bewusst **nicht** wie in „Gameplay"/„Visual" beschrieben umgesetzt: kein Angebot aus mehreren Relic-Optionen mit Spieler-Auswahl, keine eigenständige, hochwertige Reveal-UI. Stattdessen wird ein zufälliges, noch nicht besessenes Relic automatisch vergeben und über das bestehende `NotificationPresenter`-Toast angekündigt. Grund: eine echte Auswahl-UI hätte neue UI-Elemente in eine der bereits als mehrdeutig dokumentierten Szenen-Hierarchien einfügen müssen (siehe UI.md/CardRewardPresenter-Grenzen an anderer Stelle) — dieselbe Grenze, die auch Card-Tag-Icons und Card-Reveal-Flash im aktuell aktiven Reward-Screen noch unsichtbar hält. Die mechanische Wirkung (Relics verändern den Build spürbar und dauerhaft) ist damit real und spielbar; die im Spec geforderte Präsentation ist ein dokumentierter Nachtrag, sobald Editor-Zugriff für eine echte Reveal-Screen-Erweiterung verfügbar ist.

**Update 2026-09-09 — Pool 3 → 7 erweitert:** Vision nennt Relikte explizit gleichrangig neben Karten unter „Build Diversity"; der Pool war mit 3 Einträgen (bei 26 Cards) der mit Abstand dünnste Content-Bereich des Projekts. 4 neue Relics ergänzt, alle weiterhin über die bestehenden 4 `CardEffectTarget`-Hebel, ohne neue Codepfade (`RelicData._effects` ist bereits ein Array beliebiger Länge, daher kein Codechange nötig):

- **Kinetic Reserve** — WeaponDamageMultiplier ×1.28 + ResourceGainMultiplier ×1.22
- **Momentum Cycler** — WeaponFireIntervalMultiplier ×0.82 + ResourceGainMultiplier ×1.25
- **Bastion Protocol** — WeaponFireIntervalMultiplier ×0.85 + TowerDamageMultiplier ×1.3
- **Singularity Core** — WeaponDamageMultiplier ×1.2 + TowerDamageMultiplier ×1.2 + ResourceGainMultiplier ×1.2 (3 statt 2 Effekte — die bislang stärkste, build-unabhängigste Relic, als bewusste qualitative Eskalation über die 2-Effekt-Norm der ersten 3 Relics hinaus)

Mit den ursprünglichen 3 Relics (Overcharged Core: Weapon+Tower, Foundry Bond: Resource+Tower, Adrenal Surge: Weapon+Interval) deckt der Pool jetzt alle 6 möglichen Zweier-Kombinationen der 4 Stat-Hebel ab — keine willkürliche Zahl, sondern vollständige kombinatorische Abdeckung der Synergie-Achsen, die Vision's „Build Diversity"-Pillar explizit fordert.

**Update 2026-09-09 — Risiko/Reward-Relics (Pool 7 → 10), neuer Effekt-Hebel `IncomingDamageTakenMultiplier`:** Vision's Pillar 1 „Meaningful Decisions" nennt „Risiko eingehen?" explizit als Beispiel-Entscheidung — bislang hatten aber weder Cards noch Relics einen echten Malus-Hebel, nur reine Buffs. Ergänzt:

- Neuer `CardEffectTarget.IncomingDamageTakenMultiplier` (ans Ende des Enums angehängt, damit bestehende, per Index serialisierte Assets unverändert gültig bleiben). Skaliert Schaden, den Player und Core erhalten — live ausgelesen bei `PlayerCombatController.ReceiveHit` bzw. über eine optionale `CardService`-Referenz an `CombatHitReceiver` (nur am Core gesetzt, via `WaveManager.EnsureCoreTarget()`; Enemy-/Boss-Receiver bleiben unberührt, Standardwert 1 = kein Verhaltensunterschied für alle bestehenden Runs).
- **Reckless Circuit** — WeaponDamageMultiplier ×1.4 + ResourceGainMultiplier ×1.35, aber +10 % erhaltener Schaden.
- **Exposed Core** — TowerDamageMultiplier ×1.5 (stärkster Einzelwert im gesamten Card-/Relic-Pool), aber +18 % erhaltener Schaden.
- **Overdriven Barrel** — WeaponFireIntervalMultiplier ×0.75, aber +12 % erhaltener Schaden.

Balancing-Logik: der Malus skaliert mit dem Wert des Bonus (18 % Risiko für den bislang stärksten Einzel-Buff im Spiel, 10 % für einen breiten Zwei-Stat-Buff, 12 % für einen fokussierten Feuerrate-Buff) — durchgängig über dem Powerlevel malusfreier Relics/Cards derselben Kategorie, da das Risiko den Mehrwert rechtfertigen muss. Bewusst nicht auf den Burn-/Freeze-Statustick-Damagepfad angewendet (nur der direkte Hit-Pfad) — ein kleinerer, klar abgegrenzter Wirkungsbereich, der die Kernaussage „Schaden am Player/Core steigt" bereits korrekt trägt, ohne den selteneren Statuseffekt-Pfad mit anzufassen.

**Update 2026-09-09 — weitere Malus-Achsen (Projektil-/Bewegungstempo), Pool 10 → 12:** `IncomingDamageTakenMultiplier` war die erste Malus-Achse, aber nicht die einzig sinnvolle. Zwei neue `CardEffectTarget`-Hebel ergänzt (ebenfalls ans Enum-Ende angehängt, keine Migration nötig):

- `ProjectileSpeedMultiplier` — skaliert nur die eigenen Weapon-Projektile des Spielers (`CombatProjectile.Launch` bekommt einen neuen, optionalen `speedMultiplier`-Parameter; Tower-/Enemy-/Boss-Projektile bleiben unberührt, da deren `Launch`-Aufrufe den Parameter nicht setzen und damit beim Default 1 bleiben).
- `PlayerMovementSpeedMultiplier` — multipliziert sich in `PlayerCombatController.MovementSpeedMultiplier` mit dem bereits bestehenden Status-Effekt-Multiplikator (Freeze/Slow) statt ihn zu ersetzen.

Beide Hebel sind in beide Richtungen nutzbar (Wert > 1 = Buff, Wert < 1 = Malus) — genau wie von der Anfrage gefordert, nicht nur „mehr erhaltener Schaden", sondern auch Kugelgeschwindigkeit und Movementspeed als mögliche negative (oder positive) Effektachsen.

**Update 2026-09-09 — Evolving Relics (Pool 12, `RelicEvolutionCondition`):** `RelicData` unterstützt jetzt optional einen zweiten Effektzustand, der automatisch freigeschaltet wird, sobald ein laufend von `CardService` mitgezählter Run-Counter einen Schwellwert erreicht — `_effects` bleibt der Startzustand, `_evolvedEffects` der Zielzustand, `_evolutionCondition`/`_evolutionThreshold` steuern wann. Nicht-evolvierende Relics (alle 10 vorherigen) bleiben unverändert, da `_evolutionCondition` standardmäßig `None` ist.

Zwei Trigger-Typen implementiert, exakt nach Anfrage:

- `RangedEnemiesKilled` — zählt hoch in `WaveManager.OnCombatantDied`, wenn der besiegte Gegner `EnemyRoleType.RangedPressure` ist (bereits vorhandene Rolle, keine neue Enemy-Kategorie nötig), sowohl im normalen Combat- als auch im Boss-Helfer-Pfad.
- `WavesCleared` — zählt hoch in `WaveManager.CompleteCurrentWave()`, bewusst nur für reguläre Wellen, nicht für Boss-Abschlüsse (die vergeben bereits eigenständig ein Relic über `TryGrantRandomRelic`).

Beide Counter sind run-weit persistent (`RunSaveData.RangedEnemiesKilledCount`/`WavesClearedCount`/`EvolvedRelicIds`, geladen/gespeichert wie `SelectedRelicIds`) und werden bei Inaktivierung des Runs zurückgesetzt. Die Schwellwerte wurden anhand der tatsächlichen `SpawnGroup_Wave*.asset`-Daten kalibriert (≈4 Ranged-Gegner pro Stage über die vorhandenen Spawn Groups, 6 Wellen pro Stage × 2 Stages = 12 nicht-Boss-Wellen im gesamten Run), nicht geraten:

- **Marksman's Trial** — Start: WeaponDamageMultiplier ×1.1 (matcht das Anfrage-Beispiel „gibt 10% für etwas"). Nach 6 besiegten Ranged-Gegnern (erreichbar bereits in Stage 1) → **Marksman's Mastery**: WeaponDamageMultiplier ×1.35 + ProjectileSpeedMultiplier ×1.25.
- **Adaptive Plating** — Start: PlayerMovementSpeedMultiplier ×0.9 (matcht „gibt -10% auf etwas", hier auf einer der neuen Bewegungstempo-Achse statt Incoming-Damage). Nach 8 gecleärten Wellen (spät in Stage 2, aber vor dem Run-Ende erreichbar) → **Adaptive Plating (Calibrated)**: PlayerMovementSpeedMultiplier ×1.15 + IncomingDamageTakenMultiplier ×0.9 — Malus dreht exakt wie im Anfrage-Beispiel auf einen positiven Effekt.

Evolution löst denselben `NotificationPresenter`-Toast-Mechanismus wie ein Relic-Erhalt aus ("Relic evolved: …"), aus denselben Gründen keine neue Reveal-UI (siehe oben). Datengetrieben erweiterbar: ein drittes `RelicEvolutionCondition` (z. B. Elite-Kills, Boss-Siege) ließe sich analog ergänzen, ohne bestehende Relics anzufassen.

Festgelegte Entscheidungen:

- Relics bleiben im Projektvokabular erhalten und sind laut Vision explizit Teil von „Build Diversity" und „Progression" — ihr Fehlen im ersten Vertical Slice ist eine Scope-, keine Konzept-Entscheidung.
- Relics sind passive, seltene und buildprägende Run-Modifikatoren.
- Relics erweitern ausschließlich bestehende Systeme (inklusive Factory, siehe „Foundry Bond"-Beispiel) und führen keine neue aktive Bedienmechanik ein.
- Relics werden nach Cards priorisiert gebaut: der Card-/Reward-/Offer-Flow muss zuerst stabil sein, bevor Relics denselben Modifier-Pfad mit eigener Seltenheitssteuerung wiederverwenden.

## Acceptance Criteria

- Relics sind als seltene passive Build-Modifikatoren eindeutig von Cards abgegrenzt.
- Relics können auf bestehende Modifier- und Reward-Systeme aufsetzen.
- Relics erzeugen hohe Build-Identity ohne eigene Bedienmechanik.
- Der erste Vertical Slice bleibt auch ohne aktive Relic-Integration vollständig spielbar.
- Neue Relics lassen sich datengetrieben ergänzen.
