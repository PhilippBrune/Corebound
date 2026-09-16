# Arena Ground Tileset Specification

## Purpose

Definiert das modulare Ground-Tileset fuer die Arena des ersten Vertical Slice.

Das Tileset bildet die Grundlage fuer die Erstellung rechteckiger Arena-Flaechen aus wiederverwendbaren Boden-, Rand- und Corner-Tiles.

## Design Goals

- Modulare Erstellung unterschiedlich grosser Arena-Flaechen
- Wiederverwendbare Tiles statt vieler individueller Arena-Texturen
- Klare Trennung zwischen Arena-Grenzen und freien Innenflaechen
- Nahtloses Zusammensetzen benachbarter Tiles
- Unterstuetzung fuer unterschiedliche visuelle Varianten innerhalb derselben funktionalen Tile-Kategorie
- Wiederverwendung von Corner-Tiles durch Rotation
- Konsistente sci-fi / industrial Bodenoptik innerhalb der Arena

## Gameplay

Die Ground-Tiles definieren die begehbare Arena-Flaeche und deren sichtbare aeussere Grenzen.

Arena-Boundaries markieren die Grenzen des spielbaren Bereichs und duerfen nicht mit dekorativen Panel-Linien, Rissen, Metallfugen oder anderen Oberflaechendetails verwechselt werden.

Interior-/Free-Tiles besitzen keine aeussere Arena-Boundary und werden fuer die innere Flaeche der Arena verwendet.

Edge-Tiles werden verwendet, wenn eine Seite der Arena an die aeussere Grenze anschliesst.

Corner-Tiles werden verwendet, wenn zwei angrenzende Seiten der Arena gleichzeitig eine aeussere Grenze bilden.

## Rules

- Arena-Tiles werden als modulare Bausteine behandelt.
- Jede Tile besitzt eine klar definierte Boundary-Funktion.
- Top-Edge-Tiles besitzen eine Arena-Boundary entlang der oberen Seite.
- Bottom-Edge-Tiles besitzen eine Arena-Boundary entlang der unteren Seite.
- Left-Edge-Tiles besitzen eine Arena-Boundary entlang der linken Seite.
- Right-Edge-Tiles besitzen eine Arena-Boundary entlang der rechten Seite.
- Interior-/Free-Tiles besitzen keine aeussere Arena-Boundary.
- Corner-Tiles besitzen zwei angrenzende Arena-Boundaries.
- Benachbarte Tiles muessen an ihren Verbindungsseiten nahtlos zusammenpassen.
- Oberflaechendetails wie Metallplatten, Fugen, Kratzer und Beleuchtung sind Teil der visuellen Tile-Variante und keine zusaetzlichen Arena-Boundaries.
- Tile-Varianten innerhalb derselben funktionalen Kategorie duerfen zufaellig oder gezielt gemischt werden.
- Corner-Tiles muessen rotierbar sein.
- Ein vorhandener Corner-Tile kann durch Rotation fuer mehrere Arena-Ecken verwendet werden.
- Der Bottom-Right-Corner muss nicht als separates Asset vorhanden sein, wenn ein vorhandener Corner-Tile durch Rotation entsprechend ausgerichtet werden kann.

## Data Model

Arena-Ground-Daten werden als wiederverwendbare Tile- und Placement-Familien verstanden.

- ArenaTilesetData: Tileset, Slice-Konventionen und Importregeln
- ArenaTileData: Funktion, Boundary-Seiten, Variante und Rotationsmoeglichkeiten eines Tiles
- ArenaLayoutData: Anordnung und Groesse einer konkreten Arena
- ArenaBoundaryData: Definition der aeusseren spielbaren Arena-Grenzen
- ArenaTileVariantData: Zuordnung von visuellen Varianten innerhalb einer Tile-Kategorie

## Technical Design

Der aktuelle Arena-Ground-Atlas ist:

C:\Users\Philipp\Documents\Corebound\Assets\Art\Environment\Arena\stage1_ground_tileset.png

Das Tileset besteht aus einem 4 x 8 Raster mit insgesamt 32 Tile-Slots.

Die funktionale Position innerhalb des Source-Spritesheets entspricht dabei nicht zwingend der Funktion des Tiles im fertigen Arena-Layout.

Runtime-Systeme sollen einzelne Tiles aus dem Atlas wiederverwenden, anstatt fuer jede Arena-Groesse neue Texturen anzulegen.

Tiles koennen je nach Funktion horizontal, vertikal oder rotierend wiederverwendet werden.

Corner-Tiles duerfen rotiert werden, um verschiedene Arena-Ecken abzubilden.

## Dependencies

- Arena
- Stages
- Level Generation
- Graphics
- Combat
- Player
- Enemies
- Towers

## Visual

Cheat Sheet fuer stage1_ground_tileset.png, von oben links nach unten rechts:

### Row 1

- Top Left Arena Corner: oberer linker Arena-Corner mit Boundaries an der oberen und linken Seite.
- Top Edge Variant A: oberer Arena-Rand mit Boundary an der oberen Seite.
- Top Edge Variant B: oberer Arena-Rand mit Boundary an der oberen Seite.
- Top Edge Variant C: oberer Arena-Rand mit Boundary an der oberen Seite.
- Interior Free Variant A: freie Innenflaeche ohne aeussere Arena-Boundary.
- Top Edge Variant D: oberer Arena-Rand mit Boundary an der oberen Seite.
- Top Edge Variant E: oberer Arena-Rand mit Boundary an der oberen Seite.
- Top Right Arena Corner: oberer rechter Arena-Corner mit Boundaries an der oberen und rechten Seite.

### Row 2

- Left Edge Variant A: linker Arena-Rand mit Boundary an der linken Seite.
- Interior Free Variant B: freie Innenflaeche ohne aeussere Arena-Boundary.
- Interior Free Variant C: freie Innenflaeche ohne aeussere Arena-Boundary.
- Interior Free Variant D: freie Innenflaeche ohne aeussere Arena-Boundary.
- Interior Free Variant E: freie Innenflaeche ohne aeussere Arena-Boundary.
- Bottom Edge Variant A: unterer Arena-Rand mit Boundary an der unteren Seite.
- Interior Free Variant F: freie Innenflaeche ohne aeussere Arena-Boundary.
- Right Edge Variant A: rechter Arena-Rand mit Boundary an der rechten Seite.

### Row 3

- Left Edge Variant B: linker Arena-Rand mit Boundary an der linken Seite.
- Interior Free Variant F: freie Innenflaeche ohne aeussere Arena-Boundary.
- Interior Free Variant G: freie Innenflaeche ohne aeussere Arena-Boundary.
- Interior Free Variant H: freie Innenflaeche ohne aeussere Arena-Boundary.
- Interior Free Variant I: freie Innenflaeche ohne aeussere Arena-Boundary.
- Bottom-Corner Detail Variant A: Tile mit zusaetzlichen Boundaries in den unteren linken und unteren rechten Bereichen.
- Bottom-Corner Detail Variant B: Tile mit zusaetzlichen Boundaries in den unteren linken und unteren rechten Bereichen.
- Right Edge Variant B: rechter Arena-Rand mit Boundary an der rechten Seite.

### Row 4

- Bottom Left Arena Corner: unterer linker Arena-Corner mit Boundaries an der unteren und linken Seite.
- Bottom Edge Variant A: unterer Arena-Rand mit Boundary an der unteren Seite.
- Bottom Edge Variant B: unterer Arena-Rand mit Boundary an der unteren Seite.
- Bottom Edge Variant C: unterer Arena-Rand mit Boundary an der unteren Seite.
- Bottom Edge Variant D: unterer Arena-Rand mit Boundary an der unteren Seite.
- Multi-Corner Detail Variant A: Tile mit Boundaries an den oberen linken, oberen rechten, unteren linken und unteren rechten Bereichen.
- Multi-Corner Detail Variant B: Tile mit Boundaries an den oberen linken, oberen rechten, unteren linken und unteren rechten Bereichen.
- Multi-Corner Detail Variant C: Tile mit Boundaries an den oberen linken, oberen rechten, unteren linken und unteren rechten Bereichen.

## Tile Categories

Das Tileset besitzt folgende funktionale Tile-Familien:

- Top Left Arena Corner
- Top Right Arena Corner
- Bottom Left Arena Corner
- Bottom Right Arena Corner, erzeugt durch Rotation eines vorhandenen Corner-Tiles
- Top Edge Tiles
- Bottom Edge Tiles
- Left Edge Tiles
- Right Edge Tiles
- Interior / Free Tiles
- Bottom-Corner Detail Tiles
- Multi-Corner Detail Tiles

## Corner Rotation Rule

Corner-Tiles sind rotationssymmetrisch als wiederverwendbare Bausteine gedacht.

Nicht jede Arena-Ecke benoetigt deshalb ein eigenes Source-Asset.

Beispiel:

- Top Left Corner: Originalorientierung
- Top Right Corner: Corner-Tile entsprechend rotieren
- Bottom Left Corner: Corner-Tile entsprechend rotieren
- Bottom Right Corner: vorhandenen Corner-Tile entsprechend rotieren

Insbesondere der Bottom-Right-Corner muss nicht als eigener Tile-Slot im Source-Spritesheet existieren.

Das Runtime-/Layout-System darf einen vorhandenen Corner-Tile verwenden und ihn um 90°, 180° oder 270° drehen, sofern dadurch die erforderlichen Boundary-Seiten korrekt ausgerichtet werden.

## Tile Placement

Bei der Erstellung einer Arena werden die Tiles anhand ihrer Boundary-Funktion platziert.

Eine typische rechteckige Arena besteht aus:

- einem Corner-Tile oben links
- beliebig vielen Top-Edge-Tiles
- einem Corner-Tile oben rechts
- beliebig vielen Left-Edge-Tiles
- beliebig vielen Interior-/Free-Tiles
- beliebig vielen Right-Edge-Tiles
- einem Corner-Tile unten links
- beliebig vielen Bottom-Edge-Tiles
- einem rotierten Corner-Tile unten rechts

Interior-/Free-Tiles koennen innerhalb der Arena beliebig wiederholt und mit ihren Varianten gemischt werden.

Edge- und Corner-Tiles duerfen ebenfalls aus ihren jeweiligen Varianten gemischt werden, solange ihre Boundary-Funktion erhalten bleibt.

## Important Source Sheet Rule

Die Position eines Tiles im 4 x 8 Source-Spritesheet bestimmt nicht automatisch seine funktionale Verwendung.

Beispielsweise ist das Tile an Position (1,5) trotz seiner Position in der ersten Reihe ein Interior-/Free-Tile ohne obere Arena-Boundary.

Die in diesem Dokument definierte funktionale Kategorie ist massgeblich fuer die Verwendung des Tiles.

Das Source-Spritesheet ist somit als Asset-Atlas zu verstehen und nicht als direkte Darstellung einer fertigen Arena.

## Balancing

Arena-Ground-Tiles beeinflussen keine direkten Combat-Werte.

Die visuelle Tile-Dichte und Variation soll jedoch so kontrolliert werden, dass:

- die Arena als zusammenhaengende Flaeche lesbar bleibt
- Arena-Boundaries jederzeit eindeutig erkennbar sind
- dekorative Panel-Linien nicht mit Gameplay-Grenzen verwechselt werden
- wiederholte Tiles nicht zu stark als sichtbares Muster auffallen
- wichtige Gameplay-Elemente und Combat-VFX nicht durch Bodendetails ueberladen werden

## Open Questions

Keine offenen Fragen fuer den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Das erste Slice nutzt einen gemeinsamen Arena-Ground-Tileset statt vieler individueller Arena-Texturen.
- Das Tileset wird als 4 x 8 Source-Atlas mit 32 Tile-Slots behandelt.
- Interior-, Edge- und Corner-Tiles werden funktional getrennt.
- Tile-Varianten duerfen innerhalb ihrer jeweiligen Kategorie wiederverwendet werden.
- Corner-Tiles duerfen rotiert werden.
- Ein separater Bottom-Right-Corner ist nicht erforderlich, da dieser durch Rotation eines vorhandenen Corner-Tiles erzeugt werden kann.
- Die visuelle Position eines Tiles im Source-Atlas bestimmt nicht seine Gameplay-Funktion.

## Acceptance Criteria

- Der Arena-Ground-Atlas besitzt eine klar dokumentierte Aufgabenverteilung.
- Alle 32 Tile-Slots des 4 x 8 Source-Atlases sind funktional dokumentiert.
- Top-, Bottom-, Left- und Right-Edge-Tiles koennen fuer den Aufbau rechteckiger Arenen wiederverwendet werden.
- Interior-/Free-Tiles koennen ohne aeussere Arena-Boundary innerhalb der Arena wiederholt werden.
- Corner-Tiles koennen rotiert und fuer unterschiedliche Arena-Ecken wiederverwendet werden.
- Ein Bottom-Right-Corner kann ohne separates Source-Asset erzeugt werden.
- Benachbarte Tiles bilden eine konsistente und visuell zusammenhaengende Arena-Boundary.
- Die visuelle Unterscheidung zwischen dekorativen Oberflaechendetails und Gameplay-relevanten Arena-Boundaries ist dokumentiert.