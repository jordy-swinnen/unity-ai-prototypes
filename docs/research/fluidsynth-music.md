# Research: FluidSynth music pipeline

For the spec `docs/prototypes/fluidsynth-music.md`. Facts checked while proposing it, with confidence tags.

## The state of audio today [read]

Every `.wav` in the repository is a short one-shot sound effect, synthesized from nothing (tones, filtered noise, partials) by `Tools/audio/<Name>/make_sounds.py`, stdlib-only Python. Neon Pong: 6 files, longest 0.90 s. Medieval Chess: 22 files, longest 1.90 s. No file loops; every AudioSource is a one-shot. **There is no music in either shipped prototype.** DioramaTurn's approved spec plans one 16–24 s music loop (stage 3). No `AudioImporter` code exists anywhere in `Assets/`, so music import settings need a new postprocessor. `*.wav` is already Git LFS-tracked; `*.sf2` is not (needs a rule).

## FluidSynth 2.6.2, installed locally [read]

Installed by the user with Homebrew (`/opt/homebrew/bin/fluidsynth`, runtime 2.6.2, LGPL). Checked on this machine:

- `-F, --fast-render=<file>` exists, with libsndfile support (writes `.wav` by extension).
- `--help` lists `-g/--gain`, `-r/--sample-rate`, `-C/--chorus`, `--reverb` options; `-ni` runs without a shell.
- Supports SF2, SF3 (compressed) and DLS.
- **Fast render does not cut the tail.** Spike run 2026-11-11: a 1.0 s MIDI (one 0.5 s piano note) rendered to a **3.535 s** wav. FluidSynth keeps rendering after the last MIDI event until the synth output falls silent, so reverb tails survive without padding the MIDI. The output faded to zero on its own (last sample 2/32768).
- Output from the spike: **stereo** 16-bit 44.1 kHz wav. The SFX convention is mono; music in stereo is a deliberate deviation, proposed in the spec.
- At `-g 0.5` with a GM soundfont, a single piano note peaked at 0.075. Post-processing normalization is required for any sane target.

## SoundFont candidates

### GeneralUser GS 2.0.3 — S. Christian Collins [read]

- Author page: https://www.schristiancollins.com/generaluser; official GitHub mirror: https://github.com/mrbumpy409/GeneralUser-GS (the `.sf2` is in the repo root, 31 MB, downloaded and rendered with during the spike).
- License text read in full from the author's own repo (`documentation/LICENSE.txt`, fetched 2026-11-11): "You may use GeneralUser GS without restriction for your own music creation, private or commercial." Samples: "all of which allow full use in music production, including the ability to make profit from musical recordings created with GeneralUser GS." The author notes he cannot be 100% sure of every sample's origin (the bank predates 2000) and asks to be told of any restricted sample; he knows of no complaint since 2000.
- 261 presets, 13 drum kits, Roland GS + GM compatible, "very low memory footprint (30.7 MB)". Well regarded for mallets and tuned percussion, which DioramaTurn needs (marimba, music box).
- Small enough that Git LFS storage is painless.

### FluidR3 GM — Frank Wen [secondary]

- Debian packages it as `fluid-soundfont-gm`; the Debian package page for the mono variant states "As it comes under the MIT licence". The license text itself could not be fetched (sources.debian.org and salsa.debian.org return a bot-block page), so this is second-hand.
- ~148 MB. Good all-round GM bank, the long-time MuseScore default.

### Musyng Kite — K. Urata and others [secondary]

- License reported as Creative Commons Attribution-ShareAlike 3.0 (via the midi-js-soundfonts README: "Released under Creative Commons Attribution Share-Alike 3.0 license"); attribution required, share-alike on distribution of the font itself. A KVRAudio forum thread by people involved with the bank also describes parts of it as edited from "different free sources" with unclear commercial intent.
- ~990 MB. Attribution plus size plus sample-origin murkiness make it a poor fit for a repository that plans to sell a game.

## MIDI writing without dependencies [read]

The existing audio scripts are stdlib-only Python run with `python3 -I`. A format-0 MIDI writer (delta times as VLQ, tempo meta, program change, note on/off) is ~60 lines of stdlib Python — written and used for the spike above. `mido` would be more comfortable but adds a dependency, so the spec proposes the stdlib writer. The proposal to skip `mido` is in the plan's decisions.

## Loudness [secondary]

The existing scripts normalize every SFX to a peak of 0.8 and check "peak < 0.95" and "not silent"; there is no LUFS infrastructure. True LUFS (ITU-R BS.1770 K-weighting) is implementable in stdlib numpy-less Python but is overkill for looped game music under short SFX. The spec proposes peak-normalized music at 0.8 with an RMS-level check, and per-game `music` volume in the settings asset (DioramaTurn's spec already uses `audio.music` 0.35). Game-music loudness targets from general practice (music mixed well under SFX, roughly 10 dB below dialogue/SFX) are common knowledge, not a checked source.

## Sources

- https://raw.githubusercontent.com/mrbumpy409/GeneralUser-GS/main/documentation/LICENSE.txt `[read]`
- https://github.com/mrbumpy409/GeneralUser-GS and https://www.schristiancollins.com/generaluser `[read]`
- https://packages.debian.org/sid/fluidr3mono-gm-soundfont (MIT claim for FluidR3, mono variant) `[secondary]`
- https://paulrosen.github.io/midi-js-soundfonts/ (Musyng Kite, CC BY-SA 3.0) `[secondary]`
- `fluidsynth --help`, `--version` and the fast-render spike, run locally on FluidSynth 2.6.2 `[read]`
