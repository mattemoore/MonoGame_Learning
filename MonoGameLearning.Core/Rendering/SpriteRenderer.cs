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

    /// <summary>The animation key most recently applied through <see cref="SetAnimation"/>.</summary>
    public string? CurrentAnimationKey { get; private set; }

    /// <summary>
    /// The animation-relative frame index, starting at 0 in <see cref="SetAnimation"/> and
    /// incrementing on every frame change (growing past the frame count while an animation
    /// loops). This is the clock for per-frame pose data such as hand anchors and weapon
    /// swing frames. It is NOT the atlas region index: MonoGame.Extended's
    /// <c>AnimationController.CurrentFrame</c> returns
    /// <c>TextureAtlas.GetIndexOfRegion(...)</c>, so using it here would index poses by
    /// their position in the atlas. Distinct from <see cref="AnimationFrameTracker"/>,
    /// which only counts new-frame events.
    /// </summary>
    public int AnimationFrame { get; private set; }

    public void Render(SpriteBatch spriteBatch, Vector2 position, float rotation)
    {
        Debug.Assert(Sprite is not null, "SpriteRenderer has no Sprite assigned");
        if (Sprite is null) return;
        spriteBatch.Draw(Sprite, position, MathHelper.ToRadians(rotation), new Vector2(Scale));
    }

    public void SetAnimation(string key)
    {
        CurrentAnimationKey = key;
        AnimationFrame = 0;
        if (Sprite is { } s) s.SetAnimation(key);
    }

    public void Update(GameTime gameTime)
    {
        if (Sprite is not { } s) return;
        var controller = s.Controller;
        int before = controller.CurrentFrame;
        s.Update(gameTime);
        // A non-looping animation raises AnimationCompleted synchronously inside Update and a
        // handler may SetAnimation, replacing Controller: that is a new animation starting at
        // its first frame, not a frame advance of the old one.
        if (ReferenceEquals(controller, s.Controller))
            ObserveFrameChange(before, s.Controller.CurrentFrame);
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
        if (Sprite is not { } s) return;
        var controller = s.Controller;
        int before = controller.CurrentFrame;
        tracker.AdvanceOnFrameChange(s, gameTime);
        if (ReferenceEquals(controller, s.Controller))
            ObserveFrameChange(before, s.Controller.CurrentFrame);
    }

    /// <summary>
    /// Advances <see cref="AnimationFrame"/> when the sprite actually changed frame. Called
    /// with the atlas frame before/after an update (split out so the pose-clock contract is
    /// testable without a graphics device).
    /// </summary>
    internal void ObserveFrameChange(int atlasFrameBefore, int atlasFrameAfter)
    {
        if (atlasFrameAfter != atlasFrameBefore) AnimationFrame++;
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