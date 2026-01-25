using System;
using System.Collections.Generic;
using Gum.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.GameCore;
using MonoGameLearning.Core.Input;
using MonoGameLearning.Game.Entities;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game;

public class GameLoop() : GameCore("Game Demo", 1280, 720, GAME_WIDTH, GAME_HEIGHT, false)
{
    public const int GAME_WIDTH = 800;
    public const int GAME_HEIGHT = 600;
    private PlayerEntity _player, _player1;
    private List<BackgroundEntity> _levelSegments;
    private List<ActorEntity> _actorEntities;
    private List<Entity> _entities;
    private InputManager _input;
    private TextRuntime _textInstance;
    private CollisionComponent _collision;
    private static GumService GumService => GumService.Default;

    protected override void Initialize()
    {
        _input = new InputManager();
        _input.Action1Pressed += (sender, e) => _player.Attack1();
        _input.Action2Pressed += (sender, e) => _player.Attack2();
        _input.Action3Pressed += (sender, e) => _player.Attack3();
        _input.BackPressed += (sender, e) => Exit();
        _input.DebugPressed += (sender, e) => ToggleDebug();
        _collision = new CollisionComponent(new RectangleF(0, 0, GAME_WIDTH, GAME_HEIGHT));

        GumService.Initialize(this, DefaultVisualsVersion.V3);
        _textInstance = new TextRuntime();
        _textInstance.AddToRoot();
        _textInstance.Visible = false;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        Sprite background = new Sprite(Content.Load<Texture2D>("backgrounds/background"));

        float bgCenterX = GAME_WIDTH / 2f;
        float bgCenterY = GAME_HEIGHT / 2f;
        var bg1 = new BackgroundEntity("bg1", background, new Vector2(bgCenterX, bgCenterY), GAME_WIDTH, GAME_HEIGHT);
        var bg2 = new BackgroundEntity("bg2", background, new Vector2(bgCenterX + GAME_WIDTH, bgCenterY), GAME_WIDTH, GAME_HEIGHT);
        _levelSegments = [bg1, bg2];

        AnimatedSprite playerSprite = PlayerSprite.GetPlayerSprite(Content);
        AnimatedSprite playerSprite1 = PlayerSprite.GetPlayerSprite(Content);
        _player = new PlayerEntity("player", new Vector2(30, 30), 2.0f, playerSprite);
        _player1 = new PlayerEntity("player1", new Vector2(75, 75), 2.0f, playerSprite1);
        _actorEntities = [_player, _player1];
        _entities = [.. _levelSegments, .. _actorEntities];

        foreach (var entity in _actorEntities)
        {
            _collision.Insert(entity);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update(gameTime);

        float totalLevelWidth = GAME_WIDTH * 2;
        float minX = GAME_WIDTH / 2f;
        float maxX = totalLevelWidth - (GAME_WIDTH / 2f);
        float clampedX = Math.Clamp(_player.Position.X, minX, maxX);
        Camera.LookAt(new Vector2(clampedX, GAME_HEIGHT / 2f));

        _player.MovementDirection = _input.MovementDirection;
        _player.MovementBounds = Camera.BoundingRectangle;

        foreach (var entity in _entities)
        {
            entity.Update(gameTime);
        }
        _collision.Update(gameTime);
        GumService.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        Matrix transformMatrix = Camera.GetViewMatrix();
        SpriteBatch.Begin(transformMatrix: transformMatrix);

        var cameraBounds = Camera.BoundingRectangle;
        foreach (var bg in _levelSegments)
        {
            if (cameraBounds.Intersects(bg.Frame))
            {
                bg.Draw(SpriteBatch);
            }
        }

        foreach (var entity in _actorEntities)
        {
            entity.Draw(SpriteBatch);
        }
        if (IsDebug)
        {
            foreach (var entity in _actorEntities)
            {
                entity.DrawDebug(SpriteBatch);
            }
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
