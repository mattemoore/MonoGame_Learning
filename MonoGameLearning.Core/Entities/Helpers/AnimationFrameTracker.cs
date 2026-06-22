using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities.Helpers;

public class AnimationFrameTracker
{
    private int _frameIndex;
    private int _lastRegisteredFrame = -1;

    public void Reset()
    {
        _frameIndex = 0;
        _lastRegisteredFrame = 0;
    }

    public void AdvanceOnFrameChange(AnimatedSprite sprite, GameTime gameTime)
    {
        int oldAtlasFrame = sprite.Controller.CurrentFrame;
        sprite.Update(gameTime);
        if (sprite.Controller.CurrentFrame != oldAtlasFrame)
            _frameIndex++;
    }

    public int FrameIndex => _frameIndex;

    public bool TryGetNewFrame(out int newFrameIndex)
    {
        if (_frameIndex == _lastRegisteredFrame)
        {
            newFrameIndex = -1;
            return false;
        }

        var frameDelta = _frameIndex - _lastRegisteredFrame;
        Debug.Assert(frameDelta > 0, "Frame counter should only advance forward");
        if (frameDelta > 1)
            Debug.WriteLine($"[AnimationFrameTracker] Skipped {frameDelta - 1} animation frame(s) — hitboxes not registered for intermediate frames");

        _lastRegisteredFrame = _frameIndex;
        newFrameIndex = _frameIndex;
        return true;
    }
}