using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Props;

public class OilDrumEntity : PropBase, IUpdatable
{
    private bool _isHitStunned;
    private float _hitStunTimer;
    private const float HitStunDuration = 0.3f;

    private string SelectAnimation() => HealthComponent.Value switch
    {
        <= 2 => OilDrumSprite.AnimationCritical,
        <= 4 => OilDrumSprite.AnimationDamaged,
        _ => OilDrumSprite.AnimationIdle
    };

    public OilDrumEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
        : base(name, position, sprite, scale, 6)
    {
        Sprite.Color = Color.White;
        Sprite.SetAnimation(SelectAnimation());
    }

    public void Update(GameTime gameTime)
    {
        if (!HealthComponent.IsAlive) return;

        Sprite?.Update(gameTime);

        if (_isHitStunned)
        {
            _hitStunTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_hitStunTimer <= 0)
            {
                _isHitStunned = false;
                Sprite.SetAnimation(SelectAnimation());
            }
        }
    }

    public override void TakeDamage(DamageInfo info)
    {
        if (!HealthComponent.IsAlive || _isHitStunned) return;

        int effective = info.Strength switch { AttackStrength.Heavy => 6, AttackStrength.Medium => 3, _ => 2 };
        HealthComponent.Subtract(effective);

        if (!HealthComponent.IsAlive)
            OnDestroyed();
        else
        {
            _isHitStunned = true;
            _hitStunTimer = HitStunDuration;
            Sprite.SetAnimation(SelectAnimation());
        }
    }
}