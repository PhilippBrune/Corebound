# VFX Specification

Seven Specs verlinken dieses Dokument (Cards, Combat, Core, Enemies, Graphics, Stages, Towers). Es
existierte bis 2026-09-22 nicht — jeder dieser Links lief ins Leere. Diese Datei ist der Einstieg in
die VFX-Dokumentation und beschreibt die Regeln, die für *alle* Effekte gelten; die konkreten
Atlas-Aufteilungen stehen in den drei Detaildokumenten.

## Die drei Detaildokumente

| Dokument | Inhalt |
| --- | --- |
| [VFX_combat_vfx_sheet_essentials.md](VFX_combat_vfx_sheet_essentials.md) | der gemeinsame Combat-Atlas `Assets/Art/Combat/VFX/combat_vfx_sheet_essentials.png`: Treffer-Funken, Death-Burst, Burn- und Freeze-Layer, Core-Flare, Reward-Flash, Tower-Aktivierungspuls |
| [VFX_stage1_ground_tileset.md](VFX_stage1_ground_tileset.md) | Boden-Atlas der Stage 1, Zellenzuordnung und Kantenregeln |
| [VFX_stage1_prop_set.md](VFX_stage1_prop_set.md) | Prop-Atlas der Stage 1, Platzierung und Sortierung |

## Gemeinsame Regeln

- **Eine Effektfamilie pro Ereignisart.** Treffer, Tod, Status, Core-Schaden, Reward und
  Tower-Aktivierung benutzen jeweils dieselbe Form- und Farbsprache, unabhängig davon, wer sie
  auslöst. Ein Burn auf einem Enemy sieht aus wie ein Burn auf dem Boss.
- **Lesbarkeit vor Spektakel.** Gefahrensignale (Enemy-Telegraph, Core-Schaden, Boss-Angriff) haben
  Vorrang vor dekorativen Effekten und dürfen von diesen nie verdeckt werden.
- **Sorting-Reihenfolge**, von hinten nach vorn: Boden, Props, Player-Projektile (-3),
  Boss-Telegraphs (-1), Einheiten, Enemy-Projektile (2), Treffer-VFX, Weltraum-Prompts und
  Schadenszahlen (58-60).
- **Pooling ist Pflicht.** Jeder wiederkehrende Effekt läuft über einen Pool, nie über
  `Instantiate` pro Treffer.
- **Farbe trägt Bedeutung**: Gold für kritische Treffer, Orange-Rot für Burn, Cyan für Freeze und
  Verlangsamung, Violett für Boss- und Elite-Telegraphs, Grün nur für Heilung und Reparatur.
- Effekte werden aus Atlas-Zellen per Pixel-Rect geschnitten; die Zuordnung Zelle → Funktion steht an
  genau einer Stelle im Code und im zugehörigen Dokument (siehe
  [Graphics.md](Graphics.md), Technical Design).

## Offene Punkte

- Für Stage 2-5 existieren noch keine eigenen Boden- und Prop-Atlanten; sie nutzen Stage 1 mit
  Tönung (siehe [Stages.md](Stages.md)).
- Zone Denier und Relay Warden haben keine eigenen Sprites und damit auch keine eigenen Telegraph-
  Effekte (siehe [Enemies.md](Enemies.md)).
