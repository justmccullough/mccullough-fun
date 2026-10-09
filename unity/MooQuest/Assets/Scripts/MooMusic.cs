using System;
using System.Collections.Generic;
using UnityEngine;

// A tiny synth "band" that turns written-out tunes into looping music at runtime, so no audio files are needed.
// Each song is a few sections (verse, chorus, breakdown...) played in an order, with its own lead instrument,
// chords, bass style, drums, and sparkly arpeggios, so the loops are long and change as they go.
public static class MooMusic
{
    public enum Track { Barnyard, Meadow, Marsh, Woods, Boss, Sunset, Party }
    public enum Voice { Triangle, Square, Flute, Bell, Pluck, Accordion, Bass, Pad }
    public enum BassStyle { Oompah, Walk, Bounce, Drone }

    // Melody: one token per eighth note, 8 per bar, bars split by '|'. "C5" / "F#4" / "Bb5" play a note,
    // "-" holds the previous note, "." is a rest. Chords: one per bar, like "C", "Am", "G7", "Bb".
    public sealed class Section
    {
        public string Melody, Chords;
        public Voice Lead = Voice.Triangle;
        public bool Drums = true, Arp;
    }

    public sealed class Tune
    {
        public string Name, Order, Kick = "", Snare = "", Hat = "";
        public float Step, HatVolume = 1;
        public BassStyle Bass;
        public Section[] Parts;
    }

    private const int Rate = 22050;
    private static readonly Dictionary<Track, AudioClip> cache = new Dictionary<Track, AudioClip>();

    public static AudioClip Get(Track track)
    {
        if (cache.TryGetValue(track, out AudioClip clip) && clip != null) return clip;
        clip = Render(Tunes[(int)track]);
        cache[track] = clip;
        return clip;
    }

    // ---------- The songs ----------

    public static readonly Tune[] Tunes =
    {
        new Tune
        {
            Name = "Moo-ville Hoedown", Step = .2f, Order = "ABACBDB", Bass = BassStyle.Oompah,
            Kick = "x...x...", Snare = "..x...x.", Hat = ".x.x.x.x",
            Parts = new[]
            {
                new Section { Chords = "C F G C", Lead = Voice.Triangle, Melody =
                    "C5 . E5 G5 C6 - G5 E5 | F5 - A5 - G5 F5 E5 D5 | D5 . G5 . B5 A5 G5 F5 | E5 - C5 - C5 . . ." },
                new Section { Chords = "C Am G C", Lead = Voice.Triangle, Melody =
                    "C5 . E5 G5 C6 - D6 C6 | A5 - E5 - A5 G5 F5 E5 | D5 E5 F5 G5 A5 B5 C6 D6 | C6 - G5 - C5 - . ." },
                new Section { Chords = "F C Dm G", Lead = Voice.Square, Melody =
                    "A5 - A5 G5 F5 - C5 - | E5 - G5 - C6 - . . | F5 - F5 E5 D5 - A4 - | B4 - D5 - G5 - . ." },
                new Section { Chords = "Am F G C", Lead = Voice.Flute, Drums = false, Arp = true, Melody =
                    "E5 - - - C5 - - - | F5 - - - A5 - - - | G5 - - - B5 - D6 - | C6 - - - - - . ." },
            },
        },
        new Tune
        {
            Name = "Clover Meadow Breeze", Step = .22f, Order = "ABCADBC", Bass = BassStyle.Walk,
            Kick = "x.......", Hat = "..x...x.", HatVolume = .6f,
            Parts = new[]
            {
                new Section { Chords = "G C D G", Lead = Voice.Flute, Melody =
                    "D5 - G5 - B5 - A5 G5 | E5 - G5 - C6 - B5 A5 | F#5 - A5 - D6 - C6 A5 | B5 - - - G5 - . ." },
                new Section { Chords = "G Em C D", Lead = Voice.Flute, Melody =
                    "D5 - G5 - B5 - D6 - | E6 - D6 B5 G5 - E5 - | C6 - B5 A5 G5 - E5 - | D5 - - - - - . ." },
                new Section { Chords = "Em C G D", Lead = Voice.Bell, Arp = true, Melody =
                    "B5 . B5 C6 D6 - B5 - | C6 . C6 B5 A5 - G5 - | B5 . B5 A5 G5 - D5 - | F#5 - A5 - D6 - . ." },
                new Section { Chords = "C D G G", Lead = Voice.Pluck, Drums = false, Arp = true, Melody =
                    "E5 G5 C6 G5 E5 G5 C6 E6 | F#5 A5 D6 A5 F#5 A5 D6 F#6 | G6 - D6 - B5 - G5 - | G5 - - - - - . ." },
            },
        },
        new Tune
        {
            Name = "Mudpuddle Stomp", Step = .24f, Order = "ABCDABC", Bass = BassStyle.Bounce,
            Kick = "x..x..x.", Snare = "....x...", Hat = "..x...x.",
            Parts = new[]
            {
                new Section { Chords = "Dm G Dm A", Lead = Voice.Pluck, Melody =
                    "D5 . F5 . A5 G5 F5 . | G5 - B4 . D5 . G5 . | F5 E5 D5 . A4 . D5 . | E5 - C#5 - A4 - . ." },
                new Section { Chords = "Dm G C Dm", Lead = Voice.Square, Melody =
                    "D5 . F5 . A5 . C6 B5 | B5 - G5 - D5 . E5 F5 | G5 - E5 - C5 . D5 E5 | D5 - - - . . . ." },
                new Section { Chords = "F C Gm A", Lead = Voice.Triangle, Arp = true, Melody =
                    "C6 - A5 - F5 - A5 - | G5 - E5 - C5 - E5 - | D5 F5 G5 A5 Bb5 - A5 G5 | A5 - E5 - C#5 - . ." },
                new Section { Chords = "Dm Dm G A", Lead = Voice.Bell, Drums = false, Melody =
                    "D5 . . D5 . . F5 . | A4 . . A4 . . D5 . | G4 . . B4 . . D5 . | E5 . C#5 . A4 . . ." },
            },
        },
        new Tune
        {
            Name = "Moonberry Lullaby", Step = .26f, Order = "ABCDBC", Bass = BassStyle.Drone,
            Hat = "..x...x.", HatVolume = .45f,
            Parts = new[]
            {
                new Section { Chords = "Am F G Em", Lead = Voice.Bell, Arp = true, Melody =
                    "E5 - A5 - C6 - B5 A5 | C6 - - - A5 - F5 - | D5 - G5 - B5 - A5 G5 | B4 - - - E5 - . ." },
                new Section { Chords = "Am F Dm E", Lead = Voice.Bell, Arp = true, Melody =
                    "A5 - C6 - E6 - D6 C6 | A5 - - - F5 - A5 - | D6 - C6 - A5 - F5 - | G#5 - - - E5 - . ." },
                new Section { Chords = "C G Am E", Lead = Voice.Flute, Arp = true, Melody =
                    "G5 - E5 - C5 - E5 G5 | D6 - B5 - G5 - - - | C6 - A5 - E5 - A5 C6 | B5 - - - G#5 - - -" },
                new Section { Chords = "F G Am E", Lead = Voice.Flute, Drums = false, Melody =
                    "A5 - - - - - G5 F5 | G5 - - - - - . . | E5 - A5 - B5 - C6 - | B5 - - - - - . ." },
            },
        },
        new Tune
        {
            Name = "Goat Polka", Step = .15f, Order = "ABCADCB", Bass = BassStyle.Oompah,
            Kick = "x...x...", Snare = "..x...x.", Hat = "xxxxxxxx", HatVolume = .5f,
            Parts = new[]
            {
                new Section { Chords = "Dm A Dm A", Lead = Voice.Accordion, Melody =
                    "D5 F5 A5 F5 D5 F5 A5 D6 | C#6 - A5 - E5 - A5 - | D6 C6 A5 F5 D5 E5 F5 G5 | A5 - E5 - A4 - . ." },
                new Section { Chords = "Dm Gm A Dm", Lead = Voice.Accordion, Melody =
                    "D5 F5 A5 F5 D5 F5 A5 D6 | Bb5 - G5 - D5 - G5 - | A5 G5 F5 E5 C#5 E5 A5 G5 | F5 - D5 - D5 . . ." },
                new Section { Chords = "F C Bb A", Lead = Voice.Square, Melody =
                    "F5 . F5 . A5 . C6 . | E5 . E5 . G5 . C6 . | D6 - Bb5 - F5 - D5 - | C#6 - E6 - A5 - . ." },
                new Section { Chords = "Dm Gm Dm A", Lead = Voice.Pluck, Arp = true, Melody =
                    "A5 A5 . A5 A5 . G5 F5 | G5 G5 . G5 G5 . F5 E5 | F5 F5 . F5 F5 . E5 D5 | C#5 - E5 - A5 - . ." },
            },
        },
        new Tune
        {
            Name = "Hilltop Sunset", Step = .25f, Order = "ABAC", Bass = BassStyle.Walk,
            Kick = "x.......", Hat = "....x...", HatVolume = .5f,
            Parts = new[]
            {
                new Section { Chords = "F Bb C F", Lead = Voice.Flute, Melody =
                    "C5 - F5 - A5 - G5 F5 | D5 - F5 - Bb5 - A5 G5 | E5 - G5 - C6 - Bb5 G5 | A5 - - - F5 - . ." },
                new Section { Chords = "Dm Bb F C", Lead = Voice.Bell, Arp = true, Melody =
                    "D6 - C6 A5 F5 - A5 - | Bb5 - A5 F5 D5 - F5 - | C6 - A5 - F5 - A5 C6 | G5 - - - E5 - . ." },
                new Section { Chords = "Bb C Am Dm Bb C F F", Lead = Voice.Pluck, Arp = true, Melody =
                    "F5 - D5 - Bb4 - D5 F5 | G5 - E5 - C5 - E5 G5 | A5 - C6 - E6 - C6 A5 | F5 - A5 - D6 - - - | " +
                    "D6 - Bb5 - F5 - Bb5 D6 | E6 - C6 - G5 - C6 E6 | F6 - - - C6 - A5 - | F5 - - - - - . ." },
            },
        },
        new Tune
        {
            Name = "Barnyard Dance Party", Step = .16f, Order = "ABACBA", Bass = BassStyle.Walk,
            Kick = "x...x...", Snare = "..x...x.", Hat = ".x.x.x.x",
            Parts = new[]
            {
                new Section { Chords = "C F G C", Lead = Voice.Square, Melody =
                    "C5 E5 G5 C6 G5 E5 C5 E5 | F5 A5 C6 F6 C6 A5 F5 A5 | G5 B5 D6 G6 D6 B5 G5 B5 | C6 - G5 - C6 - . ." },
                new Section { Chords = "Am F C G", Lead = Voice.Triangle, Arp = true, Melody =
                    "E6 - D6 C6 A5 - C6 - | C6 - A5 - F5 - A5 - | G5 - E5 G5 C6 - E6 - | D6 - - - B5 - . ." },
                new Section { Chords = "C C F G", Lead = Voice.Pluck, Melody =
                    "C6 . C6 . G5 . C6 . | E6 . D6 . C6 . G5 . | A5 . A5 . F5 . A5 . | B5 - D6 - G6 - . ." },
            },
        },
    };

    // ---------- Rendering ----------

    public static AudioClip Render(Tune tune)
    {
        var parts = new List<(Section section, List<string> notes, string[] chords)>();
        foreach (Section s in tune.Parts) parts.Add((s, Tokens(tune.Name, s), s.Chords.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
        int steps = 0;
        foreach (char key in tune.Order) steps += parts[Part(tune, key)].notes.Count;
        int length = Mathf.CeilToInt(steps * tune.Step * Rate);
        var buffer = new float[length + Rate * 2];
        var noise = new System.Random(tune.Name.Length * 7919);
        float step = tune.Step;
        int at = 0;
        foreach (char key in tune.Order)
        {
            var (section, notes, chords) = parts[Part(tune, key)];
            for (int i = 0; i < notes.Count; i++)
            {
                float time = (at + i) * step;
                int midi = Midi(tune.Name, notes[i]);
                if (midi < 0) continue;
                int hold = 1;
                while (i + hold < notes.Count && notes[i + hold] == "-") hold++;
                Note(buffer, time, hold * step * (hold == 1 ? .8f : .95f), midi, section.Lead, .5f);
            }
            for (int bar = 0; bar < chords.Length; bar++)
            {
                int[] chord = Chord(tune.Name, chords[bar]);
                float t0 = (at + bar * 8) * step;
                Accompany(buffer, tune, section, chord, t0, step);
                if (section.Drums) Drums(buffer, tune, t0, step, noise);
            }
            at += notes.Count;
        }
        // Wrap the tails of the last notes back onto the start so the loop is seamless.
        for (int i = length; i < buffer.Length; i++) buffer[i - length] += buffer[i];
        float peak = .001f;
        for (int i = 0; i < length; i++) peak = Mathf.Max(peak, Mathf.Abs(buffer[i]));
        var samples = new float[length];
        for (int i = 0; i < length; i++) samples[i] = buffer[i] / peak * .32f;
        var clip = AudioClip.Create(tune.Name, length, 1, Rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static int Part(Tune tune, char key)
    {
        int index = key - 'A';
        if (index < 0 || index >= tune.Parts.Length) throw new Exception(tune.Name + ": unknown section '" + key + "'");
        return index;
    }

    private static List<string> Tokens(string song, Section section)
    {
        var tokens = new List<string>();
        string[] bars = section.Melody.Split('|');
        int chords = section.Chords.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        if (bars.Length != chords) throw new Exception(song + ": " + bars.Length + " melody bars but " + chords + " chords");
        for (int b = 0; b < bars.Length; b++)
        {
            string[] bar = bars[b].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (bar.Length != 8) throw new Exception(song + ": bar \"" + bars[b].Trim() + "\" has " + bar.Length + " steps, not 8");
            tokens.AddRange(bar);
        }
        return tokens;
    }

    private static int Semitone(string song, string text, ref int index)
    {
        int s = "C D EF G A B".IndexOf(char.ToUpperInvariant(text[0]));
        if (s < 0) throw new Exception(song + ": bad note \"" + text + "\"");
        index = 1;
        if (text.Length > 1 && text[1] == '#') { s++; index = 2; }
        else if (text.Length > 1 && text[1] == 'b') { s--; index = 2; }
        return s;
    }

    private static int Midi(string song, string token)
    {
        if (token == "." || token == "-") return -1;
        int index = 0;
        int s = Semitone(song, token, ref index);
        if (index >= token.Length || !char.IsDigit(token[index])) throw new Exception(song + ": note \"" + token + "\" needs an octave");
        return (token[index] - '0' + 1) * 12 + s;
    }

    // Returns root, third, fifth (and flat seventh for "7" chords) as MIDI notes around C3.
    private static int[] Chord(string song, string name)
    {
        int index = 0;
        int root = 48 + (Semitone(song, name, ref index) + 12) % 12;
        string rest = name.Substring(index);
        bool minor = rest.StartsWith("m");
        var tones = new List<int> { root, root + (minor ? 3 : 4), root + 7 };
        if (rest.EndsWith("7")) tones.Add(root + 10);
        return tones.ToArray();
    }

    private static void Accompany(float[] buffer, Tune tune, Section section, int[] chord, float t0, float step)
    {
        int root = chord[0] - 12, fifth = chord[2] - 12, third = chord[1] - 12;
        switch (tune.Bass)
        {
            case BassStyle.Oompah:
                Note(buffer, t0, step * 1.6f, root, Voice.Bass, .45f);
                Note(buffer, t0 + step * 4, step * 1.6f, fifth - 12 < 33 ? fifth : fifth - 12, Voice.Bass, .4f);
                foreach (float off in new[] { 2f, 6f })
                    for (int k = 0; k < 3; k++) Note(buffer, t0 + step * off, step * .7f, chord[k] + 12, Voice.Pluck, .1f);
                break;
            case BassStyle.Walk:
                int[] walk = { root, third, fifth, third };
                for (int k = 0; k < 4; k++) Note(buffer, t0 + step * k * 2, step * 1.8f, walk[k], Voice.Bass, .42f);
                break;
            case BassStyle.Bounce:
                Note(buffer, t0, step * .8f, root, Voice.Bass, .45f);
                Note(buffer, t0 + step * 3, step * .8f, root, Voice.Bass, .35f);
                Note(buffer, t0 + step * 6, step * .8f, root + 12, Voice.Bass, .35f);
                break;
            default:
                Note(buffer, t0, step * 7.8f, root, Voice.Bass, .35f);
                break;
        }
        if (tune.Bass != BassStyle.Oompah)
            for (int k = 0; k < chord.Length; k++) Note(buffer, t0, step * 7.6f, chord[k] + 12, Voice.Pad, .09f);
        if (section.Arp)
        {
            int[] pattern = { 0, 1, 2, 3, 2, 1, 0, 1 };
            for (int k = 0; k < 8; k++)
            {
                int p = pattern[k];
                int midi = p < 3 ? chord[p] + 24 : chord[0] + 36;
                Note(buffer, t0 + step * k, step * .6f, midi, Voice.Bell, .1f);
            }
        }
    }

    private static void Drums(float[] buffer, Tune tune, float t0, float step, System.Random noise)
    {
        for (int k = 0; k < 8; k++)
        {
            float t = t0 + step * k;
            if (Hit(tune.Kick, k)) Kick(buffer, t);
            if (Hit(tune.Snare, k)) Snare(buffer, t, noise);
            if (Hit(tune.Hat, k)) Hat(buffer, t, noise, .14f * tune.HatVolume * (k % 2 == 0 ? 1 : .7f));
        }
    }

    private static bool Hit(string pattern, int k) => pattern.Length == 8 && pattern[k] == 'x';

    private static void Note(float[] buffer, float start, float duration, int midi, Voice voice, float volume)
    {
        float hz = 440 * Mathf.Pow(2, (midi - 69) / 12f);
        float release = voice == Voice.Pad ? .35f : voice == Voice.Bell || voice == Voice.Pluck ? .3f : .07f;
        int first = Mathf.RoundToInt(start * Rate), count = Mathf.CeilToInt((duration + release) * Rate);
        for (int i = 0; i < count && first + i < buffer.Length; i++)
        {
            float t = i / (float)Rate;
            buffer[first + i] += Osc(voice, hz, t) * Envelope(voice, t, duration, release) * volume;
        }
    }

    private static float Sin(float hz, float t) => Mathf.Sin(t * hz * Mathf.PI * 2);

    private static float Osc(Voice voice, float hz, float t)
    {
        float phase = t * hz % 1;
        switch (voice)
        {
            case Voice.Triangle: return Mathf.Abs(phase * 4 - 2) - 1;
            case Voice.Square: return (phase < .3f ? .45f : -.45f) + (Mathf.Abs(phase * 4 - 2) - 1) * .3f;
            case Voice.Flute:
                float vibrato = 1 + .006f * Mathf.Sin(t * 34.5f) * Mathf.Clamp01(t * 4 - .4f);
                return Sin(hz * vibrato, t) + Sin(hz * 2, t) * .12f;
            case Voice.Bell: return Sin(hz, t) + Sin(hz * 2.76f, t) * .35f * Mathf.Exp(-t * 6) + Sin(hz * 5.4f, t) * .2f * Mathf.Exp(-t * 12);
            case Voice.Pluck:
                return Sin(hz, t) + Sin(hz * 2, t) * .5f * Mathf.Exp(-t * 6) + Sin(hz * 3, t) * .33f * Mathf.Exp(-t * 9) +
                    Sin(hz * 4, t) * .25f * Mathf.Exp(-t * 12);
            case Voice.Accordion: return (Sin(hz * 1.004f, t) + Sin(hz * .996f, t)) * .45f + Sin(hz * 3, t) * .3f + Sin(hz * 5, t) * .15f;
            case Voice.Bass: return Sin(hz, t) + Sin(hz * 2, t) * .25f;
            default: return Sin(hz, t) + Sin(hz * 2.004f, t) * .3f;
        }
    }

    private static float Envelope(Voice voice, float t, float duration, float release)
    {
        float attack = voice == Voice.Flute ? .05f : voice == Voice.Pad ? .3f : .008f;
        float body;
        switch (voice)
        {
            case Voice.Bell: body = Mathf.Exp(-t * 2.2f); break;
            case Voice.Pluck: body = Mathf.Exp(-t * 5); break;
            case Voice.Bass: body = .3f + .7f * Mathf.Exp(-t * 1.5f); break;
            case Voice.Pad: body = 1; break;
            default: body = Mathf.Lerp(1, .7f, Mathf.Clamp01(t / .2f)); break;
        }
        float end = t > duration ? Mathf.Clamp01(1 - (t - duration) / release) : 1;
        return Mathf.Clamp01(t / attack) * body * end;
    }

    private static void Kick(float[] buffer, float start)
    {
        int first = Mathf.RoundToInt(start * Rate), count = (int)(.25f * Rate);
        for (int i = 0; i < count && first + i < buffer.Length; i++)
        {
            float t = i / (float)Rate;
            float phase = 50 * t + 100 / 30f * (1 - Mathf.Exp(-30 * t));
            buffer[first + i] += Mathf.Sin(phase * Mathf.PI * 2) * Mathf.Exp(-t * 12) * .55f;
        }
    }

    private static void Snare(float[] buffer, float start, System.Random noise)
    {
        int first = Mathf.RoundToInt(start * Rate), count = (int)(.2f * Rate);
        for (int i = 0; i < count && first + i < buffer.Length; i++)
        {
            float t = i / (float)Rate;
            float n = (float)noise.NextDouble() * 2 - 1;
            buffer[first + i] += (n * Mathf.Exp(-t * 18) * .6f + Sin(190, t) * Mathf.Exp(-t * 20) * .4f) * .3f;
        }
    }

    private static void Hat(float[] buffer, float start, System.Random noise, float volume)
    {
        int first = Mathf.RoundToInt(start * Rate), count = (int)(.06f * Rate);
        float last = 0;
        for (int i = 0; i < count && first + i < buffer.Length; i++)
        {
            float t = i / (float)Rate;
            float n = (float)noise.NextDouble() * 2 - 1;
            buffer[first + i] += (n - last) * .5f * Mathf.Exp(-t * 60) * volume;
            last = n;
        }
    }
}
