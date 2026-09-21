# Art Pipeline Automation

Written 2026-09-21, answering: "Currently I use Gemini for the image creation and remove.bg for removing the background. Could that be automated?"

---

## Short answer

Yes, and the background-removal half should be automated first because it is free, local, and removes a manual step from every single asset.

The generation half is automatable too, but it is the half where automation buys less than it looks: the bottleneck is judging whether an image is right, not producing it.

---

## What the manual pipeline costs today

For each of the 95 assets in [GRAPHICS_ASSET_PROMPTS.md](GRAPHICS_ASSET_PROMPTS.md):

1. Copy the prompt into Gemini.
2. Download the result.
3. Upload it to remove.bg.
4. Download that result.
5. Rename it to the target filename.
6. Drop it in the right folder under `Assets/Art/`.
7. Run `Corebound > Sync StreamingAssets Art Mirror`.

Steps 3 to 7 are pure mechanics with no judgement in them, and they are where this project has repeatedly lost assets: the audit on 2026-09-21 found two files with a doubled `.png.png` extension, one that exists only in the StreamingAssets mirror, and fifteen enemy sprites deleted from the working tree with nothing in their place. Every one of those is a step-5-to-7 failure, not an art failure.

**That is the case for automation here.** Not speed, correctness.

---

## Recommendation, in order of value

### 1. Local background removal (do this first)

`remove.bg` is a paid web service with an upload round-trip per image. The same job runs locally, offline, for free:

- **`rembg`** (Python, `pip install rembg`) wraps the U²-Net family of models. One command per file, batch mode built in, no API key, no upload.
- It is the same class of model remove.bg uses. For clean-background generated art like this, output is effectively equivalent.

```
rembg p input_dir output_dir
```

Nothing in this project needs remove.bg's paid tier. A local pass also means the art never leaves the machine, and there is no per-image cost to re-running it after a regeneration.

### 2. An import script that does steps 4 to 7

This is the highest-value piece and it is entirely local. A script that:

1. Reads the `Target:` path for a given asset number straight out of `GRAPHICS_ASSET_PROMPTS.md`.
2. Runs background removal.
3. Verifies the result: is it actually square where the prompt demanded square, does it actually have transparent pixels, is it a PNG and not a JPEG behind a `.png` name.
4. Writes it to the exact target path, creating directories as needed.
5. Syncs the StreamingAssets mirror.
6. Updates that asset's `**Status:**` line in the prompts file.

Point 3 is worth more than the rest combined. This project has already shipped a JPEG behind a `.png` extension twice, and `stage2_ground_tileset.png` and `stage3_ground_tileset.png` are still JPEG data today. A validator that refuses the wrong thing at import time is how that stops happening.

Usage would be: generate in Gemini, drop the download into an `Art_Inbox/` folder named `85.png`, run the script.

### 3. Generation via API (optional, and the least urgent)

Gemini's image models are reachable through the Google AI API, so the prompt-to-file step can be scripted. Worth knowing before committing to it:

- **Judgement does not automate.** Roughly a third of the assets in this project needed a regeneration for a reason a script cannot detect: a corner drawn in 3/4 perspective when the set is flat (Asset 46), a non-square canvas (Assets 24e, 24f, 43), a silhouette that read as the wrong enemy role. A script can check dimensions and alpha; it cannot check "does this look like it belongs".
- **Batch generation is a cost multiplier on mistakes.** Generating all 11 outstanding assets unattended produces 11 images to review at once, and a prompt flaw affects all of them.
- **It is genuinely useful for variants.** Where it pays is the per-stage reskins: the same silhouette in five palettes, where the prompt differs only in the colour clause. That is 10 of the 11 outstanding assets.

Recommended split: script the generation for reskin families, keep generating new silhouettes by hand where the first look matters.

---

## What not to automate

- **Choosing which asset to make next.** That follows from what the game needs, which is a design decision.
- **Accepting an asset.** A human should look at every image once before it enters `Assets/Art/`. The validator checks that a file is well-formed, never that it is good.
- **Slicing sprite sheets.** This project already reads sheets by explicit pixel rect at runtime rather than through Unity's Sprite Editor, precisely because the automatic grid assumption was wrong for hand-composed sheets. Leave that alone.

---

## Concrete first step

Build the import script (item 2) with `rembg` (item 1) inside it. That is a self-contained tool, it needs no API key, it eliminates the file-handling class of bug that has already cost this project real time, and it works with the Gemini-by-hand workflow exactly as it is today.

API generation can be added to the same script later as an optional front end, for the reskin families only.

---

## Open questions

- Does `rembg` handle the crystalline and energy-glow edges in this art set as cleanly as remove.bg? Worth testing on three existing assets with soft glow edges before switching over wholesale.
- Should the import script auto-commit? Recommend no. Art changes deserve a human look at the diff, and this project has already had art deleted without anyone noticing.
