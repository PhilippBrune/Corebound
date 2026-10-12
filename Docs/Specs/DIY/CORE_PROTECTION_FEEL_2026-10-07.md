# Protecting the Core — how it should feel, 2026-10-07

**Owner request:** *"Create suggestions how the player can get more the feeling of protecting the
core."*

**Decided 2026-10-08: P1-1, P2-1, P3-1, P4-1** (all four recommendations). Built 2026-10-12 — see
"As built" at the end; the rules live in `Specs/Core.md`, "Protecting the Core".

---

## Where it stands

The Core is the run's loss condition, and it is how runs end: three of the four real campaign
runs on 2026-10-01 were lost as **"Core destroyed"**, not "Player died". Yet in play the Core is
mostly a blue bar in the top-right corner.

| What exists | What the player gets from it |
| --- | --- |
| Enemies head for the Core unless the player gets within their aggro range or shoots them | The threat exists, but nothing on screen says "these are going for the Core" |
| A Core hit: a flare at the Core, a camera shake, the HUD bar drains and flashes | Visible only if the Core is on screen and the player is watching the bar |
| No Core-hit sound (none of the 28 audio cues is one) | A Core under attack off-screen is silent |
| Core Guard: hold Interact within 2.2 units, the Core takes 15% damage | Strong, but a hint shows once and nothing rewards using it |
| Core Repair: Scrap in an intermission, near the Core | A chore at the end of a wave, not part of the fight |
| The Core's voice (story): a line at stage start, boss arrival, boss defeat and a lost run | Gives the Core a personality, but it never speaks while it is being hurt |
| The Custodian's tether, Core Aegis, Core Contingency | Protection exists in builds, but only for players who choose it |

**The diagnosis:** the player defends a health bar, not a thing. They learn the Core was in trouble
when the bar is already low, and keeping it whole is never celebrated. "Protecting" needs three
things the game mostly lacks: **seeing the threat coming, feeling the hit when it lands, and being
rewarded for stopping it.**

---

## Suggestions

Each one expands an existing system (Vision §19.8): the HUD, the Core's voice, enemy targeting,
the guard and repair, the Scrap economy. No new resource, no new screen.

### A — See the threat coming

1. **Threat lines.** An enemy that has chosen the Core as its target draws a thin, faint red line
   to it for its first second of approach, and again while it is within 4 units. Readable at a glance,
   cheap (the Custodian's tether already draws exactly this kind of beam), and it answers "which of
   these are going for the Core?" without a single word.
2. **Edge alerts.** When the Core is off-screen and something within 3 units of it is hostile, a
   small Core-blue marker sits on the screen edge in the Core's direction and pulses. The same
   marker turns red while the Core is taking damage.
3. **Danger states on the Core itself.** At 60% and 30% the Core sprite cracks a little more, its
   light flickers, and its glow shifts from blue to amber to red. The bar says how much is left; the
   Core says how bad it is.

### B — Feel the hit when it lands

4. **A Core-hit sound** — a deep, resonant thud, different from every combat sound, rate-limited to
   one per 0.4 s so a swarm reads as an alarm rather than noise. An art-and-audio prompt is all it
   takes; the playback hook exists.
5. **Screen-edge pulse.** A short Core-blue vignette on the screen edge nearest the Core on every
   hit, stronger as health drops. It reaches the player when the bar does not.
6. **The Core speaks when it is hurt.** One short line at 50% and one at 25%, once per stage each,
   through the existing voice panel ("They're inside the ring." / "I can't hold this alone."). The
   story already made it a character; this makes it a character that needs you.

### C — Be able to act on it

7. **Guard with a payoff.** A hit that lands on a guarded Core charges it; after 5 blocked hits the
   guard releases a short knockback pulse (the Custodian's shockwave without the class). Guarding
   turns from waiting into a counter-attack, and the hint becomes a skill.
8. **Repair in the fight.** Repair can also be held during combat, slowly (channelled, like the
   Engineer's overcharge, 1% per second while standing still at the Core). A risky choice in the
   moment instead of a purchase after it.
9. **Intercept bonus.** An enemy killed while it was heading for the Core, within 5 units of it,
   pays +1 Scrap and shows a small "Intercepted" pop-up. Defending becomes something the player
   does on purpose, and the reward is in the currency they already use.

### D — Be rewarded for keeping it whole

10. **"Core intact" per wave.** A wave cleared without a Core hit shows "Core untouched" on the wave
    banner and pays a small Scrap bonus; three in a row raise it. The stage quest for an untouched
    Core already exists — this is the same idea at a scale the player meets every two minutes.
11. **Integrity carried into the next stage is visible.** Between stages the transition banner says
    what the Core brought along ("Core 74%"), so a well-defended stage is a head start.
12. **The Core grows with you.** Every stage cleared with the Core above 75% gives it one visible
    upgrade for the rest of the run (a ring of light, a stronger guard) — the vision's "the improved
    systems generate greater value" applied to the thing being defended.

---

## Decisions

### P1 — How loud should threats to the Core be?

1. **Threat lines + edge alerts + danger states** (A1–A3).
2. **Edge alerts and danger states only** — no lines, less on screen.
3. **Danger states only** — the Core itself, nothing else.

**Recommendation: 1.** The campaign says players lose the Core without noticing; the lines are
the only suggestion that shows the threat *before* the hit. Each can be switched off in Settings
(Accessibility) if it proves busy.

### P2 — Feedback on a hit

1. **Sound + screen-edge pulse + voice lines** (B4–B6).
2. **Sound + pulse**, no new voice lines.

**Recommendation: 1.** All three are cheap; the voice lines cost one data entry each.

### P3 — Active defence

1. **Guard payoff + intercept bonus** (C7, C9).
2. **All three**, including repair in combat (C7–C9).
3. **None** — the classes and cards already cover it.

**Recommendation: 1.** Repair in combat changes the economy and needs balancing; the other two
reward what players should already be doing.

### P4 — Rewards for a whole Core

1. **Per-wave "Core untouched" bonus + visible carried integrity** (D10, D11).
2. **All three**, including the growing Core (D10–D12).
3. **Banner text only**, no Scrap.

**Recommendation: 1**, with D12 kept for after a human playtest — it compounds with Core Aegis
and the Custodian and may make the Core too safe.

---

## What the code needs

- `CoreThreatPresenter` (new, small): threat lines and edge markers, reading `EnemyRuntimeController`'s
  current target (exposed as a property) and the Core's position; a toggle in Settings.
- `CombatHitReceiver` (Core only): a hit event the HUD, audio and presenter listen to; danger state
  thresholds on the Core sprite via its existing tint/sprite repair path.
- Audio: one new cue (an `AUDIO_ASSET_PROMPTS` entry), played from `WaveAudioCuePlayer`.
- `StoryData`: two Core lines (EN/DE) per stage or shared.
- `CoreGuardController`: a blocked-hit counter and the pulse (reuse the Custodian's shockwave).
- `WaveManager`: per-wave "Core hit" flag (the stage quest already tracks it per stage), the bonus
  and the banner text; the stage banner shows carried integrity.
- Autopilot: a `core` probe that logs threat lines, alerts and the per-wave bonus.

**Rough size:** a day for P1–P2 (most of it the threat presenter), half a day for P3 and P4, the
audio cue on top.

---

## As built (2026-10-12)

Everything in P1-1, P2-1, P3-1 and P4-1, with the numbers from the suggestions above. The table of
rules is in `Specs/Core.md`, "Protecting the Core"; every number is a field on `CoreData`.

### Code

| Piece | Where |
| --- | --- |
| `CoreController.CoreHit` - one static event per landed hit (position, amount, health after, guarded); `CoreController.Active` and `.Tuning` | `Gameplay/Combat/CoreController.cs` |
| Threat lines, danger glow, edge pulse, edge alert (A1-A3, B5), on their own overlay canvas | `UI/Gameplay/CoreThreatPresenter.cs` |
| Hit sound, intercept bonus, per-wave untouched bonus and streak (B4, C9, D10) | `Gameplay/Waves/CoreDefenseDirector.cs` |
| Hurt lines (B6) - `StoryData._coreHurtFirst/_coreHurtSecond`, EN/DE | `Story/StoryDirector.cs` |
| Guard charge and counter-pulse (C7); the shockwave shared with the Custodian | `Gameplay/Combat/CoreGuardController.cs`, `CoreShockwave.cs` |
| "Core untouched +N XP" and "Wave 1 · Core 74%" banners (D10, D11) | `Gameplay/Waves/WaveStatePresenter.cs` |
| Enemies expose `IsHeadingForCore`, `CoreApproachStartedAt`, `LastHeadingForCoreAt` | `Gameplay/Enemies/EnemyRuntimeController.cs` |
| The Core Alerts setting | `SettingsOptionKey.ShowCoreAlerts`, `SettingsSaveData`, `SettingsDefinition_Default` |
| The hit cue (procedural until Audio 29 exists) | `ScriptableObjects/Audio/AudioCue_CoreHit.asset` |

WaveManager attaches the director and the presenter at runtime, as it does `StoryDirector`; no scene
was edited.

### Choices the concept left open

- **Hurt lines** follow the Core's established voice (no contractions): "They are inside the ring." /
  "Sie sind schon im Ring.", "I cannot hold this alone." / "Allein halte ich das nicht."
- **Intercept** counts kills by the player or a tower (poison and burn ticks are the player's) of an
  enemy that was heading for the Core **within the last 3 s** - an enemy the player shoots turns on
  the player, so "heading for the Core at the moment of death" would almost never be true.
- **Guard charge** is kept while the Guard is lowered, so stepping out to shoot does not waste it.
- **The untouched banner** reads "Core untouched +25 XP" in place of "Wave Clear +25 XP"; the Scrap
  and the streak go to the notification line ("Core untouched ×3 · +6 Scrap"). The banner holds
  one line, and longer messages now shrink to fit instead of wrapping under the plate (that
  affected German banners before this too).
- **Crack overlays** (A3 "cracks a little more") are not built: there is no art. The glow carries
  the danger state alone; a crack overlay would be one art prompt and a few lines in
  `CoreThreatPresenter.UpdateGlow`.
- **D12** (the Core grows with the run) stays out, as recommended.

### Verified (autopilot `core` probe, Profile 2, 2026-10-12)

Four runs; counters from the `CORE` lines, every visual from a screenshot:
- 24 Core hits → 22 hit sounds (the 0.4 s limit held two back) and 24 edge pulses.
- Threat lines: 195 shown over four stages, up to 7 at once; drawn from each attacker to the Core.
- Danger glow amber at 48 %, red at 24 % (the probe lowers the Core with a restore, not a hit,
  because this profile's full skill tree keeps it near 100 %).
- Hurt lines: both spoken, the first at 25.1 % and the second at 23.1 % (counter). The first is
  photographed on the voice panel in English and German ("Sie sind schon im Ring."); the second
  queued behind it and was not photographed.
- Guard: 5 blocked hits → 1 pulse ("Guard pulse"); one pulse reached an enemy, another fired with
  none in range (ranged enemies hit from outside 4 units). A separate zero-damage check confirmed
  the pulse finds an enemy 3.5 units away.
- Intercepts: 64 in one run, +64 Scrap, "Intercepted +1" at each.
- Untouched waves: +3, +3, then +6 from the third in a row; the streak resets on a hit.
- Carried integrity: "Wave 1 · Core 74%" / "Welle 1 · Kern 24%" on the next stage's first banner.
- Edge alert: the camera shows every arena whole, so the Core is **never** off-screen in play; with
  the probe pretending it was, the arrow appeared on the screen edge toward the Core, red while it
  was hit.

Not verified: how the hit sound sounds (procedural until Audio 29; nobody here can hear it), the
Settings toggle switching the alerts off (code path only), and any balance - 3/6 Scrap per untouched
wave and +1 per intercept are the concept's numbers, untuned.
