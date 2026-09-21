# UI Proposals — 2026-09-21

Four playtest items that need a design decision before code. Each has a concrete proposal and a reason. None of them is implemented.

---

## 1. Shop screen should look like the Reward screen

**Reported:** *"Shop Screen -> make it similar to the Reward screen."*

The two screens already share their structure: three card-shaped slots in a row, the same rarity frame art, a title, a resource line and a footer. What differs is the panel art behind them (`shop_offer_panel.png` versus `card_reward_choice_panel.png`), the wash colour, and the slot geometry.

This is the opposite of the note in `Shop.md`, which asks the shop to *differ* from the reward screen. That earlier requirement came from the spec; this one comes from playing it. **The playtest wins** — a player who has to re-learn a layout every time they open a different panel is paying a cost for a distinction that serves the document rather than them.

**Proposal.** Make the shop a *variant* of the reward screen rather than a separate design:

- Use the reward screen's slot geometry exactly: same size, same spacing, same y-offset. Those numbers were tuned against the real art in an earlier pass and should not be re-derived.
- Keep `shop_offer_panel.png` as the backing panel, so the two are still distinguishable at a glance.
- Keep the price chip and the tower-upgrade badge; those carry information the reward screen has no equivalent for.
- Keep the warm amber wash. It is the cheapest possible "you are somewhere else" signal and costs no layout change.

**Why not just reuse `CardRewardPresenter`?** Because the two differ in what a slot *means*: a reward slot is one of three choices and picking one ends the screen, a shop slot is a purchase and the screen stays open. Merging them would put a mode flag through every method. Copying the geometry is the cheap 90%.

**Effort:** a layout pass on `ShopCanvas.unity` plus matching constants in `ShopPresenter`. No new art.

---

## 2. Tower build selection

**Reported:** *"i dont like the current selection how to chose to build a tower. I miss e.g. information what the towers are able of and what are there pro/cons."*

**Partly shipped 2026-09-21.** The selection line now carries the full comparison: name, roster position, cost, damage per second, range, and an authored strength and weakness line per tower. That is the information half of the complaint, and it is live.

**Still proposed: the selection half.** Cycling with Previous/Next tower type is still a blind cycle — the player sees one tower at a time and has to cycle through the roster to compare.

**Proposal.** A build browser that appears when the player stands on an empty slot during an Intermission:

- All four towers shown at once as small cards, the selected one enlarged.
- Each card: icon, name, cost, and a two-bar stat glyph (damage, range). Locked towers greyed with their unlock requirement.
- The enlarged card additionally shows the strength and weakness lines that already exist.
- Left/Right moves along the roster; Interact builds. Both already bound.

This keeps every existing input and adds no new verb. It replaces "remember four towers" with "look at four towers", which is the actual request.

Needs Asset 99 (tower info card frame).

---

## 3. `stage1_spawn_gate.png` is placed but unused

**Reported:** *"is placed but not in use, think about a good use of it."*

It is drawn: `StageArenaArtPresenter.CreateSpawnGateVisual` places it at `StageThemeData.SpawnGateLocalPosition`. But it is **decoration only** — one static gate at a fixed spot, while enemies actually spawn from a fan around `EnemySpawnOrigin`, and since 2026-09-20 from a clamped, rotated, jittered fan that frequently puts them nowhere near the gate.

So the art makes a promise the spawning does not keep: it says "enemies come from here", and they do not.

**Proposal: make the gate tell the truth, and make it tell it early.**

1. **One gate per spawn point.** `StageData.SpawnPoints` already exists and Stage 2 already authors two. Draw a gate at each, instead of one gate at an unrelated authored position.
2. **Light the gate that is about to be used.** `WaveSpawnPlacement` already knows which spawn point each enemy will come from. Pulse that gate for the second or so before a wave starts.

That converts a decorative asset into the clearest possible answer to vision Section 5.5's "the current wave and upcoming danger", and it uses a system that is already built. It also gives the per-wave fan rotation something visible to communicate, which currently reads as randomness with no tell.

**Effort:** small. Move the gate creation into the spawn-point loop, and add a pulse driven by the existing wave-start delay timer.

---

## 4. Card tag icons on cards and in the pause menu

**Reported:** *"slice card_tag_icon_sheet.png and add them to the cards corresponding to the effects. Use them as well in the pause menu left to the current active perks."*

**Already sliced and already drawn.** `CardTagIconAtlas` resolves the 4×2 grid, and `CardRewardPresenter.ApplyTagIcons` draws up to four per reward slot. A pixel measurement of the sheet on 2026-09-21 confirms eight clean components on a regular 4×2 grid matching `CardTagType`'s declared order, so the uniform slicing is correct and needs no custom rects.

**The real gap is the data.** Most `CardData` assets have an empty `_tags` array, so the icons have nothing to draw. `Docs/10_OPEN_POINTS.md` already recorded this as "authored on several cards but read by nothing", which was half right: it is read, but most cards have no tags to read.

**Proposal, two parts:**

1. **Populate `_tags` on all 34 cards from their effects.** This is mechanical and should be a one-off script with a human review, not hand-editing: map `CardEffectTarget` to `CardTagType` (`WeaponDamageMultiplier` → Weapon, `ProjectileSpeedMultiplier` → Projectile, `TowerDamageMultiplier` → Tower, `ResourceGainMultiplier` → Resource, and so on), then add `Critical`, `Burn` and `Freeze` from the weapon-modifier and status fields. Cards added since, like Critical Focus and Split Barrel, already have tags and are the pattern to follow.
2. **Draw them in the pause menu**, left of each active perk, reusing `CardTagIconAtlas` exactly as the reward screen does.

Part 2 is small once part 1 exists. Doing part 2 first would ship an empty column.

---

## Priority

1. **Spawn gate** — smallest, and it fixes art that currently lies to the player.
2. **Card tags** — mechanical, and it lights up icon code that already works.
3. **Shop layout** — a layout pass, no new art, closes a repeated report.
4. **Build browser** — largest, and the information half of it already shipped, so the remaining value is comparison rather than knowledge.
