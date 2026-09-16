
# HUD Frame Manual Layout Notes

Der HUD-Frame soll jetzt als normale Scene-UI in Unity bearbeitet werden.

Die Runtime in `Assets/Scripts/Gameplay/Waves/WaveStatePresenter.cs` aktualisiert nur noch die dynamischen Werte.

## Was aktuell in den Frame geschrieben wird

- obere linke Zone: Scrap, Player-Leben, Core-Leben
- obere rechte Zone: Stage und Wave
- untere linke Zone: Tower- und Intermission-Build-Info
- untere rechte Zone: Combat-, Boss- oder Intermission-Status

## Welche Runtime-UI-Elemente relevant sind

Unter dem GameObject mit `WaveStatePresenter` wird im Editor eine Scene-Hierarchie unter `HUDCanvas/HUD` verwendet:

- `HUD_Frame_Panel`
- `Currency`
- `PlayerHealth`
- `CoreHealth`
- `Wave`
- `TowerInfo`
- `StateInfo`

## So passt du das im aktuellen Setup praktisch an

1. Öffne `SampleScene`.
2. Starte Play Mode.
3. Wähle `HUDCanvas/HUD` oder eines seiner Kinder aus.
4. Wähle z. B. `Currency`, `Wave` oder `TowerInfo`.
5. Ändere direkt im `RectTransform` Anchor, Pivot, `Left/Top/Right/Bottom`, `Width` oder `Height`.
6. Ändere im `RectTransform` testweise Anchor, Pivot, `Left/Top/Right/Bottom` oder `Width/Height`.
7. Die Position bleibt in der Scene gespeichert; du musst sie nicht in Code übertragen.

## Welche Code-Stellen du dafür anfasst

Die Runtime schreibt nur noch die Inhalte in:

- `Currency`
- `PlayerHealth`
- `CoreHealth`
- `Wave`
- `TowerInfo`
- `StateInfo`

## Wichtige Hinweise fuer dieses HUD

- Die Positionen werden nicht mehr bei jedem Start durch Gameplay-Code neu gesetzt.
- Wenn Text abgeschnitten wirkt, vergroessere zuerst den jeweiligen `RectTransform`-Bereich.
- Wenn der ganze HUD-Block verschoben werden soll, verschiebe `HUD` oder `HUD_Frame_Panel`.
- Wenn du `hud_frame_panel.png` neu arrangierst, verschiebe die Text-Children direkt im Scene-Hierarchy-Setup.
