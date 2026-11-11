---
name: music-composition
description: Write and render game music in this repository as note-level Python track scripts through FluidSynth and a SoundFont, then check length, loudness and the loop seam. Use whenever a game needs a background track or a looping theme, when a track must be retuned, or when adding a new instrument or stem convention. Do not use it for sound effects; those stay in each prototype's make_sounds.py.
license: MIT
compatibility: Needs Python 3 (stdlib only), FluidSynth on PATH, and a SoundFont (SOUNDFONT_PATH or Tools/audio/soundfonts/GeneralUser-GS.sf2).
metadata:
  author: unity-ai-prototypes
  version: "1.0"
allowed-tools: Bash(python3:*) Bash(fluidsynth:*) Bash(Tools/audio/render_music.sh:*)
---

# music-composition

Music here is **notes in code**, rendered by instruments. A track is a readable Python script; the rendered `.wav` is an artefact, never edited by hand.

## Before you start

- Read the game's spec audio direction: mood, instruments, loop length, volume and ducking.
- `fluidsynth` must be on PATH; the SoundFont is `$SOUNDFONT_PATH` or `Tools/audio/soundfonts/GeneralUser-GS.sf2`.
- Read `docs/audio-credits.md` before adding an instrument or a SoundFont.

## The shape of a track

```
Tools/audio/<Name>/music/<track>.py      the notes (the source)
Tools/audio/midi.py                      the shared MIDI writer
Tools/audio/postprocess_music.py         trim, normalize, loop-wrap, check
Tools/audio/render_music.sh              run script -> MIDI -> FluidSynth -> checks -> wav
Assets/Prototypes/<Name>/Audio/music.wav the rendered clip (16-bit stereo 44.1 kHz)
```

The track script takes a MIDI path as `argv[1]`, composes with named constants, and prints:

```
loop_seconds=<seconds>     # only for looping tracks
```

The wrapper renders with `fluidsynth -ni -r 44100 -g 0.55 -F`, then wraps the reverb tail around the loop point and normalizes to peak 0.8.

## Writing the notes

- **Constants first:** `BPM`, `BEATS_PER_BAR`, `BARS`, `LOOP_SECONDS`, program numbers and a pitch table. A diff should read as a musical change.
- **One scale per game.** DioramaTurn uses C major pentatonic (C D E G A) for marimba, music box and bass, so nothing clashes.
- **Layers as functions or lists:** bass (one note per bar), arpeggio (eighth notes), melody (music-box tines with rests). Keep each layer's velocities separate: bass 45–50, arpeggio 48–56, melody 70–82.
- **Loop-friendly endings:** end on a note that leads back to the first, and let the wrapper wrap the tail; do not fade the last bar out.
- **Instrument choices that work with GeneralUser GS:** Marimba (12), Music Box (10), Glockenspiel (9), Vibraphone (11) for toys; Acoustic Grand (0) and Cello (42) for warmth. Avoid dense GM drums and heavy reverb patches — they smear at low loop volume.

## Render and check

```bash
Tools/audio/render_music.sh <Prototype> <track>
```

The wrapper fails (non-zero) on a script error, a missing MIDI or an empty render, and the post-processor fails when:

- the duration is outside 8–130 s,
- the RMS is outside −18 ± 4 dBFS,
- the peak does not normalize to 0.8,
- a loop seam jumps more than 0.02.

Read every printed line. Then look at a spectrogram or per-second RMS to confirm the structure; you cannot hear it.

## Wiring the clip into Unity

- One looping `AudioSource` per game with `volume = audio.music × audio.master` (DioramaTurn: `MusicPlayer`).
- Music import: `Assets/Shared/Editor/MusicClipImporter.cs` sets the streaming load type by path (the prototype's `Audio/music.wav`).
- Ducking: dip by `audio.musicDuckDb` for `audio.musicDuckSeconds` at the goal moment (DioramaTurn's `MusicPlayer.Duck`).
- A test asserts the scene has one looping source with the clip and the settings volume.

## Honest limits

- You can check length, silence, clipping, loudness, the loop seam and a spectrogram. You cannot judge whether the music is good or fits the game: one track at a time, the person listens.
- A GM SoundFont is a GM SoundFont. If a part cannot sound convincing, say so rather than pretending.
- Keep the SoundFont a build-time input; never ship it as a runtime asset.
