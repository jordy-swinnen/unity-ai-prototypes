"""A minimal, dependency-free MIDI writer for the music track scripts.

Writes a format-0 MIDI file: one track, tempo and time signature, program changes, notes and a few
controllers. Enough for FluidSynth, and readable in any DAW. Everything is in beats; the writer turns
beats into ticks.

Used by Tools/audio/<Name>/music/<track>.py:

    from midi import Song
    song = Song(bpm=84, beats_per_bar=4)
    song.program(0, 12)                 # Marimba
    song.note(0, 60, start=0, length=1, velocity=60)
    song.write("out.mid")
"""
import struct


class Song:
    def __init__(self, bpm=90, beats_per_bar=4, ppq=480, name="Track"):
        self.bpm = bpm
        self.beats_per_bar = beats_per_bar
        self.ppq = ppq
        self.name = name
        self.events = []  # (tick, order, bytes)

    def _tick(self, beats):
        return int(round(beats * self.ppq))

    def _add(self, tick, order, data):
        self.events.append((tick, order, bytes(data)))

    def program(self, channel, program, at=0.0):
        self._add(self._tick(at), 0, [0xC0 | (channel & 0x0F), program & 0x7F])

    def control(self, channel, controller, value, at=0.0):
        self._add(self._tick(at), 1, [0xB0 | (channel & 0x0F), controller & 0x7F, value & 0x7F])

    def note(self, channel, pitch, start, length, velocity=64):
        on = self._tick(start)
        off = self._tick(start + max(0.05, length))
        self._add(on, 2, [0x90 | (channel & 0x0F), pitch & 0x7F, velocity & 0x7F])
        self._add(off, 1, [0x80 | (channel & 0x0F), pitch & 0x7F, 0])

    def _tempo_events(self):
        micros = int(round(60_000_000 / self.bpm))
        return [
            (0, 0, bytes([0xFF, 0x51, 0x03]) + micros.to_bytes(3, "big")),
            (0, 0, bytes([0xFF, 0x58, 0x04, self.beats_per_bar, 2, 24, 8])),  # time signature
            (0, 0, bytes([0xFF, 0x03, len(self.name)]) + self.name.encode("ascii", "replace")),  # track name
        ]

    def write(self, path):
        events = self._tempo_events() + sorted(self.events, key=lambda e: (e[0], e[1]))
        last_tick = 0
        track = bytearray()
        for tick, _, data in events:
            delta = tick - last_tick
            last_tick = tick
            track += _vlq(delta)
            track += data
        track += _vlq(0) + bytes([0xFF, 0x2F, 0x00])  # end of track
        midi = b"MThd" + struct.pack(">IHHH", 6, 0, 1, self.ppq)
        midi += b"MTrk" + struct.pack(">I", len(track)) + bytes(track)
        with open(path, "wb") as f:
            f.write(midi)


def _vlq(value):
    """A variable-length quantity, as MIDI delta times use."""
    value = max(0, int(value))
    out = [value & 0x7F]
    value >>= 7
    while value:
        out.append((value & 0x7F) | 0x80)
        value >>= 7
    return bytes(reversed(out))
