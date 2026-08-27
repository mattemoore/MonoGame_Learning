using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.UI;
using MonoGameLearning.Game.Entities.Player;

namespace MonoGameLearning.Game.Tests;

class PlayerEntityTester(string name, Vector2 position, float scale)
    : PlayerEntity(name, position, scale, null!, null!)
{
    protected override PlayerStateController CreateStateController()
    {
        return new PlayerStateController(new()
        {
            OnAttackingExit = Callbacks.OnAttackingExit,
            OnHurtEntry = Callbacks.OnHurtEntry,
            OnHurtExit = Callbacks.OnHurtExit,
            OnKnockdownEntry = Callbacks.OnKnockdownEntry,
            OnKnockdownExit = Callbacks.OnKnockdownExit,
            OnDyingEntry = Callbacks.OnDyingEntry,
            OnDyingExit = Callbacks.OnDyingExit,
            OnDeadEntry = Callbacks.OnDeadEntry,
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

[TestFixture]
class PlayerEntityInvulnerabilityTests
{
    private static PlayerEntity CreatePlayer()
    {
        return new PlayerEntityTester("Test", Vector2.Zero, 1f);
    }

    private static GameTime ZeroGameTime => new(TimeSpan.Zero, TimeSpan.Zero);

    [Test]
    public void IsInvulnerable_Default_False()
    {
        var player = CreatePlayer();
        Assert.That(player.IsInvincible, Is.False);
    }

    [Test]
    public void IsInvulnerable_True_AfterHit()
    {
        var player = CreatePlayer();
        player.TakeDamage(new DamageInfo { Amount = 1, Knockdown = false });
        Assert.That(player.IsInvincible, Is.True);
    }

    [Test]
    public void IsInvulnerable_True_AfterRespawn()
    {
        var player = CreatePlayer();
        player.Respawn();
        Assert.That(player.IsInvincible, Is.True);
    }

    [Test]
    public void IsInvulnerable_False_AfterTimerExpires()
    {
        var player = CreatePlayer();
        player.Respawn();

        var elapsed = TimeSpan.FromSeconds(2.5f);
        for (int i = 0; i < 10; i++)
            player.Update(new GameTime(elapsed / 10, elapsed / 10));

        Assert.That(player.IsInvincible, Is.False);
    }

    [Test]
    public void IncapacitatedUpdate_HeadlessNullSprite_DoesNotThrow()
    {
        var player = CreatePlayer();
        player.TakeDamage(new DamageInfo { Amount = 1, Knockdown = false });

        Assert.DoesNotThrow(() => player.Update(ZeroGameTime));
        Assert.That(player.IsInvincible, Is.True);
    }
}