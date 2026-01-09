using Gum.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using MonoGameLearning.Core.GameCore;
using MonoGameLearning.Core.Input;
using MonoGameLearning.Game.Entities;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game;

public class GameLoop() : GameCore("Game Demo", 1280, 720, GAME_WIDTH, GAME_HEIGHT, false)
{
    public const int GAME_WIDTH = 800;
    public const int GAME_HEIGHT = 600;

    private PlayerEntity _player;
    private InputManager _input;
    private TextRuntime _textInstance;
    private static GumService GumService => GumService.Default;

    protected override void Initialize()
    {
        _input = new InputManager();
        _input.Action1Pressed += (sender, e) => _player.Attack1();
        _input.Action2Pressed += (sender, e) => _player.Attack2();
        _input.Action3Pressed += (sender, e) => _player.Attack3();
        _input.BackPressed += (sender, e) => Exit();
        _input.DebugPressed += (sender, e) => ToggleDebug();

        GumService.Initialize(this, DefaultVisualsVersion.V3);
        _textInstance = new TextRuntime();
        _textInstance.AddToRoot();
        _textInstance.Visible = false;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        AnimatedSprite playerSprite = PlayerSprite.GetPlayerSprite(Content);
        _player = new PlayerEntity(new Vector2(30, 30), 300, 150, playerSprite);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update(gameTime);
        _player.MovementDirection = _input.MovementDirection;
        _player.MovementBounds = Camera.BoundingRectangle;
        _player.Update(gameTime);
        GumService.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        Matrix transformMatrix = Camera.GetViewMatrix();
        SpriteBatch.Begin(transformMatrix: transformMatrix);
        _player.Draw(SpriteBatch);
        if (IsDebug)
        {
            SpriteBatch.DrawRectangle(_player.Bounds, Color.AntiqueWhite);
            _textInstance.Text = "FPS: " + FPSCounter.FramesPerSecond;
        }
        SpriteBatch.End();
        GumService.Draw();
        base.Draw(gameTime);
    }

    private void ToggleDebug()
    {
        IsDebug = !IsDebug;
        _textInstance.Visible = !_textInstance.Visible;
    }
}
