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

- Soll der Shop feste Preis-Kategorien oder dynamische Angebote pro Stage verwenden?
- Wird ein Reroll-System Teil des Initialumfangs?
- Gibt es eine kontrollierte Refund-Regel für bestimmte Upgrades oder Tower-Entscheidungen?

## Acceptance Criteria

- Economy verwaltet Preise und Käufe zentral, klar und datengetrieben.
- Run- und Meta-Economy sind getrennt spezifiziert und gespeichert.
- Käufe reagieren atomar und eventbasiert auf Resource-Verfügbarkeit.
- Preisänderungen durch Modifier bleiben konsistent und nachvollziehbar.
- Balancing kann ohne Codeänderungen über Daten angepasst werden.
