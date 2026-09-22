# Factory Specification

## Purpose

Definiert das Factory System als Produktions- und Konvertierungsschicht innerhalb eines Runs.

Factory bedeutet Produktionsketten zwischen bestehenden Systemen, nicht vollständige Automation oder ein separates Logistikspiel.

## Design Goals

- Starke Synergy zwischen Tower, Resource, Card und Economy
- Wenige Grundregeln mit vielen Kombinationsmöglichkeiten
- Verständliche Produktionsketten statt Mikromanagement-Komplexität
- Unterstützung des Combat-Flows statt Ablenkung davon
- Datengetriebene Erweiterbarkeit für neue Produktionsmodule

## Gameplay

Der Player baut seine Production Chain über Entscheidungen in Tower-Bau, Card-Auswahl und Resource-Nutzung auf.

Ein Tower kann beispielsweise Resources erzeugen, die für Cards oder Tower Upgrade ausgegeben werden, wodurch weitere Production Chain-Boni entstehen.

Factory-Macht entsteht aus Synergy und Timing, nicht aus separatem Bauen von Förderbändern oder Logistiknetzen.

Das System soll Runs unterschiedlich strukturieren, ohne den Player aus dem aktiven Combat zu lösen.

### Die Vision-Beispielkette

Project Vision beschreibt die Factory Philosophy anhand einer konkreten Beispielkette, die als Referenzmodell für jede Production Chain in diesem Spec gilt:

`Türme erzeugen Ressourcen → Ressourcen kaufen Karten → Karten verbessern Türme → Türme erzeugen mehr Ressourcen → Neue Waffen → Stärkerer Build`

Diese Kette ist explizit ein **geschlossener Kreislauf mit Verstärkung**, kein linearer Einwegpfad: Der letzte Schritt (stärkerer Build) verbessert wieder die Fähigkeit, den ersten Schritt (Ressourcenerzeugung) effektiver zu nutzen. Jede Production Chain in diesem Spiel sollte an mindestens einer Stelle in diesen Kreislauf einspeisen oder aus ihm herausgreifen — Factory ist damit die Klammer, die Towers, Resources, Cards und Weapons über den Run hinweg gegenseitig verstärkt, statt vier isolierte Systeme zu sein.

## Rules

- Factory bleibt vollständig innerhalb der bestehenden Run-Systeme.
- Es gibt kein separates Factory-Minispiel und keine vollständige Offscreen-Automation.
- Produktionsschritte basieren auf klaren Triggern wie Kills, Zeitintervallen, Wave-Abschluss oder Tower-Aktivität.
- Jede Production Chain muss verständlich darstellbar und ökonomisch bewertbar sein.
- Factory-Synergies dürfen stark eskalieren, benötigen aber Grenzen gegen runaway snowballing.
- Factory darf den Player nicht in passives Warten statt aktives Spielen drängen.
- Neue Factory-Inhalte erweitern Tower-, Card- oder Resource-Regeln statt neue Kerninterfaces zu erzwingen.

## Data Model

Factory-Daten werden über ScriptableObjects beschrieben.

- ProductionChainData: Definition eines zusammenhängenden Produktionspfads inklusive seiner Tier-Stufen
- ProductionNodeData: einzelner Erzeuger oder Verstärker innerhalb einer Kette, mit seinem Trigger (Kill, Tower-Puls, Wave-Ende) als Feld
- ProductionModifierData: Multiplikatoren, Caps und Synergy-Bedingungen

Eine Resource-Conversion-Stufe gibt es nicht; der erste Scope kennt nur Erzeugung und Multiplikation.

## Technical Design

Ein Factory-Service wertet registrierte Producer und Consumer aus und löst Produktionsereignisse zentral aus.

Er hört auf Events wie OnEnemyKilled, OnWaveCompleted, OnTowerBuilt und OnCardApplied und erzeugt daraus standardisierte Outputs.

Factory sendet eigene Events wie OnProductionTriggered, OnProductionBlocked und OnResourceGenerated.

Die Berechnung bleibt in testbaren C# Klassen und nicht in einzelnen Tower- oder UI-Komponenten verteilt.

## Dependencies

- Resources
- Towers
- Cards
- Economy
- Waves
- UI
- Audio

## UI

Factory-UI zeigt nur die relevanten Produktionsinformationen:

- aktive Produktionsquellen
- wichtigste Multiplikatoren
- jüngste Generierung oder Konvertierung
- erkennbare Synergy-Hinweise

Die Darstellung darf nicht zu einer komplexen Tabellenoberfläche entarten.

## Audio

Factory-relevante Generierung oder bedeutsame Chain-Aktivierungen erhalten kurzes, informatives Feedback.

Hochfrequente Produktion muss gruppiert oder priorisiert werden, um Audio-Spam zu verhindern.

## Visual

Produktionsereignisse werden als unterstützende, nicht dominierende Signale dargestellt.

Der Player soll erkennen können, dass eine Chain funktioniert, ohne dass das Combat-Bild überladen wird.

## Balancing

Factory soll alternative Build-Routen ermöglichen, aber keinen sicheren Sieg ohne Combat-Leistung liefern.

Resource-Erzeugung braucht Caps, diminishing returns oder andere Grenzen gegen exponentielle Eskalation.

Factory-Builds sollen sich anders anfühlen als reine Damage-Builds, ohne das gesamte Spieltempo zu brechen.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Der erste Factory-Umfang nutzt OnEnemyKilled, zeitbasierten Tower-Pulse und OnWaveCompleted als Production Triggers.
- Resource Conversion ist nicht Teil des ersten spielbaren Umfangs; der Startfokus liegt auf Erzeugung und Multiplikation. Das deckt die ersten beiden Schritte der Vision-Beispielkette ab (Türme erzeugen Ressourcen, Ressourcen kaufen Karten über Shop/Economy); der Rückkopplungsschritt „Karten verbessern Türme" existiert bereits über bestehende Card-Effekte (`TowerDamageMultiplier`), eine explizite Resource-Conversion-Stufe bleibt offen für eine spätere Erweiterung.
- Snowballing wird über Soft Caps auf doppelte Resource-Produzenten und harte Datenobergrenzen für Wave-Boni begrenzt.
- **Jeder Tower im Katalog besitzt einen Tower-Pulse-Knoten** (Stand 2026-09-22: Ballista, Support, Mortar, Sentinel). Die Regel dahinter: ein neuer Tower-Typ bringt entweder einen eigenen Produktionsknoten mit, oder die Spec begründet, warum er reiner Kampfturm ist. Sonst entsteht wieder die Lücke von 2026-09-09, als nur der Support-Tower produzierte.

## Acceptance Criteria

- Factory erweitert bestehende Tower-, Resource- und Card-Systeme statt ein separates Spiel zu eröffnen.
- Produktionslogik ist datengetrieben und eventbasiert.
- Production Chains sind verständlich darstellbar und balancierbar.
- Factory-Builds unterstützen aktives Spielen statt passives Warten.
- Neue Produktionsmodule lassen sich ohne Architekturänderung ergänzen.

---

## Chain depth and visibility (2026-09-21)

The Factory's depth question is answered, and the answer is **chain tiers**.

A `ProductionChainData` declares ordered tiers, each with a condition: towers built, total Scrap
that chain has produced, and waves cleared. A chain's current tier switches on every node whose
`RequiredChainTier` is at or below it. Nodes default to tier 0, so every chain authored before
this keeps producing from the first wave.

> more towers built -> the chain reaches a higher tier -> more of its nodes produce -> more
> Scrap -> more towers

**Amplifier** nodes, added 2026-09-20, multiply every *other* node in their chain while their
tower condition holds. Together the two make the vision's loop genuinely amplifying rather than
merely repeating, with no transport layer, no logistics graph and no second interface - which
keeps this spec's own "kein separater Automatisierungs-Simulator" rule intact.

**Snowball guards**, since this spec names that risk directly: an amplifier never amplifies
itself, bonuses from multiple amplifiers add rather than multiply, and every result still passes
through `MaxAmountPerTrigger`.

**Visibility.** `FactoryService.DescribeActiveProduction()` produces one line - current tier,
active node count, whether an amplifier is live, and what the next tier needs - which
`WaveStatePresenter` appends to the Intermission HUD line. It reports the *next* requirement on
purpose: knowing that two more towers would tier the chain up is the investment decision the
Factory exists to create. This closes vision Section 5.5's requirement that the player be able
to recognise a system is working.
