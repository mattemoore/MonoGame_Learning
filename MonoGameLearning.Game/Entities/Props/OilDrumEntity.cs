using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Props;

public class OilDrumEntity : PropEntity
{
    private int _health;
    private float _hitStunTimer;
    private const int MaxHealth = 6;
    private const float HitStunDuration = 0.3f;
    private readonly OilDrumStateController _stateController;
    private readonly AnimatedSprite _sprite;
    private readonly float _scale;
    public bool IsAlive { get; private set; } = true;
    public event Action<OilDrumEntity> Destroyed;

    public OilDrumEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
        : base(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale))
    {
        _sprite = sprite;
        _scale = scale;
        _stateController = new(new()
        {
            OnNormalEntry = () =>
            {
                string anim = _health switch
                {
                    <= 2 => OilDrumSprite.AnimationCritical,
                    <= 4 => OilDrumSprite.AnimationDamaged,
                    _ => OilDrumSprite.AnimationIdle
                };
                _sprite.SetAnimation(anim);
            },
            OnHitStunEntry = () =>
            {
                string anim = _health switch
                {
                    <= 2 => OilDrumSprite.AnimationCritical,
                    <= 4 => OilDrumSprite.AnimationDamaged,
                    _ => OilDrumSprite.AnimationIdle
                };
                _sprite.SetAnimation(anim);
                _hitStunTimer = HitStunDuration;
            }
        });
        _sprite.Color = Color.White;
        _health = MaxHealth;
    }

    public override void TakeDamage(int amount, bool knockdown = false)
    {
        if (!IsAlive || _stateController.State == OilDrumState.HitStun) return;

        _health -= amount switch
        {
            >= 12 => 6,
            >= 8 => 3,
            _ => 2
        };

        if (_health <= 0)
        {
            IsAlive = false;
            Destroyed?.Invoke(this);
            return;
        }

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
        _sprite.Update(gameTime);
        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_sprite, Position, MathHelper.ToRadians(Rotation), new Vector2(_scale));
    }

    public override void DrawDebug(SpriteBatch spriteBatch)
    {
        base.DrawDebug(spriteBatch);
        spriteBatch.DrawRectangle(Frame, Color.Blue);
    }
}