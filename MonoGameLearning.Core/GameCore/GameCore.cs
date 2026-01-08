using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;

namespace MonoGameLearning.Core.GameCore;

public class GameCore : Game
{
    internal static GameCore s_instance;

    public static GameCore Instance => s_instance;
    public static GraphicsDeviceManager Graphics { get; private set; }
    public static new GraphicsDevice GraphicsDevice { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }
    public static new ContentManager Content { get; private set; }
    public static OrthographicCamera Camera { get; private set; }
    public static BoxingViewportAdapter ViewportAdapter { get; private set; }

    public bool IsDebug { get; set; }
    public FramesPerSecondCounter FPSCounter { get; } = new();

    public GameCore(string title, int resolutionWidth, int resolutionHeight, int virtualWidth, int virtualHeight, bool fullScreen)
    {
        if (s_instance != null)
        {
            throw new InvalidOperationException("Only a single Core instance can be created");
        }

        s_instance = this;
        Graphics = new(this);
        Graphics.PreferredBackBufferWidth = resolutionWidth;
        Graphics.PreferredBackBufferHeight = resolutionHeight;
        Graphics.IsFullScreen = fullScreen;
        Graphics.ApplyChanges();

        Window.Title = title;
        Content = base.Content;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        ViewportAdapter = new(Window, Graphics.GraphicsDevice, virtualWidth, virtualHeight);
        Camera = new(ViewportAdapter);
    }

    protected override void Initialize()
    {
        base.Initialize();
        GraphicsDevice = base.GraphicsDevice;
        SpriteBatch = new(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        FPSCounter.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        FPSCounter.Draw(gameTime);
        base.Draw(gameTime);
    }
}
