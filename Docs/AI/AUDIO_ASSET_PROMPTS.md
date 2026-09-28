# Audio Asset Prompts

Diese Datei enthält Prompts für die Audio-Assets (Musik-Loops und SFX-Cues), die `AudioCueData`/`BossAudioCueData`/`WaveAudioCueData`/`FactoryAudioCueData`/`MainMenuAudioCueData` und `MusicStateData` aktuell nur als prozedural erzeugte Sinuston-Platzhalter abbilden (`ProceduralToneFactory`). Sobald echte Clips vorliegen, werden sie einfach in die jeweiligen `AudioClip`-Felder der ScriptableObject-Assets gezogen (`_clipVariations` bzw. `_clip`) — der Code fällt automatisch auf den Platzhalterton zurück, solange die Felder leer sind, es ist also kein Code-Risiko, das schrittweise zu befüllen.

## Empfohlenes kostenloses KI-Tool

**Für SFX (kurze Ein-Schuss-Sounds): ElevenLabs Sound Effects** (elevenlabs.io, Text-zu-SFX-Modell). Kostenloses Kontingent reicht für kurze, wenige-Sekunden-Sounds wie sie hier gebraucht werden; Ergebnis ist direkt als .mp3/.wav exportierbar, keine Lizenzprobleme für ein eigenes Spielprojekt (Free-Tier-Nutzungsbedingungen prüfen, falls das Spiel später kommerziell erscheinen soll).

**Für Musik-Loops (Calm/Combat/Boss): Stable Audio Open** (Stability AI, offene Gewichte, kostenlos über den Hugging-Face-Space `stabilityai/stable-audio-open-1.0` nutzbar, keine Account-Kosten). Erzeugt bis zu ~47s Instrumentalmusik/Loops aus Textprompts, eignet sich gut für kurze, seamless-loopbare Ambient-/Combat-Layer. Alternativ, falls ein Konto-basierter Dienst bevorzugt wird: **Suno** (kostenloses Tageskontingent) — eher für songartiges Material, für reine Instrumental-Loops ist Stable Audio Open zielgenauer.

Beide Tools sind aktuell (Stand 2026) kostenlos nutzbar ohne Kreditkarte für den hier benötigten Umfang. Für Loop-Nahtlosigkeit empfiehlt sich, im Export ein kurzes Crossfade/Fade-In-Out selbst in einem freien Editor (z. B. Audacity) zu glätten, falls das generierte Ergebnis am Loop-Punkt hörbar knackt.

## Lokales Hosting (RTX 2080, 8 GB VRAM)

**Ollama selbst kann das nicht** — Ollama hostet ausschließlich Text-/Sprach-LLMs, kein Audio-Generierungsmodell. Für lokale Audio-Generierung brauchst du ein separates Setup:

- **Empfehlung: Stable Audio Open lokal über ComfyUI.** ComfyUI hat native Audio-Nodes für Stable Audio Open; das Modell läuft in fp16 komfortabel in 8 GB VRAM für die hier benötigten kurzen Clips (Sekunden bis ~45s). Lizenz (Stability AI Community License) erlaubt kommerzielle Nutzung für Einzelpersonen/kleine Teams bis zu einer Umsatzgrenze — für ein Early-Access-Solo-/Kleinteam-Projekt in der Regel unkritisch, Lizenztext vor Release trotzdem einmal selbst gegenchecken.
- **Alternative für SFX speziell: AudioGen/MusicGen (Meta AudioCraft)**, ebenfalls lokal auf einer 8-GB-Karte lauffähig (kleine/mittlere Checkpoints). Wichtig: die veröffentlichten Gewichte stehen unter einer **Non-Commercial-Lizenz (CC-BY-NC)** — nur für Prototyping/interne Tests nutzen, nicht für Assets, die im veröffentlichten Spiel landen sollen, falls das Spiel kommerziell erscheinen soll.

Für ein Early-Access-Spiel, das später verkauft werden soll, ist Stable Audio Open (lokal oder über den kostenlosen Hugging-Face-Space) der unkompliziertere Weg als AudioGen/MusicGen.

### Lokales Setup — Stand 2026-09-08

Automatisiert bereits erledigt (auf dieser Maschine):

1. ComfyUI nach `C:\Users\Philipp\ComfyUI` geklont.
2. Python-venv unter `ComfyUI\venv` mit CUDA-fähigem PyTorch 2.5.1 (cu121) — verifiziert, erkennt die RTX 2080.
3. Alle ComfyUI-`requirements.txt`-Pakete installiert (Node-Unterstützung für Audio ist bereits Teil von ComfyUI selbst: `comfy_extras/nodes_audio.py`, `comfy/ldm/audio`).

Noch manuell nötig (braucht Browser-Login, kann nicht automatisiert werden):

1. Auf https://huggingface.co/stabilityai/stable-audio-open-1.0 einloggen und die Lizenzbedingungen einmalig akzeptieren (gated Repo).
2. Die Checkpoint-Datei (`.safetensors`) herunterladen und nach `C:\Users\Philipp\ComfyUI\models\checkpoints\` legen.
3. ComfyUI starten: `C:\Users\Philipp\ComfyUI\venv\Scripts\python.exe main.py` im Ordner `C:\Users\Philipp\ComfyUI`, dann `http://127.0.0.1:8188` im Browser öffnen.
4. Im ComfyUI-Menü „Workflow → Browse Templates" nach einer Audio-/Stable-Audio-Vorlage suchen (ComfyUI bringt Standard-Vorlagen mit, sobald ein passender Checkpoint im `checkpoints`-Ordner liegt) statt einen Workflow von Hand zu bauen.
5. Die 19 Prompts oben einzeln in den Positive-Prompt-Node eintragen, `EmptyLatentAudio`-Dauer passend zur jeweiligen Zielspiellänge setzen (siehe Tabelle unten), rendern, als `.wav`/`.mp3` speichern und in die jeweiligen `AudioClip`-Felder der `AudioCueData`/`MusicStateData`-Assets ziehen.

### Wichtig: `EmptyLatentAudio`-Dauer bestimmt die Ausgabelänge, nicht der Prompt-Text

**Erkenntnis aus Audio 04/05 (2026-09-08):** `sfx_combat_hit`/`sfx_combat_death` wurden mit "roughly 0.1-0.15 seconds" im Prompt generiert, kamen aber trotzdem als ~47s-Dateien zurück — mit jeweils mehreren wiederholten Varianten des kurzen Sounds hintereinander, nicht einem einzelnen langen Klang. Die Sekundenangabe im Prompt-Text beeinflusst offenbar nur, wonach der generierte Inhalt klingt (kurzer perkussiver Treffer vs. langer Ton), nicht die tatsächliche Dateilänge — die wird ausschließlich vom `EmptyLatentAudio`-Node-Parameter bestimmt. Die Sekundenangaben in den Prompts unten bleiben deshalb als Inhalts-Vorgabe stehen, sind aber **kein Ersatz** für das manuelle Setzen der Node-Dauer.

Empfohlene `EmptyLatentAudio`-Werte je nach Ziel-Länge des Cues, mit Puffer statt exaktem Wert (zu knapp bemessen riskiert einen abgeschnittenen Klang, siehe Erfahrung mit sfx_combat_death oben):

| Ziel-Länge im Prompt | Empfohlene `EmptyLatentAudio`-Sekunden | Betrifft |
|---|---|---|
| ~0.07-0.2s | 1.5s | Audio 06-09, 13, 16-19 (Wave Start/Elite/Complete/Intermission, Boss Phase Change, alle Menu-Cues, Factory) |
| ~0.25-0.35s | 2s | Audio 10-12 (Stage Complete, Boss Warning, Boss Spawn) |
| ~0.4-0.5s | 2.5s | Audio 14 (Boss Defeat) |
| ~30-45s (Musik) | 45s (Maximum) | Audio 01-03 |

Auch bei diesen kürzeren Einstellungen kann das Modell mehrere Wiederholungen statt eines einzelnen Klangs erzeugen — das ist kein Fehler, sondern nutzbar: mehrere echte Varianten aus einer Generierung sind besser als eine (`AudioCueData._clipVariations` ist ein Array, `AudioManager` wählt bereits zufällig daraus). Bei Bedarf beim Zuschneiden helfen (Amplituden-Analyse + ffmpeg, wie bei Audio 04/05 gemacht) statt jede Datei einzeln von Hand in einem Editor zu suchen.

Globaler Stilrahmen für alle Prompts:

- stylized sci-fantasy industrial defense setting, not orchestral-epic, not chiptune
- short, punchy, readable one-shot cues for SFX; steady, non-distracting, loop-friendly beds for music
- no vocals, no spoken lines, no baked-in game logos or voice tags
- deliver as mono or stereo .wav/.mp3, normalized, no clipping

## Musik-Loops (MusicStateData)

### Audio 01 - Music Calm

Target: Assets/Audio/Music/music_calm_loop.mp3

**Status:** DELIVERED and wired.

Prompt:

A calm, low-tension ambient loop for the "between waves" state of a sci-fantasy industrial defense game. Sparse, slow synth pads with a faint mechanical hum undertone, subtle rhythmic pulse suggesting an idle factory/Core system rather than silence. Should feel safe but purposeful, not sleepy. Seamlessly loopable, roughly 30-45 seconds, no percussion hits, no melody spikes that would clash with UI sounds.

### Audio 02 - Music Combat

Target: Assets/Audio/Music/music_combat_loop.mp3

**Status:** DELIVERED and wired.

Prompt:

A mid-tension driving combat loop for a top-down action roguelite with tower-defense and factory elements. Steady rhythmic percussion, industrial synth bass, a repeating tense melodic motif, energy that supports fast decision-making without becoming exhausting over a multi-minute wave. Seamlessly loopable, roughly 30-45 seconds, no vocals, headroom left for hit/impact SFX layered on top.

### Audio 03 - Music Boss

Target: Assets/Audio/Music/music_boss_loop.mp3

**Status:** DELIVERED and wired.

Prompt:

A high-tension boss-encounter loop for a sci-fantasy industrial defense roguelite. Heavier percussion, darker bass tone, a more aggressive and urgent melodic motif than the standard combat loop, communicating a distinct threat escalation while staying in the same instrumental palette/family as the combat loop so the transition feels related, not jarring. Seamlessly loopable, roughly 30-45 seconds, no vocals.

## Combat SFX (AudioCueData)

### Audio 04 - Combat Hit

Target: Assets/Audio/SFX/sfx_combat_hit.mp3

**Status:** DELIVERED and wired.

Prompt:

A short, punchy impact sound effect for a projectile or melee hit landing on an enemy in a sci-fantasy action game. Crisp, tight, mid-frequency thud with a subtle metallic/energy edge. Roughly 0.1-0.15 seconds, no reverb tail, mono, suitable for rapid repeated triggering without becoming fatiguing.

### Audio 05 - Combat Death

Target: Assets/Audio/SFX/sfx_combat_death.mp3

**Status:** DELIVERED and wired.

Prompt:

A short enemy-death/dissolve sound effect for a top-down sci-fantasy action game. A quick downward-pitched energy dissipation sound, slightly longer and more resolved than a hit sound, communicating finality without being harsh. Roughly 0.3-0.4 seconds, no reverb tail, mono.

## Wave-Event SFX (WaveAudioCueData)

### Audio 06 - Wave Start

Target: Assets/Audio/SFX/sfx_wave_start.mp3

**Status:** DELIVERED and wired.

Prompt:

A short alert chime signaling a new enemy wave beginning in a sci-fantasy defense game. Bright, clear, single rising tone, urgent but not alarming, roughly 0.15 seconds, mono, cuts through combat noise without being harsh.

### Audio 07 - Elite Wave

Target: Assets/Audio/SFX/sfx_wave_elite.mp3

**Status:** DELIVERED and wired.

Prompt:

A short warning chime signaling an elite/stronger enemy wave beginning in a sci-fantasy defense game. Lower-pitched and slightly more dissonant variant of a standard wave-start chime, communicating heightened danger. Roughly 0.18 seconds, mono, no reverb tail.

### Audio 08 - Wave Complete

Target: Assets/Audio/SFX/sfx_wave_complete.mp3

**Status:** DELIVERED and wired.

Prompt:

A short resolving success chime signaling an enemy wave has been cleared in a sci-fantasy defense game. Bright, upward two-note resolution, satisfying but brief, roughly 0.18 seconds, mono.

### Audio 09 - Intermission

Target: Assets/Audio/SFX/sfx_wave_intermission.mp3

**Status:** DELIVERED and wired.

Prompt:

A short, calm transitional chime signaling the intermission/planning phase between waves in a sci-fantasy defense game. Soft, settling single tone, communicates a breathing moment. Roughly 0.12 seconds, mono, no reverb tail.

### Audio 10 - Stage Complete

Target: Assets/Audio/SFX/sfx_stage_complete.mp3

**Status:** DELIVERED and wired.

Prompt:

A short triumphant fanfare-style sound effect signaling an entire stage has been completed in a sci-fantasy defense roguelite. More elaborate and satisfying than the wave-complete chime, a brief rising multi-note flourish, roughly 0.25-0.3 seconds, mono, no vocals, no baked reverb longer than the cue itself.

## Boss-Event SFX (BossAudioCueData)

### Audio 11 - Boss Warning

Target: Assets/Audio/SFX/sfx_boss_warning.mp3

**Status:** DELIVERED and wired.

Prompt:

A short ominous warning sound signaling an imminent boss encounter in a sci-fantasy defense game. Low rumbling tone with a sharp rising edge, communicates dread and urgency. Roughly 0.25 seconds, mono, no reverb tail.

### Audio 12 - Boss Spawn

Target: Assets/Audio/SFX/sfx_boss_spawn.mp3

**Status:** DELIVERED and wired.

Prompt:

A short, heavy impact/arrival sound effect for a boss entering the arena in a sci-fantasy defense game. Deep bass hit with a metallic industrial resonance, communicates scale and weight. Roughly 0.35 seconds, mono, no melodic content.

### Audio 13 - Boss Phase Change

Target: Assets/Audio/SFX/sfx_boss_phase_change.mp3

**Status:** DELIVERED and wired.

Prompt:

A short, sharp alert sound effect signaling a boss transitioning to a new combat phase in a sci-fantasy defense game. Bright, tense, single distinctive tone, easily distinguishable mid-fight from other cues. Roughly 0.2 seconds, mono, no reverb tail.

### Audio 14 - Boss Defeat

Target: Assets/Audio/SFX/sfx_boss_defeat.mp3

**Status:** DELIVERED and wired.

Prompt:

A short but satisfying victory sound effect for defeating a boss in a sci-fantasy defense roguelite. A resolving downward-then-upward energy collapse followed by a bright final resolve tone, communicating a big win. Roughly 0.4-0.5 seconds, mono, no vocals.

## Factory SFX (FactoryAudioCueData)

### Audio 15 - Resource Generated

Target: Assets/Audio/SFX/sfx_factory_resource_generated.mp3

**Status:** DELIVERED and wired.

Prompt:

A very short, light mechanical "ding"/chime sound effect for a factory production node generating a resource in a sci-fantasy industrial defense game. Small, crisp, positive tick sound, must be pleasant even when triggered frequently in quick succession. Roughly 0.08 seconds, mono, no reverb tail.

## Main-Menu SFX (MainMenuAudioCueData)

### Audio 16 - Menu Open

Target: Assets/Audio/SFX/sfx_menu_open.mp3

**Status:** DELIVERED and wired.

Prompt:

A short UI sound effect for opening a menu panel in a controller-first sci-fantasy game. Soft rising swoosh with a subtle mechanical panel-sliding character. Roughly 0.14 seconds, mono, no reverb tail.

### Audio 17 - Menu Navigate

Target: Assets/Audio/SFX/sfx_menu_navigate.mp3

**Status:** DELIVERED and wired.

Prompt:

A very short, subtle UI tick sound effect for moving focus between menu entries in a controller-first sci-fantasy game. Light, neutral click/tick, unobtrusive even when triggered rapidly. Roughly 0.07 seconds, mono.

### Audio 18 - Menu Confirm

Target: Assets/Audio/SFX/sfx_menu_confirm.mp3

**Status:** DELIVERED and wired.

Prompt:

A short, positive UI confirmation sound effect for a controller-first sci-fantasy game. Clear, bright, single affirmative tone communicating a successful selection. Roughly 0.12 seconds, mono, no reverb tail.

### Audio 19 - Menu Cancel

Target: Assets/Audio/SFX/sfx_menu_cancel.mp3

**Status:** DELIVERED and wired.

Prompt:

A short, neutral UI cancel/back sound effect for a controller-first sci-fantasy game. Slightly lower-pitched and softer than the confirm sound, communicating a step back without feeling like an error. Roughly 0.1 seconds, mono, no reverb tail.

## Stage Boss Loops (2026-09-14, 5-Stage Concept)

`MusicStateData` only has three global states (Calm/Combat/Boss) - there is no per-stage music-swapping code yet, and adding one is a bigger architecture change than this pass covers. The pragmatic middle ground matching [Stages.md](../Specs/Stages.md)'s 5-stage concept: keep the shared Calm/Combat loops (Audio 01-02) as-is across all stages, but give each stage's boss fight its own **variant of the Boss loop** (Audio 03) that layers a stage-specific motif on top of the same instrumental family - `BossData` would need a `_musicOverride` field (`AudioClip`, falls back to the shared boss loop when unset) for `MusicStateController` to check, the same optional-override pattern `WaveAudioCueData`'s `HasRealXClip` already uses elsewhere. These four loops are that per-boss layer for Stage 2-5's bosses (Stage 1 keeps the existing shared Boss loop, Audio 03).

### Audio 20 - Boss Loop Reactor Sentinel (Stage 2)

Target: Assets/Audio/Music/music_boss_stage2_loop.mp3

**Status:** DELIVERED and wired.

Prompt:

A high-tension boss-encounter loop for a reactor-guardian fight in a sci-fantasy industrial defense roguelite, in the same instrumental family as the base combat/boss loops. Add a pulsing, alarm-like containment-field motif and a resonant metallic drone underneath the percussion, suggesting an unstable reactor core. Heavier percussion and darker bass than the standard combat loop. Seamlessly loopable, roughly 30-45 seconds, no vocals.

### Audio 21 - Boss Loop Salvage Overseer (Stage 3)

Target: Assets/Audio/Music/music_boss_stage3_loop.mp3

**Status:** DELIVERED and wired.

Prompt:

A high-tension boss-encounter loop for a scrapyard-overseer fight in a sci-fantasy industrial defense roguelite, in the same instrumental family as the base combat/boss loops. Grittier, more percussive than the standard boss loop, with a mechanical clanking/junkyard-machinery rhythm layer suggesting drones being launched from wreckage. Heavier percussion and darker bass than the standard combat loop. Seamlessly loopable, roughly 30-45 seconds, no vocals.

### Audio 22 - Boss Loop Cryo Warden (Stage 4)

Target: Assets/Audio/Music/music_boss_stage4_loop.mp3

**Status:** DELIVERED and wired.

Prompt:

A high-tension boss-encounter loop for a frost-guardian fight in a sci-fantasy industrial defense roguelite, in the same instrumental family as the base combat/boss loops. Colder and sparser than the standard boss loop, with a shimmering high-register crystalline motif (bell-like or glassy texture) layered over the usual heavier percussion and dark bass, suggesting an icy, controlled threat rather than raw aggression. Seamlessly loopable, roughly 30-45 seconds, no vocals.

### Audio 23 - Boss Loop Final Boss (Stage 5, Core Nexus)

Target: Assets/Audio/Music/music_boss_stage5_loop.mp3

**Status:** DELIVERED and wired.

Prompt:

The most intense boss-encounter loop in the game for the final Core Nexus fight in a sci-fantasy industrial defense roguelite, in the same instrumental family as the base combat/boss loops but pushed further: the heaviest percussion, darkest bass and most aggressive melodic motif of the set. Briefly and subtly echo the containment-alarm motif (Audio 20), the junkyard-machinery rhythm (Audio 21) and the crystalline high-register motif (Audio 22) within the arrangement, so the final fight musically resolves the previous three boss themes instead of introducing an unrelated fourth one. Seamlessly loopable, roughly 30-45 seconds, no vocals.

## Run Completion (2026-09-15)

Neither a "you beat the game" cue nor a credits track exists yet - `WaveManager.CompleteStage()`'s `ShouldEndRunOnCompletion()` branch (Stage 5/Core Nexus, `IsFinalStage`) currently has no dedicated musical payoff beyond the existing Audio 10 (Stage Complete) SFX chime. These two fill that gap: a short victory sting for the moment the run actually ends, and a longer credits loop/track for whatever screen follows it.

### Audio 24 - Run Victory Sting

Target: Assets/Audio/Music/music_run_victory_sting.mp3

**Status:** DELIVERED but NOT WIRED - the file exists; its `AudioClip` field is still empty, so the placeholder tone is what actually plays.

Prompt:

A short, triumphant victory sting for completing an entire run in a sci-fantasy industrial defense roguelite - the payoff moment after defeating the Final Boss (Core Nexus). More elaborate, resolved and ceremonial than the Stage Complete chime (Audio 10): a rising fanfare-like flourish that resolves into a warm, settled final chord, echoing the white-gold "Core Nexus" motif established in the Stage 5 boss loop (Audio 23) rather than introducing new unrelated melodic material. Roughly 4-6 seconds, not intended to loop, no vocals, no baked reverb longer than the cue itself.

### Audio 25 - Credits Loop

Target: Assets/Audio/Music/music_credits_loop.mp3

**Status:** DELIVERED but NOT WIRED - the file exists; its `AudioClip` field is still empty, so the placeholder tone is what actually plays.

Prompt:

A warm, reflective instrumental loop for the credits screen of a sci-fantasy industrial defense roguelite, in the same instrumental family as the game's Calm loop (Audio 01) but more melodically developed and satisfying to listen to on repeat for several minutes - this plays while the player reads scrolling credits, not during gameplay. Gentle synth pads, a slow, memorable melodic theme that echoes the game's amber/steel-blue "Core" motif established across the Calm and Combat loops, subtle mechanical-hum undertone kept very low in the mix. Seamlessly loopable, roughly 45-60 seconds, no percussion hits or sudden dynamic spikes that would feel jarring on a long loop, no vocals.

## Arena Objectives (2026-09-27)

Die Salvage Cache (Arena-Objective, siehe [OPEN_TOPICS.md](../OPEN_TOPICS.md)) erscheint mitten in
einer Wave an einer zufälligen Position und läuft ab, wenn niemand sie einsammelt. Der ganze Sinn
ist eine Entscheidung unter Zeitdruck — und genau die funktioniert nur, wenn der Spieler das
Erscheinen **hört**, ohne hinsehen zu müssen: er kämpft gerade woanders. Das Ablaufen braucht
ebenfalls einen eigenen Cue, sonst bleibt "verpasst" unsichtbar.

Alle drei gehören in eine gemeinsame Klangfamilie, damit sie als ein System lesbar sind, und dürfen
Boss-Telegraphen (Audio 11–13) nicht überdecken.

### Audio 26 - Arena Objective Spawn

Target: Assets/Audio/SFX/sfx_objective_spawn.mp3

**Status:** DELIVERED and wired.

Prompt:

A short attention-pulling alert for a salvage container appearing somewhere in the arena of a sci-fantasy industrial defense game. A bright two-note rising ping with a faint metallic container-latch undertone, clearly audible over ongoing combat but distinctly lighter and less threatening than a boss or wave warning - this is an opportunity, not a danger. Roughly 0.25 seconds, mono, no reverb tail.

### Audio 27 - Arena Objective Claimed

Target: Assets/Audio/SFX/sfx_objective_claimed.mp3

**Status:** DELIVERED and wired.

Prompt:

A short satisfying pickup sound for claiming a salvage container in a sci-fantasy industrial defense game. A clasp releasing followed by a warm resolving chime, richer and more rewarding than the small resource-pickup tick but shorter than a wave-complete fanfare. Must feel like a reward worth having crossed the arena for. Roughly 0.3 seconds, mono.

### Audio 28 - Arena Objective Lost

Target: Assets/Audio/SFX/sfx_objective_lost.mp3

**Status:** DELIVERED and wired.

Prompt:

A short deflating sound for a salvage container expiring uncollected in a sci-fantasy industrial defense game. A soft downward two-note fall with a faint mechanical powering-down undertone. It must read as a missed opportunity, not as damage or failure - disappointed, never punishing. Roughly 0.25 seconds, mono, no reverb tail.
