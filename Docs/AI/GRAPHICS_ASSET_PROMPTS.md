# Graphics Asset Prompts

Diese Datei enthält vollständige Einzelprompts für die grafischen Assets des ersten Vertical Slice.

Globaler Stilrahmen für alle Prompts:

- Top-down 2D gameplay view for a Unity 6 URP 2D game
- optional 2.5D look only through painted volume, lighting response, shadow language and layered sprite depth
- stylized sci-fantasy defense aesthetic
- readable silhouettes, strong contrast, clean shapes, no photorealism
- controller-first readability, no tiny noisy details
- output as clean production-ready game asset with transparent background unless explicitly noted otherwise
- no baked text unless the prompt explicitly asks for logo lettering

Korrektur (2026-09-08): Ein früherer Eintrag hier behauptete, mehrere Assets (u. a. `card_frame_common.png`, `vfx_hit_spark.png`, `player_base_topdown.png`, `projectile_sheet_core.png`) hätten opakes Weiß statt echter Alpha-Transparenz. Das war ein Fehlschluss aus einer Bildvorschau, die transparente Bereiche als Weiß rendert statt als Schachbrettmuster. Eine direkte Pixel-Prüfung (PNG-Alphakanal-Histogramm) zeigt: alle betroffenen Dateien haben tatsächlich einen korrekten, substantiellen Alpha-0-Anteil (61–97 % der Fläche, passend zum jeweiligen Freistellungs-Motiv) — es gibt kein systemisches Transparenz-Problem. Nur `combat_vfx_sheet_essentials.png` (veraltet, wird nicht mehr genutzt), `stage1_ground_tileset.png` und `mainmenu_background.png` haben 0 % Alpha-0-Fläche, was für diese drei korrekt ist (bewusst voll deckende Assets). Tatsächlich real: 3 Dateien (`vfx_burn_status_tick.png`, `vfx_freeze_status_pulse.png`, `shop_tower_upgrade_icon.png`) kamen mit einer nicht-quadratischen Leinwand (1354×738 statt der angeforderten 1:1) zurück — das ist ein reines Seitenverhältnis-Problem, kein Transparenz-Problem. Die betroffenen Prompts (24e, 24f, 43) haben trotzdem eine verstärkte "1:1 square aspect ratio" Formulierung erhalten, das schadet nicht und hilft gegen das echte Seitenverhältnis-Problem.

## Asset audit - 2026-09-21

Every `Target:` path in this file was checked against disk. Each asset now carries a
`**Status:**` line directly under its target, so this never has to be re-derived by hand.

| Result | Count |
| --- | --- |
| Present | 76 |
| Present but misnamed | 2 |
| Only in the StreamingAssets mirror | 1 |
| Not created yet | 11 |
| **Total** | **90** |

### Needs attention

**Misnamed (doubled extension).** These load today only because the code carries an explicit
fallback path for the doubled name. Renaming them to the target name and re-running
`Corebound > Sync StreamingAssets Art Mirror` would let those fallbacks be deleted:

- `Assets/Art/Towers/tower_ballista_topdown.png`
- `Assets/Art/UI/HUD/shop_offer_panel.png`

**Present only in the StreamingAssets mirror.** The runtime reads the mirror first, so these
work in a build and in play, but the source copy under `Assets/Art` is gone. That is a silent
single-point-of-failure: clearing the mirror would lose the asset entirely.
- `Assets/Art/Combat/VFX/ranged_pressure_telegraph.png`

**Not created yet.**
- `Assets/Art/UI/MainMenu/glyph_hint_capsule.png`
- `Assets/Art/Characters/Enemies/enemy_zone_denier_topdown.png`
- `Assets/Art/Characters/Enemies/enemy_zone_denier_stage2_topdown.png`
- `Assets/Art/Characters/Enemies/enemy_zone_denier_stage3_topdown.png`
- `Assets/Art/Characters/Enemies/enemy_zone_denier_stage4_topdown.png`
- `Assets/Art/Characters/Enemies/enemy_zone_denier_stage5_topdown.png`
- `Assets/Art/Characters/Enemies/enemy_relay_warden_topdown.png`
- `Assets/Art/Characters/Enemies/enemy_relay_warden_stage2_topdown.png`
- `Assets/Art/Characters/Enemies/enemy_relay_warden_stage3_topdown.png`
- `Assets/Art/Characters/Enemies/enemy_relay_warden_stage4_topdown.png`
- `Assets/Art/Characters/Enemies/enemy_relay_warden_stage5_topdown.png`


**Recovered 2026-09-21:** all 15 `enemy_*_topdown.png` sprites were missing from
`Assets/Art/Characters/Enemies/` when this audit ran - deleted from the working tree with
nothing put in their place. They were still in git and still in the StreamingAssets mirror, so
nothing was lost, and they have been restored with `git checkout`. Worth knowing why they went:
the runtime reads the mirror first, so the game kept rendering enemies correctly and the
deletion was invisible in play.

---
## Main Menu Review Note

- Die Task-Formulierung verweist auf `Assets/Arts`; die aktuelle Unity-Implementierung nutzt die vorhandenen Main-Menu-Dateien unter `Assets/Art/UI/MainMenu`.
- Bereits vorhanden und durch die erste Main-Menu-Implementierung genutzt: Background, Logo, Primary Button Frame und Focus Marker.
- Zusätzliche dedizierte Main-Menu-Art, die im aktuellen Slice noch durch das generische Button-Frame ersetzt wird, ist unten als neue Prompt-Ergänzung dokumentiert.

## Asset 01 - MainMenu Background

Target: Assets/Art/UI/MainMenu/mainmenu_background.png

**Status:** present.

Prompt:

Create a wide 2D title-screen background for a top-down sci-fantasy action roguelite that blends deck builder, tower defense and factory themes. The scene should show a defended central Core platform in the distance, modular towers, subtle production-chain pipes or energy conduits, and a surrounding arena silhouette. Keep it clearly readable behind menu UI, with the visual focus centered slightly below the logo area. Use a stylized painterly look with optional 2.5D depth cues through layered foreground and background elements, soft atmospheric fog, and URP-friendly lighting logic. Color palette: steel blue, warm amber energy, dark slate, muted copper. No characters in close-up, no readable text, no photorealism, transparent UI-safe negative space in the middle-left area.

## Asset 02 - MainMenu Logo

Target: Assets/Art/UI/MainMenu/corebound_logo.png

**Status:** present.

Prompt:

Design a logo for the game title "Corebound" for a stylized top-down sci-fantasy defense roguelite. The logo should feel bold, memorable and slightly industrial, combining energy-core motifs with modular mechanical framing. Use clean shapes, strong readability at large and medium sizes, and subtle depth through layered 2D shading rather than true 3D extrusion. Palette should coordinate with steel blue, ember orange and pale metallic highlights. Deliver a transparent-background logo asset only, no mockup, no background scene, no extra subtitle.

## Asset 03 - MainMenu Primary Button Frame

Target: Assets/Art/UI/MainMenu/button_primary_frame.png

**Status:** present.

Prompt:

Create a modular 2D UI button frame for the main menu of a controller-first sci-fantasy Unity game. The frame should support 9-slice scaling, have a clear central content area, and communicate interactive weight without looking bulky. Style: clean industrial fantasy panel with soft energy seams and subtle metallic wear. The silhouette should be easy to distinguish when focused or idle. Use transparent background, no text, no icon, no decorative clutter that would hurt readability.

## Asset 04 - MainMenu Focus Marker

Target: Assets/Art/UI/MainMenu/focus_marker.png

**Status:** present.

Prompt:

Create a 2D controller-focus marker for menu navigation in a top-down sci-fantasy roguelite. The asset should feel precise, energetic and readable from a distance, working as a highlight around or beside a selected menu entry. Use a bright accent color that contrasts with dark menu panels, with a subtle animated-ready energy motif but no baked motion blur. Transparent background, clean silhouette, no text.

## Asset 05 - HUD Frame Panel

Target: Assets/Art/UI/HUD/hud_frame_panel.png

**Status:** present.

Prompt:

Create a modular 2D HUD frame panel for a top-down action-defense roguelite. It should support health, Core status, wave status and resource display, using a clean industrial sci-fantasy interface language that feels purposeful and readable during heavy combat. The frame must work as a scalable panel with transparent inner area and minimal visual noise. Use subtle metallic structure, faint energy accents, and strong edge readability. No text, no icons, transparent background.

## Asset 06 - Tutorial Prompt Panel

Target: Assets/Art/UI/Tutorial/tutorial_prompt_panel.png

**Status:** present.

Prompt:

Create a tutorial prompt panel for a controller-first top-down Unity roguelite. The panel should be compact, highly readable and non-intrusive, designed to float over gameplay without blocking the arena. Use a stylized sci-fantasy UI treatment with clean corners, soft glow accents and obvious content hierarchy. The asset must support 9-slice scaling and localized text. Transparent background outside the panel shape, no baked words or icons.

## Asset 07 - Card Frame Common

Target: Assets/Art/Cards/Frames/card_frame_common.png

**Status:** present.

Prompt:

Create a 2D card frame for a passive upgrade card in a sci-fantasy top-down roguelite. This is the Common rarity version. The frame should look readable, clean and modular, with a large center area for illustration and a distinct region for icon and title overlays. Use restrained industrial ornament, low-intensity energy accents and neutral metal tones. Transparent background, no text, no icon, no card art inside the frame.

## Asset 08 - Card Frame Rare

Target: Assets/Art/Cards/Frames/card_frame_rare.png

**Status:** present.

Prompt:

Create a 2D card frame for the Rare rarity of a passive upgrade card in a sci-fantasy top-down roguelite. The structure should match a shared family with the Common frame but feel more valuable through stronger energy detailing, cleaner lines and a richer accent color. Keep the illustration window and UI overlay zones highly readable. Transparent background, no text, no icon, no embedded card art.

## Asset 09 - Card Frame Epic

Target: Assets/Art/Cards/Frames/card_frame_epic.png

**Status:** present.

Prompt:

Create a 2D card frame for the Epic rarity of a passive upgrade card in a stylized sci-fantasy roguelite. The frame should feel powerful and aspirational without becoming visually noisy. Use higher-contrast materials, richer glow accents and a distinct silhouette from lower rarities while preserving the same layout family. Transparent background, no text, no icon, no illustration inside the frame.

## Asset 10 - Card Frame Legendary

Target: Assets/Art/Cards/Frames/card_frame_legendary.png

**Status:** present.

Prompt:

Create a 2D card frame for the Legendary rarity of a passive upgrade card in a top-down sci-fantasy roguelite. The frame should feel extremely important, rare and build-defining, with strong contrast, luminous energy architecture and a ceremonial but still readable structure. Maintain a clean illustration area and clear UI overlay zones. Transparent background, no text, no icon, no embedded artwork.

## Asset 11 - Card Tag Icon Sheet

Target: Assets/Art/Cards/Icons/card_tag_icon_sheet.png

**Status:** present.

Unity Import Note:

Die Datei bleibt als ein gemeinsames Sheet erhalten und wird in Unity als Multiple Sprite importiert und dort in einzelne Icons gesliced. Physisch getrennte Einzeldateien sind dafür nicht erforderlich.

Prompt:

Create a unified 2D icon sheet for the core card tags of a sci-fantasy defense roguelite: Weapon, Projectile, Tower, Resource, Status Effect, Burn, Freeze and Critical. Each icon must be readable at small size, visually consistent, simple in silhouette and optimized for UI use. Use transparent background, even spacing, and a shared line and shape language. Avoid text labels inside the asset.

## Asset 12 - Primary Resource Icon

Target: Assets/Art/Resources/resource_primary_icon.png

**Status:** present.

Prompt:

Create a primary run-resource icon for a top-down sci-fantasy roguelite. The icon should look collectible, valuable and immediately distinct from health, ammo or generic coins. Combine industrial energy-core language with a compact readable silhouette suitable for HUD, pickups and shop pricing. Transparent background, no text, no number overlays.

## Asset 13 - Tower Slot Marker

Target: Assets/Art/Towers/tower_slot_marker.png

**Status:** present.

Prompt:

Create a top-down tower-slot marker for a controller-first tower defense roguelite. The marker should clearly indicate a valid build position in the arena without overwhelming combat readability. Use a clean circular or geometric pad language with subtle energy insets and clear contrast against ground tiles. The asset should support visible idle and highlighted states later through tinting or overlays. Transparent background, no text.

## Asset 14 - Player Base Sprite

Target: Assets/Art/Characters/Player/player_base_topdown.png

**Status:** present.

Prompt:

Create the main playable character sprite for a top-down sci-fantasy action roguelite. The design should read instantly from above, with a strong silhouette, readable facing direction and a compact combat-ready shape. Keep the character stylized rather than realistic, with a light 2.5D feel through shading and layered materials, but it must still function as a flat sprite in Unity 2D. Palette should coordinate with friendly technology and warm accent energy. Transparent background, no weapon effects baked into the sprite.

## Asset 15 - Core Sprite

Target: Assets/Art/Environment/Arena/core_main_sprite.png

**Status:** present.

Prompt:

Create the central Core sprite for a top-down defense roguelite. The Core must feel important, powerful and worth defending, combining arcane-energy and industrial machinery motifs. It should be readable from the gameplay camera, with a stable silhouette, visible energy center and optional layered 2.5D shading cues. Transparent background, no environment baked around it, no text.

## Asset 16 - Enemy Chaser Sprite

Target: Assets/Art/Characters/Enemies/enemy_chaser_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Chaser role in a sci-fantasy action roguelite. This enemy should look fast, aggressive and uncomplicated, with a sharp silhouette that communicates direct pursuit behavior. Use hostile color language that contrasts with the Player and friendly towers. Keep the design readable at small to medium gameplay scale, with transparent background and no baked shadow.

## Asset 17 - Enemy Ranged Pressure Sprite

Target: Assets/Art/Characters/Enemies/enemy_ranged_pressure_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Ranged Pressure role in a sci-fantasy defense roguelite. The design should communicate a ranged attack threat through silhouette, posture and integrated weapon or casting structure. Make it visually distinct from the Chaser through shape balance and head or body language. Transparent background, stylized hostile design, clear gameplay readability.

## Asset 18 - Enemy Bruiser Sprite

Target: Assets/Art/Characters/Enemies/enemy_bruiser_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Bruiser role in a sci-fantasy defense roguelite. The enemy should read as heavier, slower and more forceful than the Chaser and Ranged Pressure roles, with broad shapes and visible mass. Keep the sprite readable from a distance and compatible with Unity 2D. Transparent background, no baked environment.

## Asset 19 - Stage 1 Boss Sprite

Target: Assets/Art/Characters/Bosses/boss_stage1_topdown.png

**Status:** present.

Prompt:

Create a Stage 1 boss sprite for a top-down sci-fantasy action-defense roguelite. The boss must feel visually dominant, memorable and threatening from the gameplay camera, with a strong top-down silhouette, readable front orientation and clear attack-read zones built into the design. Use layered shading to suggest 2.5D volume, but keep it fully usable as a Unity 2D sprite. Transparent background, no environment, no text.

## Asset 20 - Rapid Weapon Sprite

Target: Assets/Art/Combat/Projectiles/weapon_rapid_sprite.png

**Status:** present.

Prompt:

Create a weapon sprite or top-down held-device visual for the rapid projectile starter weapon of a sci-fantasy roguelite. The design should feel agile, precise and compact, readable from above and visually compatible with the Player sprite. Use a stylized technological look with controlled energy accents. Transparent background, no muzzle flash baked in.

## Asset 21 - Spread Weapon Sprite

Target: Assets/Art/Combat/Projectiles/weapon_spread_sprite.png

**Status:** present.

Prompt:

Create a weapon sprite or top-down held-device visual for a spread-shot starter weapon in a sci-fantasy roguelite. The design should feel wider, heavier and more explosive than the rapid weapon while remaining readable from the top-down camera. Keep it stylized, clean and compatible with a Unity 2D pipeline. Transparent background, no projectile effects baked in.

## Asset 22 - Beam Weapon Sprite

Target: Assets/Art/Combat/Projectiles/weapon_beam_sprite.png

**Status:** present.

Prompt:

Create a weapon sprite or top-down held-device visual for a short piercing or beam-style starter weapon in a sci-fantasy roguelite. The design should feel focused, energy-driven and more advanced than the baseline projectile weapon. Use a clean silhouette, layered material shading and strong centerline readability. Transparent background, no active beam baked in.

## Asset 23 - Projectile Sheet

Target: Assets/Art/Combat/Projectiles/projectile_sheet_core.png

**Status:** present.

Prompt:

Create a 2D projectile sprite sheet for a top-down sci-fantasy roguelite containing a basic fast projectile, a spread projectile variant and a short beam-hit segment or piercing pulse variant. The assets should be compact, highly readable, color-disciplined and suitable for pooling-heavy combat. Use transparent background, shared art style, no trailing VFX larger than the projectiles themselves.

## Asset 24 - Combat VFX Essentials (Einzelbilder)

Der ursprüngliche Sammel-Prompt für ein einziges `combat_vfx_sheet_essentials.png`-Sheet erzeugte in der Praxis ein unregelmäßiges Layout (unterschiedliche Effektgrößen, keine gleichmäßigen Zellen), das sich nicht zuverlässig automatisch in Unity zuschneiden lässt (`DebugSpriteFactory.LoadProjectAtlasCellSprite` setzt ein gleichmäßiges Spalten/Zeilen-Raster voraus). Die folgenden sieben Prompts erzeugen daher je ein einzelnes, freistehendes Effektbild statt eines Sheets. Jedes Bild wird einzeln in Unitys Sprite-Modus (kein Multiple/Grid-Slicing nötig) importiert und direkt referenziert.

### Asset 24a - Hit Spark

Target: Assets/Art/Combat/VFX/vfx_hit_spark.png

**Status:** present.

Prompt:

Create a single 2D impact hit-spark effect frame for a top-down sci-fantasy action roguelite, as it would appear at the moment a projectile strikes an enemy. Small, punchy, radial burst of bright energy shards with a bright white-hot core fading to the weapon's accent color at the edges. Square canvas, effect centered, transparent background, no motion blur baked in, no other elements, readable at small HUD-adjacent size during heavy combat.

### Asset 24b - Enemy Death Burst

Target: Assets/Art/Combat/VFX/vfx_enemy_death_burst.png

**Status:** present.

Prompt:

Create a single 2D enemy-death burst effect frame for a top-down sci-fantasy action roguelite. A slightly larger radial dissolve/burst than a hit spark, suggesting an enemy dissipating into fragments and energy motes, using cool desaturated tones with one accent highlight color. Square canvas, effect centered, transparent background, no character silhouette included, no text.

### Asset 24c - Core Damage Flare

Target: Assets/Art/Combat/VFX/vfx_core_damage_flare.png

**Status:** present.

Prompt:

Create a single 2D warning flare effect frame for a top-down sci-fantasy action roguelite, used when the defended Core takes damage. Sharp radial flash in an alarming red-orange tone with a thin bright rim, communicating urgency without being visually overwhelming. Square canvas, effect centered, transparent background, no text, no UI frame.

### Asset 24d - Card Reward Reveal Flash

Target: Assets/Art/Combat/VFX/vfx_card_reveal_flash.png

**Status:** present.

Prompt:

Create a single 2D reveal-flash effect frame for a top-down sci-fantasy roguelite, used behind a card the instant it is revealed in the post-wave reward screen. Soft radial glow with a bright core and elegant light rays, in a warm inviting gold-white tone that reads as a reward rather than a threat. Square canvas, effect centered, transparent background, no card shape included, no text.

### Asset 24e - Burn Status Tick

Target: Assets/Art/Combat/VFX/vfx_burn_status_tick.png

**Status:** present.

Regenerate note (2026-09-08): the first generation came back as 1354x738 (wide) instead of square. Lead the prompt explicitly with the aspect-ratio constraint below.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas. Create a single small 2D status-effect tick icon/effect frame for a top-down sci-fantasy roguelite, representing an ongoing burn/fire damage-over-time tick on an affected unit. Compact upward-licking flame wisp in warm orange-red tones with a bright ember core, simple enough to read clearly when tiled repeatedly above a small enemy sprite. Square canvas, effect centered, transparent background, no text.

### Asset 24f - Freeze Status Pulse

Target: Assets/Art/Combat/VFX/vfx_freeze_status_pulse.png

**Status:** present.

Regenerate note (2026-09-08): the first generation came back as 1354x738 (wide) instead of square. Lead the prompt explicitly with the aspect-ratio constraint below.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas. Create a single small 2D status-effect pulse frame for a top-down sci-fantasy roguelite, representing an ongoing freeze/slow effect on an affected unit. Compact radiating cold-blue frost pulse with sharp crystalline edges and a pale icy core, simple enough to read clearly above a small enemy sprite. Square canvas, effect centered, transparent background, no text.

### Asset 24g - Tower Activation Pulse

Target: Assets/Art/Combat/VFX/vfx_tower_activation_pulse.png

**Status:** present.

Prompt:

Create a single 2D activation-pulse effect frame for a top-down sci-fantasy defense roguelite, used the moment a placed tower fires or activates. Outward radial energy ring in the tower's steel-blue/amber accent palette, industrial and mechanical rather than organic, with a bright thin leading edge. Square canvas, effect centered, transparent background, no tower silhouette included, no text.

## Asset 25 - Stage 1 Arena Ground Tile Set

Target: Assets/Art/Environment/Arena/stage1_ground_tileset.png

**Status:** present.

Prompt:

Create a seamless top-down 2D ground tile set for Stage 1 of a sci-fantasy defense roguelite. The arena should feel like a defended industrial ritual platform: modular metal plates, subtle energy seams, worn surfaces and clean gameplay readability. Avoid overly busy contrast patterns that would fight projectiles or pickups. Deliver a tile set suitable for Unity 2D tilemaps, no text, no characters.

## Asset 26 - Stage 1 Arena Prop Set

Target: Assets/Art/Environment/Arena/stage1_prop_set.png

**Status:** present.

Prompt:

Create a top-down prop set for Stage 1 of a sci-fantasy defense roguelite, including edge pylons, broken machinery, energy relays, pipe clusters and small structural blockers. Props should support a slight 2.5D feel through layered shading and shadow hints while remaining readable in a 2D orthographic game. Transparent background for each prop element inside the sheet, consistent palette, no text.

## Asset 27 - Wave Banner UI

Target: Assets/Art/UI/HUD/wave_banner.png

**Status:** present.

Prompt:

Create a 2D UI banner for wave start, elite warning and boss warning in a controller-first top-down roguelite. The asset should feel urgent and ceremonial without taking too much screen space. Design it as a scalable UI piece with room for dynamic text overlays. Use strong contrast and a sci-fantasy industrial language. Transparent background outside the banner shape, no baked words.

## Asset 28 - Tutorial Glyph Strip

Target: Assets/Art/UI/Tutorial/tutorial_glyph_strip.png

**Status:** present.

Prompt:

Create a clean icon strip for controller and keyboard hints used in a Unity top-down roguelite tutorial. Include a neutral presentation style that matches a sci-fantasy interface and supports later mapping to Confirm, Cancel, Move, Fire and Interact actions. The asset should be highly legible at small size, use transparent background and avoid decorative clutter.

## Asset 29 - MainMenu Route Panel Frame

Target: Assets/Art/UI/MainMenu/route_panel_frame.png

**Status:** present.

Prompt:

Create a large modular 2D overlay panel frame for the main menu of a controller-first sci-fantasy roguelite. This frame is used for New Run confirmation, Quit confirmation, Settings overlay content and Meta Progression overview panels. It must support 9-slice scaling, preserve a strong readable inner content area for multiple text blocks, and feel more substantial than a standard button frame without becoming heavy or ornate. Use the same industrial-energy visual language as the existing main menu art: steel blue structure, pale metallic edges, restrained amber energy seams, subtle wear, high readability, transparent background outside the panel shape, no text, no icon.

## Asset 30 - MainMenu Glyph Hint Capsule

Target: Assets/Art/UI/MainMenu/glyph_hint_capsule.png

**Status:** NOT CREATED YET.

Prompt:

Create a compact modular 2D UI capsule panel for main menu controller hints in a top-down sci-fantasy roguelite. The asset should support short glyph-and-label pairs like Confirm, Cancel and Navigate, work well in a horizontal hint bar, and remain highly legible at small to medium UI scale. Match the established Corebound menu language with clean industrial framing, subtle energy accents, low visual noise and strong contrast against darker backgrounds. The asset must support 9-slice scaling, have a transparent background, and contain no baked text or symbols.

## Asset 31 - Ballista Tower Sprite

Target: Assets/Art/Towers/tower_ballista_topdown.png

**Status:** present but misnamed - on disk as `tower_ballista_topdown.png.png` (doubled extension)

Prompt:

Create a top-down tower sprite for the Ballista Tower in a sci-fantasy defense roguelite. The tower should read as a precise single-target support weapon with a compact mechanical footprint, a stable base silhouette and a clear forward-facing firing head. Match the established industrial-energy language of Corebound with steel-blue materials, pale metallic edges and warm amber energy accents. Keep the design highly readable at gameplay scale, compatible with Unity 2D, transparent background, no baked shadow, no text.

## Asset 32 - Support Tower Sprite

Target: Assets/Art/Towers/tower_support_topdown.png

**Status:** present.

Prompt:

Create a top-down tower sprite for the Support Tower in a sci-fantasy defense roguelite. This tower should feel more utility-oriented than the Ballista Tower, with a silhouette that suggests relay, field projection or pulse support rather than pure damage output. Use the same Corebound industrial-energy language with disciplined shapes, readable top-down forms, steel-blue structural materials and subtle cyan or amber power accents. Transparent background, no baked shadow, no text, optimized for Unity 2D gameplay scale.

## Asset 33 - Gameplay Modal Frame

Target: Assets/Art/UI/HUD/gameplay_modal_frame.png

**Status:** present.

Prompt:

Create a medium-sized modular 2D modal frame for gameplay overlays in a sci-fantasy defense roguelite. This frame is used for pause, run failed and start-of-wave confirmation overlays, so it must fit centered title text and two to four short lines of instructions without crowding. Match the established Corebound industrial-energy language with clean steel-blue paneling, pale metallic edges, restrained amber energy seams and strong contrast for dark text over a bright inner surface. Support 9-slice scaling, transparent background outside the frame, no baked text, no icons.

## Asset 34 - Card Reward Choice Panel

Target: Assets/Art/UI/HUD/card_reward_choice_panel.png

**Status:** present.

Prompt:

Create a wide 2D reward-selection panel for a controller-first sci-fantasy roguelite. The asset should support a title row, resource summary, three side-by-side card choices and a footer description area, with enough inner contrast that white or pale text remains readable. It should feel more like a deliberate reward or drafting surface than a generic menu frame. Use the existing Corebound language of steel-blue structure, subtle industrial wear and warm energy accents. Deliver it as a scalable 9-slice-compatible UI panel with transparent background outside the panel and no baked text.

## Asset 35 - Stage 1 Arena Boundary Set

Target: Assets/Art/Environment/Arena/stage1_boundary_set.png

**Status:** present.

Prompt:

Create a top-down 2D arena boundary set for Stage 1 of a sci-fantasy defense roguelite. The set should clearly define the playable combat space with readable edge walls, rail segments, barricade corners, hazard-stripe trims and optional gateway pieces that fit an industrial ritual platform. Prioritize gameplay readability over decorative density: the player should immediately understand where movement stops and where enemies approach. Match the Stage 1 ground and prop palette with modular steel-blue and dark slate materials plus restrained amber energy elements. Transparent background for each piece inside the sheet, no text, optimized for Unity 2D placement.

## Asset 36 - Stage 1 Spawn Gate Marker

Target: Assets/Art/Environment/Arena/stage1_spawn_gate.png

**Status:** present.

Prompt:

Create a top-down Stage 1 enemy spawn gate marker for a sci-fantasy defense roguelite. The marker should communicate a dangerous enemy entry point at the far edge of the arena using industrial ritual technology, restrained warning lights and clear directional framing toward the playable space. It should be readable at gameplay scale, fit the same steel-blue, dark slate and amber accent palette as the Stage 1 arena, and work both as a static map element and as a base for future spawn VFX. Transparent background outside the gate structure, no text.

## Asset 37 - Ranged Pressure Telegraph

Target: Assets/Art/Combat/VFX/ranged_pressure_telegraph.png

**Status:** INCOMPLETE - only in the StreamingAssets mirror, missing from `Assets/Art`

Prompt:

Create a 2D telegraph marker for ranged enemy attacks in a top-down sci-fantasy roguelite. The asset should warn the player about an incoming ranged pressure shot with a clear, fast-to-read silhouette that can sit beneath or in front of the attacking enemy. Use hostile but disciplined color language that contrasts with the arena floor and friendly UI, favoring readable angular energy cues over noisy particles. Transparent background, no text, suitable for pooling and short repeated use in combat.

## Asset 38 - Tower Upgrade Indicator Ring

Target: Assets/Art/Towers/tower_upgrade_ring.png

**Status:** present.

Prompt:

Create a top-down upgrade indicator ring for towers in a controller-first sci-fantasy defense roguelite. The asset should sit underneath or around a selected tower to communicate that the tower can be upgraded, without obscuring nearby enemies or projectiles. Match the existing industrial-energy visual language with clear concentric geometry, restrained amber or pale gold emphasis, and clean readability at small to medium scale. Transparent background, no text, no baked arrows.

## Asset 39 - Enemy Bolt Projectile

Target: Assets/Art/Combat/Projectiles/enemy_bolt_projectile.png

**Status:** present.

Prompt:

Create a compact top-down projectile sprite for ranged enemy attacks in a sci-fantasy defense roguelite. The bolt should feel hostile, fast and readable, with a silhouette that clearly differs from player-fired projectiles. Use disciplined red-orange or crimson energy with a compact mechanical or arcane tech framing, but avoid oversized trails that would clutter the arena. Transparent background, no text, suitable for pooling-heavy combat and repeated burst use.

## Asset 40 - Elite Enemy Marker

Target: Assets/Art/Combat/VFX/elite_enemy_marker.png

**Status:** present.

Prompt:

Create a top-down elite-enemy marker for a sci-fantasy defense roguelite. The asset should make elite variants immediately distinguishable in crowded waves without hiding the base enemy silhouette. It should work as an overhead or underfoot accent, using a sharp, readable shape language with amber-gold or heated orange emphasis that communicates elevated danger. Transparent background, no text, optimized for clear visibility at gameplay scale.

## Asset 41 - Shop Offer Panel

Target: Assets/Art/UI/HUD/shop_offer_panel.png

**Status:** present but misnamed - on disk as `shop_offer_panel.png.png` (doubled extension)

Prompt:

Create a wide 2D shop panel for a controller-first sci-fantasy defense roguelite. The panel should feel like a controlled purchase space distinct from the reward screen, with room for a title, wallet summary, three horizontally arranged offers and a short footer hint. Use the existing Corebound industrial-energy language with steel-blue framing, restrained amber accents, readable inner contrast and low visual noise. Deliver it as a scalable 9-slice-compatible UI panel with transparent background outside the panel shape and no baked text.

## Asset 42 - Shop Price Chip

Target: Assets/Art/UI/HUD/shop_price_chip.png

**Status:** present.

Prompt:

Create a compact 2D price chip for shop offers in a top-down sci-fantasy roguelite. It should frame a short cost label or icon cleanly, remain readable at small UI scale and visually communicate value without overpowering the offer card itself. Match the Corebound UI language with industrial framing, subtle metallic structure and a warm accent seam suited for pricing. Transparent background, no baked numbers, scalable for UI reuse.

## Asset 43 - Shop Tower Upgrade Icon

Target: Assets/Art/UI/HUD/shop_tower_upgrade_icon.png

**Status:** present.

Needed because:

`ShopOfferType.TowerUpgrade` (2026-09-07) reuses the same Common/Rare/Epic/Legendary card-rarity frame as Card offers for its shop slot, since `ShopPresenter` currently has no distinct visual identity for a non-Card offer type. Without this icon, a Tower Upgrade offer is visually indistinguishable from a plain Common card in the shop, which risks reading as a weak card rather than a tower purchase. This icon is meant to sit in the slot's existing frame corner (matching Asset 42's chip scale) as a small type badge, not replace the frame.

Regenerate note (2026-09-08): the first generation came back as 1354x738 (wide) instead of square; code now sets `preserveAspect` on the badge as a safety net, but a square source still reads cleaner at 40x40 UI scale. Lead the prompt explicitly with the aspect-ratio constraint below.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas. Create a small 2D badge icon representing a "Tower Upgrade" shop offer in a top-down sci-fantasy defense roguelite, distinct from a card icon. Combine a compact tower silhouette with a clear upward arrow or ascending-chevron motif to read as "upgrade" at a glance, even at small UI scale. Match the Corebound UI language: steel-blue structure, restrained amber-gold energy accent, clean industrial framing, strong silhouette. Transparent background, no text, no card-frame shape, optimized to sit as a small corner badge over a shop offer slot.

## Asset 44 - Mortar Tower Sprite

Target: Assets/Art/Towers/tower_mortar_topdown.png

**Status:** present.

Needed because:

Only 2 towers exist (Ballista: single-target precision, Support: utility pulse) against the Project Vision's "mehreren Türmen" Early Access goal, and neither covers area-of-effect damage, which is a real Build Diversity gap against grouped/swarm enemies. A third tower fills that gap. `TowerData`/`CombatDamageProfileData`/upgrade-level assets will be authored once this sprite exists (same deferred pattern as Asset 43), since `TowerData._sprite` needs a real imported Sprite GUID and cannot be wired against a file that doesn't exist yet.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background — the canvas outside the tower silhouette must be fully transparent (alpha 0), not white. Create a top-down tower sprite for the Mortar Tower in a sci-fantasy defense roguelite. This tower should read as a heavier area-damage support weapon with a wider, lower stance than the Ballista Tower, a visible barrel or launcher angled slightly upward, and a stable tripod-like base silhouette. Match the established Corebound industrial-energy language with steel-blue structural materials, pale metallic edges and warm amber energy accents, but give it a heavier, blunter silhouette than the Ballista and Support towers so all three read as distinct at a glance. Keep the design highly readable at gameplay scale, compatible with Unity 2D, no baked shadow, no text.

## Asset 45 - Sentinel Tower Sprite

Target: Assets/Art/Towers/tower_sentinel_topdown.png

**Status:** present.

Needed because:

`Tower_Sentinel.asset` (`_sprite` is unassigned, `_towerId: tower_sentinel`) has no art at all and no code-level fallback either (`TowerRuntimeController.ResolveSprite()`'s switch only covers `tower_ballista`, `tower_support` and `tower_mortar`) - it renders as a ~0.09-unit debug square tinted its DebugColor, effectively invisible at gameplay scale. Sentinel is a fully-designed, buildable tower from Wave 1 (`TowerCatalog_Default`, `_buildCost: 16`, `_range`/`_fireIntervalSeconds` already authored) with no distinguishing visual identity of its own. Once this sprite exists, add a matching `case "tower_sentinel":` fallback in `ResolveSprite()` (same pattern as Ballista/Support/Mortar) and/or wire `Tower_Sentinel.asset._sprite` directly, following whichever import outcome (Single vs Multiple sprite mode) actually resolves cleanly - Mortar's own `_sprite` reference silently broke this same way after a Sprite Editor re-slice, so verify the assigned reference actually resolves at runtime rather than trusting the Inspector alone.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background — the canvas outside the tower silhouette must be fully transparent (alpha 0), not white. Create a top-down tower sprite for the Sentinel Tower in a sci-fantasy defense roguelite. This tower should read as a fast, precise anti-personnel turret: a slim, elevated targeting head on a narrow rotating mount, with a twin-lens or sensor-array motif suggesting rapid target acquisition rather than raw damage. Give it a leaner, taller-proportioned silhouette than the Ballista (compact single-target support), the Mortar (heavy, blunt, tripod-based) and the Support (radially symmetric utility relay), so all four towers read as instantly distinct from each other at a glance even in a crowded build. Match the established Corebound industrial-energy language: steel-blue structural materials, pale metallic edges, warm amber energy accents. Keep the design highly readable at gameplay scale, compatible with Unity 2D, no baked shadow, no text.

## Asset 46 - Arena Boundary Set Rework (flat top-down)

Target: Assets/Art/Environment/Arena/stage1_boundary_set.png (replacement)

**Status:** present.

Needed because:

Playtest feedback (2026-09-14): the arena boundary walls "look weird" at the arena edges. Cropping the exact pixel rects `StageArenaArtPresenter` uses confirms why: the straight wall segment (`BoundaryWallPixelRect`, the widest plain barricade run) is a flat, near-orthographic panel design that sits reasonably well next to `stage1_ground_tileset.png`'s flat top-down floor - but the sheet's dedicated corner piece (formerly `BoundaryCornerPixelRect`) is drawn from a 3/4 perspective as two faces meeting at a visible V, unlike anything else in the arena. Mixing that angled corner into an otherwise flat boundary made corners visibly pop out relative to the straight runs. As an interim code fix (this pass), corners now reuse the flat wall crop instead of the angled corner piece, which resolves the style clash but loses the sheet's dedicated corner geometry (an actual mitered/rounded corner shape) - a purpose-built flat corner tile would look better than a stretched wall-segment square once art time is available. Also worth re-checking once regenerated: the four straight-run variants (2/4/5-panel) and the two gate pieces on the same sheet share the flat style and were not part of this complaint, so only the corner needs replacing, not the whole sheet.

Prompt:

Flat top-down (orthographic, camera looking straight down) sci-fi industrial barrier corner piece for a 2D top-down arena boundary, matching an already-flat straight wall segment (steel-blue paneled barricade with a black-and-amber hazard-stripe top edge and a thin dark base trim) - NOT an isometric or 3/4-perspective piece, no visible depth/side faces, no angled V-shape. Design it as a single continuous barrier that turns a 90-degree corner while staying flat/orthographic, like a rounded or mitered outer corner of the same paneled barricade, with the hazard stripe continuing around the bend. Square canvas, transparent background (alpha 0) outside the barrier silhouette, no text, no baked shadow, matching the steel-blue/amber industrial-energy palette used elsewhere in the arena set.

## 5-Stage Concept Art (2026-09-14)

Playtest asked for a full visual concept covering up to 5 stages (see [Stages.md](../Specs/Stages.md) for the accompanying enemy-pool/map/boss/music design per stage). Stage 1 already has full art; Stage 2 has data (`Stage_02`, `StageTheme_Stage2`, `Boss_Stage2`) but no dedicated art yet ("Reactor Sentinel" boss, cool blue-violet theme). Stages 3-5 are new. Each stage below gets the same four asset types Stage 1 has: ground tileset, prop set, boundary set and a boss sprite - loaded the same way (`StageThemeData.GroundTexture`/`PropTexture`/`BoundaryTexture`, `BossData.Sprite`), so no new code is needed to wire them in once generated, only new `StageThemeData`/`BossData` asset values pointing at the files. Asymmetric layouts (per Stages.md's "Technical Design" note) are a placement-data/code concern, not an art concern - these prompts only need to read as flat top-down tiles/props/walls/a boss sprite in the stage's own palette, the same way Stage 1's do.

### Asset 47 - Stage 2 Ground Tile Set (Reactor Depths)

Target: Assets/Art/Environment/Arena/stage2_ground_tileset.png

**Status:** present.

Prompt:

Create a seamless top-down 2D ground tile set for a reactor-depths stage of a sci-fantasy defense roguelite - a vertical, energy-charged industrial chamber deep inside a facility. Modular metal floor plates with exposed conduit seams, faint pulsing containment-field lines and grated maintenance panels, in a cool blue-violet and cyan palette (cooler and more saturated than Stage 1's warm steel-blue/amber floor). Avoid overly busy contrast patterns that would fight projectiles or pickups. Deliver a tile set suitable for Unity 2D tilemaps, no text, no characters.

### Asset 48 - Stage 2 Prop Set (Reactor Depths)

Target: Assets/Art/Environment/Arena/stage2_prop_set.png

**Status:** present.

Prompt:

Create a top-down prop set for a reactor-depths stage of a sci-fantasy defense roguelite: coolant pipe clusters, pulsing containment-field emitters, exposed conduit relays and reactor-core support pylons. Cool blue-violet and cyan palette matching Asset 47's floor. Props should support a slight 2.5D feel through layered shading and shadow hints while remaining readable in a 2D orthographic game. Transparent background for each prop element inside the sheet, consistent palette, no text.

### Asset 49 - Stage 2 Boundary Set (Reactor Depths)

Target: Assets/Art/Environment/Arena/stage2_boundary_set.png

**Status:** present.

Prompt:

Flat top-down (orthographic) arena boundary set for a reactor-depths stage: containment-field wall segments, rail barricades and hazard-striped corners, in the same cool blue-violet/cyan palette as Assets 47-48. Match Asset 46's flat, non-isometric construction (no 3/4-perspective corner pieces) so it stays visually consistent with the flat ground tileset. Transparent background per piece, no text, optimized for Unity 2D placement.

### Asset 50 - Reactor Sentinel Boss Sprite (Stage 2)

Target: Assets/Art/Characters/Bosses/boss_stage2_topdown.png

**Status:** present.

Needed because: `Boss_Stage2.asset` names this boss "Reactor Sentinel" but has no sprite assigned yet.

Prompt:

Create a Stage 2 boss sprite for a top-down sci-fantasy defense roguelite: the "Reactor Sentinel", a heavy containment-core guardian construct. It should read as an area-denial threat - a broad, anchored silhouette built around a glowing central containment core, with vented emitter arms that telegraph an expanding hazard-zone attack. Cool blue-violet/cyan energy accents matching the stage palette, contrasting with Stage 1's boss. Strong top-down silhouette, readable front orientation, clear attack-read zones built into the design, layered shading for 2.5D volume while staying a usable Unity 2D sprite. Transparent background, no environment, no text.

### Asset 51 - Stage 3 Ground Tile Set (Salvage Fields)

Target: Assets/Art/Environment/Arena/stage3_ground_tileset.png

**Status:** present.

Prompt:

Create a seamless top-down 2D ground tile set for a salvage-fields stage of a sci-fantasy defense roguelite - an open, sun-worn scrapyard of downed mech wreckage. Cracked ferrocrete and packed dirt patches, embedded scrap plating and rust streaks, in a rusty orange and verdigris-green palette (warmer and grittier than Stage 1's clean steel floor). Avoid overly busy contrast patterns that would fight projectiles or pickups. Deliver a tile set suitable for Unity 2D tilemaps, no text, no characters.

### Asset 52 - Stage 3 Prop Set (Salvage Fields)

Target: Assets/Art/Environment/Arena/stage3_prop_set.png

**Status:** present.

Prompt:

Create a top-down prop set for a salvage-fields stage of a sci-fantasy defense roguelite: downed mech husk fragments, stacked scrap-metal piles, exposed rebar clusters and broken cargo crates. Rusty orange and verdigris-green palette matching Asset 51's floor. Props should read as larger, fewer, more open obstacle clusters than Stage 1's props (an open field, not a dense maze), with layered shading for a slight 2.5D feel. Transparent background for each prop element inside the sheet, consistent palette, no text.

### Asset 53 - Stage 3 Boundary Set (Salvage Fields)

Target: Assets/Art/Environment/Arena/stage3_boundary_set.png

**Status:** present.

Prompt:

Flat top-down (orthographic) arena boundary set for a salvage-fields stage: makeshift scrap-barricade wall segments, chain-link rail sections and hazard-striped corners, in the same rusty orange/verdigris-green palette as Assets 51-52. Match Asset 46's flat, non-isometric construction. Transparent background per piece, no text, optimized for Unity 2D placement.

### Asset 54 - Salvage Fields Boss Sprite (Stage 3)

Target: Assets/Art/Characters/Bosses/boss_stage3_topdown.png

**Status:** present.

Prompt:

Create a Stage 3 boss sprite for a top-down sci-fantasy defense roguelite: a scrapyard overseer construct built from salvaged mech parts, designed around a summon/add-phase fight - visible drone-launch ports or a wreckage-crane arm silhouette that reads as "calls in reinforcements" rather than pure melee threat. Rusty orange/verdigris-green energy accents matching the stage palette. Strong top-down silhouette, readable front orientation, clear attack-read zones, layered shading for 2.5D volume while staying a usable Unity 2D sprite. Transparent background, no environment, no text.

### Asset 55 - Stage 4 Ground Tile Set (Cryo Vaults)

Target: Assets/Art/Environment/Arena/stage4_ground_tileset.png

**Status:** present.

Prompt:

Create a seamless top-down 2D ground tile set for a cryo-vaults stage of a sci-fantasy defense roguelite - a frozen coolant storage facility. Frost-rimed metal floor plates, cracked ice sheet overlays and pale condensation streaks, in an icy white-blue and pale cyan palette (colder and lower-saturation than Stage 2's blue-violet). Avoid overly busy contrast patterns that would fight projectiles or pickups. Deliver a tile set suitable for Unity 2D tilemaps, no text, no characters.

### Asset 56 - Stage 4 Prop Set (Cryo Vaults)

Target: Assets/Art/Environment/Arena/stage4_prop_set.png

**Status:** present.

Prompt:

Create a top-down prop set for a cryo-vaults stage of a sci-fantasy defense roguelite: frost-crusted coolant tanks, cracked stasis-pod fragments, icicle-hung pipe clusters and frozen vent stacks. Icy white-blue and pale cyan palette matching Asset 55's floor. Props should support a slight 2.5D feel through layered shading and shadow hints while remaining readable in a 2D orthographic game. Transparent background for each prop element inside the sheet, consistent palette, no text.

### Asset 57 - Stage 4 Boundary Set (Cryo Vaults)

Target: Assets/Art/Environment/Arena/stage4_boundary_set.png

**Status:** present.

Prompt:

Flat top-down (orthographic) arena boundary set for a cryo-vaults stage: frost-rimed barrier wall segments, frozen rail sections and hazard-striped corners, in the same icy white-blue/pale cyan palette as Assets 55-56. Match Asset 46's flat, non-isometric construction. Transparent background per piece, no text, optimized for Unity 2D placement.

### Asset 58 - Cryo Warden Boss Sprite (Stage 4)

Target: Assets/Art/Characters/Bosses/boss_stage4_topdown.png

**Status:** present.

Prompt:

Create a Stage 4 boss sprite for a top-down sci-fantasy defense roguelite: the "Cryo Warden", a frost-sealed guardian construct built around a freeze-pulse attack. It should read as a control/status threat rather than raw damage - a crystalline ice-core chest emitter and frost-vented shoulder mounts that telegraph an outward freeze pulse. Icy white-blue/pale cyan energy accents matching the stage palette. Strong top-down silhouette, readable front orientation, clear attack-read zones, layered shading for 2.5D volume while staying a usable Unity 2D sprite. Transparent background, no environment, no text.

### Asset 59 - Stage 5 Ground Tile Set (Core Nexus, Final)

Target: Assets/Art/Environment/Arena/stage5_ground_tileset.png

**Status:** present.

Prompt:

Create a seamless top-down 2D ground tile set for the final Core Nexus stage of a sci-fantasy defense roguelite - a climactic core chamber whose architecture echoes fragments of the run's four earlier stages. Polished dark floor plating inlaid with white-gold energy seams, with subtle regional color hints (warm amber, blue-violet, rust-orange, icy cyan) bleeding faintly into the otherwise high-contrast white-gold-on-black palette, as if the chamber were built from salvaged fragments of the previous four stages. Avoid overly busy contrast patterns that would fight projectiles or pickups. Deliver a tile set suitable for Unity 2D tilemaps, no text, no characters.

### Asset 60 - Stage 5 Prop Set (Core Nexus, Final)

Target: Assets/Art/Environment/Arena/stage5_prop_set.png

**Status:** present.

Prompt:

Create a top-down prop set for the final Core Nexus stage of a sci-fantasy defense roguelite: monumental white-gold energy pylons, fused terrain-fragment obstacles that hint at the previous four stages' materials (industrial steel, reactor conduit, scrap wreckage, frost crystal) unified under one high-contrast finale palette. Props should read as fewer, larger, more monumental set-pieces than earlier stages' clutter. Transparent background for each prop element inside the sheet, consistent palette, no text.

### Asset 61 - Stage 5 Boundary Set (Core Nexus, Final)

Target: Assets/Art/Environment/Arena/stage5_boundary_set.png

**Status:** present.

Prompt:

Flat top-down (orthographic) arena boundary set for the final Core Nexus stage: monumental white-gold energy-barrier wall segments and hazard-striped corners, the most imposing boundary set of the run. Match Asset 46's flat, non-isometric construction. Transparent background per piece, no text, optimized for Unity 2D placement.

### Asset 62 - Final Boss Sprite (Stage 5, Core Nexus)

Target: Assets/Art/Characters/Bosses/boss_stage5_topdown.png

**Status:** present.

Needed because: Stage 5 is the run's Final Boss (`IsFinalStage`), combining Stage 2's area-denial, Stage 3's summon-phase and Stage 4's freeze-pulse mechanics into one multi-phase fight - the sprite should read as the culmination of the previous three bosses' visual languages, not a fourth unrelated design.

Prompt:

Create the Final Boss sprite for a top-down sci-fantasy defense roguelite: a towering core-guardian construct that visually unifies the run's three earlier bosses - a broad area-denial-ready core chamber (Reactor Sentinel), drone-launch mounts (Salvage overseer) and a crystalline freeze-emitter (Cryo Warden) - fused into one imposing, clearly-the-final-fight silhouette. High-contrast white-gold energy against a near-black body, the most visually dominant and threatening design in the game. Strong top-down silhouette, readable front orientation, clear attack-read zones for each of its three inherited phase types, layered shading for 2.5D volume while staying a usable Unity 2D sprite. Transparent background, no environment, no text.

## Stage 2-5 Enemy Role Reskins (2026-09-15)

Playtest request: "create Enemy Assets for the other stages" now that Assets 47-62 gave Stages 2-5 their own ground/prop/boundary/boss art, leaving the regular enemy roster as the last stage-agnostic piece. Per [Stages.md](../Specs/Stages.md)'s Rules section ("Eine Stage darf... keine neue Kernmechanik exklusiv für sich einführen") and its Enemy-Pool-Akzent column, later stages deliberately reuse the same three enemy roles - Chaser, Ranged Pressure, Bruiser - at different pool weights rather than introducing new roles (Stage 2: Ranged-Pressure-Schwerpunkt; Stage 3: Bruiser-lastig; Stage 4: same roles with a stronger Frost elite-modifier accent; Stage 5: a full mix of all of the above). So these 12 prompts are palette/material reskins of the existing three role silhouettes, not new enemy types - exactly the same relationship each stage's boss already has to a shared design language, just applied one level down.

Needed because (wiring gap, not just missing art): unlike `StageThemeData`/`BossData`, which already had a dedicated per-stage asset slot that Assets 47-62 could drop straight into, `EnemyData` (`Assets/Scripts/Gameplay/Enemies/EnemyData.cs`) has exactly one shared `_sprite` field per role today, and every stage's `StageEnemyPoolData` references those same three role assets through weighting alone - there is no per-stage sprite variant slot yet. Wiring these new sprites in will need a short follow-up beyond generating the art: either new per-stage `EnemyData` variants (e.g. `EnemyData_Chaser_Stage2.asset`) referenced by each stage's own `StageEnemyPoolData` entries, or a sprite-override field added to `StageThemeData`/`StageEnemyPoolData`. That is a small data/code task for later, not a blocker for generating the art now.

### Asset 63 - Stage 2 Enemy Chaser (Reactor Depths)

Target: Assets/Art/Characters/Enemies/enemy_chaser_stage2_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Chaser role, reskinned for a reactor-depths stage of a sci-fantasy defense roguelite. Keep the same fast, aggressive, direct-pursuit silhouette established by the base Chaser design so the role still reads instantly, but rebuild its materials and color language in the stage's cool blue-violet and cyan palette, with faint pulsing containment-field seams instead of the base version's accents, as if the creature were assembled from or exposed to reactor coolant technology. Transparent background, no baked shadow, readable at small to medium gameplay scale.

### Asset 64 - Stage 2 Enemy Ranged Pressure (Reactor Depths)

Target: Assets/Art/Characters/Enemies/enemy_ranged_pressure_stage2_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Ranged Pressure role, reskinned for a reactor-depths stage of a sci-fantasy defense roguelite - this is the dominant role of the stage's enemy pool, so it should feel like the signature threat here. Keep the base role's ranged-attack silhouette (integrated weapon or casting structure, posture distinct from the Chaser and Bruiser) but rebuild it in the stage's cool blue-violet and cyan palette with a visibly charged, containment-field emitter motif befitting a higher elite density. Transparent background, no baked shadow, clear gameplay readability.

### Asset 65 - Stage 2 Enemy Bruiser (Reactor Depths)

Target: Assets/Art/Characters/Enemies/enemy_bruiser_stage2_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Bruiser role, reskinned for a reactor-depths stage of a sci-fantasy defense roguelite. Preserve the base Bruiser's heavier, slower, broad-mass silhouette relative to the Chaser and Ranged Pressure roles, but rebuild its materials in the stage's cool blue-violet and cyan palette with reinforced containment-plating details. Transparent background, no baked shadow, readable from a distance.

### Asset 66 - Stage 3 Enemy Chaser (Salvage Fields)

Target: Assets/Art/Characters/Enemies/enemy_chaser_stage3_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Chaser role, reskinned for a salvage-fields stage of a sci-fantasy defense roguelite. Keep the base Chaser's fast, aggressive, direct-pursuit silhouette, but rebuild it in the stage's rusty orange and verdigris-green palette, looking cobbled together from scavenged mech plating and exposed rebar rather than clean factory-issue armor. Transparent background, no baked shadow, readable at small to medium gameplay scale.

### Asset 67 - Stage 3 Enemy Ranged Pressure (Salvage Fields)

Target: Assets/Art/Characters/Enemies/enemy_ranged_pressure_stage3_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Ranged Pressure role, reskinned for a salvage-fields stage of a sci-fantasy defense roguelite. Keep the base role's ranged-attack silhouette and weapon/casting structure distinct from the Chaser and Bruiser, but rebuild it in the stage's rusty orange and verdigris-green palette using scavenged, jury-rigged scrap-weapon detailing. Transparent background, no baked shadow, clear gameplay readability.

### Asset 68 - Stage 3 Enemy Bruiser (Salvage Fields)

Target: Assets/Art/Characters/Enemies/enemy_bruiser_stage3_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Bruiser role, reskinned for a salvage-fields stage of a sci-fantasy defense roguelite - this is the dominant role of the stage's enemy pool, appearing in multiple simultaneous elite variants, so it needs to read clearly as a heavy threat at a glance even when several are on screen. Keep the base Bruiser's heavier, slower, broad-mass silhouette, but rebuild it in the stage's rusty orange and verdigris-green palette out of salvaged mech-husk plating and reinforced scrap armor. Transparent background, no baked shadow, readable from a distance.

### Asset 69 - Stage 4 Enemy Chaser (Cryo Vaults)

Target: Assets/Art/Characters/Enemies/enemy_chaser_stage4_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Chaser role, reskinned for a cryo-vaults stage of a sci-fantasy defense roguelite. Keep the base Chaser's fast, aggressive, direct-pursuit silhouette, but rebuild it in the stage's icy white-blue and pale cyan palette, with frost-rimed plating and faint crystalline edges that read as compatible with an on-hit freeze-pulse overlay (`vfx_freeze_status_pulse.png`) without the base sprite itself looking frozen solid. Transparent background, no baked shadow, readable at small to medium gameplay scale.

### Asset 70 - Stage 4 Enemy Ranged Pressure (Cryo Vaults)

Target: Assets/Art/Characters/Enemies/enemy_ranged_pressure_stage4_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Ranged Pressure role, reskinned for a cryo-vaults stage of a sci-fantasy defense roguelite. Keep the base role's ranged-attack silhouette and weapon/casting structure distinct from the Chaser and Bruiser, but rebuild it in the stage's icy white-blue and pale cyan palette with frost-rimed plating and faint crystalline edges. Transparent background, no baked shadow, clear gameplay readability.

### Asset 71 - Stage 4 Enemy Bruiser (Cryo Vaults)

Target: Assets/Art/Characters/Enemies/enemy_bruiser_stage4_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Bruiser role, reskinned for a cryo-vaults stage of a sci-fantasy defense roguelite. Keep the base Bruiser's heavier, slower, broad-mass silhouette, but rebuild it in the stage's icy white-blue and pale cyan palette with frost-rimed plating and faint crystalline edges, feeling like the stage's coldest, most armored threat. Transparent background, no baked shadow, readable from a distance.

### Asset 72 - Stage 5 Enemy Chaser (Core Nexus, Final)

Target: Assets/Art/Characters/Enemies/enemy_chaser_stage5_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Chaser role, reskinned for the final Core Nexus stage of a sci-fantasy defense roguelite - this stage mixes every earlier role and elite variant at once, so it should read as the culmination of the previous three Chaser reskins. Keep the base Chaser's fast, aggressive, direct-pursuit silhouette, but rebuild it in the stage's high-contrast white-gold-against-near-black palette, with the faintest material echoes of the three earlier stages (a hint of steel, coolant-blue, rust-orange or frost) bleeding subtly into an otherwise unified finale look. Transparent background, no baked shadow, readable at small to medium gameplay scale.

### Asset 73 - Stage 5 Enemy Ranged Pressure (Core Nexus, Final)

Target: Assets/Art/Characters/Enemies/enemy_ranged_pressure_stage5_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Ranged Pressure role, reskinned for the final Core Nexus stage of a sci-fantasy defense roguelite. Keep the base role's ranged-attack silhouette and weapon/casting structure distinct from the Chaser and Bruiser, but rebuild it in the stage's high-contrast white-gold-against-near-black palette, with the faintest material echoes of the three earlier stages bleeding subtly into an otherwise unified finale look. Transparent background, no baked shadow, clear gameplay readability.

### Asset 74 - Stage 5 Enemy Bruiser (Core Nexus, Final)

Target: Assets/Art/Characters/Enemies/enemy_bruiser_stage5_topdown.png

**Status:** present.

Prompt:

Create a top-down enemy sprite for the Bruiser role, reskinned for the final Core Nexus stage of a sci-fantasy defense roguelite - the most imposing regular-enemy variant in the game, appearing alongside every other role and elite variant at once. Keep the base Bruiser's heavier, slower, broad-mass silhouette, but rebuild it in the stage's high-contrast white-gold-against-near-black palette, with the faintest material echoes of the three earlier stages bleeding subtly into an otherwise unified finale look. Transparent background, no baked shadow, readable from a distance.

## Zone Denier and Relay Warden (2026-09-20)

Two new enemy roles shipped on 2026-09-20 and close the Vision Section 9 target of five distinct
enemy types. Both names come from [Enemies.md](../Specs/Enemies.md)'s own list of example roles
("Flaechenkontrolle" and "Support fuer andere Enemies") rather than being invented, so they
extend the roster along an axis the spec already anticipated instead of adding a new kind of
thing.

Unlike the Stage 2-5 reskins above, these are **new silhouettes, not palette swaps** - there is no
base sprite to preserve, because neither role existed when Assets 16-18 were written. Asset 75 and
Asset 80 define the two base designs; the eight that follow are stage reskins of those two in the
established per-stage palettes, exactly the relationship Assets 63-74 have to the original three
roles.

**Why each silhouette is specified the way it is:** both roles are read positionally rather than
by their attack, so the shape has to carry the behaviour. The Zone Denier has no front because it
has no facing - it fires in every direction at once, and a player who reads it as "aimed" will
dodge the wrong way. The Relay Warden must never look armed, because the whole decision it forces
is "ignore the thing shooting at me and kill the thing that is not".

**Current state (placeholder):** `Enemy_AreaControl.asset` and `Enemy_Support.asset` reference the
ranged and chaser sheets respectively and are separated only by `EnemyData.SpriteTint` (green and
violet). That is deliberate and temporary; it lets both roles ship and be balanced before the art
exists, and it is why they currently read as recolored versions of other enemies.

**Wiring once generated:** `EnemyRuntimeController.ResolveStageSpritePath()` resolves per-stage art
by enemy id. Add `case "enemy_area_control":` and `case "enemy_support":` blocks following the
exact pattern the three existing roles use, with the five paths below, and clear the placeholder
`_spriteTint` on both `EnemyData` assets back to white so the real art is not tinted on top of.

### Asset 75 - Zone Denier (Stage 1, Foundry)

Target: Assets/Art/Characters/Enemies/enemy_zone_denier_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Zone Denier role in a sci-fantasy defense roguelite. This enemy never chases anyone: it advances on the defended Core, anchors itself, and fires full 360-degree rings of projectiles on a long, clearly telegraphed windup. The silhouette must communicate that at a glance - a wide, low, planted stance with visible ground anchors or splayed bracing legs, built around a raised central emitter crown ringed with evenly spaced ports that read as firing outward in every direction at once. It must be instantly distinguishable from the three existing roles: not sharp and forward-leaning like the Chaser, not posed around a single directional weapon like the Ranged Pressure, and not a mass of forward armor like the Bruiser. Radial symmetry is the key read - this thing has no front. Leave clear space around the emitter crown so a charge-up glow can be tinted over it at runtime without hiding the silhouette. Palette for this stage: steel-blue structural plating, dark slate and warm amber energy accents, matching the base enemy roster. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

### Asset 76 - Zone Denier (Stage 2, Reactor Depths)

Target: Assets/Art/Characters/Enemies/enemy_zone_denier_stage2_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Zone Denier role in a sci-fantasy defense roguelite. This enemy never chases anyone: it advances on the defended Core, anchors itself, and fires full 360-degree rings of projectiles on a long, clearly telegraphed windup. The silhouette must communicate that at a glance - a wide, low, planted stance with visible ground anchors or splayed bracing legs, built around a raised central emitter crown ringed with evenly spaced ports that read as firing outward in every direction at once. It must be instantly distinguishable from the three existing roles: not sharp and forward-leaning like the Chaser, not posed around a single directional weapon like the Ranged Pressure, and not a mass of forward armor like the Bruiser. Radial symmetry is the key read - this thing has no front. Leave clear space around the emitter crown so a charge-up glow can be tinted over it at runtime without hiding the silhouette. Palette for this stage: cool blue-violet and cyan, with faint pulsing containment-field seams. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

### Asset 77 - Zone Denier (Stage 3, Salvage Fields)

Target: Assets/Art/Characters/Enemies/enemy_zone_denier_stage3_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Zone Denier role in a sci-fantasy defense roguelite. This enemy never chases anyone: it advances on the defended Core, anchors itself, and fires full 360-degree rings of projectiles on a long, clearly telegraphed windup. The silhouette must communicate that at a glance - a wide, low, planted stance with visible ground anchors or splayed bracing legs, built around a raised central emitter crown ringed with evenly spaced ports that read as firing outward in every direction at once. It must be instantly distinguishable from the three existing roles: not sharp and forward-leaning like the Chaser, not posed around a single directional weapon like the Ranged Pressure, and not a mass of forward armor like the Bruiser. Radial symmetry is the key read - this thing has no front. Leave clear space around the emitter crown so a charge-up glow can be tinted over it at runtime without hiding the silhouette. Palette for this stage: rusty orange and verdigris-green, cobbled together from scavenged mech plating and exposed rebar. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

### Asset 78 - Zone Denier (Stage 4, Cryo Vaults)

Target: Assets/Art/Characters/Enemies/enemy_zone_denier_stage4_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Zone Denier role in a sci-fantasy defense roguelite. This enemy never chases anyone: it advances on the defended Core, anchors itself, and fires full 360-degree rings of projectiles on a long, clearly telegraphed windup. The silhouette must communicate that at a glance - a wide, low, planted stance with visible ground anchors or splayed bracing legs, built around a raised central emitter crown ringed with evenly spaced ports that read as firing outward in every direction at once. It must be instantly distinguishable from the three existing roles: not sharp and forward-leaning like the Chaser, not posed around a single directional weapon like the Ranged Pressure, and not a mass of forward armor like the Bruiser. Radial symmetry is the key read - this thing has no front. Leave clear space around the emitter crown so a charge-up glow can be tinted over it at runtime without hiding the silhouette. Palette for this stage: icy white-blue and pale cyan, with frost-rimed plating and faint crystalline edges. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

### Asset 79 - Zone Denier (Stage 5, Core Nexus, Final)

Target: Assets/Art/Characters/Enemies/enemy_zone_denier_stage5_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Zone Denier role in a sci-fantasy defense roguelite. This enemy never chases anyone: it advances on the defended Core, anchors itself, and fires full 360-degree rings of projectiles on a long, clearly telegraphed windup. The silhouette must communicate that at a glance - a wide, low, planted stance with visible ground anchors or splayed bracing legs, built around a raised central emitter crown ringed with evenly spaced ports that read as firing outward in every direction at once. It must be instantly distinguishable from the three existing roles: not sharp and forward-leaning like the Chaser, not posed around a single directional weapon like the Ranged Pressure, and not a mass of forward armor like the Bruiser. Radial symmetry is the key read - this thing has no front. Leave clear space around the emitter crown so a charge-up glow can be tinted over it at runtime without hiding the silhouette. Palette for this stage: high-contrast white-gold against a near-black body, with the faintest material echoes of the three earlier stages bleeding subtly into an otherwise unified finale look. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

### Asset 80 - Relay Warden (Stage 1, Foundry)

Target: Assets/Art/Characters/Enemies/enemy_relay_warden_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Relay Warden role in a sci-fantasy defense roguelite. This enemy barely attacks. It shadows the enemies around it and strengthens them, and it backs away when the player closes, so it has to read as a priority target rather than a threat: lighter and more fragile than every other role, with a tall slender mast or antenna array, visible link nodes or tethering emitters along its upper body, and an obviously unarmed front. Nothing on it should look like a gun. It must be instantly distinguishable from the three existing roles by being the least armored and least aggressive shape in the roster. Leave a clean ring of empty space around its base so a support-aura effect can be drawn beneath it at runtime without fighting the sprite. Palette for this stage: steel-blue structural plating, dark slate and warm amber energy accents, matching the base enemy roster. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

### Asset 81 - Relay Warden (Stage 2, Reactor Depths)

Target: Assets/Art/Characters/Enemies/enemy_relay_warden_stage2_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Relay Warden role in a sci-fantasy defense roguelite. This enemy barely attacks. It shadows the enemies around it and strengthens them, and it backs away when the player closes, so it has to read as a priority target rather than a threat: lighter and more fragile than every other role, with a tall slender mast or antenna array, visible link nodes or tethering emitters along its upper body, and an obviously unarmed front. Nothing on it should look like a gun. It must be instantly distinguishable from the three existing roles by being the least armored and least aggressive shape in the roster. Leave a clean ring of empty space around its base so a support-aura effect can be drawn beneath it at runtime without fighting the sprite. Palette for this stage: cool blue-violet and cyan, with faint pulsing containment-field seams. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

### Asset 82 - Relay Warden (Stage 3, Salvage Fields)

Target: Assets/Art/Characters/Enemies/enemy_relay_warden_stage3_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Relay Warden role in a sci-fantasy defense roguelite. This enemy barely attacks. It shadows the enemies around it and strengthens them, and it backs away when the player closes, so it has to read as a priority target rather than a threat: lighter and more fragile than every other role, with a tall slender mast or antenna array, visible link nodes or tethering emitters along its upper body, and an obviously unarmed front. Nothing on it should look like a gun. It must be instantly distinguishable from the three existing roles by being the least armored and least aggressive shape in the roster. Leave a clean ring of empty space around its base so a support-aura effect can be drawn beneath it at runtime without fighting the sprite. Palette for this stage: rusty orange and verdigris-green, cobbled together from scavenged mech plating and exposed rebar. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

### Asset 83 - Relay Warden (Stage 4, Cryo Vaults)

Target: Assets/Art/Characters/Enemies/enemy_relay_warden_stage4_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Relay Warden role in a sci-fantasy defense roguelite. This enemy barely attacks. It shadows the enemies around it and strengthens them, and it backs away when the player closes, so it has to read as a priority target rather than a threat: lighter and more fragile than every other role, with a tall slender mast or antenna array, visible link nodes or tethering emitters along its upper body, and an obviously unarmed front. Nothing on it should look like a gun. It must be instantly distinguishable from the three existing roles by being the least armored and least aggressive shape in the roster. Leave a clean ring of empty space around its base so a support-aura effect can be drawn beneath it at runtime without fighting the sprite. Palette for this stage: icy white-blue and pale cyan, with frost-rimed plating and faint crystalline edges. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

### Asset 84 - Relay Warden (Stage 5, Core Nexus, Final)

Target: Assets/Art/Characters/Enemies/enemy_relay_warden_stage5_topdown.png

**Status:** NOT CREATED YET.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the creature silhouette must be fully transparent (alpha 0), not white. Create a top-down enemy sprite for the Relay Warden role in a sci-fantasy defense roguelite. This enemy barely attacks. It shadows the enemies around it and strengthens them, and it backs away when the player closes, so it has to read as a priority target rather than a threat: lighter and more fragile than every other role, with a tall slender mast or antenna array, visible link nodes or tethering emitters along its upper body, and an obviously unarmed front. Nothing on it should look like a gun. It must be instantly distinguishable from the three existing roles by being the least armored and least aggressive shape in the roster. Leave a clean ring of empty space around its base so a support-aura effect can be drawn beneath it at runtime without fighting the sprite. Palette for this stage: high-contrast white-gold against a near-black body, with the faintest material echoes of the three earlier stages bleeding subtly into an otherwise unified finale look. Transparent background, no baked shadow, no text, readable at small to medium gameplay scale, flat top-down orthographic view consistent with the existing enemy roster.

## Skill Tree, Weapon Loadout, Currencies and Factory Tiers (2026-09-21)

These cover the systems added by the owner decisions of 2026-09-21, specified in
[SkillTree.md](../Specs/SkillTree.md), [WeaponLoadout.md](../Specs/WeaponLoadout.md) and
[Upgrades.md](../Specs/Upgrades.md).

Two things separate this batch from every earlier one. First, it is almost entirely **UI art**,
so the global "flat top-down gameplay view" framing does not apply - these are interface pieces
and should be judged on legibility at small scale. Second, several of them are **state-bearing**:
one piece of art has to read correctly when tinted for three or four different states, because
drawing separate art per state multiplies the asset count for no gain.

The three currency icons are the highest-risk set in the whole project for a readability mistake.
Scrap, Skill Points and Alloy can appear in a single menu, and if two of them are confusable the
player cannot tell what they are spending. They must differ in silhouette, not only in colour.

### Asset 85 - Skill Tree Key Node Frame

Target: Assets/Art/UI/SkillTree/skill_node_key_frame.png

**Status:** DELIVERED 2026-09-22, DEFECTIVE - the frame's centre is a baked-in checkerboard (fake transparency). Not wired; the Progression screen keeps its procedural rows. Needs a re-render with a real transparent centre.

Needed because:

SkillTree.md's Key Nodes are the large, build-defining entries in each branch. They need a frame that reads as substantially more important than a Sub Node at a glance, and that supports three states through tinting alone: locked, affordable, purchased.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the artwork must be fully transparent (alpha 0), not white. Create a large hexagonal 2D UI node frame for a skill tree in a sci-fantasy defense roguelite. It must read as the anchor of a cluster: a heavy outer rim with a deep inset centre that leaves room for an icon, and small connector nubs at the six vertices where links to sub-nodes attach. Keep the interior flat and low-contrast so an icon placed inside stays readable, and keep the rim distinctive enough that the same shape tinted three different ways still reads as locked, available and owned. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 86 - Skill Tree Sub Node Frame

Target: Assets/Art/UI/SkillTree/skill_node_sub_frame.png

**Status:** DELIVERED 2026-09-22, WRONG CONTENT - the image is a node-cluster diagram, not a single sub-node frame. Not wired. Needs a re-render as one small frame.

Needed because:

Sub Nodes are the small, rankable modifiers that fan around a Key Node. They must be instantly subordinate to the Key Node frame and must not compete with it for attention.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the artwork must be fully transparent (alpha 0), not white. Create a small circular 2D UI node frame for a skill tree in a sci-fantasy defense roguelite, designed to sit around a larger hexagonal key node. Simple ring with a shallow inset centre and a single connector nub. It must read as clearly smaller and lighter than the key node frame even when both are scaled to the same size on screen - the difference has to be in the shape language, not just the dimensions. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 87 - Skill Tree Link Segment

Target: Assets/Art/UI/SkillTree/skill_link_segment.png

**Status:** DELIVERED 2026-09-22. Mirrored to StreamingAssets; not wired yet (the tree is a tab strip, so there is nothing to link).

Needed because:

The tree draws links between a Key Node and its Sub Nodes, and between prerequisite Key Nodes. A single tileable segment lets any link length be drawn without per-link art.

Prompt:

Create a short horizontal 2D UI connector segment for a skill tree, designed to tile end-to-end along a straight line of any length without visible seams. A slim conduit with a faint energy channel running down its centre, flat ends so repeats butt together cleanly. It must read as inert when tinted dark and as energised when tinted bright, since the same art is used for both an unearned and an earned link. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 88 - Ascension Rank Badge

Target: Assets/Art/UI/SkillTree/ascension_rank_badge.png

**Status:** DELIVERED 2026-09-22 and wired (main-menu Ascension badge).

Needed because:

Ascension is the endless track past a completed skill tree. It needs one badge that can carry a rank number and that reads as prestige rather than as another purchasable node.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the artwork must be fully transparent (alpha 0), not white. Create a 2D prestige badge for the endless post-completion progression track of a sci-fantasy defense roguelite. A radiant layered medallion with a clear flat centre for a rank number to be drawn over at runtime. It should feel earned and ceremonial, distinctly more valuable than any skill node frame, without becoming so busy that a two- or three-digit number placed on it stops being readable. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 89 - Skill Branch Icon Sheet

Target: Assets/Art/UI/SkillTree/skill_branch_icon_sheet.png

**Status:** DELIVERED 2026-09-22 and wired (Progression screen branch emblem, four rects cut from the sheet).

Needed because:

Four branches (Combat, Construction, Industry, Command) each need an identifying icon for the branch selector. One sheet keeps them visually consistent.

Prompt:

Create a unified 2D icon sheet containing exactly four icons in a single row, evenly spaced, for the four skill branches of a sci-fantasy defense roguelite: Combat (a weapon or strike motif), Construction (a tower or scaffold motif), Industry (a gear or conduit motif) and Command (a banner or command-node motif). Each icon must be readable at small size, share one line and shape language, and be distinguishable from the other three in silhouette alone. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 90 - Weapon Loadout Slot Frame

Target: Assets/Art/UI/HUD/weapon_slot_frame.png

**Status:** DELIVERED 2026-09-22 and wired with a workaround - the centre is a baked-in checkerboard, so the HUD draws it as a 9-slice with the centre not filled (border only). A re-render with a transparent centre would let it be used as-is.

Needed because:

WeaponLoadout.md puts six weapon slots on screen during combat. Each needs a compact frame that supports a cooldown sweep drawn over it and reads clearly as empty when it holds nothing.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the artwork must be fully transparent (alpha 0), not white. Create a compact square 2D UI slot frame for a weapon loadout bar in a top-down sci-fantasy roguelite. Thin bevelled border with a flat, dark, empty interior, designed so a weapon icon sits inside it and a radial cooldown sweep can be drawn over the whole slot without fighting the frame. An empty slot must look deliberately empty rather than broken, so keep the interior clean and evenly toned. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 91 - Weapon Set Bonus Tier Pips

Target: Assets/Art/UI/HUD/weapon_set_tier_pips.png

**Status:** DELIVERED 2026-09-22 and wired (HUD Set Bonus pips, off/on cells).

Needed because:

Set bonuses run through five named tiers (Paired, Aligned, Focused, Synchronised, Perfected). The combat HUD shows them as pips rather than text, so they need five escalating states.

Prompt:

Create a 2D icon sheet containing exactly five small pip shapes in a single row, escalating in visual weight from the first to the fifth, for a weapon set-bonus indicator in a sci-fantasy roguelite. The first should read as a faint mark and the fifth as a fully charged emblem, with the three between forming an obvious progression. Each pip must be legible at roughly 16 pixels and must read as a rank rather than as a quantity. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 92 - Alloy Currency Icon

Target: Assets/Art/Resources/resource_alloy_icon.png

**Status:** DELIVERED 2026-09-22 and wired (Progression header).

Needed because:

Upgrades.md introduces Alloy as the third currency. It must be instantly distinguishable from Scrap (the in-run currency) and from Skill Points, because all three can appear in the same menu.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the artwork must be fully transparent (alpha 0), not white. Create a 2D currency icon for a permanent between-runs upgrade currency called Alloy in a sci-fantasy defense roguelite. A refined ingot or layered plate motif with a cool metallic sheen and a single warm energy seam. It must be immediately distinguishable in silhouette from a scrap-salvage icon and from a crystalline skill-point icon, since all three can appear in one menu at once. Compact, collectible-looking, readable at HUD scale. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 93 - Skill Point Icon

Target: Assets/Art/Resources/resource_skill_point_icon.png

**Status:** DELIVERED 2026-09-22 and wired (Progression header).

Needed because:

The second currency needs its own icon, distinct from both Scrap and Alloy.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the artwork must be fully transparent (alpha 0), not white. Create a 2D currency icon for a permanent character-progression currency called Skill Points in a sci-fantasy defense roguelite. A faceted crystalline shard with an inner glow, cool and arcane rather than industrial, so it contrasts with a metallic alloy ingot and a rough scrap salvage icon. Compact silhouette, readable at HUD scale. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 94 - Core Repair Prompt Icon

Target: Assets/Art/UI/HUD/core_repair_icon.png

**Status:** DELIVERED 2026-09-22 and wired (world prompt above the Core during an Intermission).

Needed because:

The Core is repairable by standing near it and pressing Interact during an Intermission. That affordance needs an icon that appears above the Core, distinct from the tower-build prompt.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the artwork must be fully transparent (alpha 0), not white. Create a 2D interaction prompt icon for repairing a damaged energy core in a top-down sci-fantasy defense roguelite. A wrench or weld-spark motif fused with a core-energy glyph, reading unmistakably as restore rather than as build or upgrade. It will float above the Core in the world, so it needs a strong silhouette against a busy arena floor. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

### Asset 95 - Factory Chain Tier Indicator

Target: Assets/Art/UI/HUD/factory_chain_tier_indicator.png

**Status:** DELIVERED 2026-09-22. Mirrored to StreamingAssets; not wired yet.

Needed because:

Vision Section 5.5 requires the player be able to recognise that a system is working. Production chains now climb tiers, and the Amplifier currently pays out invisibly. This indicator is what makes the chain's state legible during an Intermission.

Prompt:

Create a horizontal 2D UI indicator for a production-chain tier in a sci-fantasy defense roguelite: a short row of four linked conduit segments that fill progressively from left to right as the chain advances. Unfilled segments must read as dormant-but-present rather than absent, so the player can see how much chain is still to be earned. Designed to be tinted at runtime for filled and unfilled states from the same art. Match the established Corebound UI language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text, readable at small UI scale.

## Boss Telegraphs, Tower Info and Character Select (2026-09-21)

Three systems shipped on 2026-09-21 that need art to be fully legible.

**The boss telegraphs are the highest-priority set in this file.** Boss attack shapes now differ per
stage, which is what finally makes the five bosses play differently - but the existing telegraph is
a single ghost projectile that flies toward the target, and that only describes a Fan. A Ring, a
Volley and a Lance warned by a Fan telegraph are worse than no warning, because the player reads a
direction that is not there. Until these exist, three of the five bosses telegraph a lie.

Fan and Spiral are covered by the existing ghost-projectile telegraph and need no new art: Fan is
what it was designed for, and Spiral is a Fan that rotates.

### Asset 96 - Boss Telegraph - Ring

Target: Assets/Art/Combat/VFX/vfx_boss_telegraph_ring.png

**Status:** CREATED 2026-09-21 and wired 2026-09-22 - `BossRuntimeController` draws it for the Ring and Spiral wind-ups (scales up over the wind-up). Mirrored to StreamingAssets.

Needed because:

Each boss now fires a different shape (Fan, Ring, Spiral, Volley, Lance). A shape the player cannot read before it fires is a memorisation test, not a dodge. Ring needs its own warning because it has no direction to read off the boss's facing.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the artwork must be fully transparent (alpha 0), not white. Create a 2D attack telegraph for a top-down sci-fantasy defense roguelite, warning that a boss is about to fire projectiles outward in every direction at once. An expanding concentric warning ring with a bright leading edge and evenly spaced outward tick marks around its circumference, communicating 'this goes everywhere, find a gap' rather than 'this goes that way'. It is drawn under the boss and must stay readable over a busy arena floor. Match the established Corebound language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text.

### Asset 97 - Boss Telegraph - Lance

Target: Assets/Art/Combat/VFX/vfx_boss_telegraph_lance.png

**Status:** CREATED 2026-09-21 and wired 2026-09-22 for the Lance wind-up. **Defect:** the far end of the lance has a baked-in grey checkerboard (fake transparency) where it should be transparent - regenerate or erase that tip. Mirrored to StreamingAssets.

Needed because:

Lance fires every projectile along one bearing. Its whole counterplay is 'do not stand in that line', which needs a warning that draws the line.

Prompt:

Create a 2D attack telegraph for a top-down sci-fantasy defense roguelite, warning that a boss is about to fire a concentrated stream along a single bearing. A long narrow tapering beam-path marker, brightest at the origin and fading along its length, with hard parallel edges so the danger line is unambiguous. Designed to be stretched to any length at runtime, so it must tile or stretch cleanly along its long axis. Match the established Corebound language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text.

### Asset 98 - Boss Telegraph - Volley

Target: Assets/Art/Combat/VFX/vfx_boss_telegraph_volley.png

**Status:** CREATED 2026-09-21 and wired 2026-09-22 for the Volley wind-up, oriented along the firing direction. Mirrored to StreamingAssets.

Needed because:

Volley fires parallel shots abreast, a wall rather than a cone. The player dodges around an end, which only works if the wall's extent is visible before it fires.

Prompt:

Create a 2D attack telegraph for a top-down sci-fantasy defense roguelite, warning that a boss is about to fire a wall of parallel projectiles. A broad rectangular warning band made of several distinct parallel lanes, so the player can see both how wide the wall is and where its ends are. Designed to be stretched horizontally at runtime. Match the established Corebound language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text.

### Asset 99 - Tower Info Card Frame

Target: Assets/Art/UI/HUD/tower_info_card_frame.png

**Status:** CREATED 2026-09-21 and wired 2026-09-22 in the tower build browser. Delivered as a sheet (one large card at x44 y44 525x525, three small cards at x626-868), sliced by pixel rect in `TowerBuildBrowserPresenter`. The art bakes in the words "Strong against" / "Weak against" in English, so they will not localise. Mirrored to StreamingAssets.

Needed because:

Choosing between four towers previously meant remembering their stats. The build selection now shows cost, damage per second, range, and what each tower is strong and weak against. That needs a frame that holds a heading, a stat row and two short lines without crowding.

Prompt:

Create a compact 2D UI info card frame for a top-down sci-fantasy defense roguelite, used to describe a buildable tower during a planning phase. It must hold a title row, a short row of numeric stats, and two short body lines, with clear internal separation between those three zones so the eye lands on the stats first. Support 9-slice scaling, keep the inner surface bright enough for dark text, and keep the whole thing small enough to sit near the bottom of the screen without covering the arena. Match the established Corebound language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text.

### Asset 100 - Character Select Portrait Frame

Target: Assets/Art/UI/SkillTree/character_portrait_frame.png

**Status:** CREATED 2026-09-21 in `Assets/Art/UI/Skilltree/` (lower-case t; the target path above says `SkillTree`). Not used yet - there is no character select. **Defect:** the centre, which should be transparent for the portrait, is a baked-in checkerboard.

Needed because:

Each playable character now has its own skill tree and is levelled separately, so the skill tree route needs a character selector. One frame, reused per character, with room for a level number.

Prompt:

1:1 square aspect ratio, exactly equal width and height, do not output a wide or landscape canvas, and do not add a white or colored page background - the canvas outside the artwork must be fully transparent (alpha 0), not white. Create a 2D portrait frame for a character selector in a sci-fantasy defense roguelite. A rounded-square frame with a heavy lower band where a character name and level number can be drawn over it at runtime, and a clean inner window for the portrait. It must read as selected or unselected purely through tinting, since one piece of art serves both states. Match the established Corebound language: steel-blue structure, pale metallic edges, restrained amber-gold energy accents, clean industrial framing, strong silhouette. Transparent background, no text.

