# Audio

Coding agents can't listen. They can make sounds from a script, check the files, and hand the judging to a person. This part is untested in this repository until the first game uses it.

## Making simple sounds with a script
- **Approach:** a Python script (standard library only: `wave`, `struct`, `math`, `random`) writes short `.wav` files: a sine, square or triangle tone with an envelope (fast attack, short decay), optional pitch slide, optional noise burst for impacts.
- **Where:** `Tools/audio/<Name>/make_sounds.py`, writing into `Assets/Prototypes/<Name>/Audio/`. Run it with `python3 -I`. `.wav` files are tracked by Git LFS.
- **Format:** 16-bit mono, 44.1 kHz. Keep each file under a second or two.
- **Per feel-table row:** one sound. Vary pitch by event (higher for a point, lower for a wall hit). Add a second version a few semitones up to randomise.
- **Checks an agent can do:** the file exists, the length and sample rate are right, the peak level is below clipping, it isn't silent, and the envelope ends near zero (no click). Unity imports it as an AudioClip.
- **What it can't judge:** whether it sounds good or fits the mood, and the loudness balance. Hand that to the person.

## In Unity
- **[unverified]** Playing clips from code with `AudioSource.PlayOneShot`, with volume and pitch from the settings asset. Confirm by a PlayMode test that the clip is assigned and the source is triggered. Judging the sound itself needs a person.
- Keep one master volume setting.

## Licensing
All sounds in this repository are made by the scripts, or are clearly licensed and recorded in the spec's sources. Never copy sounds from another game.
