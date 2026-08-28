using Microsoft.Xna.Framework;
using MonoGameLearning.Core.AI;

namespace MonoGameLearning.Game.Tests;

sealed class FacingChangeForcingEnemy : TestEnemyEntity
{
    public FacingChangeForcingEnemy() : base("FacingChangeForcer", Vector2.Zero) { }

    protected override void ApplyFacingFromResult(in AIUpdateResult result)
    {
        base.ApplyFacingFromResult(new AIUpdateResult { FacingChanged = true, NewFacingX = -1 });
    }
}