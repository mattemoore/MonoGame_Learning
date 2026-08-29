using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.UI;
using MonoGameLearning.Game.Entities.Player;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class PlayerHudTests
{
    [Test]
    public void PlayerHud_Respawn_GrantsInvincibility()
    {
        var player = new PlayerEntityTester("test", Vector2.Zero, 1f);
        Assert.That(player.IsInvincible, Is.False);
        player.Respawn();
        Assert.That(player.IsInvincible, Is.True);
    }

    [Test]
    public void PlayerHud_IsInvincible_Default_False()
    {
        var player = new PlayerEntityTester("test", Vector2.Zero, 1f);
        Assert.That(player.IsInvincible, Is.False);
    }

    [Test]
    public void PlayerHud_Implements_IHudPlayerData()
    {
        var player = new PlayerEntityTester("test", Vector2.Zero, 1f);
        Assert.That(player, Is.InstanceOf<IHudPlayerData>());
    }

    [Test]
    public void PlayerHud_IHudPlayerData_ReportsInvincibility()
    {
        var player = new PlayerEntityTester("test", Vector2.Zero, 1f);
        var hudData = (IHudPlayerData)player;
        Assert.That(hudData.IsInvincible, Is.False);
        player.Respawn();
        Assert.That(hudData.IsInvincible, Is.True);
    }

    [Test]
    public void PlayerHud_IHudPlayerData_ReportsHealth()
    {
        var player = new PlayerEntityTester("test", Vector2.Zero, 1f);
        var hudData = (IHudPlayerData)player;
        Assert.That(hudData.Health, Is.EqualTo(hudData.MaxHealth));
    }

    [Test]
    public void PlayerHud_IHudPlayerData_ReportsName()
    {
        var player = new PlayerEntityTester("Cody", Vector2.Zero, 1f);
        var hudData = (IHudPlayerData)player;
        Assert.That(hudData.Name, Is.EqualTo("Cody"));
    }

    // --- Respawn position computation ---

    [Test]
    public void ComputeRespawnPoint_IsCameraRelative()
    {
        var bounds = new RectangleF(0, 100, 2000, 500);
        var result = RespawnTestHelper.ComputeRespawnPosition(2000f, bounds, 100f);
        float expectedX = Math.Clamp(2000f + 60f, 0f + 10f, 2000f - 10f);
        Assert.That(result.X, Is.EqualTo(expectedX));
        Assert.That(result.Y, Is.EqualTo(100f));
    }

    [Test]
    public void ComputeRespawnPoint_ClampsToLevelLeft()
    {
        var bounds = new RectangleF(100, 100, 2000, 500);
        var result = RespawnTestHelper.ComputeRespawnPosition(-500f, bounds, 100f);
        Assert.That(result.X, Is.EqualTo(110f)); // levelLeft + LEVEL_EDGE_BUFFER
    }

    [Test]
    public void ComputeRespawnPoint_ClampsToLevelRight()
    {
        var bounds = new RectangleF(0, 100, 2000, 500);
        var result = RespawnTestHelper.ComputeRespawnPosition(10000f, bounds, 100f);
        Assert.That(result.X, Is.EqualTo(1990f)); // levelRight - LEVEL_EDGE_BUFFER
    }

    [Test]
    public void ComputeRespawnPoint_WithinMidLevel_NoClamping()
    {
        var bounds = new RectangleF(0, 100, 2000, 500);
        var result = RespawnTestHelper.ComputeRespawnPosition(500f, bounds, 100f);
        Assert.That(result.X, Is.EqualTo(560f)); // 500 + SPAWN_BUFFER_X
    }
}