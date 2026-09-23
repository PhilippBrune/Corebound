# UI Proposals — 2026-09-21

Four playtest items that needed a design decision before code. **All four shipped on 2026-09-22**;
the proposals and their reasoning are removed now that the decisions are in the build. What stayed
is the decision each one settled, because those still govern how the screens may change.

| # | Reported | Decision that now stands |
| --- | --- | --- |
| 1 | *"Shop Screen -> make it similar to the Reward screen."* | The shop is a **variant of the reward screen**, not a separate design: identical slot geometry (264x336 at x -291 / 0 / 291, y -17), its own panel art and warm amber wash, plus the price chip and tower-upgrade badge the reward screen has no equivalent for. This deliberately overrides `Shop.md`'s older requirement that the two look *different* - a player should not re-learn a layout per panel. |
| 2 | *"I don't like the current selection how to choose to build a tower ... I miss information about what the towers are able of."* | A **build browser** on an empty slot during an Intermission shows every tower at once: the selected one large with cost, DPS, range, strengths and weaknesses; the rest small; locked ones greyed. Previous/Next Tower Type and Interact - no new verb. In combat the one-line summary remains. |
| 3 | *"`stage1_spawn_gate.png` is placed but not in use."* | **One gate per spawn point**, turned to face the arena, and the gates the coming wave will use light up during the 2 s wave-start delay. The art now tells the truth about where enemies come from; a tint alone was not visible on the dark gate, so a light pool glows behind it. |
| 4 | *"Slice `card_tag_icon_sheet.png` and add the icons to the cards ... and the pause menu."* | Tags are **authored on the cards** (32 of 34; Brace For Impact and Sure Footing have none, because the sheet has no survivability or mobility icon) and drawn from `CardTagIconAtlas` in both the reward slots and the pause menu's perk grid. |

Two follow-ups from these four are tracked in [`10_OPEN_POINTS.md`](../../10_OPEN_POINTS.md), not
here: the shop and reward screens are still structurally alike (an art-direction call), and offer
selection still ignores card tags.
