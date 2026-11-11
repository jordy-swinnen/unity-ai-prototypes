---
name: aseprite-art
description: Makes pixel art for a prototype in this repository with headless Aseprite Lua scripts (sprites, icons, animations, sprite sheets), runs them with Tools/aseprite/run.sh, and checks the exported PNG. Use it whenever a game needs 2D pixel art: sprites, icons, UI glyphs, tiles or animation frames. Never draw pixel art by hand or with an image tool.
compatibility: Needs Aseprite 1.3 or newer (bought or self-compiled; the trial cannot save or export) and the ASEPRITE_PATH variable. Works the same in Claude Code and OpenCode.
allowed-tools:
  - Bash
---

# aseprite-art

Pixel art here is code: a Lua script holds the drawing as text, so a diff shows what changed and the art can be rebuilt. Read the "Pixel art (Aseprite)" section of `AGENTS.md` first.

## Before you start
- `ASEPRITE_PATH` must point at the `aseprite` executable. Check with `"$ASEPRITE_PATH" --version`. If it is unset or fails, **stop and ask the user**. Never install Aseprite, never download it, never search the disk and pick a binary yourself.
- The spec's look brief decides the style. Read the prototype's spec (`docs/prototypes/<kebab-name>.md`) for size, palette and mood before drawing anything.

## The process
1. **Decide the palette first.** Write it as a named table (`ink`, `steel`, `gold`), not loose numbers. Few colours: 4 to 16 for the whole game, shared by every sprite, so the art looks like one set. One transparent character, always `.`. Use the spec's palette if it has one. Don't invent a new colour for one sprite, add it to the palette and say why.
2. **Choose the size.** Small and on a grid: 16 or 32 px for characters and props, 8 or 16 for icons and tiles. Use powers of two so Unity scales cleanly. One size per kind of thing. Never draw big and shrink it, the result is mush.
3. **Draw as a character map.** One string per row, one character per pixel, a legend from character to named colour. Keep every row the same length. Start from `references/example-icon.lua` (one image) or `references/example-sheet.lua` (frames plus a sprite sheet and its JSON). Copy it to `Tools/aseprite/<Prototype>/<name>.lua`.
4. **Keep the checks in the script.** Aseprite ignores an unknown character (`drawPixel` with a nil colour draws nothing and exits 0), so the templates `assert` on row length and unknown characters. Keep those asserts.
5. **Print every file you write**, as `print("wrote " .. path)`. `run.sh` fails if a `wrote` line is missing or its file is not there, new and non-empty.
6. **Run it:** `Tools/aseprite/run.sh <Prototype> <name>.lua`. `.png` and sheets go to `Assets/Prototypes/<Prototype>/Art/Sprites/` (the `out_dir` param), and `.aseprite` sources go to `Art/source/<Prototype>/` (the `src_dir` param). The script reads both with `app.params["out_dir"]` and `app.params["src_dir"]`. Exit codes: 0 good, 1 the script or a file failed, 2 no usable Aseprite or bad usage.
7. **Look at the PNG.** Always. Read the exported image and compare it with the map: shape, colours, no stray pixels, transparent background. Pixel art this small is hard to see, so make a scaled copy outside the repository and read that: `"$ASEPRITE_PATH" -b <file>.aseprite --scale 16 --save-as <scratch-folder>/preview.png` (write it outside the repository, never in `Assets/`). If it looks wrong, fix the map and run again. Don't report art you have not looked at.
8. **Compile check when C# changed.** Importing the PNG needs no compile. Run `Tools/unity/check.sh compile` only after you change C# (for example code that slices a sheet from its JSON). Unity then imports new PNGs as a Point-filtered Sprite by itself, through `Assets/Shared/Editor/PixelArtTextureImporter.cs`, for any path containing `/Art/Sprites/`.

## Sprite sheets
Export from Lua with `app.command.ExportSpriteSheet{ ... dataFormat = SpriteSheetDataFormat.JSON_ARRAY }`, as in `references/example-sheet.lua`. The command-line equivalent for a saved file is `"$ASEPRITE_PATH" --batch <src>.aseprite --sheet <out>.png --data <out>.json --format json-array`. Unity imports the sheet as one Single sprite. Slicing it into frames from the JSON (`frames[].frame` x, y, w, h and `duration` in ms) is the prototype's own editor code, with a test.

## Rules
- Scripts only. No hand-drawn sprites, no GUI sessions, no importing images from elsewhere. Art is original, and its source is recorded in the spec.
- Never edit a file in `Art/Sprites/` by hand, change the script and run it again.
- Write only inside `out_dir` and `src_dir`. `run.sh` fails otherwise.
- Make temporary files outside the repository, and delete any test output you made in `Assets/`.

## What you can and cannot check
- You can check that the files exist, the sizes, the palette and that the image matches the map.
- Judging whether the art looks good needs a model that reads images. In OpenCode that is not every model: the phase commands for `game-build` and `game-polish` use `opencode-go/glm-5.3-flash`, and `AGENTS.md` names `gemini-4-argon` for render judgment. Full `glm-5.3` is text-only. If your model cannot read images, say so in your report and ask the user to look at the PNG. Don't claim a look you didn't see.
- Say what you did not check: how the sprite looks in the game at its real size and under lighting, and how an animation feels in motion.
