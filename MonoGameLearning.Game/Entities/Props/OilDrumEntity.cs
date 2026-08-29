using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Game.AnimatedSprites;

namespace MonoGameLearning.Game.Entities.Props;

public class OilDrumEntity : PropBase, IUpdatable
{
    private const float DrumCollisionHeightFraction = 0.5f;
    private const float HitStunDuration = 0.3f;
    public override float CollisionHeightFraction => DrumCollisionHeightFraction;

    private readonly AudioService _audio;
    private bool _isHitStunned;
    private float _hitStunTimer;

    private string SelectAnimation() => HealthComponent.Value switch
    {
        <= 2 => OilDrumSprite.AnimationCritical,
        <= 4 => OilDrumSprite.AnimationDamaged,
        _ => OilDrumSprite.AnimationIdle
    };

    public OilDrumEntity(string name, Vector2 position, float scale, AnimatedSprite sprite, AudioService audio, CollisionAnchor anchor = CollisionAnchor.Top)
        : base(name, position, sprite, scale, 6, anchor)
    {
        _audio = audio;
        SpriteRenderer.SetColor(Color.White);
        SpriteRenderer.SetAnimation(SelectAnimation());
    }

    // Sprite-less overload for test doubles — SpriteRenderer gets null sprite (ops are no-ops).
    internal OilDrumEntity(string name, Vector2 position, int width, int height, AudioService audio)
        : base(name, position, width, height, 6, CollisionAnchor.Top)
    {
        _audio = audio;
    }

    public void Update(GameTime gameTime)
    {
        if (!HealthComponent.IsAlive) return;

        SpriteRenderer.Update(gameTime);

        if (UpdateHitStun((float)gameTime.ElapsedGameTime.TotalSeconds))
            SpriteRenderer.SetAnimation(SelectAnimation());
    }

    public override void TakeDamage(DamageInfo info)
    {
        if (!HealthComponent.IsAlive || _isHitStunned) return;

        // Unlike every other IDamageable, which consumes DamageInfo.Amount directly,
        // the drum's durability is intentionally tiered by AttackStrength instead of
        // budgeted to raw damage numbers. Its max HP is a tiny 6, so a single
        // rebalanced attack value would silently change its designed hit count.
        // Mapping tiers (heavy 6 / medium 3 / light 2) pins the feel — heavy
        // one-shots, light needs three hits — regardless of how attack amounts are
        // tuned later. See OilDrumDamage.
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
            ApplyStun();
            SpriteRenderer.SetAnimation(SelectAnimation());
        }
    }

    private void ApplyStun()
    {
        _isHitStunned = true;
        _hitStunTimer = HitStunDuration;
    }

    private bool UpdateHitStun(float deltaSeconds)
    {
        if (!_isHitStunned) return false;
        _hitStunTimer -= deltaSeconds;
        if (_hitStunTimer <= 0)
        {
            _isHitStunned = false;
            return true;
        }
        return false;
    }
}