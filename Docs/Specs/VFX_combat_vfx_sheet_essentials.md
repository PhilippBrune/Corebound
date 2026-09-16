# VFX Specification

## Purpose

Definiert die gemeinsame VFX-Sprache des ersten Vertical Slice.

VFX verbinden Combat-Lesbarkeit, Reward-Gewichtung, Stage-Übergänge und Bedrohungskommunikation über wiederverwendbare 2D-Effektfamilien.

## Design Goals

- Klare, schnelle Lesbarkeit bei hoher Combat-Dichte
- Wiederverwendbare Effektfamilien statt vieler isolierter Einzelassets
- Gute Synchronisierung mit Audio-Cues, Schaden und Reward-Momenten
- Controller-first-Erkennbarkeit ohne Detailrauschen
- Daten- und Pooling-freundliche Nutzung innerhalb von Unity 2D

## Gameplay

VFX markieren Treffer, Bedrohung, Zerstörung, Status Effect, Reward-Reveal und Aktivierungsmomente — genau die Art von Feedback, die Vision's Art Direction unter „Viele Partikeleffekte" und „Klare Silhouetten" fordert.

Sie verstärken Lesbarkeit und Gewichtung eines Ereignisses, dürfen aber nie die Kerngefahr oder Interaktionsaufforderung überdecken.

**Update (2026-09-08):** Der ursprünglich geplante gemeinsame Atlas unter `Assets/Art/Combat/VFX/combat_vfx_sheet_essentials.png` erwies sich als unregelmäßig layoutet (unterschiedliche Effektgrößen, kein gleichmäßiges Zellraster) und ist damit weder per Sprite Editor noch per Runtime-Grid-Math zuverlässig automatisch nutzbar. Der erste Vertical Slice nutzt stattdessen **7 einzelne, freistehende Effektbilder** unter `Assets/Art/Combat/VFX/` (`vfx_hit_spark.png`, `vfx_enemy_death_burst.png`, `vfx_core_damage_flare.png`, `vfx_card_reveal_flash.png`, `vfx_burn_status_tick.png`, `vfx_freeze_status_pulse.png`, `vfx_tower_activation_pulse.png`), jedes direkt als eigener Sprite importiert und referenziert. Das alte Sheet bleibt im Repository, ist aber vollständig verwaist (0 Code-Referenzen) und sollte perspektivisch entfernt werden. Die „wiederverwendbare Effektfamilie" aus den Design Goals gilt weiterhin — nur die technische Umsetzung ist jetzt „ein Bild pro Effekt" statt „ein Atlas mit Sub-Regionen".

## Rules

- VFX priorisieren Lesbarkeit vor dekorativer Dichte.
- Jede Effektfamilie besitzt eine klare Aufgabe wie Treffer, Warnung, Zerstörung oder Reward-Reveal.
- Combat-kritische Signale bleiben kurz, kontraststark und pooling-freundlich.
- Core-Schaden, Boss-Gefahr und Enemy-Telegraphs haben Vorrang vor sekundären Effekten.
- Neue Effekte erweitern bestehende Familien, statt unverbundene Einzelsprachen einzuführen.

## Data Model

VFX-Daten werden als wiederverwendbare Effekt-Präsentationsfamilien verstanden — im ersten Slice als einzelne Sprites statt Atlas-Sub-Regionen, das Rollenmodell bleibt aber identisch:

- VfxAtlasData: im ersten Slice nicht benötigt (kein Atlas), bleibt als Konzept für eine spätere Rückkehr zu Sheet-basierten Effekten vorgesehen
- VfxEventBindingData: Zuordnung von Combat-, Reward-, Core- oder Tower-Ereignissen auf konkrete Effektbilder
- VfxPlaybackData: Dauer, Layer, Tint, Skalierung und Pooling-Verhalten
- VfxPriorityData: Priorisierung bei gleichzeitigen Warn- und Trefferereignissen

## Technical Design

Der erste Slice nutzt 7 einzelne Effektbilder unter `Assets/Art/Combat/VFX/` (siehe Update-Hinweis unter Gameplay) statt eines gemeinsamen Atlas:

- Hit-Spark (`vfx_hit_spark.png`) — Projectile-Hit-Sparks und kleine Impact-Bursts
- Enemy-Death-Burst (`vfx_enemy_death_burst.png`) — größere Zerstörungsimpulse
- Core-Damage-Flare (`vfx_core_damage_flare.png`)
- Card-Reveal-Flash (`vfx_card_reveal_flash.png`) — Card-Reward-Reveal-Momente
- Burn-Status-Tick (`vfx_burn_status_tick.png`)
- Freeze-Status-Pulse (`vfx_freeze_status_pulse.png`)
- Tower-Activation-Pulse (`vfx_tower_activation_pulse.png`) — Tower-Aktivierungsringe und Emittor-Pulse

Größere Device-, Boss- oder Stage-Übergangs-Eruptionen sind noch nicht als eigenes Bild erstellt und bleiben eine offene Erweiterung (siehe Open Questions).

Runtime-Systeme laden jedes Bild einmal (`DebugSpriteFactory.LoadProjectSprite`) und cachen den resultierenden Sprite statisch, statt pro Spawn neu zu laden — das ersetzt die ursprünglich vorgesehene Atlas-Region-Wiederverwendung 1:1 durch Sprite-Wiederverwendung.

## Dependencies

- Combat
- Enemies
- Towers
- Cards
- Core
- Stages
- Graphics
- UI
- Audio

## UI

UI-nahe VFX bleiben kurz und unterstuetzen Reward-, Warn- und Confirm-Momente ohne Texteingaben zu verdecken.

Reward-Reveals und Stage-Abschlussmomente duerfen groesser gewichtet sein als normale Combat-Treffer.

## Audio

Jede groessere Effektfamilie braucht einen passenden Audio-Partner mit derselben Prioritaetensprache.

Hit-Sparks, Core-Schaden, Reward-Reveal und Tower-Aktivierung muessen visuell und akustisch als zusammengehoerige Ereignisse lesbar bleiben.

## Visual

Effekt-Übersicht für die 7 Einzelbilder unter `Assets/Art/Combat/VFX/` (ersetzt das frühere Atlas-Cheat-Sheet):

- **Hit-Spark**: kleiner, punktueller weiß-gelber Energieburst mit farbigem Rand, für jeden Projectile-Treffer. Muss bei sehr häufiger Wiederholung nicht ermüden.
- **Enemy-Death-Burst**: etwas größerer, kühlerer Auflösungs-Burst mit einem Akzentton, für besiegte Gegner.
- **Core-Damage-Flare**: scharfer, alarmierender rot-oranger Warnblitz — höchste Signal-Priorität im gesamten Effekt-Set, siehe Balancing.
- **Card-Reveal-Flash**: warmes, einladendes gold-weißes Glühen hinter einer Karte im Moment der Aufdeckung.
- **Burn-Status-Tick**: kompakte, nach oben züngelnde Flammen-Wisp für laufende Burn-Ticks, muss auch mehrfach gleichzeitig über kleinen Gegnersprites lesbar bleiben.
- **Freeze-Status-Pulse**: kompakter, radial ausstrahlender Frost-Puls mit scharfen Kristallkanten für laufende Freeze-Effekte.
- **Tower-Activation-Pulse**: nach außen laufender Energiering im stahlblau/amber Turm-Farbschema für jede Tower-Aktivierung.

Größere Boss-/Stage-Übergangs-Eruptionen (ursprünglich als „Dark Geometric Energy Eruptions"/„Large Glowing Energy Ring" im alten Atlas-Konzept vorgesehen) existieren noch nicht als eigenes Bild.

## Balancing

VFX-Balancing bedeutet in diesem Projekt Signaldichte, Prioritaet und Dauer zu kontrollieren.

Kleine Treffer duerfen haeufig sein, grosse Reward- oder Core-Warnmomente dagegen selten und eindeutig.

Effekte fuer Burn, Freeze und Projectile-Impacts muessen in hohen Spawnzahlen lesbar bleiben, ohne die Arena zu verrauschen.

## Open Questions

- 5 der 7 Effektbilder (Core-Damage-Flare, Card-Reveal-Flash, Burn-Tick, Freeze-Pulse, Tower-Activation-Pulse) existieren als Datei, sind aber noch an keinen Event-Hook angebunden — welche davon zuerst?
- Soll das verwaiste `combat_vfx_sheet_essentials.png` aus dem Repository entfernt werden, jetzt wo keine der 7 Ersatzdateien mehr ein gemeinsames Sheet ist?
- Braucht der erste Slice tatsächlich noch größere Boss-/Stage-Übergangs-Eruptionen, oder reichen die 7 vorhandenen Familien für den aktuellen Umfang?

Festgelegte Entscheidungen:

- Der erste Slice nutzt 7 einzelne Effektbilder statt eines gemeinsamen Combat-VFX-Atlas (Update 2026-09-08, siehe Gameplay/Technical Design) — der ursprüngliche Atlas-Ansatz scheiterte am unregelmäßigen Layout des Quellbilds.
- Stage-, Reward- und Tower-Momente dürfen dieselbe visuelle Sprache (Farbpalette, Kontraststärke) teilen, auch wenn sie als getrennte Bilder statt Atlas-Regionen vorliegen.
- Combat-kritische Warnungen (Core-Damage-Flare) bleiben kürzer und kontrastreicher als dekorative Abschluss- oder Reveal-Effekte (Card-Reveal-Flash).

## Acceptance Criteria

- Jedes der 7 Effektbilder besitzt eine klar dokumentierte Aufgabe (siehe Visual).
- Combat-, Core-, Reward- und Tower-Systeme können ihr jeweiliges Effektbild referenzieren, ohne eigene VFX-Logik zu duplizieren.
- VFX bleiben auch bei hoher Kampfdichte lesbar und pooling-freundlich.
- Die visuelle Priorität von Treffer, Warnung, Reward und Stage-Übergang ist dokumentiert und konsistent.
- Mindestens Hit-Spark und Enemy-Death-Burst sind aktiv an Combat-Events angebunden (erfüllt); die restlichen 5 Bilder folgen demselben Muster, sobald sie priorisiert werden.