using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Props;

public class OilDrumEntity : ActorEntity
{
    private int _health;
    private float _hitCooldown;
    private const int MaxHealth = 6;
    public bool IsAlive { get; private set; } = true;
    public event Action<OilDrumEntity> Destroyed;

    public OilDrumEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
        : base(name, position, scale, sprite)
    {
        sprite.SetAnimation(OilDrumSprite.AnimationIdle);
        sprite.Origin = new Vector2(sprite.Size.X / 2f, sprite.Size.Y / 2f);
        sprite.Color = Color.White;
        _health = MaxHealth;
    }

    public override void TakeDamage(int amount, bool knockdown = false)
    {
        if (!IsAlive || _hitCooldown > 0) return;

        _health -= amount switch
        {
            >= 12 => 6,
            >= 8 => 3,
            _ => 2
        };
        _hitCooldown = 0.3f;

        if (_health <= 0)
        {
            IsAlive = false;
            Destroyed?.Invoke(this);
            return;
        }

        string anim = _health switch
        {
            <= 2 => OilDrumSprite.AnimationCritical,
            <= 4 => OilDrumSprite.AnimationDamaged,
            _ => OilDrumSprite.AnimationIdle
        };
        Sprite.SetAnimation(anim);
        Sprite.Origin = new Vector2(Sprite.Size.X / 2f, Sprite.Size.Y / 2f);
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsAlive) return;
        if (_hitCooldown > 0)
            _hitCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        base.Update(gameTime);
    }
}