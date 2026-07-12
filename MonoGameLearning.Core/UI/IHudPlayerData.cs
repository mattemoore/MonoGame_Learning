namespace MonoGameLearning.Core.UI;

public interface IHudPlayerData
{
    string Name { get; }
    int Lives { get; }
    bool IsInvincible { get; }
    int Health { get; }
    int MaxHealth { get; }
}