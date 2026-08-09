#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace MonoGameLearning.Core.Audio;

public class AudioService
{
    private static readonly int PoolSize = 3;

    private readonly Dictionary<SfxId, SoundEffectInstance[]> _sfxInstances = [];
    private readonly Dictionary<SfxId, int> _nextIndex = [];
    private readonly Dictionary<MusicId, SoundEffect> _musicEffects = [];

    private SoundEffectInstance? _musicInstance;
    private MusicId? _currentMusicTrack;
    private bool _isPaused;
    private float _sfxVolume = 1f;
    private float _musicVolume = 1f;
    private ContentManager? _content;

    public float SfxVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Math.Clamp(value, 0f, 1f);
            foreach (var instances in _sfxInstances.Values)
                for (int i = 0; i < instances.Length; i++)
                    if (instances[i] is not null)
                        instances[i].Volume = _sfxVolume;
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Math.Clamp(value, 0f, 1f);
            ApplyMusicVolume();
        }
    }

    internal SoundEffectInstance? GetMusicInstanceForTest() => _musicInstance;
    internal bool IsPausedForTest => _isPaused;
    internal float RawMusicVolumeForTest => _musicVolume;

    public void LoadContent(ContentManager content)
    {
        _content = content;

        LoadSfxGroup(
            (SfxId.AttackSwing1, "audio/attack_swing1"),
            (SfxId.AttackSwing2, "audio/attack_swing2"),
            (SfxId.AttackSwing3, "audio/attack_swing3"),
            (SfxId.EnemyAttackSwing, "audio/enemy_attack_swing"),
            (SfxId.HitLight, "audio/hit_light"),
            (SfxId.HitHeavy, "audio/hit_heavy"),
            (SfxId.HitMetal, "audio/hit_metal"),
            (SfxId.Knockdown, "audio/knockdown"),
            (SfxId.PlayerHurt, "audio/player_hurt"),
            (SfxId.EnemyHurt, "audio/enemy_hurt"),
            (SfxId.EnemyDeath, "audio/enemy_death"),
            (SfxId.PlayerDeath, "audio/player_death"),
            (SfxId.PropExplosion, "audio/prop_explosion"),
            (SfxId.MenuNavigate, "audio/menu_navigate"),
            (SfxId.MenuConfirm, "audio/menu_confirm"),
            (SfxId.GoPromptBell, "audio/go_prompt_bell"),
            (SfxId.PickupHeal, "audio/pickup_heal"));

        LoadMusic(MusicId.TitleMenu, "audio/music_titlemenu");
        LoadMusic(MusicId.Gameplay, "audio/music_gameplay");
        LoadMusic(MusicId.LevelComplete, "audio/music_levelcomplete");
    }

    private void LoadMusic(MusicId id, string path)
    {
        try
        {
            _musicEffects[id] = _content!.Load<SoundEffect>(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Failed to load music '{path}': {ex.Message}");
        }
    }

    private void LoadSfxGroup(params (SfxId id, string path)[] entries)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            var (id, path) = entries[i];
            try
            {
                var effect = _content!.Load<SoundEffect>(path);
                var pool = new SoundEffectInstance[PoolSize];
                for (int j = 0; j < PoolSize; j++)
                {
                    pool[j] = effect.CreateInstance();
                    pool[j].Volume = _sfxVolume;
                }
                _sfxInstances[id] = pool;
                _nextIndex[id] = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Failed to load '{path}': {ex.Message}");
            }
        }
    }

    public void PlaySfx(SfxId sfx)
    {
        if (!_sfxInstances.TryGetValue(sfx, out var pool))
            return;

        int idx = _nextIndex[sfx];
        var instance = pool[idx];

        if (instance.State == SoundState.Playing)
            instance.Stop();

        instance.Play();
        _nextIndex[sfx] = (idx + 1) % PoolSize;
    }

    public void PlayMusic(MusicId? track)
    {
        if (track == _currentMusicTrack && _musicInstance?.State == SoundState.Playing)
            return;

        _musicInstance?.Stop();
        _musicInstance?.Dispose();
        _musicInstance = null;
        _currentMusicTrack = null;

        if (track is null || !_musicEffects.TryGetValue(track.Value, out var effect))
            return;

        _currentMusicTrack = track;
        _musicInstance = effect.CreateInstance();
        _musicInstance.IsLooped = track != MusicId.LevelComplete;
        ApplyMusicVolume();
        _musicInstance.Play();
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;
        ApplyMusicVolume();
    }

    public void Update()
    {
        if (_musicInstance is null)
            return;

        if (_currentMusicTrack == MusicId.LevelComplete && _musicInstance.State == SoundState.Stopped)
        {
            _musicInstance.Dispose();
            _musicInstance = null;
            _currentMusicTrack = null;
        }
    }

    private const float MusicPauseDuck = 0.3f;

    internal static float ComputeMusicVolume(float baseVolume, bool isPaused) =>
        isPaused ? baseVolume * MusicPauseDuck : baseVolume;

    private void ApplyMusicVolume()
    {
        if (_musicInstance is null) return;
        _musicInstance.Volume = ComputeMusicVolume(_musicVolume, _isPaused);
    }
}