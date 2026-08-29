namespace MonoGameLearning.Core.UI;

public interface IHudPlayerData
{
    string Name { get; }
    bool IsInvincible { get; }
    int Health { get; }
    int MaxHealth { get; }
}