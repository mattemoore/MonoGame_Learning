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

    /// <summary>
    /// Creates a new Core instance.
    /// </summary>
    /// <param name="title">The title to display in the title bar of the game window.</param>
    /// <param name="resolutionWidth">The initial width, in pixels, of the game resolution.</param>
    /// <param name="resolutionHeight">The initial height, in pixels, of the game resolution.</param>
    /// <param name="virtualWidth">The initial width of the viewport adapter</param>
    /// <param name="virtualHeight">The initial height of the viewport adapter</param>
    /// <param name="fullScreen">Indicates if the game should start in fullscreen mode.</param>
    public GameCore(string title, int resolutionWidth, int resolutionHeight, int virtualWidth, int virtualHeight, bool fullScreen)
    {
        // Ensure that multiple cores are not created.
        if (s_instance != null)
        {
            throw new InvalidOperationException($"Only a single Core instance can be created");
        }

        // Store reference to engine for global member access.
        s_instance = this;

        // Create a new graphics device manager.
        Graphics = new GraphicsDeviceManager(this);

        // Set the graphics defaults.
        Graphics.PreferredBackBufferWidth = resolutionWidth;
        Graphics.PreferredBackBufferHeight = resolutionHeight;
        Graphics.IsFullScreen = fullScreen;

        // Apply the graphic presentation changes.
        Graphics.ApplyChanges();

        // Set the window title.
        Window.Title = title;

        // Set the core's content manager to a reference of the base Game's
        // content manager.
        Content = base.Content;

        // Set the root directory for content.
        Content.RootDirectory = "Content";

        // Mouse is visible by default.
        IsMouseVisible = true;

        ViewportAdapter = new BoxingViewportAdapter(Window, Graphics.GraphicsDevice, virtualWidth, virtualHeight);
        Camera = new OrthographicCamera(ViewportAdapter);
    }

    protected override void Initialize()
    {
        base.Initialize();

        // Set the core's graphics device to a reference of the base Game's
        // graphics device.
        GraphicsDevice = base.GraphicsDevice;

        // Create the sprite batch instance.
        SpriteBatch = new SpriteBatch(GraphicsDevice);
    }
}
