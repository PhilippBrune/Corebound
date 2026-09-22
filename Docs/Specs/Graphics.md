# Graphics Specification

## Purpose

Definiert alle grafischen Anforderungen für den ersten spielbaren Vertical Slice.

Graphics unterstützt Readability, Combat-Lesbarkeit, Build-Entscheidungen und die Identität des Games innerhalb eines 2D-Unity-Projekts mit optionaler 2.5D-Präsentation.

## Design Goals

- Klare Top-Down-Lesbarkeit für Player, Core, Enemy, Tower und Projectile
- Visuelle Identität für Deck Builder, Tower Defense, Factory und Action Roguelite innerhalb eines konsistenten Stils
- 2D-Gameplay mit gezielter 2.5D-Tiefenwirkung ohne 3D-Kameraabhängigkeit
- Modulare Asset-Produktion für spätere Content-Erweiterungen
- Gute Kompatibilität mit Unity 6, URP 2D und controller-first UI-Flows

## Gameplay

Graphics vermittelt jederzeit, wo sich der Player befindet, welche Enemy-Angriffe gefährlich sind, welche Towers aktiv arbeiten und welche Reward- oder Card-Entscheidung gerade relevant ist.

Während Combat priorisiert die Darstellung Player, Core, Enemy-Telegraphs, Projectile-Flugbahnen und Status Effect-Zustände.

Zwischen Waves und in Menüs verschiebt sich der Fokus auf Card-Auswahl, Ressourcenstand, Build-Lesbarkeit und den nächsten Interaktionsschritt.

Das Grafiksystem muss sowohl die Action-Komponente als auch die Build- und Factory-Komponente lesbar tragen, ohne in ein separates 3D-Spielgefühl zu kippen.

## Rules

- Das Gameplay findet auf einer orthographischen 2D-XY-Ebene statt.
- 2.5D ist nur als Präsentationsschicht erlaubt, zum Beispiel über gemalte Volumenwirkung, Normal-Maps, Layer-Tiefe, Kontakt-Schatten oder gestapelte Sprite-Elemente.
- Es gibt keine frei drehbare 3D-Kamera und keine Abhängigkeit von 3D-Modelle-Gameplay.
- Alle gameplay-kritischen Objekte benötigen starke Silhouetten und klare Farbtrennung.
- Wichtige Zustände dürfen nicht ausschließlich über kleine UI-Elemente oder subtile Shader signalisiert werden.
- Text bleibt editierbarer UI-Text und wird nicht in Sprites eingebrannt.
- Alle Asset-Sets müssen modular in Varianten erweiterbar bleiben.

## Data Model

Grafikdaten werden soweit sinnvoll über ScriptableObjects und konsistente Importregeln verwaltet.

- GraphicsAssetCatalogData: zentrale Zuordnung von Sprite-Sets, UI-Skins, VFX-Sets und Themenpaketen
- SpriteSetData: zusammengehörige Sprites für Player, Enemy, Tower, Resource oder Environment
- CardVisualSetData: Card Frames, Card Back, Tag-Icons und Rarity-Farbcodes
- UiSkinData: Panels, Button Frames, Fokusmarker, Banner, Piktogramme und Typografie-Referenzen
- VfxSetData: Treffer-, Impact-, Spawn-, Warn- und Status-Effect-VFX
- ArenaThemeData: Boden, Randmodule, Props, Tower Slots und Core-Sockel für eine Stage

## Technical Design

Unity 6 URP 2D ist die technische Baseline für Rendering, 2D-Lights und Materialreaktionen.

Sprites werden mit konsistenten Pivots, Pixels-per-Unit-Regeln und klaren Sorting Layers importiert.

2.5D-Optik entsteht über gezeichnete Perspektivhinweise, Light-Response, Schattensprache, kleine Höhenstaffelungen in der Darstellung und optional normal-mapped Sprites, nicht über frei platzierte 3D-Meshes als Kernlösung.

UI-Grafiken werden für 9-Slice, variable Localized Text Längen und Fokuszustände vorbereitet.

VFX und Projektile werden auf Readability und Pooling-Kompatibilität ausgelegt.

Die aktuelle Familienzuordnung des Shared-VFX-Atlas unter Assets/Art/Combat/VFX/combat_vfx_sheet_essentials.png ist in der [VFX Specification](./VFX.md) dokumentiert.

Für Arena-Atlanten mit uneinheitlichem, nicht-quadratischem Layout (zum Beispiel Assets/Art/Environment/Arena/stage1_ground_tileset.png und stage1_prop_set.png) ist eine zur Laufzeit berechnete Grid-Aufteilung (Zeile/Spalte auf Pixel-Rect abgebildet, per `Sprite.Create` erzeugt) eine gültige, gleichwertige Alternative zu vorab in Unity gesliceten Einzel-Sprites. Ausschlaggebend ist, dass die Zuordnung von Grid-Zelle zu Gameplay-Funktion an einer Stelle im Code dokumentiert und nachvollziehbar bleibt, nicht die Wahl des Slicing-Mechanismus selbst. Für Atlanten mit unregelmäßigen, nicht rasterförmigen Ausschnitten (zum Beispiel Assets/Art/Environment/Arena/stage1_boundary_set.png) werden stattdessen die von Unity bereits vorgeschnittenen Einzel-Sprite-Rects direkt aus den Import-Metadaten übernommen und per explizitem Pixel-Rect referenziert.

## Dependencies

- MainMenu
- Player
- Combat
- Weapons
- Cards
- Towers
- Resources
- Factory
- Waves
- Enemies
- Bosses
- UI
- Tutorial
- Audio

## UI

Für UI werden mindestens folgende grafische Assets benötigt:

- Main Menu Hintergrund und Logo
- Button Frames und Fokusmarker
- Gameplay-HUD-Rahmen und Ressourcensymbole
- Card Frames nach Rarity
- Card-Tag-Icons
- Wave-, Boss- und Tutorial-Banner
- Panels für Settings, Rewards, Tutorial und Modale Bestätigungen

Alle UI-Assets müssen für Controller-Navigation, Fokuswechsel und Lokalisierung robust sein.

## Audio

Graphics und Audio müssen dieselbe Prioritätensprache sprechen.

Große Warnungen wie Boss-Telegraphs, Core-Schaden, Card-Reveals oder Reward-Momente benötigen passende visuelle Gewichte, die mit Audio-Cues synchronisiert werden können.

## Visual

Visuelle Baseline des Vertical Slice:

- Top-Down 2D Gameplay Plane
- stylisierte 2D-Sprites mit optionaler 2.5D-Licht- und Materialwirkung
- klare, warme bis industrielle Sci-Fantasy-Farbwelt für Core, Tower, Resource und Arena
- starke Kontraste zwischen freundlichen, feindlichen und neutralen Elementen
- geringe Überlagerung kritischer Combat-Signale durch rein dekorative Effekte

Benötigte Kern-Asset-Familien:

- Main Menu und Frontend
- HUD und Tutorial-Oberflächen
- Player und Core
- Weapons und Projectiles
- Cards und Icons
- Towers und Tower Slots
- Resource-Drops und Wallet-Symbole
- Enemy-Rollen und Bosses
- Arena-Boden, Ränder, Props und VFX

Die Stage-1-spezifische Aufteilung des Ground-Atlases und des Prop-Atlases ist ergänzend in [VFX_stage1_ground_tileset.md](./VFX_stage1_ground_tileset.md) und [VFX_stage1_prop_set.md](./VFX_stage1_prop_set.md) dokumentiert.

## Balancing

Grafik-Balancing bedeutet in diesem Projekt vor allem Lesbarkeit, Priorität und visuelle Ökonomie.

Player, Core, Enemy-Telegraphs und Projectile-Gefahren haben immer Vorrang vor schönem, aber störendem Effekt-Overload.

Rarity, Reward-Wichtigkeit und Bedrohungsgrad müssen konsistent visuell abgestuft werden, damit der Player Entscheidungen schnell treffen kann.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Das Spiel bleibt technisch 2D mit orthographischer Kamera und Top-Down-Gameplay-Ebene.
- 2.5D wird nur als visuelle Vertiefung innerhalb von Sprite-, Material- und Lichtarbeit verwendet.
- Die erste Grafikproduktion priorisiert den Vertical Slice von Main Menu bis Stage-1-Boss statt breite Content-Menge.

## Acceptance Criteria

- Alle gameplay-kritischen Systeme besitzen klar definierte grafische Asset-Familien.
- Das visuelle Ziel ist mit Unity 6 URP 2D kompatibel.
- Das Spiel bleibt im Kern ein 2D- oder 2.5D-Top-Down-Spiel und weicht nicht in ein 3D-Kameraspiel ab.
- UI, Combat und Reward-Lesbarkeit bleiben auch bei hoher Effektdichte verständlich.
- Die Asset-Produktion ist modular genug, um neue Cards, Towers, Weapons, Enemies und Bosses ohne Stilbruch zu erweitern.

---

## Abnahmekriterien für geliefertes Artwork (2026-09-22)

Mehrfach kamen Assets mit einem *eingebackenen* Schachbrettmuster an - also gemalter statt echter
Transparenz (`skill_node_key_frame`, `weapon_slot_frame`, `vfx_boss_telegraph_lance`,
`character_portrait_frame`), einmal auch ein Diagramm statt des bestellten Rahmens
(`skill_node_sub_frame`). Im Editor fällt das erst auf, wenn jemand hinsieht.

Ein geliefertes Asset gilt als abgenommen, wenn:

1. Es liegt unter dem im Prompt genannten Pfad und ist nach `Assets/StreamingAssets/Art/...`
   gespiegelt (`Corebound > Validate Art Pipeline`).
2. Es ist ein PNG mit echtem Alphakanal. Ein Rahmen oder Icon hat **transparente Ecken**; ein Rahmen
   hat zusätzlich eine **transparente Mitte**.
3. Es enthält **kein** graues Schachbrettmuster als Pixel.
4. Es enthält keinen eingebrannten Text (Graphics.md, Rules: Text bleibt UI-Text).
5. Sein Inhalt entspricht dem Prompt - ein Rahmen ist ein Rahmen, kein Diagramm und keine Szene.

Punkte 2 und 3 prüft der Validator automatisch: Er misst die Alpha-Deckung von Ecken und Mitte
gegen den im Prompt deklarierten Asset-Typ (`frame`, `icon`, `sheet`) und erkennt das
Zwei-Ton-Karomuster. Punkte 4 und 5 bleiben eine menschliche Sichtprüfung.
