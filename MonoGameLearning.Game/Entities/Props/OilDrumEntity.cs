using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Game.AnimatedSprites;

namespace MonoGameLearning.Game.Entities.Props;

public class OilDrumEntity : PropBase, IUpdatable
{
    private const float DrumCollisionHeightFraction = 0.5f;
    public override float CollisionHeightFraction => DrumCollisionHeightFraction;

    private readonly OilDrumBehavior _behavior = new();
    private readonly AudioManager _audio;

    private string SelectAnimation() => HealthComponent.Value switch
    {
        <= 2 => OilDrumSprite.AnimationCritical,
        <= 4 => OilDrumSprite.AnimationDamaged,
        _ => OilDrumSprite.AnimationIdle
    };

    public OilDrumEntity(string name, Vector2 position, float scale, AnimatedSprite sprite, AudioManager audio, CollisionAnchor anchor = CollisionAnchor.Top)
        : base(name, position, sprite, scale, 6, anchor)
    {
        _audio = audio;
        Sprite.Color = Color.White;
        Sprite.SetAnimation(SelectAnimation());
    }

    public void Update(GameTime gameTime)
    {
        if (!HealthComponent.IsAlive) return;

        Sprite?.Update(gameTime);

        if (_behavior.Update((float)gameTime.ElapsedGameTime.TotalSeconds))
            Sprite.SetAnimation(SelectAnimation());
    }

    public override void TakeDamage(DamageInfo info)
    {
        if (!_behavior.CanTakeDamage(HealthComponent.IsAlive)) return;

        int effective = OilDrumDamage.GetEffectiveDamage(info.Strength);
        HealthComponent.Subtract(effective);

        if (!HealthComponent.IsAlive)
        {
            _audio.PlaySfx(SfxId.PropExplosion);
            OnDestroyed();
        }
        else
        {
            _audio.PlaySfx(SfxId.HitMetal);
            _behavior.ApplyStun();
            Sprite.SetAnimation(SelectAnimation());
        }
    }
}