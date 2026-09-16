
# Reward Screen Manual Layout Notes

Der Reward-Screen soll jetzt als normale Scene-UI in Unity bearbeitet werden.

Die Runtime in `Assets/Scripts/Gameplay/Cards/CardRewardPresenter.cs` fuellt nur noch Texte, Auswahlzustand und Card-Frames.

## Was aktuell angezeigt wird

- Titel oben mittig
- Ressourcen links oben im Panel
- Tower-Info rechts oben im Panel
- drei Card-Slots in der Mitte
- Beschreibungszeile unten

## Welche Runtime-UI-Elemente im Play Mode entstehen

Unter dem GameObject mit `CardRewardPresenter` wird im Editor eine Scene-Hierarchie unter `RewardCanvas/RewardScreen` verwendet:

- `Overlay`
- `CardRewardChoicePanel`
- `Title`
- `Resources`
- `TowerInfo`
- `RewardSlot0`
- `RewardSlot1`
- `RewardSlot2`
- `Description`

Jeder Slot enthaelt zusaetzlich:

- `SlotFrame`
- `SlotTitle`
- `SlotRarity`
- `SlotDescription`

## So passt du die Reward-UI praktisch an

1. Öffne `SampleScene`.
2. Starte einen Run bis zum Reward-Screen.
3. Wähle `RewardCanvas/RewardScreen/CardRewardChoicePanel` oder einen Slot darunter aus.
4. Klappe `RewardSlot0-2` auf.
5. Passe direkt die `RectTransform`-Werte der Slots oder Texte an.
6. Die Position bleibt in der Scene gespeichert; du musst keine Werte in Code übertragen.

## Welche Code-Werte das Layout aktuell steuern

- `CardRewardChoicePanel`
- `RewardSlot0-2`
- `SlotFrame`
- `SlotTitle`
- `SlotRarity`
- `SlotDescription`

## Wofuer du welche Elemente verschiebst

- verschiebe `CardRewardChoicePanel`, wenn die ganze Reward-Fläche anders sitzen soll
- verschiebe `RewardSlot0-2`, wenn die Karten anders verteilt werden sollen
- verschiebe `SlotTitle`, `SlotRarity` und `SlotDescription`, wenn Text innerhalb einer Karten-Grafik anders sitzen soll
- passe `Description` an, wenn die untere Erklaerungszeile mit dem Art-Frame kollidiert

## Wichtige Hinweise fuer dieses Setup

- Die Kartenrahmen liegen als eigene `SlotFrame`-UI-Elemente in jedem Slot.
- Layout-Aenderungen passieren jetzt direkt in der Scene-Hierarchie statt in `BuildPanel()`.
- Wenn du die Reward-Grafik austauschst, beginne mit `CardRewardChoicePanel`, dann `RewardSlot0-2`, dann erst die Texte innerhalb der Slots.
