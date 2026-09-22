# Economy Specification

## Purpose

Definiert die ökonomischen Regeln für Preise, Käufe und Opportunity Costs innerhalb des Spiels.

Economy bestimmt, wie Resources in Build-Fortschritt, Unlocks und strategische Entscheidungen übersetzt werden.

## Design Goals

- Meaningful Decisions bei jedem Kauf
- Klar verständliche Preisbildung und Wertigkeit
- Balance zwischen kurzfristiger Power und langfristiger Planung
- Starke Verzahnung mit Resources, Cards, Towers und Meta Progression
- Datengetriebene Anpassbarkeit für Balancing ohne Codeänderungen

## Gameplay

Im Run entscheidet der Player, wann Resources für Cards, Towers, Upgrades oder Shop-Angebote ausgegeben werden.

Economy erzeugt dabei Spannung zwischen sofortiger Stärke, Synergy-Vorbereitung und Sparen für seltene Optionen.

Außerhalb des Runs unterstützt Economy die Ausgabe von Meta-Currency für Unlocks, bleibt aber logisch von der Run-Economy getrennt.

Economy selbst erzeugt keine Inhalte, sondern bewertet und vermittelt vorhandene Inhalte.

## Rules

- Jeder Kauf hat klare, sichtbare Kosten und ein eindeutiges Ergebnis.
- Run-Economy und Meta-Economy verwenden getrennte Wallets und Balance-Kurven.
- Preise werden datengetrieben definiert und nicht per Hardcode an Einzelfällen ausgerichtet.
- Hochwertige Angebote wie Legendary Card oder starke Tower Upgrades müssen echte Opportunity Costs erzeugen.
- Rabatte, Boni oder Refunds nutzen standardisierte Modifier-Regeln.
- Käufe sind atomar: entweder vollständig erfolgreich oder ohne Seiteneffekte abgebrochen.
- Economy darf keine dominante Spar- oder Spend-All-Strategie belohnen.

## Data Model

**Update 2026-09-16 — Umgesetzt:** Die ursprünglich geplanten fünf separaten ScriptableObject-Typen wurden zu drei real implementierten Mechanismen konsolidiert, die den gleichen Zweck ohne leere Platzhalter-Klassen erfüllen:

- **Preiskurve pro Stage ("PriceCurveData"):** `ShopData._stagePriceMultiplier` (`ShopData.cs`) — jede Stage besitzt ihr eigenes ShopData-Asset mit eigenem Preis-Multiplikator, angewendet in `ShopService.ResolveOfferCost()`. Für Tower Build/Upgrade-Kosten ist die Kostenstaffel direkt in `TowerUpgradeLevelData.UpgradeCost` pro Level datengetrieben hinterlegt.
- **Rabatt-Modifikatoren ("DiscountModifierData"):** neuer `CardEffectTarget.PurchaseCostMultiplier` (`CardEffectTarget.cs`) — folgt exakt demselben Pull-Model-Muster wie `WeaponDamageMultiplier`/`IncomingDamageTakenMultiplier`. Ein `CardEffectData`-Asset mit diesem Target speist `CardService.PurchaseCostMultiplier`, live gelesen von `EconomyService.ResolveCost()`. Damit lassen sich Rabatt-Cards/-Relics rein datengetrieben ergänzen, ohne Codeänderungen (siehe Acceptance Criteria).
- **Angebots-Zusammenstellung ("OfferTableData"):** bereits vorhanden als `ShopOfferTableData`/`ShopData` (Gewichtung, Min/Max-Stage-Filter pro Angebot).

`EconomyBalanceData`/`PurchaseRuleData` als eigene globale Asset-Typen wurden nicht gebaut: Kaufvalidierung (Wallet-Prüfung, atomarer Commit) ist bereits zentral in `EconomyService` selbst verortet, ohne dass dafür ein separates Daten-Asset nötig ist — das würde reine Konfigurationsindirektion ohne echten Zusatznutzen bedeuten.

## Technical Design

`EconomyService` (`Assets/Scripts/Gameplay/Economy/EconomyService.cs`) validiert Käufe, fragt Wallet-Zustände ab und commitet Ausgaben zentral — sowohl `ShopService` als auch `TowerService` routen jeden Kauf ausschließlich hierüber.

Er kommuniziert über die vier dokumentierten Events: `PurchaseRequested`, `PurchaseSucceeded`, `PurchaseFailed`, `PriceModified` (Letzteres feuert, sobald `ResolveCost()` einen Rabatt-Multiplikator ≠ 1 anwendet).

**Wichtige Invariante:** `ResolveCost(baseCost)` darf pro Kauf nur genau einmal aufgerufen werden — sowohl für die UI-Anzeige als auch für den tatsächlichen Kauf muss derselbe aufgelöste Wert verwendet werden (`ShopService.ResolveOfferCost()` und `TowerService.ResolveTowerCost()` sind die jeweils einzigen Auflösungspunkte). `TryPurchase()` selbst wendet keinen weiteren Modifier an, um Doppel-Rabattierung auszuschließen.

Economy enthält keine UI-Logik und keine Fachlogik einzelner Inhalte, sondern nur deren Bewertungs- und Kaufregeln.

Persistente Daten werden über SaveSystem geladen und gespeichert.

## Dependencies

- Resources
- Cards
- Towers
- Factory
- MetaProgression
- UI
- Audio
- SaveSystem

## UI

Economy-UI zeigt mindestens:

- klare Kosten je Angebot
- aktuelle Kaufbarkeit
- Änderungen durch Rabatte oder Boni
- spürbare Bestätigung bei erfolgreichem Kauf

## Audio

Kauf, zu wenig Resources, seltene Angebote und starke Discounts erhalten eindeutige Audio-Cues.

Audio muss informative Funktion haben und nicht nur dekorativ sein.

## Visual

Preise, Seltenheit und Wertigkeit werden klar voneinander getrennt dargestellt.

Legendary- oder teure Angebote brauchen visuelle Gewichtung, dürfen aber andere Informationen nicht verdecken.

## Balancing

Economy soll regelmäßig interessante Spannungen zwischen sofortiger Effizienz und späteren Chancen erzeugen.

Preissteigerungen müssen verständlich bleiben und nicht willkürlich wirken.

Seltene High-Impact-Angebote dürfen stark sein, wenn sie spürbare Investitionskosten und Risiko tragen.

## Open Questions

Keine offenen Fragen. Die drei früheren Fragen sind beantwortet (2026-09-22):

- *Feste Preis-Kategorien oder dynamische Angebote?* Feste Kategorien pro Angebotsart plus
  `ShopData._stagePriceMultiplier` — siehe [Shop.md](Shop.md).
- *Reroll im Initialumfang?* Nein — siehe [Shop.md](Shop.md). Ein Reroll-Key-Node der Command-Branch
  existiert getrennt davon, siehe [SkillTree.md](SkillTree.md).
- *Refund-Regel?* Ja, eine — die Tabelle "Refunds und Verkauf" unten ist die einzige Quelle.

## Acceptance Criteria

- Economy verwaltet Preise und Käufe zentral, klar und datengetrieben.
- Run- und Meta-Economy sind getrennt spezifiziert und gespeichert.
- Käufe reagieren atomar und eventbasiert auf Resource-Verfügbarkeit.
- Preisänderungen durch Modifier bleiben konsistent und nachvollziehbar.
- Balancing kann ohne Codeänderungen über Daten angepasst werden.

---

## Three currencies (2026-09-21)

This spec owns **Scrap**, the in-run currency, and that scope is unchanged. Two permanent
currencies now sit alongside it and are deliberately outside this document:

| Currency | Scope | Spec |
| --- | --- | --- |
| Scrap | one run | this document |
| Skill Points | permanent | [SkillTree.md](SkillTree.md) |
| Alloy | permanent | [Upgrades.md](Upgrades.md) |

**The separation rule:** neither permanent currency may buy anything a run itself can buy, and
Scrap never crosses between runs. If two currencies can buy the same thing, the in-run economy
stops mattering.

New Scrap sinks added 2026-09-21:

- **Core repair.** During an Intermission the player may stand near the Core and spend Scrap to
  restore a fraction of its health. Each repair within a run costs more than the last, so it
  competes with towers and weapons rather than replacing the need to defend the Core.
- **Weapon purchases**, once [WeaponLoadout.md](WeaponLoadout.md) is implemented, including a
  50% sell-back which is the only Scrap income outside combat.

---

## Refunds und Verkauf (2026-09-22)

Die einzige Quelle für Rückerstattungen. Shop.md, Towers.md und WeaponLoadout.md verweisen hierher
und wiederholen die Regeln nicht mehr.

| Gekauft | Rückgabe möglich? | Satz | Wann |
| --- | --- | --- | --- |
| Card (Reward oder Shop) | nein | — | — |
| Weapon (Shop oder Reward) | ja, Verkauf aus dem Loadout-Panel | 50 % des Kaufpreises plus aller Tier-Upgrades, abgerundet | Intermission; die letzte Waffe kann nicht verkauft werden |
| Weapon, die nicht mehr passt (pending) | ja, Ablehnen | 100 % bei Shop-Kauf, 0 % bei Reward | im Moment des Angebots |
| Tower (Bau oder Upgrade) | nein | — | — |
| Core-Reparatur | nein | — | — |
| Meta-Käufe (Unlocks, Stat-Upgrades) | nein | — | — |
| Skill-Tree-Punkte | ja, voller Respec | 100 % der ausgegebenen SP | Hauptmenü; erster Respec je Tree-Version gratis, danach 30 Legierung |

`ShopService.RefundPurchase()` ist kein Spieler-Refund, sondern ein Korrektheits-Guard: Er rollt eine
Zahlung zurück, deren *Anwendung* danach fehlgeschlagen ist.

**Scrap-Einnahmen außerhalb des Kampfes** (die frühere Behauptung, der Waffenverkauf sei die einzige):
Waffenverkauf, Factory-Produktion (Salvage Trickle, Tower-Pulse, Wave-Abschluss-Bonus) und die
Skill-Tree-Knoten Dividend, Deep Reserves, War Chest und Seed Capital.

## Währungen und ihre Quellen (2026-09-22)

Die einzige Quelle für Verdienstraten. [SkillTree.md](SkillTree.md) und [Upgrades.md](Upgrades.md)
verweisen hierher.

| Währung | Reichweite | Verdient durch | Ausgegeben für |
| --- | --- | --- | --- |
| Scrap | ein Run | Enemy-Kills, Wave-Abschluss, Factory-Produktion, Waffenverkauf, Skill-Tree-Startboni | Towers, Tower-Upgrades, Shop-Angebote, Waffen, Waffen-Tier-Upgrades, Core-Reparatur |
| Skill Points | dauerhaft | **nur Boss-Siege**: Stage 1-5 = 1 / 1 / 2 / 2 / 3, plus 2 beim ersten Sieg über eine Boss-Id | Skill-Tree-Knoten, danach Ascension-Ränge |
| Alloy | dauerhaft | **nur Boss- und Elite-Kills**: Boss Stage 1-5 = 6 / 8 / 10 / 12 / 14, plus 10 beim ersten Sieg; Elite = 1 | Unlocks, permanente Stat-Upgrades, bezahlter Respec (30) |

Weder das Abschließen noch das Scheitern eines Runs zahlt an sich etwas. Die Werte stehen in
`MetaProgressionDefinitionData`, nicht im Code.

## Trennungsregel, präzisiert (2026-09-22)

Die frühere Formulierung ("keine dauerhafte Währung darf etwas kaufen, was ein Run selbst kaufen
kann") war mehrdeutig: Alloy-Magazin-Upgrades und die In-Run-Waffentiers berühren dieselbe Zahl.

Die geltende Regel lautet:

- Eine dauerhafte Währung kauft **niemals eine Run-Ressource**: kein Scrap, keine Card, keine Waffe,
  keinen Tower, keine Reparatur. Diese entstehen ausschließlich im Run.
- Eine dauerhafte Währung darf **Grundwerte und Rabatte** verändern — innerhalb der harten
  Obergrenzen in [Upgrades.md](Upgrades.md), und niemals so weit, dass sie die Build-Entscheidungen
  eines einzelnen Runs überwiegt.
