# FluidSynth music pipeline: plan and tasks

Plan for `docs/prototypes/fluidsynth-music.md`. Status: waiting for your approval. Nothing is built yet; the SoundFont spike ran outside the repository.

Your answers on 2026-11-11, recorded so nobody re-asks: **build DioramaTurn first, this feature comes after it**; the pilot track is **DioramaTurn's** loop (not Medieval Chess as proposed below); the track is composed in this feature's final stage, after the pipeline works. Stereo music and the stdlib MIDI writer (no mido) were not objected to and stand as proposed. Nothing here is approved for building yet.

Verified before this plan (see `docs/research/fluidsynth-music.md`): FluidSynth 2.6.2 is installed and on PATH; fast render keeps the reverb tail past the last MIDI event (a 1.0 s MIDI rendered 3.535 s and faded to zero on its own); output is stereo 16-bit 44.1 kHz wav; GeneralUser GS downloads as a 31 MB `.sf2` and renders. Still unverified: nothing below depends on an assumption that was not checked.

## Decisions needed first

1. **SoundFont (your pick).** Recommended: **GeneralUser GS 2.0.3** by S. Christian Collins — 31 MB, license read in full at his repository ("use without restriction for your own music creation, private or commercial"; samples "allow full use in music production"), 261 GM/GS presets with well-regarded mallets for DioramaTurn, small enough for Git LFS. Alternatives, not recommended: **FluidR3 GM** (~148 MB, MIT per Debian's packaging, but the license text itself could not be fetched — second-hand), **Musyng Kite** (~990 MB, CC BY-SA 3.0, attribution required, sample origins murky). The chosen `.sf2` is committed to `Tools/audio/soundfonts/` under Git LFS. **This commits a 31 MB file to the repository — approve that here.** The wrapper finds it through `SOUNDFONT_PATH`, falling back to the committed copy, and stops with a message when neither exists.
2. **Stereo music.** FluidSynth renders stereo; the SFX convention is mono. Proposed: stereo for music, mono unchanged for SFX. Alternative: downmix music to mono for one convention across all audio.
3. **No new Python dependencies.** A shared stdlib format-0 MIDI writer (~60 lines, already written for the spike and folded into this plan as `Tools/audio/midi.py`), matching the repo's `python3 -I` stdlib convention. If you would rather have `mido` for comfort, that needs your approval and a repo-local venv or similar, and this plan would change.
4. **Pilot track: Medieval Chess.** The game is complete so you can listen immediately, and a slow lute-and-drone loop is the most tolerant of a GM SoundFont's limits. Alternative: Neon Pong. One track is rendered, checked and stopped for your listen before anything else is composed.
5. **New music for Neon Pong and Medieval Chess is a spec change to those two specs** (audio sections: one loop each, source pipeline, music volume). The specs are edited in the stage that wires the sound, and only after you approve this plan.
6. **DioramaTurn's track is composed through this pipeline but wired in its own plan's stage 3** (feel/audio step), not here. Its `.wav` can be rendered and committed once, or with that stage — your call at the pilot review; the default is with its stage.

## Layout

```
Tools/audio/
  render_music.sh              the wrapper (bash)
  midi.py                      stdlib MIDI writer, shared
  postprocess_music.py         trim, normalize, loudness, loop seam, checks
  soundfonts/GeneralUser-GS.sf2  (LFS, 31 MB, after approval)
  <Name>/music/<track>.py      one script per track
  <Name>/make_sounds.py        unchanged (SFX)
```

Track scripts write MIDI to a temp path, the wrapper renders to a temp wav, post-processing writes the final `Assets/Prototypes/<Name>/Audio/music.wav`. No MIDI file is committed. The wrapper contract: `Tools/audio/render_music.sh <Prototype> <track>`, exit 0 only when every step and check passed.

## Files added or changed (complete list)

**Added**
- `docs/research/fluidsynth-music.md` (written)
- `docs/prototypes/fluidsynth-music.md` (written)
- `docs/prototypes/fluidsynth-music-plan.md` (this file)
- `docs/audio-credits.md` — SoundFont name, author, source URL, license text, date read
- `Tools/audio/render_music.sh`
- `Tools/audio/midi.py`
- `Tools/audio/postprocess_music.py`
- `Tools/audio/soundfonts/GeneralUser-GS.sf2` + `.meta` (LFS — decision 1)
- `Tools/audio/MedievalChess/music/torchlight.py` (pilot)
- `Tools/audio/NeonPong/music/neon_drift.py`
- `Tools/audio/DioramaTurn/music/diorama_loop.py`
- `Assets/Shared/Editor/MusicClipImporter.cs` (postprocessor, streaming load for music paths — three prototypes use it, so it lives in Shared like `PixelArtTextureImporter`)
- `Assets/Shared/Tests/EditMode/MusicClipImporterTests.cs`
- `Assets/Prototypes/MedievalChess/Scripts/Feel/MusicPlayer.cs` — one looping AudioSource fed by the bank
- `Assets/Prototypes/NeonPong/Scripts/NeonPongAudio.cs` gains a music source and clip (edit, listed below)
- Rendered, LFS: `Assets/Prototypes/MedievalChess/Audio/music.wav`, `Assets/Prototypes/NeonPong/Audio/music.wav`, `Assets/Prototypes/DioramaTurn/Audio/music.wav`
- `.claude/skills/music-composition/SKILL.md`

**Changed**
- `.gitattributes` — one rule: `*.sf2 filter=lfs diff=lfs merge=lfs -text`
- `AGENTS.md` — new section "Music (FluidSynth)": scripts only, never hand-edited audio; the render command; required env vars (`SOUNDFONT_PATH`, PATH `fluidsynth`); one-track-at-a-time review; the person does the listening
- `README.md` — FluidSynth under "What you need"; `Tools/audio/` music folders under "Repository layout"
- `docs/prototypes/neon-pong.md` and `docs/prototypes/medieval-chess.md` — audio sections gain the music loop (decision 5)
- `Assets/Prototypes/MedievalChess/Editor/GameSceneBuilder.cs`, `Assets/Prototypes/NeonPong/Editor/NeonPongSceneBuilder.cs` — build the music source (loop on, clip assigned, volume from settings)
- `Assets/Prototypes/MedievalChess/Settings/...` and `Assets/Prototypes/NeonPong/...` settings classes/assets — a `music` volume field (0.35), clamped, with tests

**Untouched by design:** all SFX scripts and wavs, `Assets/Shared/Feel/`, `ProjectSettings/`, packages, the other prototypes' code.

## Tasks

### Stage 1: the pipeline (no game code)
- [ ] `.gitattributes` rule, then download GeneralUser GS into `Tools/audio/soundfonts/` and commit it with `docs/audio-credits.md` (after approval).
- [ ] `midi.py` (writer) with a self-test in the script.
- [ ] `postprocess_music.py`: trim trailing silence below a threshold but keep the reverb tail; normalize peak to 0.8; measure and report RMS against −18 dBFS ±2 dB; for loops, wrap the tail across the seam and check the first/last samples and a click measure; assert duration bounds (10–120 s) and non-empty output. Stdlib only.
- [ ] `render_music.sh`: env checks (`fluidsynth` on PATH, `SOUNDFONT_PATH` or committed fallback), run the track script, render `fluidsynth -ni -r 44100 -g <gain> -F <temp.wav> <sf2> <temp.mid>`, post-process into the prototype's `Audio/` folder, print every check, exit non-zero loudly on any failure. Pass `ASEPRITE_PATH`-style inline env only; never persist it.
- [ ] Deliberate-bug check on the wrapper: a track script that exits 1, a missing MIDI, an empty wav — each must exit non-zero with the reason.
- [ ] `MusicClipImporter.cs` + EditMode tests (streaming load type for music paths, defaults unchanged for SFX paths).
- [ ] `AGENTS.md` section, `README.md` updates, `.claude/skills/music-composition/SKILL.md` (portable frontmatter only), run `python3 Tools/agents/check_config.py`.
- [ ] `Tools/unity/check.sh all`. Commit: `feat(tools): add FluidSynth music pipeline`.

### Stage 2: the pilot track (Medieval Chess) — stop for your listen
- [ ] Write `Tools/audio/MedievalChess/music/torchlight.py`: slow lute/dulcimer over a low drone, ~60–90 s, seamless loop, named constants for tempo/key/instruments, stem-friendly structure (one function per layer).
- [ ] Render through the wrapper; read the checks; look at a spectrogram (outside the repository).
- [ ] Spec edit `docs/prototypes/medieval-chess.md` (audio section) per decision 5.
- [ ] Code: `music` field on the settings asset (clamped, tested), `MusicPlayer.cs`, builder wiring, EditMode tests (source loops, clip assigned, volume from settings; the whole scene still builds).
- [ ] `Tools/unity/check.sh all`. Commit: `feat(medieval-chess): add looping music track`.
- [ ] **Stop.** You listen in the Editor and say what to change; nothing else is composed until then.

### Stage 3: the remaining track for the shipped games (Neon Pong)
- [ ] `neon_drift.py`: bright minimal synth loop, ~60–90 s, seamless loop, using the pilot's lessons.
- [ ] Render, checks, spectrogram; spec edit `neon-pong.md`; `NeonPongAudio` music field + builder wiring + tests; `check.sh all`. Commit: `feat(neon-pong): add looping music track`.
- [ ] **Stop** for your listen.

### Stage 4: DioramaTurn's track (pipeline only)
- [ ] `diorama_loop.py`: marimba and music-box pentatonic loop, 16–24 s, per its spec. Render, checks, spectrogram.
- [ ] Wiring, ducking and the Audio component happen in the DioramaTurn plan's own stage 3 — nothing here but the script and the rendered wav (or defer both to that stage if you prefer at the pilot review). Commit: `feat(diorama-turn): add music loop track`.

## Verification
| Criterion (from the spec) | How it is checked |
|---|---|
| Wrapper renders end to end, fails on a broken script, missing MIDI, empty wav | run in stage 1, one deliberate-bug pass |
| Duration, peak, RMS, clip, seam checks per track | printed by `postprocess_music.py` on every render |
| Music imports as streaming, SFX import unchanged | `MusicClipImporterTests` (EditMode) |
| Each game's scene has one looping music source at the settings volume | per-game EditMode builder tests |
| Nothing else regressed | `Tools/unity/check.sh all` before and after (currently 808 EditMode, 69 PlayMode passing) |
| Sounds good, fits the game, seam inaudible | you, listening — one track at a time |

## Honest limits
- I can check length, silence, clipping, loudness, the loop seam and a spectrogram. I cannot hear the music or judge whether it fits. Every stage ends with "you listen".
- A GM SoundFont is a GM SoundFont: a lute will sound like a sampled lute, and a neon synth pad may need layering to feel alive. If a track cannot sound convincing with GeneralUser GS I will say so instead of pretending.

## Risks
- **The track feels wrong on the first listen.** Expected; that is why there is a pilot and a stop after it.
- **Loop seam clicks.** The tail-wrap in post-processing handles the math; if it still clicks, the fallback is composing tracks that end on a rest with a matched start.
- **31 MB in LFS.** Small by LFS standards; if you object, the fallback is a smaller or compressed SF3 (FluidSynth supports it), with the same license question.
- **Stereo music doubles the wav size.** ~10 MB per minute; under the 15 MB budget for all three tracks.
