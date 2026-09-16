# Environment Main Assets Specification

## Purpose

Definiert die gemeinsamen Environment-Prop-Assets des ersten Vertical Slice.

Die Props erweitern die Arena um wiederverwendbare industrielle, mechanische und sci-fi Elemente und dienen der visuellen Strukturierung der Umgebung.

## Design Goals

- Wiederverwendbare Environment-Props statt vieler isolierter Einzelassets
- Klare Silhouetten und schnelle Erkennbarkeit
- Konsistente industrielle Sci-Fi-Bildsprache
- Unterschiedliche Prop-Silhouetten fuer eine abwechslungsreiche Arena
- Sinnvolle Platzierung innerhalb des Arena-Ground-Tilesets
- Visuelle Vielfalt durch mechanische, elektrische, strukturelle und dekorative Elemente

## Gameplay

Environment-Props dienen primaer der visuellen Strukturierung und Kontextualisierung der Arena.

Sie koennen industrielle Infrastruktur, Energieversorgung, technische Systeme, Ruinen, Lagerung und mechanische Komponenten darstellen.

Die konkreten Gameplay-Funktionen einzelner Props sind durch diesen Asset-Satz nicht festgelegt, sofern sie nicht explizit als solche definiert werden.

## Rules

- Props priorisieren klare Silhouetten und schnelle Erkennbarkeit.
- Einzelne Props muessen visuell voneinander unterscheidbar bleiben.
- Mechanische, elektrische und strukturelle Details sollen konsistent mit der Arena-Umgebung wirken.
- Beschaedigte Varianten duerfen bewusst asymmetrisch und fragmentiert wirken.
- Props sollen nicht mit Combat-VFX verwechselt werden.
- Props sollen die Lesbarkeit von Player, Enemies, Towers und Combat-Effekten nicht unnoetig beeintraechtigen.
- Mehrere Exemplare desselben Props duerfen wiederverwendet werden.
- Varianten innerhalb einer Prop-Kategorie duerfen kombiniert werden.

## Data Model

- EnvironmentPropAtlasData: Sprite-Atlas, Slice-Konventionen und Importregeln
- EnvironmentPropData: Typ, Variante, Groesse und visuelle Eigenschaften
- EnvironmentPropPlacementData: Positionierung und Ausrichtung innerhalb einer Arena
- EnvironmentPropVariantData: Varianten innerhalb einer Prop-Familie
- EnvironmentPropCategoryData: Gruppierung nach visueller oder funktionaler Kategorie

## Technical Design

Der aktuelle Environment-Prop-Atlas ist:

C:\Users\Philipp\Documents\Corebound\Assets\Art\Environment\Arena\stage1_prop_set.png

Der Atlas enthaelt einen 3 x 7 Grid-Aufbau mit insgesamt 21 dokumentierten Asset-Slots.

Die Grid-Koordinaten dienen zur eindeutigen Identifikation der Assets und bestimmen nicht automatisch deren Gameplay-Funktion.

Runtime-Systeme sollen einzelne Atlasregionen wiederverwenden, anstatt fuer jede Platzierung neue Texturen anzulegen.

## Dependencies

- Arena
- Stages
- Graphics
- Combat
- Player
- Enemies
- Towers
- Core

## Visual

Cheat Sheet fuer stage1_prop_set.png, von oben links nach unten rechts:

### Row 1 - Top

- Rounded Pylon: abgerundeter industrieller Pylon.
- Bulkier Segmented Pylon: groesserer, massiverer Pylon mit segmentierter Struktur.
- Angular Bulk Pylon: massiver Pylon mit kantiger geometrischer Form.
- Broken/Damaged Pylon (Sparking Wires): beschaedigter Pylon mit sichtbaren Kabeln und Funken.
- Blue Crystal Energy Hub: blau leuchtender Kristall-Energie-Hub.
- Electrical Generator/Compressor: elektrischer Generator beziehungsweise Kompressor.
- Ruined Pylon Structure: zerstoerte oder stark beschaedigte Pylon-Struktur.

### Row 2 - Middle

- Pile of Broken Gears and Shafts: Haufen aus zerbrochenen Zahnrädern und Wellen.
- Complex Mechanical Generator/Engine: komplexe mechanische Generator-/Maschinenstruktur.
- Old Computer Terminal (Broken Screen): altes Computerterminal mit beschaedigtem Bildschirm.
- Energy Terminal/Relay Panel: Energie-Terminal beziehungsweise Relay-/Verteilerpanel.
- Large Complex Pipe Junction: grosse komplexe Rohrverbindung.
- Vertical Pipes Emerging from the Ground: vertikale Rohre, die aus dem Boden hervortreten.
- Right-Angle Pipe Segments: rechtwinklige Rohrsegmente.

### Row 3 - Bottom

- Stack of Three Cargo Crates: Stapel aus drei Frachtkisten.
- Large Concrete Barrier Block: grosser Beton-/Barriereblock.
- Small Pile of Damaged Metal Girders: kleiner Haufen beschaedigter Metalltraeger.
- Damaged Pipes / Extended Objects Area: beschaedigte Rohre beziehungsweise laengliche technische Objekte.
- Horizontal Pipe Runs: horizontale Rohrleitungen.
- Loose Cables, Small Piles of Scrap: lose Kabel und kleine Schrottansammlungen.
- Levers/Switches: technische Hebel beziehungsweise Schalter.

## Grid Coordinate Key

### Row 1 (X=1) - Top

- **(1,1):** Rounded Pylon
- **(1,2):** Bulkier Segmented Pylon
- **(1,3):** Angular Bulk Pylon
- **(1,4):** Broken/Damaged Pylon (Sparking Wires)
- **(1,5):** Blue Crystal Energy Hub
- **(1,6):** Electrical Generator/Compressor
- **(1,7):** Ruined Pylon Structure

### Row 2 (X=2) - Middle

- **(2,1):** Pile of broken gears and shafts
- **(2,2):** Complex mechanical generator/engine
- **(2,3):** Old computer terminal (broken screen)
- **(2,4):** Energy terminal/relay panel
- **(2,5):** Large complex pipe junction
- **(2,6):** Vertical pipes emerging from the ground
- **(2,7):** Right-angle pipe segments

### Row 3 (X=3) - Bottom

- **(3,1):** Stack of three cargo crates
- **(3,2):** Large Concrete Barrier Block
- **(3,3):** Small pile of damaged metal girders
- **(3,4):** Damaged pipes / extended objects area
- **(3,5):** Horizontal pipe runs
- **(3,6):** Loose cables, small piles of scrap
- **(3,7):** Levers/switches

## Asset Categories

- Pylon Structures
- Energy and Electrical Devices
- Mechanical Machinery
- Computer and Technical Terminals
- Pipe and Infrastructure Elements
- Cargo and Storage Elements
- Barriers and Structural Elements
- Scrap and Ruined Metal
- Cables and Small Technical Debris
- Levers and Switches

## Placement

Environment-Props werden auf der Arena-Ground-Flaeche platziert und koennen je nach Asset unterschiedlich ausgerichtet und kombiniert werden.

Grosse Props wie Pylons, Generatoren, Maschinen und Betonbarrieren koennen als visuelle Landmarken oder groessere Environment-Elemente eingesetzt werden.

Kleinere Elemente wie Kabel, Schrott, Zahnräder, Metalltraeger und Schalter koennen zur Detailierung einzelner Bereiche verwendet werden.

Rohr- und Infrastruktur-Assets koennen kombiniert werden, um zusammenhaengende technische Strukturen darzustellen.

Beschaedigte und ruinierte Assets koennen gezielt eingesetzt werden, um unterschiedliche Bereiche innerhalb einer Stage visuell zu unterscheiden.

## Visual Consistency

Alle Main Assets sollen als zusammengehoeriger Environment-Asset-Satz wahrgenommen werden.

Besonders wichtig ist die konsistente Verbindung von:

- Industriellen Metallstrukturen
- Mechanischen Maschinen
- Elektrischen Systemen
- Energie-Komponenten
- Rohrleitungen
- Beschaedigten und zerstoerten Strukturen
- Technischem Schrott
- Lagerungs- und Barriereelementen

Die Props sollen gemeinsam mit dem Arena-Ground-Tileset eine konsistente Environment-Language bilden.

## Balancing

Environment-Props beeinflussen primaer die visuelle Signaldichte der Arena.

Die Platzierung soll so kontrolliert werden, dass:

- Gameplay-relevante Bereiche frei und lesbar bleiben
- Player und Enemies nicht durch Props verdeckt werden
- Combat-VFX sichtbar bleiben
- grosse Props als Landmarken funktionieren koennen
- kleine Props die Arena detailreicher machen, ohne visuelles Rauschen zu erzeugen
- wiederholte Props nicht zu stark als sichtbares Muster auffallen

## Open Questions

Keine offenen Fragen fuer den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Der erste Slice nutzt einen gemeinsamen Environment-Prop-Atlas.
- Der aktuelle Atlas ist `stage1_prop_set.png`.
- Der Atlas wird als 3 x 7 Grid mit 21 dokumentierten Main-Asset-Slots behandelt.
- Die Grid-Koordinaten dienen als eindeutige Referenz fuer die einzelnen Assets.
- Die Assets werden als wiederverwendbare Environment-Props behandelt.
- Beschaedigte und ruinierte Varianten sind eigenstaendige visuelle Asset-Varianten.
- Props koennen innerhalb der Arena wiederverwendet und kombiniert werden.
- Props sollen gemeinsam mit dem Arena-Ground-Tileset eine konsistente industrielle Sci-Fi-Umgebung bilden.

## Acceptance Criteria

- Der Environment-Prop-Atlas besitzt eine klar dokumentierte Aufgabenverteilung.
- Alle 21 Grid-Slots des 3 x 7 Source-Atlases sind dokumentiert.
- Jedes Main Asset besitzt eine eindeutige Grid-Koordinate.
- Die Assets koennen als wiederverwendbare Environment-Props innerhalb der Arena platziert werden.
- Die verschiedenen Prop-Familien bleiben visuell unterscheidbar.
- Grosse und kleine Props koennen gemeinsam fuer Environment-Detaillierung eingesetzt werden.
- Props beeintraechtigen die Lesbarkeit von Player, Enemies und Combat-VFX nicht unnoetig.
- Die visuelle Sprache des Prop-Sets bleibt konsistent mit dem Arena-Ground-Tileset.
