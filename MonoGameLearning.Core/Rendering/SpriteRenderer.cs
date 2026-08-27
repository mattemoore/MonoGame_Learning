using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Animation;

namespace MonoGameLearning.Core.Rendering;

public class SpriteRenderer(AnimatedSprite? sprite, float scale)
{
    public AnimatedSprite? Sprite { get; set; } = sprite;
    public float Scale { get; set; } = scale;

    public void Render(SpriteBatch spriteBatch, Vector2 position, float rotation)
    {
        Debug.Assert(Sprite is not null, "SpriteRenderer has no Sprite assigned");
        if (Sprite is null) return;
        spriteBatch.Draw(Sprite, position, MathHelper.ToRadians(rotation), new Vector2(Scale));
    }

    public void SetAnimation(string key)
    {
        if (Sprite is { } s) s.SetAnimation(key);
    }

    public void Update(GameTime gameTime)
    {
        if (Sprite is { } s) s.Update(gameTime);
    }

    public void SetEffect(SpriteEffects effect)
    {
        if (Sprite is { } s) s.Effect = effect;
    }

    public void SetColor(Color color)
    {
        if (Sprite is { } s) s.Color = color;
    }

    public void AdvanceFrame(AnimationFrameTracker tracker, GameTime gameTime)
    {
        if (Sprite is { } s) tracker.AdvanceOnFrameChange(s, gameTime);
    }

    public void SubscribeAnimationEvents(Action<IAnimationController, AnimationEventTrigger> handler)
    {
        if (Sprite is { } s) s.Controller.OnAnimationEvent += handler;
    }

    public void UnsubscribeAnimationEvents(Action<IAnimationController, AnimationEventTrigger> handler)
    {
        if (Sprite is { } s) s.Controller.OnAnimationEvent -= handler;
    }
}