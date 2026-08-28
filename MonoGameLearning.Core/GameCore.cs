using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;
using MonoGameLearning.Core.UI;

namespace MonoGameLearning.Core;

public class GameCore : Game
{
    public GraphicsDeviceManager Graphics { get; }
    public SpriteBatch SpriteBatch { get; private set; } = null!;
    public OrthographicCamera Camera { get; private set; } = null!;
    public BoxingViewportAdapter ViewportAdapter { get; private set; } = null!;

    public GumUiService Gum { get; }

    public bool IsDebug { get; set; }
    public FramesPerSecondCounter FPSCounter { get; } = new();

    private readonly int _virtualWidth;
    private readonly int _virtualHeight;

    public GameCore(string title, int resolutionWidth, int resolutionHeight, int virtualWidth, int virtualHeight, bool fullScreen)
    {
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;

        Graphics = new(this)
        {
            PreferredBackBufferWidth = resolutionWidth,
            PreferredBackBufferHeight = resolutionHeight,
            IsFullScreen = fullScreen,
            HardwareModeSwitch = false
        };

        Gum = new GumUiService();

        Window.Title = title;
        Window.AllowUserResizing = true;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        Graphics.ApplyChanges();
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