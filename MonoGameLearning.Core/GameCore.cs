using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;
using MonoGameLearning.Core.UI;

namespace MonoGameLearning.Core;

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

    public GumManager Gum { get; }

    public bool IsDebug { get; set; }
    public FramesPerSecondCounter FPSCounter { get; } = new();

    private readonly int _virtualWidth;
    private readonly int _virtualHeight;

    public GameCore(string title, int resolutionWidth, int resolutionHeight, int virtualWidth, int virtualHeight, bool fullScreen)
    {
        if (s_instance != null)
            throw new InvalidOperationException("Only a single Core instance can be created");

        s_instance = this;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;

        Graphics = new(this)
        {
            PreferredBackBufferWidth = resolutionWidth,
            PreferredBackBufferHeight = resolutionHeight,
            IsFullScreen = fullScreen,
            HardwareModeSwitch = false
        };

        Gum = new GumManager();

        Window.Title = title;
        Window.AllowUserResizing = true;
        Content = base.Content;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        Graphics.ApplyChanges();
        GraphicsDevice = base.GraphicsDevice;
        ViewportAdapter = new(Window, GraphicsDevice, _virtualWidth, _virtualHeight);
        Camera = new(ViewportAdapter);
        SpriteBatch = new(GraphicsDevice);
        Gum.Initialize(this);
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