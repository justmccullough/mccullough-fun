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
}
