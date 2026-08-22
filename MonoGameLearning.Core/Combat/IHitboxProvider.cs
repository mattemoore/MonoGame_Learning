using MonoGameLearning.Core.Movement;

namespace MonoGameLearning.Core.Combat;

public interface IHitboxProvider
{
    MoveData? CurrentMove { get; set; }
    HitboxService? HitboxService { get; set; }
    FacingDirection Direction { get; set; }
}
