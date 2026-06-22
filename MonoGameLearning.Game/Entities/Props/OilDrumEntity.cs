using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Props;

public class OilDrumEntity : PropBase, IUpdatable
{
    private readonly OilDrumStateController _stateController;
    private float _hitStunTimer;
    private const float HitStunDuration = 0.3f;

    public OilDrumEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
        : base(name, position, sprite, scale, 6)
    {
        Sprite.Color = Color.White;
        _stateController = new(new()
        {
            OnNormalEntry = () =>
            {
                string anim = HealthComponent.Value switch
                {
                    <= 2 => OilDrumSprite.AnimationCritical,
                    <= 4 => OilDrumSprite.AnimationDamaged,
                    _ => OilDrumSprite.AnimationIdle
                };
                Sprite.SetAnimation(anim);
            },
            OnHitStunEntry = () =>
            {
                string anim = HealthComponent.Value switch
                {
                    <= 2 => OilDrumSprite.AnimationCritical,
                    <= 4 => OilDrumSprite.AnimationDamaged,
                    _ => OilDrumSprite.AnimationIdle
                };
                Sprite.SetAnimation(anim);
                _hitStunTimer = HitStunDuration;
            }
        });
    }

    public void Update(GameTime gameTime)
    {
        if (!HealthComponent.IsAlive) return;

        Sprite?.Update(gameTime);

        if (_stateController.IsInState(OilDrumState.HitStun))
        {
            _hitStunTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_hitStunTimer <= 0)
                _stateController.Fire(OilDrumTrigger.HitStunCompleted);
        }
    }

    public override void TakeDamage(DamageInfo info)
    {
        if (!HealthComponent.IsAlive || _stateController.State == OilDrumState.HitStun) return;

        int effective = info.Strength switch { AttackStrength.Heavy => 6, AttackStrength.Medium => 3, _ => 2 };
        base.TakeDamage(new DamageInfo { Amount = effective });

        if (HealthComponent.IsAlive)
            _stateController.Fire(OilDrumTrigger.Hit);
    }
}