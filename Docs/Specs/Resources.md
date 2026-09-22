# Resources Specification

## Purpose

Definiert Resources als sammelbare und ausgebbare Werte innerhalb des Runs.

Resources verbinden Combat, Factory, Towers, Cards und Economy zu einer gemeinsamen Entscheidungsbasis.

## Design Goals

- Kurze, häufige Belohnungsschleifen
- Klar lesbare Gewinne, Verluste und Verwendungen
- Flexible Unterstützung für mehrere Resource-Typen
- Starke Synergy mit Combat- und Factory-Systemen
- Saubere Trennung zwischen Run- und Meta-Ressourcen

## Gameplay

Resources entstehen durch besiegte Enemies, Tower-Effekte, Card-Boni oder andere klar definierte Systeme.

Der Player sammelt Resources direkt in der Arena oder erhält sie über explizite Gutschriften.

Resources werden für Tower-Bau, Upgrades, Card-Käufe, Shop-Entscheidungen und andere Run-bezogene Ausgaben verwendet.

Das Resource-System ist der zentrale Übersetzer von Kampfleistung in Build-Fortschritt.

## Rules

- Resources besitzen klar definierte Quellen und Senken.
- Jeder Resource-Typ hat einen eindeutigen Zweck oder Wertkontext.
- Run-Resources und Meta-Currencies dürfen nicht dieselbe Speicher- oder Balancing-Logik teilen.
- Resource-Gewinne müssen sofort lesbar und nachvollziehbar sein.
- Aufgesammelte Resources verändern den Wallet-Zustand atomar und eventbasiert.
- Resource-Drops dürfen die Arena nicht unlesbar machen.
- Neue Resource-Typen benötigen einen klaren Systemnutzen und keine reine Komplexitätssteigerung.

## Data Model

Resource-Daten werden über ScriptableObjects verwaltet.

- ResourceData: Identität, Icon, Farbe, Kategorien und Anzeigeregeln

Drop-Mengen stehen auf `EnemyData` und `WaveRewardData`, Erzeugerregeln in den Factory-Knoten
(siehe [Factory.md](Factory.md)), Preise bei den Angeboten selbst, und das Sammelverhalten in
`ResourcePickupController`. Eigene Assets dafür gibt es nicht.

## Technical Design

Ein Resource-Service verwaltet Wallets, Droplogik und Transaktionen für den aktuellen Run.

On-ground Pickups nutzen Pooling und senden bei Sammlung Events wie OnResourceCollected.

Andere Systeme interagieren mit Resources über Events oder Service-Schnittstellen wie OnResourceDropped, OnResourceSpent und OnResourceWalletChanged.

Persistente Speicherung erfolgt ausschließlich über SaveSystem und nicht direkt über Gameplay-Komponenten.

Der erste Vertical Slice nutzt einen save-gebundenen ResourceWalletService. Combat-, Reward- und Intermission-Hooks schreiben darüber atomar in RunSaveData, und Card-Modifikatoren können Resource-Gewinne bereits direkt beeinflussen.

Mit dem ersten Wave-Loop entstehen Resource-Gutschriften nun über echte Enemy-Kills innerhalb von Waves und über definierte WaveRewardData-Abschlüsse.

## Dependencies

- Combat
- Enemies
- Factory
- Towers
- Cards
- Economy
- UI
- Audio
- SaveSystem

## Linked Slice Files

- [Wave Boss Balance Slice](../Slices/14_WAVE_BOSS_BALANCE_SLICE.md)
- [Bosses Stage 1 Slice](../Slices/12_BOSSES_STAGE1_SLICE.md)
- [Elite Variants Slice](../Slices/13_ELITE_VARIANTS_SLICE.md)

## UI

Resource-UI zeigt mindestens:

- aktuelle Wallet-Werte
- momentane Gewinne oder Ausgaben
- Kostenrelevanz bei Kaufentscheidungen
- klare Unterscheidung mehrerer Resource-Typen

## Audio

Sammeln, große Belohnungen und Ausgaben erhalten eindeutige Audio-Rückmeldungen.

Hochfrequente kleine Pickups müssen über Mix oder Gruppierung entschärft werden.

## Visual

Resource-Drops müssen in der Arena schnell identifizierbar und vom restlichen Combat-Bild unterscheidbar sein.

Sammel- und Ausgabesignale sollen den Flow bestätigen, ohne das Sichtfeld zu überfrachten.

## Balancing

Resources sollen regelmäßig genug fließen, damit echte Entscheidungen entstehen, aber nicht so reichlich, dass Kosten bedeutungslos werden.

Resource-Typen müssen unterschiedliche Entscheidungsspannung schaffen statt nur parallele Zahlen zu erzeugen.

Frühe Economy soll verständlich bleiben und nicht durch zu viele Resource-Kanäle überfordern.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Der erste Scope nutzt genau eine primäre Run Resource und eine davon getrennte Meta Currency.
- Die primäre Run Resource besitzt im ersten Scope kein hartes Wallet-Cap.
- Resource-Sammlung kombiniert kurze Auto-Pickup-Reichweite im Combat mit vollständigem Wave-Ende-Vacuuming.

## Acceptance Criteria

- Resources besitzen klar definierte Quellen, Senken und Anzeigen.
- Run-Resources sind sauber von Meta-Currencies getrennt.
- Resource-Gewinne und -Ausgaben werden eventbasiert und datengetrieben verarbeitet.
- Resource-Drops bleiben auch in intensiven Waves lesbar.
- Andere Systeme können neue Resource-Interaktionen ergänzen, ohne Kernlogik zu duplizieren.
- Reward- und Intermission-Hooks können Resource-Gewinne im ersten Slice direkt mit SaveSystem synchron halten.

---

## Still one in-run resource (2026-09-21)

Scrap remains the only **in-run** resource. `ResourceWalletService` holds one `int` and that is
still correct.

The second and third currencies added 2026-09-21 - Skill Points and Alloy - are **permanent**,
live in `MetaProgressionSaveData`, and never enter the run wallet. Nothing in this spec changes
because of them, which is the intended outcome: the run economy stays single-currency.

`resource_primary_icon.png` is used both for the world pickup and, since 2026-09-21, beside the
Scrap count in the HUD.
