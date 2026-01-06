using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.GameCore;
using MonoGameLearning.Core.Input;
using MonoGameLearning.Game.Entities;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game;

public class GameLoop : GameCore
{
    public const int GAME_WIDTH = 800;
    public const int GAME_HEIGHT = 600;

    private PlayerEntity _player;
    private InputManager _input;


    public GameLoop() : base("Game Demo", 1280, 720, GAME_WIDTH, GAME_HEIGHT, false)
    {

    }

    protected override void Initialize()
    {
        _input = new InputManager();
        _input.Action1Pressed += (sender, e) => { _player.Attack1(); };
        _input.Action2Pressed += (sender, e) => { _player.Attack2(); };
        _input.Action3Pressed += (sender, e) => { _player.Attack3(); };
        _input.BackPressed += (sender, e) => { Exit(); };

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        AnimatedSprite playerSprite = PlayerSprite.GetPlayerSprite(Content);
        _player = new PlayerEntity(new Vector2(30, 30), 50, 50, playerSprite);
        _player.Rotation = 0f;
        _player.Scale = new Vector2(5, 5);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update(gameTime);
        _player.MovementDirection = _input.MovementDirection;
        _player.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        Matrix transformMatrix = Camera.GetViewMatrix();
        SpriteBatch.Begin(transformMatrix: transformMatrix);
        SpriteBatch.DrawRectangle(new RectangleF(new Vector2(0, 0), new SizeF(GAME_WIDTH, GAME_HEIGHT)), Color.AntiqueWhite);
        SpriteBatch.Draw(_player.Sprite, _player.Position, MathHelper.ToRadians(_player.Rotation), _player.Scale);
        SpriteBatch.End();
        base.Draw(gameTime);
    }
}
