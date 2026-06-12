using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IAnimated
{
    AnimatedSprite Sprite { get; }
    void ResetAnimationFrameIndex();
}
