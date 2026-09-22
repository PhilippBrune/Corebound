# UI Technical Implementation Specification: Card Reward Choice Screen

## 1. Overview & Architectural Goals
This document specifies the UI layout, component hierarchy, and visual structure for the **Card Reward Choice Screen** in *Corebound*. The implementation follows the established visual language (industrial sci-fantasy: steel-blue framing, pale metallic edge bevels, and amber-gold energy accents) with high-contrast inner surfaces designed for text readability.

---

## 2. Layout Structure & Zones

```
+-----------------------------------------------------------------------+
|                       [CARD REWARD SCREEN TITEL]                      |
|  [Currency]                                       [Tower Information] |
| +-------------------++-------------------++-------------------+       |
| |  [CARD TITEL]     ||                   ||                   |       |
| |  +--------------+ ||                   ||                   |       |
| |  |              | ||                   ||                   |       |
| |  |     CARD     | ||    CARD SLOT 2    ||    CARD SLOT 3    |       |
| |  | Description  | ||                   ||                   |       |
| |  |              | ||                   ||                   |       |
| |  +--------------+ ||                   ||                   |       |
| +-------------------++-------------------++-------------------+       |
|               [Explanation of the Card with % values]                 |
+-----------------------------------------------------------------------+
```

---

## 3. Detailed Component Specifications

### 3.1 Root Frame & Container
* **Component Type**: Modal overlay centered on screen.
* **Dimensions**: Fixed aspect ratio modal frame designed to scale cleanly using 9-slice principles.
* **Visual Style**: Heavy industrial outer rim in steel-blue with riveted corner plates, chamfered beveling, and four amber-gold corner glow nodes.
* **Background Surface**: Recessed pale metallic fill providing neutral contrast for interior cards and text panels.

### 3.2 Header & Top Information Bar
* **Title Banner (`CARD REWARD SCREEN TITEL`)**:
  * **Placement**: Top-center, anchored directly onto the upper edge of the primary modal frame.
  * **Visual Style**: Raised metallic header plaque with dark inner inset and clean typography support.
* **Secondary Info Left (`Currency`)**:
  * **Placement**: Upper-left inner margin beneath the primary header rail.
  * **Content**: Live run currency display (Scrap / Alloy counter).
* **Secondary Info Right (`Tower Information`)**:
  * **Placement**: Upper-right inner margin opposite the currency display.
  * **Content**: Contextual statistics or targeting information for the active tower being upgraded.

### 3.3 Card Choice Grid (3-Card Layout)
* **Layout**: Equal-width horizontal 3-column container with uniform spacing.
* **Card Slot Anchor**: Three recessed rounded-rectangular base frames with golden/pale metallic inner borders.
* **Interactive Card Component Structure**:
  * **Interactive Hitbox**: Full card region (supports hover, focus, and selection states).
  * **Frame & Trim**: Dark metallic border framed with ornate purple gem corner accents to highlight reward rarity/quality.
  * **Card Header (`CARD TITEL`)**: Top horizontal plaque dedicated to the card name.
  * **Card Core Region (`CARD Description`)**: Inset vertical panel holding artwork, key stat changes, and modifier icons.

### 3.4 Footer Details Banner
* **Banner Container (`Explanation of the Card with % values`)**:
  * **Placement**: Bottom center, anchored inside the main frame below the 3-card selection grid.
  * **Visual Style**: Wide horizontal metallic slab with a recessed light-colored interior.
  * **Functional Behavior**: Dynamically populates with comprehensive mathematical breakdowns, percentage modifiers, and mechanical descriptions when a card in the grid above is hovered or focused.

---

## 4. Component Hierarchy (DOM / Node Tree)

```text
[Panel_Root] (9-Slice Industrial Modal Frame)
 │
 ├── [Header_Plaque]
 │    └── [Text_Title] -> "CARD REWARD SCREEN TITEL"
 │
 ├── [SubHeader_Row]
 │    ├── [Currency_Display] -> Icon + Text Counter
 │    └── [Tower_Info_Display] -> Icon + Status Text
 │
 ├── [Card_Selection_Grid] (Horizontal Flex / 3 Columns)
 │    ├── [Card_Slot_1]
 │    │    └── [Card_Item] (Active Choice)
 │    │         ├── [Card_Header] -> "CARD TITEL"
 │    │         └── [Card_Body] -> "CARD Description"
 │    ├── [Card_Slot_2] (Slot Frame Container)
 │    └── [Card_Slot_3] (Slot Frame Container)
 │
 └── [Footer_Explanation_Banner]
      └── [Text_Detailed_Breakdown] -> "Explanation of the Card with % values"
```