# Slice 16 — Progression, Loadout and UI Proposals

Status: In progress — started 2026-09-22.

Implements: `Docs/Specs/SkillTree.md`, `Docs/Specs/WeaponLoadout.md`, `Docs/Specs/Upgrades.md`,
`Docs/Specs/DIY/UI_PROPOSALS_2026-09-21.md`.

---

## Owner decisions folded in (2026-09-22)

| Spec | Question | Decision |
| --- | --- | --- |
| SkillTree | Stage 1 failure pays SP? | **0.** SP is paid **only for defeating a boss**, on a curve that grows with stage depth. |
| SkillTree | Ascension prestige marker? | **Yes** — a rank badge in the main menu. |
| WeaponLoadout | Empty-slot bonus? | **No.** Reserved as a possible future *character passive*. |
| WeaponLoadout | Per-weapon upgrade levels? | **Yes, each weapon is upgraded separately** — see "Weapon tiers" below. |
| WeaponLoadout | Different rarities count toward one Set Bonus? | **Yes**, counting by `WeaponId`. |
| Upgrades | Magazine / Reload | **(a) a real reload loop** in `PlayerCombatController`. |
| Upgrades | Alloy / SP sources | **Bosses and elites.** Alloy: bosses + elite kills. SP: bosses only (per the SkillTree answer). |
| Upgrades | Respec for upgrades? | **No.** Upgrades are permanent. |

### How "each weapon is upgraded separately" is read

Two upgrade layers, deliberately different in scope:

1. **Permanent (Alloy, between runs)** — `Upgrades.md`'s stat tracks, per `WeaponId`.
2. **In-run (Scrap, Intermission)** — every *equipped copy* carries its own **tier**
   (`CardRarity`: Common → Rare → Epic → Legendary). Upgrading one slot's weapon raises that copy
   only. Tier scales damage and fire interval. This is also what gives "duplicates of different
   rarities" a meaning: two Rapids at Common and Epic are two copies of `weapon_rapid` for the Set
   Bonus, but not equally strong.

---

## Phases

Each phase ends with: compile (0 errors), an autopilot run through MainMenu → New Run, a commit.

| # | Phase | Scope |
| --- | --- | --- |
| A1 | Spawn gates | One gate per `StageData.SpawnPoints` entry (fallback: `EnemySpawnOrigin`); gates used by the coming wave pulse during the wave-start delay. |
| A2 | Card tags | Script-populate `_tags` on every `CardData` from its effects/modifiers; pause menu lists active cards with their tag icons. |
| A3 | Shop geometry | Shop slots use the reward screen's slot size, spacing and y-offset; keeps `shop_offer_panel.png`, price chip, tower badge, amber wash. |
| A4 | Tower build browser | Four tower cards on an empty slot during Intermission, selected enlarged, icon/name/cost/DPS+range bars, locked greyed, strengths/weaknesses; uses `tower_info_card_frame.png`. |
| A5 | Boss telegraph art | Ring / Lance / Volley shapes draw `vfx_boss_telegraph_*.png` instead of the ghost projectile. |
| B | Currencies + save v2 | Meta currency becomes **Alloy** (earned/spent, never stored as a balance); SP earned/spent; per-character node ranks; ascension; upgrade ranks; weapon slot count; first-kill flags. Save migration v1 → v2. Rewards on boss and elite kills; run completion/failure payouts removed. |
| C | Upgrades | `UpgradeTrackData` ×9, `UpgradeService`, `UpgradeSnapshot`; reload/magazine loop; tower damage/fire-speed/range/build-discount. |
| D | Weapon loadout | 6 slots, simultaneous fire, per-slot cooldown/magazine, stagger, projectile budget, audio de-dup, set bonuses + Arsenal, per-copy tier, acquisition via shop/reward/boss, sell, save migration `EquippedWeaponId` → `EquippedWeaponIds`, HUD strip, Intermission loadout panel. |
| E | Skill tree | Data (per character), `SkillTreeService`, `SkillModifierSnapshot`, gating, respec, 4 branches × 4 key nodes × 4 sub nodes. |
| F | Ascension + badge | Ascension ranks with 3 rolled offers; rank badge in the main menu. |
| G | Meta UI | The Meta Progression route becomes a tabbed Progression screen: Unlocks · Upgrades · Combat · Construction · Industry · Command · Ascension. Left/Right switches tab, Up/Down moves, Confirm buys. |

## Deliberate deviations from the specs (and why)

- **One tabbed route instead of two routes.** SkillTree.md asks for its own route, Upgrades.md for
  one route with two tabs. A single route with seven tabs is fewer menu entries, reuses the
  existing controller-first route target, and still gives "one branch on screen at a time".
- **Skill tree authored inline in one asset per character.** The spec lists `SkillBranchData` /
  `SkillKeyNodeData` / `SkillSubNodeData` as ScriptableObjects. 80 separate node assets would be
  unreviewable; the same types exist as `[Serializable]` classes inside `SkillTreeDefinitionData`.
- **Arsenal bonus cannot trigger yet.** It needs six distinct archetypes and the game has three
  weapons (Rapid, Spread, Beam). The rule is implemented and will light up when three more exist.
- **Missing art (Assets 85–95)** — key/sub node frames, link segment, ascension badge, branch
  icons, weapon slot frame, tier pips, Alloy/SP icons — uses procedural fallbacks until created.

## Verification limits

The autopilot can drive a run, but it cannot press menu buttons in the main menu's Progression
screen, so purchases are verified through save-file state and a debug grant, not by a human flow.
Audio de-duplication cannot be heard by an agent and is verified by counting cue calls in the log.
