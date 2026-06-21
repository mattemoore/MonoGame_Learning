using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Props;

public class OilDrumEntity : PropEntity
{
    private float _hitStunTimer;
    private const float HitStunDuration = 0.3f;
    private readonly OilDrumStateController _stateController;

    public OilDrumEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
        : base(name, position, scale, sprite)
    {
        MaxHealth = 6;
        Health = 6;
        Sprite.Color = Color.White;
        _stateController = new(new()
        {
            OnNormalEntry = () =>
            {
                string anim = Health switch
                {
                    <= 2 => OilDrumSprite.AnimationCritical,
                    <= 4 => OilDrumSprite.AnimationDamaged,
                    _ => OilDrumSprite.AnimationIdle
                };
                Sprite.SetAnimation(anim);
            },
            OnHitStunEntry = () =>
            {
                string anim = Health switch
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

    public override void TakeDamage(int amount, bool knockdown = false)
    {
        if (_stateController.State == OilDrumState.HitStun) return;

        // Non-linear damage scaling gives heavy attacks a disproportionate advantage
        // against solid objects vs chip damage. Simple HP scaling keeps ratios linear
        // (Attack3 always 2.4× Attack1). This table makes Attack3 feel 3× more
        // effective while preventing light attacks from feeling tedious. Game feel tuning.
        int effectiveDamage = amount switch
        {
            >= 12 => 6,
            >= 8 => 3,
            _ => 2
        };

        base.TakeDamage(effectiveDamage, knockdown);

        if (IsAlive)
            _stateController.Fire(OilDrumTrigger.Hit);
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsAlive) return;
        if (_stateController.IsInState(OilDrumState.HitStun))
        {
            _hitStunTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_hitStunTimer <= 0)
                _stateController.Fire(OilDrumTrigger.HitStunCompleted);
        }
        base.Update(gameTime);
    }
}