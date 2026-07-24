using System.IO;

var outputDir = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "MonoGameLearning.Game", "Content", "audio"));
Directory.CreateDirectory(outputDir);

static void WriteWav(string path, float duration, int sampleRate, Func<float, float> sampleFunc)
{
    int numSamples = (int)(duration * sampleRate);
    int dataSize = numSamples * 2;
    using var fs = File.Create(path);
    using var bw = new BinaryWriter(fs);
    bw.Write("RIFF"u8);
    bw.Write(36 + dataSize);
    bw.Write("WAVE"u8);
    bw.Write("fmt "u8);
    bw.Write(16);
    bw.Write((short)1);
    bw.Write((short)1);
    bw.Write(sampleRate);
    bw.Write(sampleRate * 2);
    bw.Write((short)2);
    bw.Write((short)16);
    bw.Write("data"u8);
    bw.Write(dataSize);
    for (int i = 0; i < numSamples; i++)
    {
        float t = (float)i / sampleRate;
        float sample = Math.Clamp(sampleFunc(t), -1f, 1f);
        bw.Write((short)(sample * short.MaxValue));
    }
}

static float Sine(float t, float freq) => MathF.Sin(t * freq * MathF.PI * 2);

static float Square(float t, float freq) => MathF.Sign(Sine(t, freq));

static float Noise(float t) => (float)(Random.Shared.NextDouble() * 2 - 1);

static float FadeOut(float t, float duration) => Math.Max(0, 1 - t / duration);

static float Envelope(float t, float attack, float release, float duration)
{
    if (t < attack) return t / attack;
    if (t > duration - release) return (duration - t) / release;
    return 1f;
}

// Attack swings
WriteWav(Path.Combine(outputDir, "attack_swing1.wav"), 0.2f, 44100, t => Sine(t, 400 + t / 0.2f * 400) * FadeOut(t, 0.2f));
WriteWav(Path.Combine(outputDir, "attack_swing2.wav"), 0.25f, 44100, t => Sine(t, 300 + t / 0.25f * 300) * FadeOut(t, 0.25f));
WriteWav(Path.Combine(outputDir, "attack_swing3.wav"), 0.3f, 44100, t => Sine(t, 500 + t / 0.3f * 500) * FadeOut(t, 0.3f));
WriteWav(Path.Combine(outputDir, "enemy_attack_swing.wav"), 0.15f, 44100, t => Sine(t, 600 - t / 0.15f * 400) * FadeOut(t, 0.15f));

// Hit sounds
WriteWav(Path.Combine(outputDir, "hit_light.wav"), 0.05f, 44100, t => Square(t, 200) * FadeOut(t, 0.05f));
WriteWav(Path.Combine(outputDir, "hit_heavy.wav"), 0.08f, 44100, t => (Square(t, 100) * 0.7f + Sine(t, 60) * 0.3f) * FadeOut(t, 0.08f));
WriteWav(Path.Combine(outputDir, "knockdown.wav"), 0.2f, 44100, t => Sine(t, 60 - t / 0.2f * 20) * FadeOut(t, 0.2f));

// Metal hit — short bright clang
WriteWav(Path.Combine(outputDir, "hit_metal.wav"), 0.15f, 44100, t =>
{
    float freq = 1200 + t / 0.15f * -800;
    return Sine(t, freq) * FadeOut(t, 0.15f) * 0.6f + Noise(t) * FadeOut(t, 0.08f) * 0.4f;
});

// Prop explosion — noise burst with low boom
WriteWav(Path.Combine(outputDir, "prop_explosion.wav"), 0.5f, 44100, t =>
{
    float noise = Noise(t) * FadeOut(t, 0.2f) * 0.7f;
    float boom = Sine(t, 60 - t / 0.5f * 40) * FadeOut(t, 0.5f) * 0.5f;
    return noise + boom;
});

// Hurt sounds
WriteWav(Path.Combine(outputDir, "player_hurt.wav"), 0.4f, 44100, t =>
{
    if (t < 0.1f) return Noise(t) * 0.5f * FadeOut(t, 0.1f);
    return Sine(t - 0.1f, 800 - (t - 0.1f) / 0.3f * 600) * FadeOut(t - 0.1f, 0.3f);
});
WriteWav(Path.Combine(outputDir, "enemy_hurt.wav"), 0.3f, 44100, t =>
{
    if (t < 0.1f) return Noise(t) * 0.6f * FadeOut(t, 0.1f);
    return Sine(t - 0.1f, 1000 - (t - 0.1f) / 0.2f * 500) * FadeOut(t - 0.1f, 0.2f);
});

// Death sounds
WriteWav(Path.Combine(outputDir, "enemy_death.wav"), 0.5f, 44100, t => Sine(t, 800 - t / 0.5f * 700) * FadeOut(t, 0.5f));
WriteWav(Path.Combine(outputDir, "player_death.wav"), 1.0f, 44100, t => Sine(t, 600 - t / 1.0f * 550) * FadeOut(t, 1.0f));

// Pickup heal — short bright chime
WriteWav(Path.Combine(outputDir, "pickup_heal.wav"), 0.3f, 44100, t => Sine(t, 1200 - t / 0.3f * 600) * Envelope(t, 0.005f, 0.1f, 0.3f));

// Menu sounds
WriteWav(Path.Combine(outputDir, "menu_navigate.wav"), 0.03f, 44100, t => Sine(t, 1000) * Envelope(t, 0.001f, 0.01f, 0.03f));
WriteWav(Path.Combine(outputDir, "menu_confirm.wav"), 0.1f, 44100, t =>
{
    if (t < 0.05f) return Sine(t, 800) * Envelope(t, 0.001f, 0.01f, 0.1f);
    return Sine(t, 1200) * Envelope(t - 0.05f, 0.001f, 0.01f, 0.05f);
});

// Go prompt bell — 2.5s of repeated bell: 150ms 1kHz sine + 350ms silence, x5
WriteWav(Path.Combine(outputDir, "go_prompt_bell.wav"), 2.5f, 44100, t =>
{
    float mod = t % 0.5f;
    if (mod < 0.15f) return Sine(t, 1000) * Envelope(mod, 0.005f, 0.02f, 0.15f);
    return 0f;
});

// Music tracks
WriteWav(Path.Combine(outputDir, "music_titlemenu.wav"), 4.0f, 44100, t =>
{
    float chord = Sine(t, 261.63f) * 0.15f + Sine(t, 329.63f) * 0.15f + Sine(t, 392.0f) * 0.15f;
    float slow = Sine(t, 0.5f) * 0.5f + 0.5f;
    return chord * slow;
});

WriteWav(Path.Combine(outputDir, "music_gameplay.wav"), 4.0f, 44100, t =>
{
    float pulse = Sine(t, 2f) * 0.5f + 0.5f;
    float bass = Sine(t, 110f) * 0.2f;
    float mid = Sine(t, 220f) * 0.15f;
    float hi = Sine(t, 440f) * 0.1f;
    return (bass + mid + hi) * (0.4f + pulse * 0.3f);
});

WriteWav(Path.Combine(outputDir, "music_levelcomplete.wav"), 2.0f, 44100, t =>
{
    float swell = t / 2.0f;
    return (Sine(t, 261.63f + swell * 200) + Sine(t, 329.63f + swell * 200) * 0.7f + Sine(t, 392.0f + swell * 200) * 0.5f) * 0.3f * swell;
});

Console.WriteLine($"Generated audio files in: {outputDir}");