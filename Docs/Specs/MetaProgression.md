# Meta Progression Specification

## Purpose

Definiert Meta Progression als dauerhaften Fortschritt außerhalb einzelner Runs.

Meta Progression soll Replayability, langfristige Ziele und kontrollierte Content-Freischaltung erzeugen.

## Design Goals

- Dauerhafte Motivation zwischen Runs
- Unlocks, die Wahlbreite erhöhen statt nur rohe Macht aufzuschichten
- Gute Verzahnung mit Main Menu, SaveSystem und Content-Pools
- Klare Trennung von Run-Fortschritt und persistentem Fortschritt
- Datengetriebene Erweiterbarkeit für neue Unlock-Pfade

## Gameplay

Nach oder zwischen Runs erhält der Player Meta-Currency, Unlock-Fortschritt oder andere persistente Belohnungen.

Diese werden genutzt, um neue Cards, Towers, Weapons oder Startoptionen freizuschalten.

Meta Progression soll neue Build-Möglichkeiten eröffnen und das Spiel langfristig verbreitern, ohne die Kernherausforderung zu entwerten.

Der Zugriff auf Meta Progression erfolgt außerhalb aktiver Combat-Situationen über dafür vorgesehene Menüs oder Post-Run-Flows.

## Rules

- Meta Progression ist vollständig getrennt vom aktuellen Run-Zustand.
- Unlocks sollen primär Auswahlbreite, Startvielfalt oder kontrollierte Komfortverbesserungen bieten.
- Dauerhafte Boni dürfen frühe Runs unterstützen, aber keine dominante Pflichtgrind-Spirale erzeugen.
- Neue freigeschaltete Inhalte werden kontrolliert in relevante Content-Pools eingespeist.
- Meta-Entscheidungen müssen per Controller vollständig bedienbar sein.
- Fortschritt wird persistent, nachvollziehbar und versionierbar gespeichert.
- Neue Meta-Systeme dürfen bestehende Run-Regeln nicht stillschweigend überschreiben.

## Data Model

Meta-Daten werden über ScriptableObjects und getrennte Save-Domains verwaltet.

- MetaCurrencyData: persistente Currency-Typen und Anzeigeinformationen
- UnlockData: freischaltbarer Inhalt, Kosten, Voraussetzungen und Zielsystem
- UnlockTrackData: thematische Progressionspfade oder Gruppen von Unlocks
- StartLoadoutData: freischaltbare Startoptionen für neue Runs
- ProgressRewardData: Regeln für Meta-Belohnungen aus Run-Abschlüssen oder Erfolgen

## Technical Design

Ein MetaProgressionService wertet persistente Fortschrittsdaten aus und schaltet Inhalte zentral frei.

Er kommuniziert über Events wie OnMetaCurrencyGained, OnUnlockPurchased, OnUnlockGranted und OnMetaProgressionLoaded.

Content-Pools für Cards, Weapons oder Towers fragen freigeschaltete Inhalte über definierte Schnittstellen ab.

Speicherung, Versionswechsel und Integritätsprüfung erfolgen über SaveSystem.

Der erste MainMenu-Slice nutzt einen echten MetaProgressionService mit persistenter Meta-Currency, Unlock-Katalog und controller-freundlicher Route-UI, damit Meta Progression bereits als funktionaler Zielpfad im Main Menu verankert ist.

Im ersten Slice besitzen Weapon-bezogene Unlocks bereits einen echten Inhaltspfad, weil PlayerCombatController die bevorzugte Weapon aus MetaProgressionSaveData und WeaponCatalogData auflösen kann.

Breitere Unlock-Pfade für Cards, Towers und Start Loadouts sind im Datenmodell desselben Services bereits verankert und können über UnlockDefinition-Daten ohne neue Grundarchitektur aktiviert werden.

## Dependencies

- SaveSystem
- MainMenu
- Cards
- Weapons
- Towers
- UI
- Audio

## UI

Meta-UI zeigt mindestens:

- verfügbare Meta-Currency
- freischaltbare Inhalte
- Voraussetzungen und Effekte eines Unlocks
- Fortschritt in einem Track oder Kategoriesystem

Die Bedienung muss klar, schnell und ohne Mauspflicht funktionieren.

## Audio

Wichtige Unlock-Momente und Kaufbestätigungen erhalten deutliches, positives Audio-Feedback.

Fehlgeschlagene Käufe oder gesperrte Inhalte werden eindeutig, aber zurückhaltend signalisiert.

## Visual

Meta Progression benötigt eine starke visuelle Trennung vom aktiven Run.

Neue Unlocks und zukünftige Ziele sollen motivierend, aber nicht überladen präsentiert werden.

## Balancing

Meta Progression soll Fortschritt spürbar machen, ohne dass unerfahrene Spieler erst viele Stunden farmen müssen, um Spaß zu haben.

Neue Inhalte sollen häufiger aufregender sein als reine Prozentboni.

Freischaltkurven müssen so gestaltet sein, dass regelmäßig neue Möglichkeiten sichtbar werden.

## Open Questions

- Welche Arten permanenter Boni sind zulässig, ohne Build Diversity zu beschädigen?
- Soll Meta Progression eher linear, verzweigt oder hybrid aufgebaut sein?
- Welche Inhalte sind zum Start bereits freigeschaltet und welche dienen als frühe Ziele?

## Acceptance Criteria

- Meta Progression speichert dauerhafte Unlocks getrennt von Run-Daten.
- Unlocks können neue Cards, Towers, Weapons und Startoptionen kontrolliert freischalten.
- Das System bleibt controller-freundlich und datengetrieben.
- Persistente Belohnungen fördern Replayability, ohne das Kernspiel zu trivialisieren.
- Neue Unlock-Pfade lassen sich ohne Architekturänderung ergänzen.
- Meta Progression ist im Main Menu als persistentes Currency- und Unlock-Ziel erreichbar.
- Weapon-, Tower-, Card- und Start-Loadout-Unlocks können im ersten Slice bereits auf konkrete Datenobjekte verweisen.

---

## Split into two currencies (2026-09-21)

Meta progression is now two systems with two currencies, decided 2026-09-21:

| Currency | Spent on | Spec |
| --- | --- | --- |
| **Skill Points** | a branching skill tree, plus an endless Ascension track | [SkillTree.md](SkillTree.md) |
| **Alloy** | unlocks, and permanent per-weapon and per-tower stat upgrades | [Upgrades.md](Upgrades.md) |

**Alloy is this spec's existing meta currency, renamed and extended.** It is not a fourth pool.
`MetaProgressionService`'s existing run-completion grant and its `IsUnlockSatisfied` gating are
kept exactly as they are; stat upgrades are added as a second sink beside unlocks.

The rule this spec already states - that unlocks widen the option pool rather than raising base
stats - stays binding for **unlocks**. Stat upgrades in `Upgrades.md` are the one deliberate
exception, and they carry hard per-stat ceilings so that permanent progress can never outweigh
an in-run build.

**Per character.** Each playable character has its own skill tree and is levelled separately.
A second character starts its tree at zero rather than inheriting the first character's.
