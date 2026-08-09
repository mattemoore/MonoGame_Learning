using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Animation;

public interface IAnimated
{
    AnimatedSprite Sprite { get; }
    void ResetAnimationFrameIndex();
}
