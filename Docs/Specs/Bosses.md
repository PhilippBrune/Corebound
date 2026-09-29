# Bosses Specification

## Purpose

Definiert Bosses als Stage-Abschlüsse und Höhepunkte eines Runs.

Bosses prüfen Build-Stärke, Positioning und Lesbarkeit stärker als normale Waves, ohne eigene Kernregeln außerhalb des Combat-Systems zu benötigen.

## Design Goals

- Klare, erinnerbare Boss-Identitäten innerhalb gemeinsamer Combat-Regeln
- Stage-Finale mit hoher Spannung und starker Belohnung
- Gute Lesbarkeit von Angriffen, Phasen und Gefahrenzonen
- Nutzung bestehender Systeme statt einzigartiger Einmalsysteme
- Datengetriebene Erweiterbarkeit für weitere Bosses

## Gameplay

Jede Stage endet in einer Boss-Begegnung.

Ein Boss besitzt mehrschichtige Angriffsmuster und eine kleine Zahl klar definierter Phasen.

Phasen variieren Druck, Bewegung, Arena-Nutzung oder Spawn-Unterstützung, bleiben aber im Rahmen der allgemeinen Combat-Regeln.

Der Sieg über einen Boss beendet die Stage und löst einen bedeutenden Reward-Moment aus.

## Rules

- Bosses nutzen dieselbe Damage-, Status- und Trefferlogik wie andere Combat-Einheiten.
- Boss-Angriffe müssen telegraphiert, lernbar und fair ausweichbar oder planbar sein.
- Phasenwechsel werden durch klare Datenregeln wie Health-Schwellen oder definierte Encounter-Trigger ausgelöst.
- Bosses dürfen Builds testen, aber keine enge einzelne Build-Art zwingend voraussetzen.
- Zusätzliche Hilfs-Enemies sind nur erlaubt, wenn sie über bestehende Wave- oder Spawn-Systeme eingebunden werden.
- Pro Boss-Begegnung ist nur ein aktiver Boss-Kerngegner vorgesehen.
- Neue Bosses sollen primär bestehende Pattern neu kombinieren.

## Data Model

Boss-Daten werden über ScriptableObjects definiert.

- BossData: Kernidentität, Stats, Präsentation und Phasenreferenzen
- BossPhaseData: Trigger, Pattern-Sets, Arena- oder Spawn-Modifikatoren
- BossAttackPatternData: Telegraph, Angriffslogik, Cooldowns und Zielverhalten
- BossRewardData: Rewards, Card-Angebote oder Stage-Abschluss-Belohnungen
- BossPresentationData: Musik, Intro, UI- und VFX-Referenzen

## Technical Design

Ein BossController koordiniert Presentation und delegiert Kampf- und Phasenlogik an spezialisierte Runtime-Klassen.

Boss-Phasen werden zustandsbasiert verwaltet und senden Events über `BossEvents` (`OnBossSpawned`, `OnBossPhaseChanged`, `OnBossDefeated`) mit `BossEventArgs` Payload (`StageIndex`, `WaveIndex`, `BossId`, `BossName`, `PhaseIndex`, `PhaseLabel`, `HealthNormalized`).

Bosses greifen für Projectiles, zusätzliche Spawns und Combat-Auflösung auf bestehende Services zurück.

Boss-spezifische Sonderfälle müssen in Daten beschrieben oder als wiederverwendbare Module umgesetzt werden.

Im aktuellen Stage-1-Slice hängen `BossRuntimeController`, `BossHudPresenter` und `BossAudioCuePlayer` direkt am bestehenden Wave-, Reward- und Save-Unterbau der SampleScene.

## Dependencies

- Combat
- Enemies
- Waves
- Resources
- Cards
- UI
- Audio

## Linked Slice Files

- [Bosses Stage 1 Slice](../Slices/12_BOSSES_STAGE1_SLICE.md)
- [Elite Variants Slice](../Slices/13_ELITE_VARIANTS_SLICE.md)
- [Wave Boss Balance Slice](../Slices/14_WAVE_BOSS_BALANCE_SLICE.md)

## UI

Boss-UI zeigt mindestens:

- Boss-Name
- Boss-Lebenszustand
- Phasenwechsel-Hinweise
- relevante Großangriff-Warnungen

Die Anzeige darf das Sichtfeld des Players nicht überladen.

## Audio

Bosses besitzen priorisierte Audio-Inszenierung für Intro, Phasenwechsel, Großangriffe und Niederlage.

Die Boss-Mischung muss sich klar von normalen Waves abheben.

## Visual

Bosses brauchen eine starke Silhouette, große Telegraphs und klare Phasenkommunikation.

Gefahrenzonen, Projektile und Arena-Effekte müssen trotz Spektakel lesbar bleiben.

## Balancing

Ein Boss soll ein Build-Check sein, aber nicht durch rohe Stat-Mauern gewinnen.

Boss-Kämpfe müssen lange genug dauern, um Pattern zu lesen und Entscheidungen zu treffen, aber kurz genug bleiben, um das Run-Tempo zu halten.

Rewards nach Bosses sollen den Fortschritt einer neuen Stage spürbar markieren.

## Open Questions

Keine offenen Fragen für den ersten Vertical Slice.

Festgelegte Entscheidungen:

- Boss-Phasen werden standardmäßig über Health-Schwellen ausgelöst; zusätzlich ist ein wiederverwendbares Enrage-Modul als optionaler Zeittrigger erlaubt.
- Hilfs-Enemies sind nur in klar begrenzten Spawn-Fenstern pro Boss-Phase erlaubt und dürfen nie die dominante Gefahr vor dem Boss selbst werden.
- Im ersten Scope besitzt jede Stage genau einen Boss; alternative Boss-Pools sind eine spätere Erweiterung.

## Acceptance Criteria

- Bosses schließen Stages als klar telegraphierte Höhepunkte ab.
- Boss-Phasen und Pattern sind überwiegend datengetrieben definierbar.
- Boss-Logik bleibt auf bestehende Combat- und Spawn-Systeme aufgesetzt.
- UI, Audio und Visuals kommunizieren Boss-Zustände eindeutig.
- Bosses prüfen unterschiedliche Builds, ohne dominante Hard-Counter zu erzeugen.
- Der erste Stage-1-Boss integriert Spawn-, Phase-, Reward- und Warn-Hooks ohne separate Boss-Sonderregeln.

---

## Attack shape variety (2026-09-21)

Vision Section 20.1 recorded boss variety as open. The audit on 2026-09-21 found the real
state was worse than uneven: only **two** `BossAttackPatternData` assets existed and all five
bosses shared them, so every boss fired the same aimed fan and differed only in numbers.

`BossAttackShape` is the fix, and it is one data field rather than a subsystem. It changes how
the existing projectile burst is *arranged*, reusing the same pool, damage context and
telegraph:

| Shape | Behaviour | How the player dodges it |
| --- | --- | --- |
| Fan | spread across the aim cone | step sideways out of the cone |
| Ring | full 360 degrees, ignores aim | find a gap, or be far away |
| Spiral | rotating offset per volley, sweeps the arena | move with the sweep |
| Volley | parallel shots abreast, a wall | go around an end |
| Lance | every shot on one bearing, staggered | never stand in line |

Assigned so that learning one boss does not teach the next:

| Stage | Boss | Phase 1 | Phase 2 |
| --- | --- | --- | --- |
| 1 | Foundry | Fan | Fan |
| 2 | Reactor Sentinel | Ring | Ring |
| 3 | Salvage Overseer | Volley | Volley |
| 4 | Cryo Warden | Spiral | Spiral |
| 5 | Core Nexus | Ring | Lance |

Stage 2's Ring matches its own design note about an expanding hazard zone. Stage 1 is unchanged,
so the first boss a player meets still behaves exactly as it did.

A full ring divides the spread by the projectile count rather than count minus one; dividing by
count minus one puts the first and last shot on the same bearing, leaving a doubled shot and a
visible gap opposite it.

**Still open:** helper-spawn variety. Only Stage 3 is designed around summoning adds, and the
per-stage phase assets currently carry no helper groups.

---

## Reward-Bundle (2026-09-22)

Was ein Boss-Sieg auszahlt, stand bisher verteilt in fünf Specs. Dies ist die einzige Liste; die
anderen Dokumente behalten ihre Zahlen und verweisen hierher.

| Teil | Wert | Quelle der Zahlen |
| --- | --- | --- |
| Skill Points | keine mehr pro Sieg (seit 2026-09-29: SP kommen aus Stufen und Quests) | [Economy.md](Economy.md), Währungstabelle |
| Skill Points, erster Sieg über diese Boss-Id | +1 über die Quest "Besiege den Boss von Stage N" (seit 2026-09-29); jeder Sieg zahlt 300 × Stage XP | dito |
| Alloy | Stage 1-5: 6 / 8 / 10 / 12 / 14 | dito |
| Alloy, erster Sieg über diese Boss-Id | +10 | dito |
| Card-Reward | ein reguläres Drei-Karten-Angebot | [Cards.md](Cards.md) |
| Weapon | **garantiert eine Weapon-Card im Angebot** | [WeaponLoadout.md](WeaponLoadout.md) |
| Relic | ein zufälliges, noch nicht besessenes Relic | [Relics.md](Relics.md) |
| Stage-Abschluss | `StageRewardData.CompletionResourceBonus` in Scrap | [Stages.md](Stages.md) |

Ausgezahlt wird im Moment des Boss-Todes direkt in den Save (`MetaRewardLedger`), damit ein Run, der
den Boss besiegt und danach scheitert, seine Punkte behält.

**Hinweis (2026-09-22):** Kommt die garantierte Weapon-Card bei vollem Loadout an, wartet sie als
*pending*. Die nächste Stage beginnt ohne Intermission, die Waffe kann also erst nach deren erster
Welle platziert werden. Solange eine Waffe wartet, werden keine weiteren Weapon-Cards angeboten.

---

## Boss-Widerstand gegen Tower (2026-09-22)

Owner-Entscheidung: Ein Boss hat **2.5x so viel Health** wie zuvor (Stage 1-5: 100 / 162 / 225 /
300 / 400) und nimmt **90 % weniger Schaden von Towers**
(`CombatHitReceiver.SetTowerDamageMultiplier(0.1f)`, nur auf dem Boss-Receiver).

Begründung: Der Boss ist laut diesem Dokument ein Build-Check. Solange eine Turmlinie mitschießt,
prüft er vor allem, wie viele Türme stehen. Mit dem Widerstand entscheidet die Waffenwahl des
Spielers, während Türme weiterhin die Helfer-Enemies abräumen.
