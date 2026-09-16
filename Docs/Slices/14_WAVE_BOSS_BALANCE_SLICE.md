# Wave Boss Balance Slice

Status: Completed (2026-09-16 audit confirmed: all systems this slice depends on are implemented and wired; "Towers > Targeting" under Balance Knobs is not an actual tunable — TowerRuntimeController.FindTarget() hardcodes nearest-in-range with no data field for targeting mode/priority, noted as a real follow-up gap, not a blocker for this slice's own scope)

Priority: High

Depends On: Bosses Stage 1 Slice, Elite Variants Slice, Cards, Resources, Towers, Waves

---

## Purpose

Dieser Slice balanciert Cards, Resources und Towers unter mehrfachem Wave- und Boss-Druck.

Ziel ist kein finaler Zahlenpass, sondern ein stabiler Stage-1-Balance-Rahmen fuer wiederholbare Tests mit verschiedenen Builds.

---

## Linked Specs

- [Cards Spec](../Specs/Cards.md)
- [Resources Spec](../Specs/Resources.md)
- [Towers Spec](../Specs/Towers.md)
- [Waves Spec](../Specs/Waves.md)
- [Enemies Spec](../Specs/Enemies.md)
- [Bosses Spec](../Specs/Bosses.md)
- [Factory Spec](../Specs/Factory.md)
- [Economy Spec](../Specs/Economy.md)

---

## Existing Runtime Links

- [CardService](../../Assets/Scripts/Gameplay/Cards/CardService.cs)
- [CardRewardController](../../Assets/Scripts/Gameplay/Cards/CardRewardController.cs)
- [ResourceWalletService](../../Assets/Scripts/Gameplay/Resources/ResourceWalletService.cs)
- [TowerService](../../Assets/Scripts/Gameplay/Towers/TowerService.cs)
- [TowerRuntimeController](../../Assets/Scripts/Gameplay/Towers/TowerRuntimeController.cs)
- [WaveManager](../../Assets/Scripts/Gameplay/Waves/WaveManager.cs)
- [BossRuntimeController](../../Assets/Scripts/Gameplay/Bosses/BossRuntimeController.cs)

---

## Data Links

- [Cards Data Folder](../../Assets/ScriptableObjects/Gameplay/Cards)
- [Resources Data Folder](../../Assets/ScriptableObjects/Gameplay/Resources)
- [Towers Data Folder](../../Assets/ScriptableObjects/Gameplay/Towers)
- [Waves Data Folder](../../Assets/ScriptableObjects/Gameplay/Waves)
- [Bosses Data Folder](../../Assets/ScriptableObjects/Gameplay/Bosses)
- [Enemies Data Folder](../../Assets/ScriptableObjects/Gameplay/Enemies)

---

## Scope In

- Stage-1-Waves plus Boss als zusammenhaengender Balance-Test.
- Card-Angebote unter wiederholtem Wave-Reward-Druck pruefen.
- Resource-Gewinne aus Enemy-Kills, WaveRewardData und BossRewardData pruefen.
- Tower-Kosten, Tower-Output und Tower-Upgrades gegen Wave- und Boss-Druck pruefen.
- Elite-Varianten als Druckspitzen in Wave 5, Wave 6 und optionalem Boss-Hilfsfenster einbeziehen.
- Controller-freundliche Entscheidungsdauer zwischen Combat-Abschnitten beobachten.

---

## Scope Out

- Kein Reroll-System.
- Keine Refunds.
- Keine zweite Run Resource.
- Keine Meta-Progression-Zahlenbalance.
- Keine neuen Tower-Rollen nur fuer diesen Balance-Pass.

---

## Test Builds

| Build | Start Focus | What It Tests |
| --- | --- | --- |
| Weapon Build | Weapon- und Projectile-Cards bevorzugen | Kann aktives Combat den Boss tragen? |
| Tower Build | Tower-Cards und Tower-Investment bevorzugen | Bleiben Towers Werkzeug statt Autowin? |
| Resource Build | Resource-Cards und sparsames Ausgeben | Entsteht echte Kaufspannung ohne Snowball? |
| Mixed Build | je eine Weapon-, Tower- und Resource-Richtung | Ist die Standardentscheidung solide spielbar? |

---

## Balance Knobs

Cards:

- Offer-Gewichtung
- Effektstaerke
- Stack-Regel
- Rarity-Gewicht

Resources:

- EnemyRewardData
- WaveRewardData
- BossRewardData
- Resource-Multiplikatoren durch Cards

Towers:

- Build-Kosten
- Upgrade-Kosten
- Damage
- Fire Interval
- Range
- Targeting

Waves und Bosses:

- SpawnGroup Count
- Spawn Interval
- Elite Modifier
- Boss Health
- Boss Fire Interval
- Boss Projectile Count
- Boss Helper SpawnGroup

---

## Measurement Notes

Pro Testlauf sollen festgehalten werden:

- Gewaehlte Cards pro Reward-Moment.
- Wallet vor und nach jeder Wave.
- Tower-Anzahl und Upgrade-Stand vor Boss-Start.
- Player-Health und Core-Health nach jeder Wave.
- Boss-Phase, in der die meiste Gefahr entsteht.
- Tod durch Wave-Druck, Boss-Projectile, Hilfs-Enemy oder Core-Schaden.
- Ob eine Entscheidung offensichtlich immer richtig wirkte.

---

## Guardrails

- Difficulty steigt ueber Komposition, Tempo und Raumdruck, nicht primaer ueber Health-Bloat.
- Resource-Flow darf Tower- und Card-Kosten nicht bedeutungslos machen.
- Towers duerfen Boss-Druck reduzieren, aber den Player nicht ersetzen.
- Boss-Reward muss sich staerker als ein normaler Wave-Reward anfuehlen.
- Elite-Rewards muessen Risiko belohnen, ohne den Economy-Loop zu sprengen.

---

## Acceptance Criteria

- Stage 1 ist mit allen vier Test Builds schlagbar oder klar begruendet nicht schlagbar.
- Keine einzelne Card-, Resource- oder Tower-Strategie dominiert alle Testlaeufe.
- Boss-Druck bleibt lesbar und fair.
- Elite-Druck erzeugt Prioritaetsentscheidungen statt unlesbarer Schadensspitzen.
- Alle Tuning-Aenderungen bleiben datengetrieben ueber ScriptableObjects.
