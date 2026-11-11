# FluidSynth music pipeline

Status: built
Kind: feature for all three prototypes (tools + new music for Neon Pong and Medieval Chess; DioramaTurn's already-specced track rides the same pipeline)

<!-- Written for the user's brief, adapted to the spec template. Research and sources: docs/research/fluidsynth-music.md -->

## One-page design
- **Pitch:** background music written as readable note-level Python scripts, rendered to `.wav` by FluidSynth with a SoundFont, checked by script, and never hand-edited.
- **Feeling:** each prototype keeps its own mood (neon arcade, torch-lit hall, toy-box), now with real instrument timbres instead of synthesized tones or silence.
- **Pillars:**
  1. **Notes, not waveforms.** So every track is a Python script of named constants (tempo, key, instruments) that a diff shows, rendered by one command. So we won't hand-edit rendered audio, commit final `.wav` files as source, or tune music by decimal polishing of samples.
  2. **One kitchen for every agent.** So the whole pipeline runs from the command line with no MCP server, the same way in Claude Code and OpenCode. So we won't add a FluidSynth MCP server, a Unity editor hook, or a per-tool script.
  3. **Music sits under the game.** So every track is normalized to a fixed peak and a checked RMS, ducked where the game needs it, and mixed at a per-game `music` volume below the SFX. So we won't let music mask a sound effect or ship an unmeasured render.
  4. **The person is the judge.** So the pipeline checks length, silence, clipping, loudness and loop seams, and one track at a time is handed over for listening. So we won't batch-deliver tracks nobody has heard, and no agent will claim music is good.
- **Core loop:** write or tune a track script, run `Tools/audio/render_music.sh <Prototype> <track>`, read its checks, listen in the Editor, repeat.
- **Verbs:** compose (edit the track script), render (the wrapper), listen (the person), approve.
- **Players, session length, platform, 2D or 3D:** not applicable; this is a build-time pipeline. Its output ships inside the three prototypes.

## Look and feel brief
Not applicable — this feature has no look. The audio direction per game:
- **Neon Pong:** one looping track, bright and minimal (a synth bass pulse, an arpeggio, a pad), about 60–90 s, seamless loop. Energetic but behind the bleeps.
- **Medieval Chess:** one slow looping track, lute or dulcimer over a low drone, about 60–90 s, seamless loop. Torch-lit, not epic.
- **DioramaTurn:** as its spec already says — one slow looping marimba and music-box tune in one pentatonic scale, 16–24 s, low volume, ducking under the goal chime.
All three are rendered from the same SoundFont, so they read as one set of instruments across the repository.

## Mechanics
- **Pipeline:** a Python script per track (`Tools/audio/<Name>/music/<track>.py`) writes a format-0 MIDI file through a shared stdlib writer (`Tools/audio/midi.py`), FluidSynth renders it to a wav, a post-processing step trims, normalizes and checks it, and the wav lands in `Assets/Prototypes/<Name>/Audio/music.wav`. The MIDI is an intermediate file, not committed.
- **Wrapper:** `Tools/audio/render_music.sh <Prototype> <track>` runs the track script, renders with `fluidsynth -ni -r 44100 -F`, then post-processes. It exits non-zero and says why if the track script fails, the MIDI is missing, or the wav is missing or empty. It finds the SoundFont through `SOUNDFONT_PATH`, falling back to the committed copy in `Tools/audio/soundfonts/`, and stops with a clear message if `fluidsynth` is not on PATH or no SoundFont is found.
- **Post-processing, with checks the wrapper prints:** trailing silence trimmed but reverb tails kept (FluidSynth 2.6.2 fast render already keeps the tail past the last MIDI event — verified, see research note); peak normalized to 0.8; RMS measured against a target; looping tracks get their reverb tail wrapped across the loop point so the seam has no click, and the first/last samples are checked.
- **Output format:** 16-bit 44.1 kHz wav like the existing SFX, but **stereo** (FluidSynth's natural output; a deliberate deviation from the mono SFX convention).
- **Unity import:** a new `AssetPostprocessor` in `Assets/Shared/Editor/` sets music clips to load on demand (streaming) with the repo's format defaults, applied by path (a `Music.wav` naming rule per prototype's Audio folder). Looping stays on the AudioSource, not the import settings.
- **Runtime:** each game gets one looping `AudioSource` (volume from a `music` field on its settings asset), plus ducking where its spec asks for it (DioramaTurn only).
- **Sound effects stay as they are.** The existing `make_sounds.py` scripts and their output are untouched.

## Tuning values
On each game's settings asset (added in this feature's stages): `music` volume 0.35 (Neon Pong, Medieval Chess) and DioramaTurn keeps its specced `audio.music` 0.35 and `audio.musicDuckDb` −9 for 1.5 s. Pipeline constants live in the wrapper and post-processor: peak 0.8, RMS target −18 dBFS ±2 dB, minimum 10 s and maximum 120 s for a track, loop-seam click threshold 0.01.

## Art and audio assets
- One SoundFont committed to `Tools/audio/soundfonts/` under Git LFS (see plan decision 1). Recommended: **GeneralUser GS 2.0.3** (31 MB, license allows commercial use, good mallets).
- Three music tracks, one per prototype, each a committed Python script plus a rendered `.wav` (LFS): Neon Pong ~60–90 s, Medieval Chess ~60–90 s, DioramaTurn 16–24 s. Budget: under 15 MB of rendered music total (stereo 16-bit 44.1 kHz is about 10 MB per minute).

## Acceptance criteria
### Checked by tests or captures
- [x] `render_music.sh` renders a track end to end and exits non-zero on a broken track script, a missing MIDI and a missing or empty wav (each shown once, as the deliberate-bug check).
- [x] Every rendered track passes its own checks: duration within bounds, peak at the target, RMS within the target band, no clipping, seam free of clicks for loops.
- [x] Every music `.wav` imports with the streaming load type (postprocessor test), and `Tools/unity/check.sh all` passes before and after.
- [x] Each game's scene builds with one music source that loops, has the clip assigned and the volume from its settings.
- [x] No existing SFX file, script or test changes.
### Needs a person
- [x] The pilot track sounds good and fits its game (mood, loudness against SFX, no fatigue over a few minutes).
- [ ] The remaining tracks, after the pilot's lessons.
- [ ] The loop seam is inaudible while actually playing.

## Tests
- EditMode: the music import postprocessor (streaming load type by path); the per-game scene builder tests (music source exists, loops, clip and volume wired); settings clamp for the new `music` field.
- Script-level (not Unity): the wrapper's own checks on every render, plus a failing-script and missing-file case that must exit non-zero.

## Out of scope
- Changing any sound effect.
- Adaptive or layered music at runtime (the track scripts may keep stem-friendly structure, but nothing layers live).
- A FluidSynth MCP server, a Unity-time FluidSynth, or runtime SoundFont rendering in the game.
- Licenses for any SoundFont other than the one committed here.

## Open questions
- **SoundFont pick (decision 1 in the plan):** GeneralUser GS recommended; your call before anything is committed.
- **Stereo output:** proposed, because FluidSynth renders stereo and music wants width; say so if you want mono to match the SFX convention exactly.
- **Pilot track:** DioramaTurn's loop, per your answer of 2026-11-11 (the plan originally proposed Medieval Chess). The whole feature waits until DioramaTurn is built.
- **Whether the committed `.sf2` is acceptable in Git LFS** (31 MB) — approved as part of the plan.

## Sources
- License texts, FluidSynth behaviour and the spike results: `docs/research/fluidsynth-music.md`.
- GeneralUser GS license `[read]`, FluidR3 MIT claim `[secondary]`, Musyng Kite CC BY-SA `[secondary]`, fast-render tail behaviour `[read]`.
