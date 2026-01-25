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

    private readonly int _virtualWidth;
    private readonly int _virtualHeight;

    public GameCore(string title, int resolutionWidth, int resolutionHeight, int virtualWidth, int virtualHeight, bool fullScreen)
    {
        if (s_instance != null)
        {
            throw new InvalidOperationException("Only a single Core instance can be created");
        }

        s_instance = this;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;

        Graphics = new(this);
        Graphics.PreferredBackBufferWidth = resolutionWidth;
        Graphics.PreferredBackBufferHeight = resolutionHeight;
        Graphics.IsFullScreen = fullScreen;
        Graphics.HardwareModeSwitch = false;
        Graphics.ApplyChanges();

        Window.Title = title;
        Window.AllowUserResizing = true;
        Content = base.Content;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        GraphicsDevice = base.GraphicsDevice;
        ViewportAdapter = new(Window, GraphicsDevice, _virtualWidth, _virtualHeight);
        Camera = new(ViewportAdapter);
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