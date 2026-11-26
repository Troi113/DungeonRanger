"""
Original orchestral-style music for DungeonRanger, synthesized from code (no samples).

Writes 16-bit mono WAV files to Assets/Audio/Resources/Music/:
  menu_theme.wav  (D minor, calm strings, harp arpeggios, flute-like melody; loops)
  dungeon.wav     (A minor, dark cello drone, pads, sparse pizzicato, distant timpani; loops)
  boss_goo.wav    (E minor, driving low-string ostinato, timpani, tense pads; loops)
  victory.wav     (C major, brass-like fanfare over strings; plays once)

Voices: additive "string section" pads, Karplus-Strong plucks (harp / pizzicato),
cello-like bass, timpani hits, and a small Schroeder reverb. Loops are seamless: each
track's release/reverb tail is folded back onto its start.

Run: python3 Tools/generate_music.py   (pure Python, takes a few minutes)
"""
import math
import os
import random
import struct
import wave

RATE = 22050
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio", "Resources", "Music")

NOTE = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6,
        "G": 7, "G#": 8, "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def freq(name):
    """'A4' -> 440 Hz."""
    pitch, octave = name[:-1], int(name[-1])
    midi = 12 * (octave + 1) + NOTE[pitch]
    return 440.0 * 2 ** ((midi - 69) / 12)


def wavetable(harmonics, size=2048):
    table = [0.0] * size
    for n, amp in harmonics:
        for i in range(size):
            table[i] += amp * math.sin(2 * math.pi * n * i / size)
    peak = max(abs(v) for v in table) or 1.0
    return [v / peak for v in table]


# Timbres: string section (rich, sawtooth-like), cello (darker), flute (nearly pure), brass (bright).
STRINGS = wavetable([(n, 1.0 / n) for n in range(1, 10)])
CELLO = wavetable([(n, 1.0 / (n ** 1.3)) for n in range(1, 8)])
FLUTE = wavetable([(1, 1.0), (2, 0.18), (3, 0.06)])
BRASS = wavetable([(n, 1.0 / (n ** 0.8)) for n in range(1, 12)])


class Track:
    def __init__(self, seconds, tail=3.0):
        self.length = int(seconds * RATE)
        self.buffer = [0.0] * (self.length + int(tail * RATE))

    def add(self, start, samples, gain=1.0):
        begin = int(start * RATE)
        buf = self.buffer
        end = min(len(buf), begin + len(samples))
        for i in range(begin, end):
            buf[i] += samples[i - begin] * gain


def tone(table, hz, seconds, attack, release, vibrato=0.0, detune=(0.0,)):
    """Sustained tone from a wavetable; several slightly detuned copies make an ensemble."""
    count = int((seconds + release) * RATE)
    out = [0.0] * count
    size = len(table)
    sustain_end = int(seconds * RATE)
    attack_n = max(1, int(attack * RATE))
    release_n = max(1, int(release * RATE))
    for cents in detune:
        step = hz * 2 ** (cents / 1200) * size / RATE
        phase = random.random() * size
        vib_phase = random.random() * 6.28
        for i in range(count):
            if vibrato:
                phase += step * (1 + vibrato * math.sin(vib_phase + i * 2 * math.pi * 5.2 / RATE))
            else:
                phase += step
            out[i] += table[int(phase) % size]
    norm = 1.0 / len(detune)
    for i in range(count):
        if i < attack_n:
            env = i / attack_n
        elif i < sustain_end:
            env = 1.0
        else:
            env = max(0.0, 1.0 - (i - sustain_end) / release_n)
        out[i] *= env * norm
    return out


def pluck(hz, seconds, brightness=0.5):
    """Karplus-Strong plucked string (harp / pizzicato)."""
    period = max(2, int(RATE / hz))
    ring = [random.uniform(-1, 1) for _ in range(period)]
    count = int(seconds * RATE)
    out = [0.0] * count
    decay = 0.996 - (1 - brightness) * 0.004
    idx = 0
    for i in range(count):
        current = ring[idx]
        nxt = ring[(idx + 1) % period]
        ring[idx] = decay * 0.5 * (current + nxt)
        out[i] = current
        idx = (idx + 1) % period
    return out


def timpani(hz, seconds=1.6):
    count = int(seconds * RATE)
    out = [0.0] * count
    phase = 0.0
    for i in range(count):
        t = i / RATE
        f = hz * (1 + 0.25 * math.exp(-t * 18))
        phase += 2 * math.pi * f / RATE
        body = math.sin(phase) + 0.35 * math.sin(phase * 1.5)
        noise = random.uniform(-1, 1) * math.exp(-t * 40) * 0.5
        out[i] = (body * math.exp(-t * 3.2) + noise)
    return out


def reverb(buf, mix=0.28):
    """Small Schroeder reverb: parallel combs plus one allpass."""
    combs = [(int(RATE * d), g) for d, g in ((0.0297, 0.80), (0.0371, 0.78), (0.0411, 0.76), (0.0437, 0.74))]
    wet = [0.0] * len(buf)
    for delay, gain in combs:
        line = [0.0] * delay
        idx = 0
        for i, x in enumerate(buf):
            y = line[idx]
            line[idx] = x + y * gain
            wet[i] += y
            idx = (idx + 1) % delay
    delay = int(RATE * 0.005)
    line = [0.0] * delay
    idx = 0
    for i in range(len(wet)):
        x = wet[i] * 0.25
        y = line[idx]
        line[idx] = x + y * 0.5
        wet[i] = y - 0.5 * x
        idx = (idx + 1) % delay
    return [d * (1 - mix) + w * mix for d, w in zip(buf, wet)]


def finish(track, name, loop=True, level=0.8):
    buf = reverb(track.buffer)
    if loop:
        # Fold the tail onto the start so the loop point is seamless.
        tail = buf[track.length:]
        buf = buf[:track.length]
        for i, v in enumerate(tail):
            buf[i % track.length] += v
    else:
        fade = int(1.5 * RATE)
        for i in range(fade):
            buf[-1 - i] *= i / fade
    peak = max(abs(v) for v in buf) or 1.0
    scale = level / peak
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, v * scale)) * 32767)) for v in buf))
    print(f"wrote {path} ({len(buf) / RATE:.1f}s)")


def chord_pad(track, start, seconds, notes, gain, table=STRINGS):
    for n in notes:
        track.add(start, tone(table, freq(n), seconds, attack=1.2, release=1.5, vibrato=0.004,
                              detune=(-7, 0, 6)), gain)


def menu_theme():
    random.seed(11)
    beat = 60 / 70
    bar = beat * 4
    progression = [("D3", "F3", "A3", "D4"), ("Bb2", "D3", "F3", "Bb3"), ("F2", "A3", "C4", "F4"), ("C3", "E3", "G3", "C4")] * 4
    track = Track(len(progression) * bar)
    melody = ["A4", "F4", "G4", "A4", "D5", "C5", "A4", "G4", "F4", "E4", "D4", "E4", "F4", "A4", "G4", "E4"]
    for i, chord in enumerate(progression):
        t = i * bar
        chord_pad(track, t, bar, chord, 0.12)
        track.add(t, tone(CELLO, freq(chord[0]) / 2, bar, 0.4, 1.0, detune=(0,)), 0.22)
        # Harp arpeggio, eighth notes.
        arp = list(chord) + [chord[2], chord[1]]
        for k in range(8):
            note = arp[k % len(arp)]
            hz = freq(note) * (2 if k % 4 == 3 else 1)
            track.add(t + k * beat / 2, pluck(hz, 1.6, 0.7), 0.16)
        if i >= 4:
            track.add(t, tone(FLUTE, freq(melody[i % len(melody)]), bar * 0.9, 0.25, 0.6, vibrato=0.006), 0.2)
    finish(track, "menu_theme.wav")


def dungeon():
    random.seed(23)
    beat = 60 / 60
    bar = beat * 4
    progression = [("A2", "E3", "A3", "C4"), ("F2", "C3", "A3", "C4"), ("D3", "A3", "D4", "F4"), ("E2", "B2", "G#3", "B3")] * 4
    track = Track(len(progression) * bar)
    scale = ["A3", "B3", "C4", "D4", "E4", "F4", "G#4", "A4"]
    for i, chord in enumerate(progression):
        t = i * bar
        chord_pad(track, t, bar, chord[1:], 0.08)
        track.add(t, tone(CELLO, freq("A1") if i % 4 != 3 else freq("E1"), bar, 1.0, 1.2, vibrato=0.002, detune=(-4, 4)), 0.3)
        for k in range(4):
            if random.random() < 0.45:
                track.add(t + k * beat + random.choice((0, beat / 2)), pluck(freq(random.choice(scale)), 1.2, 0.35), 0.18)
        if i % 4 == 0:
            track.add(t, timpani(freq("A1")), 0.25)
    finish(track, "dungeon.wav")


def boss_goo():
    random.seed(37)
    beat = 60 / 132
    bar = beat * 4
    progression = [("E3", "G3", "B3"), ("C3", "E3", "G3"), ("D3", "F#3", "A3"), ("B2", "D#3", "F#3")] * 4
    track = Track(len(progression) * bar)
    ostinato = ["E2", "E2", "G2", "E2", "A2", "E2", "B2", "E2"]
    roots = {"E3": 0, "C3": -4, "D3": -2, "B2": -5}
    for i, chord in enumerate(progression):
        t = i * bar
        chord_pad(track, t, bar, chord, 0.09)
        shift = 2 ** (roots[chord[0]] / 12)
        for k, note in enumerate(ostinato):
            track.add(t + k * beat / 2, tone(CELLO, freq(note) * shift, beat * 0.35, 0.01, 0.08, detune=(-5, 5)), 0.32)
        track.add(t, timpani(freq("E2") * shift), 0.4)
        track.add(t + 2 * beat, timpani(freq("B1") * shift), 0.3)
        if i % 2 == 1:
            for k in range(4):
                track.add(t + 3 * beat + k * beat / 4, timpani(freq("E2") * shift, 0.4), 0.12)
        if i >= 8:
            track.add(t, tone(STRINGS, freq(chord[2]) * 2, bar * 0.95, 0.05, 0.3, vibrato=0.01, detune=(-6, 0, 6)), 0.12)
    finish(track, "boss_goo.wav")


def victory():
    random.seed(5)
    beat = 60 / 100
    bar = beat * 4
    track = Track(8 * bar + 1, tail=4.0)
    chords = [("C3", "E3", "G3", "C4"), ("F3", "A3", "C4", "F4"), ("G3", "B3", "D4", "G4"), ("C3", "E3", "G3", "C4")] * 2
    fanfare = [("C4", 0, 1), ("E4", 1, 1), ("G4", 2, 1), ("C5", 3, 1), ("A4", 4, 2), ("F4", 6, 2),
               ("G4", 8, 1), ("B4", 9, 1), ("D5", 10, 2), ("C5", 12, 4),
               ("E5", 16, 1), ("D5", 17, 1), ("C5", 18, 2), ("A4", 20, 2), ("B4", 22, 2), ("G4", 24, 2), ("C5", 26, 6)]
    for i, chord in enumerate(chords):
        chord_pad(track, i * bar, bar, chord, 0.1)
        track.add(i * bar, tone(CELLO, freq(chord[0]) / 2, bar, 0.2, 0.8), 0.22)
    for note, start, length in fanfare:
        track.add(start * beat, tone(BRASS, freq(note), length * beat * 0.95, 0.04, 0.35, vibrato=0.004, detune=(-4, 4)), 0.17)
    for k in range(8):
        track.add(7 * bar + k * beat / 8, timpani(freq("C2"), 0.6), 0.12)
    track.add(8 * bar, timpani(freq("C2"), 2.5), 0.4)
    finish(track, "victory.wav", loop=False)


if __name__ == "__main__":
    menu_theme()
    dungeon()
    boss_goo()
    victory()
