using System.Collections.Generic;
using MonoGameLearning.Core.Audio;

namespace MonoGameLearning.Game.Audio;

public static class AudioManifest
{
    public static readonly IReadOnlyList<(SfxId Id, string Path)> SfxAssets =
    [
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
        (SfxId.PickupHeal, "audio/pickup_heal"),
    ];

    public static readonly IReadOnlyList<(MusicId Id, string Path)> MusicAssets =
    [
        (MusicId.TitleMenu, "audio/music_titlemenu"),
        (MusicId.Gameplay, "audio/music_gameplay"),
        (MusicId.LevelComplete, "audio/music_levelcomplete"),
    ];
}