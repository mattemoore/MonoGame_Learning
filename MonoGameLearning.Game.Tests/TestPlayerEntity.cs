using Microsoft.Xna.Framework;
using MonoGameLearning.Core.UI;
using MonoGameLearning.Game.Entities.Player;

namespace MonoGameLearning.Game.Tests;

class PlayerEntityTester(string name, Vector2 position, float scale)
    : PlayerEntity(name, position, scale, null!)
{
    protected override PlayerStateController CreateStateController()
    {
        return new PlayerStateController(new()
        {
            OnAttackingExit = AttackingExit(),
            OnHurtEntry = HurtEntry(),
            OnHurtExit = HurtExit(),
            OnKnockdownEntry = KnockdownEntry(),
            OnKnockdownExit = KnockdownExit(),
            OnDyingEntry = DyingEntry(),
            OnDyingExit = DyingExit(),
            OnDeadEntry = DeadEntry(),
        });
    }
}

class StubHudPlayerData : IHudPlayerData
{
    public string Name { get; set; } = "Cody";
    public int Lives { get; set; } = 3;
    public bool IsInvincible { get; set; }
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
}