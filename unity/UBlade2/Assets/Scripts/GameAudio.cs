using System;
using UnityEngine;

// Procedural audio for the playable UltraBlade 2 slice. No audio assets are used:
// two AudioSources (music loop + one-shot effects) are fed clips generated with
// AudioClip.Create at 44.1 kHz. Every entry point is defensive and never throws.
public static class GameAudio
{
    private const int SampleRate = 44100;
    private const int ArenaCount = 4;
    private const int CollectClipCount = 9;
    private const float Tau = Mathf.PI * 2f;

    private static readonly int[] MusicRoots = { 45, 43, 41, 38 };
    private static readonly int[][] MusicScales =
    {
        new[] { 0, 2, 3, 5, 7, 8, 10 },
        new[] { 0, 2, 3, 5, 7, 9, 10 },
        new[] { 0, 1, 3, 5, 7, 8, 10 },
        new[] { 0, 2, 3, 5, 7, 8, 11 }
    };
    private static readonly int[][] MusicArps =
    {
        new[] { 0, 2, 4, 2, 0, 4, 6, 4 },
        new[] { 0, 4, 2, 6, 4, 2, 5, 1 },
        new[] { 0, 1, 4, 1, 3, 6, 4, 2 },
        new[] { 0, 4, 7, 4, 9, 7, 4, 2 }
    };

    private static readonly AudioClip[] CollectClips = new AudioClip[CollectClipCount];
    private static readonly AudioClip[] MusicClips = new AudioClip[ArenaCount];

    private static AudioSource musicSource;
    private static AudioSource sfxSource;
    private static AudioClip hitClip;
    private static AudioClip hitTakenClip;
    private static AudioClip whistleClip;
    private static AudioClip clickClip;
    private static AudioClip stunClip;
    private static AudioClip dashClip;
    private static AudioClip victoryClip;
    private static AudioClip defeatClip;

    private static GameSettings appliedSettings;
    private static int activeArena = -1;
    private static float musicVolume = 0.425f;
    private static float sfxVolume = 0.68f;

    public static bool Ready { get; private set; }

    public static void Initialize(GameObject host)
    {
        try
        {
            EnsureSources(host);
            EnsureSfxClips();
            Ready = musicSource != null && sfxSource != null;
            ApplyVolumes();
        }
        catch (Exception)
        {
            Ready = false;
        }
    }

    public static void ApplySettings(GameSettings settings)
    {
        try
        {
            appliedSettings = settings;
            ApplyVolumes();
        }
        catch (Exception)
        {
        }
    }

    public static void PlayCollect(int combo = 0)
    {
        try
        {
            if (!EnsureReady())
            {
                return;
            }
            int index = Mathf.Clamp(combo, 0, CollectClipCount - 1);
            AudioClip clip = CollectClips[index];
            if (clip == null)
            {
                clip = BuildCollect(index);
                CollectClips[index] = clip;
            }
            PlayOneShot(clip, 0.95f);
        }
        catch (Exception)
        {
        }
    }

    public static void PlayHit(bool playerWasHit)
    {
        try
        {
            if (!EnsureReady())
            {
                return;
            }
            PlayOneShot(playerWasHit ? hitTakenClip : hitClip, 1f);
        }
        catch (Exception)
        {
        }
    }

    public static void PlayWhistle()
    {
        try
        {
            if (!EnsureReady())
            {
                return;
            }
            PlayOneShot(whistleClip, 0.85f);
        }
        catch (Exception)
        {
        }
    }

    public static void PlayClick()
    {
        try
        {
            if (!EnsureReady())
            {
                return;
            }
            PlayOneShot(clickClip, 0.6f);
        }
        catch (Exception)
        {
        }
    }

    public static void PlayStun()
    {
        try
        {
            if (!EnsureReady())
            {
                return;
            }
            PlayOneShot(stunClip, 0.9f);
        }
        catch (Exception)
        {
        }
    }

    public static void PlayDash()
    {
        try
        {
            if (!EnsureReady())
            {
                return;
            }
            PlayOneShot(dashClip, 0.85f);
        }
        catch (Exception)
        {
        }
    }

    public static void PlayVictory()
    {
        try
        {
            if (!EnsureReady())
            {
                return;
            }
            PlayOneShot(victoryClip, 1f);
        }
        catch (Exception)
        {
        }
    }

    public static void PlayDefeat()
    {
        try
        {
            if (!EnsureReady())
            {
                return;
            }
            PlayOneShot(defeatClip, 1f);
        }
        catch (Exception)
        {
        }
    }

    public static void StartMusic(int arenaIndex)
    {
        try
        {
            if (!EnsureReady())
            {
                return;
            }
            int arena = Mathf.Clamp(arenaIndex, 0, ArenaCount - 1);
            if (activeArena == arena && musicSource.isPlaying)
            {
                return;
            }
            AudioClip clip = MusicClips[arena];
            if (clip == null)
            {
                clip = BuildMusic(arena);
                MusicClips[arena] = clip;
            }
            activeArena = arena;
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
        catch (Exception)
        {
        }
    }

    public static void StopMusic()
    {
        try
        {
            activeArena = -1;
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.clip = null;
            }
        }
        catch (Exception)
        {
        }
    }

    // ------------------------------------------------------------- lifecycle

    private static bool EnsureReady()
    {
        if (Ready && musicSource != null && sfxSource != null)
        {
            return true;
        }
        Initialize(null);
        return Ready && musicSource != null && sfxSource != null;
    }

    private static void EnsureSources(GameObject host)
    {
        if (musicSource != null && sfxSource != null)
        {
            return;
        }

        GameObject root = host;
        if (root == null)
        {
            root = GameObject.Find("UBlade2Audio");
            if (root == null)
            {
                root = new GameObject("UBlade2Audio");
            }
        }

        if (musicSource == null)
        {
            musicSource = CreateSource(root, "MusicSource", true);
        }
        if (sfxSource == null)
        {
            sfxSource = CreateSource(root, "SfxSource", false);
        }
    }

    private static AudioSource CreateSource(GameObject root, string name, bool loop)
    {
        Transform existing = root.transform.Find(name);
        AudioSource source;
        if (existing != null)
        {
            source = existing.GetComponent<AudioSource>();
            if (source == null)
            {
                source = existing.gameObject.AddComponent<AudioSource>();
            }
        }
        else
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            source = child.AddComponent<AudioSource>();
        }
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.volume = loop ? musicVolume : sfxVolume;
        return source;
    }

    private static void ApplyVolumes()
    {
        GameSettings settings = appliedSettings;
        float master = settings != null ? Mathf.Clamp01(settings.masterVolume) : 0.85f;
        float music = settings != null ? Mathf.Clamp01(settings.musicVolume) : 0.5f;
        float sfx = settings != null ? Mathf.Clamp01(settings.sfxVolume) : 0.8f;
        musicVolume = master * music;
        sfxVolume = master * sfx;
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    private static void PlayOneShot(AudioClip clip, float gain)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(gain));
    }

    private static void EnsureSfxClips()
    {
        if (hitClip == null)
        {
            hitClip = BuildHit(false);
        }
        if (hitTakenClip == null)
        {
            hitTakenClip = BuildHit(true);
        }
        if (whistleClip == null)
        {
            whistleClip = BuildWhistle();
        }
        if (clickClip == null)
        {
            clickClip = BuildClick();
        }
        if (stunClip == null)
        {
            stunClip = BuildStun();
        }
        if (dashClip == null)
        {
            dashClip = BuildDash();
        }
        if (victoryClip == null)
        {
            victoryClip = BuildJingle(true);
        }
        if (defeatClip == null)
        {
            defeatClip = BuildJingle(false);
        }
    }

    // --------------------------------------------------------------- effects

    private static AudioClip BuildCollect(int index)
    {
        float frequency = 520f * Mathf.Pow(2f, index / 12f);
        const float Duration = 0.46f;
        int seed = 31 + index * 977;
        return BuildMono("sfx_collect_" + index, Duration, t =>
        {
            float attack = Mathf.Clamp01(t / 0.005f);
            float envelope = attack * Mathf.Exp(-t * 7.5f);
            float bell = Mathf.Sin(Tau * frequency * t) * 0.55f +
                         Mathf.Sin(Tau * frequency * 2.01f * t) * 0.24f +
                         Mathf.Sin(Tau * frequency * 3.02f * t) * 0.1f;
            float shimmer = Noise((int)(t * SampleRate) + seed) * Mathf.Exp(-t * 42f) * 0.12f;
            return (bell * envelope + shimmer) * 0.65f;
        });
    }

    private static AudioClip BuildHit(bool playerWasHit)
    {
        float duration = playerWasHit ? 0.34f : 0.26f;
        float start = playerWasHit ? 108f : 150f;
        float end = playerWasHit ? 42f : 64f;
        float level = playerWasHit ? 0.95f : 0.7f;
        int seed = playerWasHit ? 911 : 173;
        float lowPass = 0f;
        return BuildMono(playerWasHit ? "sfx_hit_taken" : "sfx_hit_landed", duration, t =>
        {
            float progress = Mathf.Clamp01(t / duration);
            float phase = Tau * (start * t + (end - start) * t * t / (2f * duration));
            float body = Mathf.Sin(phase) * Mathf.Exp(-t * (playerWasHit ? 8f : 12f)) * 0.6f;
            float raw = Noise((int)(t * SampleRate) + seed);
            lowPass += (raw - lowPass) * 0.08f;
            float noise = lowPass * Mathf.Exp(-t * 16f) * 1.7f;
            float tail = Mathf.Clamp01(1f - progress);
            return (body + noise * 0.45f) * tail * level;
        });
    }

    private static AudioClip BuildWhistle()
    {
        const float Duration = 0.42f;
        return BuildMono("sfx_whistle", Duration, t =>
        {
            float progress = Mathf.Clamp01(t / Duration);
            float envelope = Mathf.Clamp01(t / 0.015f) * Mathf.Clamp01((Duration - t) / 0.06f);
            float vibrato = Mathf.Sin(Tau * 26f * t) * 0.5f;
            float frequency = 2050f * (1f + 0.08f * progress);
            float tone = Mathf.Sin(Tau * frequency * t + vibrato) * 0.6f +
                         Mathf.Sin(Tau * frequency * 2f * t) * 0.18f;
            return tone * envelope * 0.55f;
        });
    }

    private static AudioClip BuildClick()
    {
        const float Duration = 0.06f;
        return BuildMono("sfx_click", Duration, t =>
        {
            float envelope = Mathf.Exp(-t * 95f);
            float tone = Mathf.Sin(Tau * 1650f * t) * 0.5f;
            float noise = Noise((int)(t * SampleRate) + 47) * 0.35f;
            return (tone + noise) * envelope * 0.7f;
        });
    }

    private static AudioClip BuildStun()
    {
        const float Duration = 0.7f;
        return BuildMono("sfx_stun", Duration, t =>
        {
            float progress = Mathf.Clamp01(t / Duration);
            float phase = Tau * (340f * t + (60f - 340f) * t * t / (2f * Duration));
            float square = Mathf.Clamp(Mathf.Sin(phase) * 3f, -1f, 1f);
            float buzz = 0.65f + 0.35f * Mathf.Sin(Tau * 37f * t);
            float envelope = Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((Duration - t) / 0.12f) *
                             Mathf.Exp(-t * 1.2f);
            float grit = Noise((int)(t * SampleRate) + 313) * 0.12f;
            return (square * buzz * 0.5f + grit) * envelope * 0.5f;
        });
    }

    private static AudioClip BuildDash()
    {
        const float Duration = 0.42f;
        float lowPass = 0f;
        return BuildMono("sfx_dash", Duration, t =>
        {
            float progress = Mathf.Clamp01(t / Duration);
            float raw = Noise((int)(t * SampleRate) + 271);
            lowPass += (raw - lowPass) * Mathf.Lerp(0.35f, 0.02f, progress);
            float envelope = Mathf.Sin(Mathf.PI * progress);
            float body = Mathf.Sin(Tau * (520f - 320f * progress) * t) * 0.12f;
            return (lowPass * 1.5f + body) * envelope * 0.55f;
        });
    }

    private static AudioClip BuildJingle(bool victory)
    {
        int[] notes = victory ? new[] { 72, 76, 79, 84 } : new[] { 69, 65, 62, 57 };
        float[] offsets = victory ? new[] { 0f, 0.13f, 0.26f, 0.39f } : new[] { 0f, 0.2f, 0.4f, 0.6f };
        float noteLength = victory ? 0.7f : 0.85f;
        float duration = victory ? 1.5f : 1.7f;
        return BuildMono(victory ? "sfx_victory" : "sfx_defeat", duration, t =>
        {
            float sum = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float local = t - offsets[i];
                if (local < 0f || local > noteLength)
                {
                    continue;
                }
                float frequency = MidiToFreq(notes[i]);
                float attack = Mathf.Clamp01(local / 0.01f);
                float release = Mathf.Clamp01((noteLength - local) / (noteLength * 0.6f));
                float envelope = attack * release * Mathf.Exp(-local * 1.4f);
                float wave = Mathf.Sin(Tau * frequency * local) * 0.6f +
                             Mathf.Sin(Tau * frequency * 2f * local) * 0.22f;
                if (victory)
                {
                    wave += Mathf.Sin(Tau * frequency * 3f * local) * 0.1f;
                }
                sum += wave * envelope * 0.25f;
            }
            return sum;
        });
    }

    // ---------------------------------------------------------------- music

    private static AudioClip BuildMusic(int arena)
    {
        arena = Mathf.Clamp(arena, 0, ArenaCount - 1);
        int bpm = 104 + arena * 8;
        float stepDuration = 60f / (bpm * 2f);
        const int Steps = 40;
        int total = Mathf.RoundToInt(stepDuration * Steps * SampleRate);
        var data = new float[total * 2];
        int[] scale = MusicScales[arena];
        int[] arp = MusicArps[arena];
        int root = MusicRoots[arena];

        for (int step = 0; step < Steps; step++)
        {
            int start = Mathf.RoundToInt(step * stepDuration * SampleRate);
            int end = step == Steps - 1 ? total : Mathf.RoundToInt((step + 1) * stepDuration * SampleRate);
            start = Mathf.Clamp(start, 0, total);
            end = Mathf.Clamp(end, start, total);
            int length = end - start;
            if (length <= 0)
            {
                continue;
            }

            int degree = arp[step % arp.Length];
            float leadFrequency = MidiToFreq(root + 24 + ScaleOffset(scale, degree));
            int bassDegree = (step % 4 == 0 || step % 4 == 3) ? 0 : 4;
            float bassFrequency = MidiToFreq(root + scale[bassDegree % scale.Length]);
            float pan = (step % 2 == 0) ? -0.3f : 0.3f;
            float leftGain = Mathf.Sqrt(0.5f * (1f - pan));
            float rightGain = Mathf.Sqrt(0.5f * (1f + pan));
            bool kick = (step % 4) == 0;
            bool snare = (step % 8) == 4 || (step % 8) == 7;

            for (int i = start; i < end; i++)
            {
                float local = (i - start) / (float)SampleRate;
                float u = (i - start) / (float)length;
                float attack = Mathf.Clamp01(u / 0.06f);
                float envelope = attack * Mathf.Pow(1f - u, 1.5f);
                float leadPhase = Tau * leadFrequency * local;
                float lead = (Mathf.Sin(leadPhase) + 0.3f * Mathf.Sin(leadPhase * 2f) +
                              0.1f * Mathf.Sin(leadPhase * 3f)) * envelope * 0.16f;
                float bassPhase = Tau * bassFrequency * local;
                float bass = (Mathf.Sin(bassPhase) + 0.25f * Mathf.Sin(bassPhase * 2f)) *
                             Mathf.Pow(1f - u, 2.1f) * 0.22f;
                float mono = bass;
                if (kick)
                {
                    float kickPhase = Tau * (45f * local + (85f / 18f) * (1f - Mathf.Exp(-18f * local)));
                    mono += Mathf.Sin(kickPhase) * Mathf.Exp(-local * 9f) * (1f - u) * 0.35f;
                }
                mono += Noise(i + step * 7919) * Mathf.Exp(-local * 45f) * 0.06f;
                if (snare)
                {
                    mono += Noise(i + 104729 + step * 313) * Mathf.Exp(-local * 14f) * (1f - u) * 0.11f;
                }
                data[i * 2] += lead * leftGain + mono;
                data[i * 2 + 1] += lead * rightGain + mono;
            }
        }

        float duration = total / (float)SampleRate;
        float padRoot = MidiToFreq(root + 12);
        float padFifth = MidiToFreq(root + 19);
        float padOctave = MidiToFreq(root + 26);
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)SampleRate;
            float loopEnvelope = Mathf.Sin(Mathf.PI * t / duration);
            loopEnvelope *= loopEnvelope;
            float left = Mathf.Sin(Tau * padRoot * t) +
                         Mathf.Sin(Tau * padFifth * t) +
                         Mathf.Sin(Tau * padOctave * t);
            float right = Mathf.Sin(Tau * padRoot * 1.002f * t) +
                          Mathf.Sin(Tau * padFifth * 1.003f * t) +
                          Mathf.Sin(Tau * padOctave * 1.002f * t);
            data[i * 2] += left * loopEnvelope * 0.045f;
            data[i * 2 + 1] += right * loopEnvelope * 0.045f;
        }

        for (int i = 0; i < data.Length; i++)
        {
            float value = data[i];
            data[i] = float.IsNaN(value) ? 0f : Mathf.Clamp(value, -1f, 1f);
        }

        var clip = AudioClip.Create("music_arena_" + arena, total, 2, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static int ScaleOffset(int[] scale, int degree)
    {
        int count = scale.Length;
        int octave = degree / count;
        int index = degree % count;
        if (index < 0)
        {
            index += count;
            octave -= 1;
        }
        return scale[index] + 12 * octave;
    }

    // -------------------------------------------------------------- synthesis

    private static AudioClip BuildMono(string name, float seconds, Func<float, float> sample)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
        var data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float value = sample(t);
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                value = 0f;
            }
            float edge = Mathf.Clamp01(t / 0.003f) * Mathf.Clamp01((seconds - t) / 0.01f);
            data[i] = Mathf.Clamp(value * edge, -1f, 1f);
        }
        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float MidiToFreq(int midi)
    {
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }

    private static float Noise(int index)
    {
        unchecked
        {
            uint value = (uint)index;
            value = (value ^ 61u) ^ (value >> 16);
            value *= 9u;
            value ^= value >> 4;
            value *= 0x27d4eb2du;
            value ^= value >> 15;
            return value / 2147483648f - 1f;
        }
    }
}
