using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Settings;

public static class SettingsService
{
    public static void Apply(GraphicsDeviceManager graphics, ResolutionSetting setting)
    {
        graphics.PreferredBackBufferWidth = setting.Width;
        graphics.PreferredBackBufferHeight = setting.Height;
        graphics.IsFullScreen = false;
        graphics.ApplyChanges();
    }
}