using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.CoreGame;
using MonoGameLearning.Game.Entities;
using MonoGameLearning.Game.Sprites;
using MonoGameLearning.Input;

namespace MonoGameLearning.Game;

public class Game1 : CoreGame
{
    private PlayerEntity _player;
    private InputManager _input;


    public Game1() : base("Game Demo", 1280, 720, false)
    {

    }

    protected override void Initialize()
    {
        _input = new InputManager();
        _input.Action1Pressed += (sender, e) => { _player.Attack(); };
        _input.Action2Pressed += (sender, e) => { Window.Title = "Action2 was pressed"; };
        _input.Action3Pressed += (sender, e) => { Window.Title = "Action3 was pressed"; };
        _input.BackPressed += (sender, e) => { Exit(); };

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        AnimatedSprite playerSprite = PlayerSprite.GetPlayerSprite(Content);
        _player = new PlayerEntity(new Vector2(30, 30), 50, 50, playerSprite);
        _player.Scale = new Vector2(3, 3);
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
        SpriteBatch.Begin();
        SpriteBatch.Draw(_player.Sprite, _player.Position, _player.Rotation, _player.Scale);
        SpriteBatch.End();
        base.Draw(gameTime);
    }
}
