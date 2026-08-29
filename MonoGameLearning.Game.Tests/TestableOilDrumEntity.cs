using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Game.Entities.Props;

namespace MonoGameLearning.Game.Tests;

internal sealed class TestableOilDrumEntity(string name)
    : OilDrumEntity(name, Vector2.Zero, 64, 64, new AudioService())
{
    public int CurrentHealth => HealthComponent.Value;
    public bool IsAliveExposed => HealthComponent.IsAlive;
}