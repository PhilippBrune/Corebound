# Shop Specification

## Purpose

Definiert den Shop als kontrollierten Kauf- und Entscheidungsraum innerhalb eines Runs.

Der Shop übersetzt Resources in gezielte Build-Verbesserungen wie Cards, Upgrades oder andere erlaubte Angebote.

## Design Goals

- Meaningful Decisions zwischen Sofortwert und Sparen
- Klare Kosten- und Angebotskommunikation
- Gute Verzahnung mit Resources, Economy, Cards und Towers
- Vollständige Controller-Bedienbarkeit
- Modulare Erweiterbarkeit für spätere Angebotsarten

## Gameplay

Der Shop erscheint an klar definierten, sicheren Entscheidungspunkten außerhalb unmittelbarer Combat-Hektik.

Der Player kann dort Resources für verfügbare Angebote ausgeben, zum Beispiel Cards, Tower Upgrades oder andere Build-Verbesserungen.

Der Shop soll strategische Planung unterstützen, aber nicht den schnelleren Reward-Loop normaler Wave-Belohnungen verdrängen.

Er ergänzt den Build-Aufbau durch gezieltere Auswahlmöglichkeiten mit klaren Opportunity Costs.

## Rules

- Der Shop ist ein Menü oder klar definierter Interaktionsort und keine begehbare eigene Spielschicht.
- Shop-Angebote verwenden die zentrale Economy- und Wallet-Logik.
- Preise und Verfügbarkeiten werden datengetrieben definiert.
- Der erste Vertical Slice nutzt feste Preis-Kategorien pro Angebotsart statt vollständig dynamischer Preisbildung.
- Ein allgemeines Reroll-System ist nicht Teil des ersten Scope.
- Refunds für Shop-Käufe existieren im ersten Scope nicht.
- Der Shop bleibt vollständig controller-first bedienbar.

## Data Model

Shop-Daten werden über ScriptableObjects definiert.

- ShopData: Identität, Präsentation und Angebotsregeln eines Shops
- ShopOfferData: einzelnes kaufbares Angebot mit Kosten, Limits und Zielsystem
- ShopOfferTableData: Pools, Gewichtungen und Stage-spezifische Verfügbarkeiten
- ShopPresentationData: Banner, UI-Stile und Audio-Referenzen

## Technical Design

Der Shop ist ein UI- und Economy-getriebener Flow und enthält keine eigene Kampflogik.

Ein Shop-Service kann später verfügbare Angebote aus ShopOfferTableData erzeugen und über den Economy-Service validieren lassen.

Events wie OnShopOpened, OnShopOfferSelected, OnPurchaseRequested und OnPurchaseSucceeded koordinieren UI, Audio und Wallet-Anpassungen.

Der erste Vertical Slice setzt den Shop als intermission-basierten Blocking-Overlay-Flow nach Wave 3 von Stage 1 um, mit festen Card-Angeboten aus `ShopOfferTableData`, zentraler Wallet-Validierung und anschließender Tower-Placement-Phase.

## Dependencies

- Economy
- Resources
- Cards
- Towers
- UI
- Audio
- Input
- SaveSystem

## UI

Die Shop-Oberfläche zeigt Kosten, Kaufbarkeit, Seltenheit und Angebotsnutzen klar und knapp an.

Controller-Fokus, Confirm und Cancel müssen sich identisch zu anderen Menüs verhalten.

## Audio

Shop-Öffnung, Hover, Kauf und fehlende Resources benötigen klar unterscheidbare UI-Sounds.

Seltene Angebote dürfen akustisch mehr Gewicht erhalten als Standardkäufe.

## Visual

Shop-Visuals priorisieren Angebotslesbarkeit, Preisverständnis und Fokusführung.

Sie müssen sich vom Combat-Bild und von Card-Reward-Screens unterscheiden, aber stilistisch zusammenpassen.

## Balancing

Der Shop soll starke gezielte Entscheidungen erlauben, aber nicht zur einzig optimalen Progressionsquelle werden.

Preise müssen relevante Opportunity Costs erzeugen und mit Reward-Frequenz, Tower-Kosten und Card-Angeboten im Gleichgewicht bleiben.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Shop-Angebote nutzen feste Preis-Kategorien pro Angebotsart und optionale Stage-Multiplikatoren.
- Ein Reroll-System und Refunds sind nicht Teil des ersten Scope.
- Der Shop ergänzt Reward- und Card-Flows, ersetzt diese aber nicht als Hauptprogression.

## Acceptance Criteria

- Der Shop nutzt zentrale Economy- und Resource-Regeln.
- Angebote, Kosten und Kaufbarkeit sind controller-freundlich lesbar.
- Keine neue Kernmechanik wie Inventarmanagement oder Reroll-Spiralen wird eingeführt.
- Shop-Käufe lassen sich eventbasiert und datengetrieben integrieren.
- Der Stage-1-Loop bleibt ohne komplexe Shop-Sonderregeln verständlich.

---

## Multiple purchases per visit (2026-09-21)

Playtest 2026-09-21: *"currently i can only buy one item in the shop, make it unlimited as
long as the player has the money".*

The shop no longer closes on the first purchase. A bought offer is removed from the list and
the shop stays open; it closes on its own when every offer has been bought, or when the player
cancels. Removing the offer rather than leaving it in place stops one card being bought twice
in a single visit.

This changes the shop's role: it was a single forced choice, and is now a spending opportunity
bounded only by the wallet. The balance consequence is that a hoarding strategy now has
somewhere to spend, which the price-multiplier fields are the lever for if it proves too strong.

Also fixed 2026-09-21: the shop was opening and closing within a single frame and was
therefore never visible at all. See the decision log in `Docs/10_OPEN_POINTS.md`.
