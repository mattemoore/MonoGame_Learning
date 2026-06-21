using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;

namespace MonoGameLearning.Core.GameCore;

public class GameCore : Game
{
    /// <summary>
    /// Singleton instance reference. MonoGame Game is single-instance per process.
    /// </summary>
    internal static GameCore s_instance;

    /// <summary>Global access to the GameCore singleton.</summary>
    public static GameCore Instance => s_instance;
    /// <summary>Global GraphicsDeviceManager for resolution/window config.</summary>
    public static GraphicsDeviceManager Graphics { get; private set; }
    /// <summary>
    /// Globally accessible GraphicsDevice. <c>new</c> silences CS0108 — intentionally shadows
    /// <see cref="Game.GraphicsDevice"/> to expose it as a static (standard MonoGame pattern).
    /// </summary>
    public static new GraphicsDevice GraphicsDevice { get; private set; }
    /// <summary>Globally accessible SpriteBatch for rendering.</summary>
    public static SpriteBatch SpriteBatch { get; private set; }
    /// <summary>
    /// Globally accessible ContentManager. <c>new</c> silences CS0108 — intentionally shadows
    /// <see cref="Game.Content"/> to expose it as a static (standard MonoGame pattern).
    /// </summary>
    public static new ContentManager Content { get; private set; }
    /// <summary>Globally accessible OrthographicCamera for view transforms.</summary>
    public static OrthographicCamera Camera { get; private set; }
    /// <summary>Globally accessible BoxingViewportAdapter for resolution independence.</summary>
    public static BoxingViewportAdapter ViewportAdapter { get; private set; }
    /// <summary>Globally accessible debug SpriteFont for in-world debug text.</summary>
    public static SpriteFont DebugFont { get; set; }

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

        Graphics = new(this)
        {
            PreferredBackBufferWidth = resolutionWidth,
            PreferredBackBufferHeight = resolutionHeight,
            IsFullScreen = fullScreen,
            HardwareModeSwitch = false
        };
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