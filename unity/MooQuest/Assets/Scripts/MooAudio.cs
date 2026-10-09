using UnityEngine;

// Every sound is synthesized at startup: no audio files, no licensing, tiny download.
public static class MooAudio
{
    private const int Rate = 22050;

    public static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
    {
        int count = Mathf.CeilToInt(seconds * Rate);
        var samples = new float[count];
        for (int i = 0; i < count; i++) samples[i] = Mathf.Clamp(wave(i / (float)Rate), -1, 1);
        var clip = AudioClip.Create(name, count, 1, Rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float Sin(float hz, float t) => Mathf.Sin(t * hz * Mathf.PI * 2);
    private static float Env(float t, float length) => t >= length ? 0 : Mathf.Sin(Mathf.PI * t / length);

    public static AudioClip Moo(float pitch = 1) => Make("Moo", .7f, t =>
    {
        float hz = (115 - t * 55) * pitch;
        return (Sin(hz, t) * .7f + Sin(hz * 2, t) * .2f + Sin(hz * 3, t) * .1f) * Env(t, .7f) * .3f;
    });

    public static AudioClip Giggle(float pitch = 1) => Make("Giggle", .55f, t =>
    {
        float syllable = t % .13f;
        float hz = (620 + 140 * Mathf.Floor(t / .13f)) * pitch * (1 + .04f * Sin(28, t));
        return Sin(hz, t) * Env(syllable, .1f) * (1 - t / .6f) * .22f;
    });

    public static AudioClip Swish() => Make("Swish", .18f, t =>
        (Random.value * 2 - 1) * .14f * (1 - t / .18f) + Sin(900 - t * 3000, t) * .05f);

    public static AudioClip Ding() => Make("Ding", .35f, t =>
        Sin(t < .1f ? 988 : 1319, t) * .2f * (1 - t / .35f));

    public static AudioClip Bonk() => Make("Bonk", .25f, t => Sin(220 - t * 600, t) * .3f * (1 - t / .25f));

    public static AudioClip Quack() => Make("Quack", .3f, t =>
    {
        float hz = 420 - t * 500;
        return (Sin(hz, t) + Sin(hz * 2, t) * .6f + Sin(hz * 3, t) * .4f) * .12f * Env(t, .3f);
    });

    public static AudioClip Sparkle() => Make("Sparkle", .5f, t =>
    {
        float[] notes = { 523, 659, 784, 1047 };
        int n = Mathf.Min(3, (int)(t / .1f));
        return Sin(notes[n], t) * .16f * (1 - t / .5f);
    });

    public static AudioClip Lullaby() => Make("Lullaby", 1.2f, t =>
    {
        float[] notes = { 784, 659, 523, 659, 587, 523 };
        int n = Mathf.Min(notes.Length - 1, (int)(t / .2f));
        return Sin(notes[n], t) * .13f * Env(t % .2f, .2f);
    });

    // A gentle little loop: triangle-wave melody over a soft bass.
    public static AudioClip Song(string name, int[] melody, int[] bass, float beat, float volume)
    {
        float length = melody.Length * beat;
        return Make(name, length, t =>
        {
            int index = (int)(t / beat) % melody.Length;
            float local = t % beat;
            float sample = 0;
            if (melody[index] > 0)
            {
                float hz = 440 * Mathf.Pow(2, (melody[index] - 69) / 12f);
                float phase = t * hz % 1;
                sample += (Mathf.Abs(phase * 4 - 2) - 1) * Mathf.Min(1, local * 40) * Mathf.Exp(-local * 3) * .5f;
            }
            int b = bass[(int)(t / (beat * 2)) % bass.Length];
            if (b > 0) sample += Sin(440 * Mathf.Pow(2, (b - 69) / 12f), t) * .35f * Mathf.Exp(-(t % (beat * 2)) * 2);
            return sample * volume;
        });
    }

    public static AudioClip FarmSong() => Song("Farm song",
        new[] { 72, 74, 76, 72, 76, 77, 79, 0, 79, 81, 79, 77, 76, 72, 74, 0, 72, 74, 76, 79, 77, 76, 74, 72, 74, 76, 74, 71, 72, 0, 0, 0 },
        new[] { 48, 53, 55, 48, 48, 53, 55, 48 }, .24f, .22f);

    public static AudioClip WoodsSong() => Song("Woods song",
        new[] { 69, 0, 72, 76, 74, 0, 72, 69, 71, 0, 74, 77, 76, 0, 74, 71, 69, 72, 76, 81, 79, 76, 74, 72, 71, 72, 74, 71, 69, 0, 0, 0 },
        new[] { 45, 41, 43, 40, 45, 41, 43, 45 }, .3f, .2f);

    public static AudioClip BossSong() => Song("Goat polka",
        new[] { 67, 0, 72, 72, 74, 0, 76, 76, 77, 76, 74, 72, 71, 0, 67, 0, 67, 0, 71, 71, 72, 0, 74, 74, 76, 74, 72, 71, 72, 0, 72, 0 },
        new[] { 48, 43, 48, 43, 43, 50, 48, 48 }, .17f, .22f);

    public static AudioClip PartySong() => Song("Dance party",
        new[] { 72, 76, 79, 84, 79, 76, 72, 76, 74, 77, 81, 86, 81, 77, 74, 77, 76, 79, 84, 88, 84, 79, 76, 79, 77, 76, 74, 72, 84, 0, 84, 0 },
        new[] { 48, 50, 52, 53, 48, 50, 55, 48 }, .16f, .22f);
}
