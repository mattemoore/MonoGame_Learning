using System;
using System.Collections.Generic;
using System.Linq;
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
using MonoGameLearning.Game.Entities.Player;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.Sprites;
using RenderingLibrary.Graphics;

namespace MonoGameLearning.Game.GameLoop;

public class GameLoop() : GameCore("Game Demo", RESOLUTION_WIDTH, RESOLUTION_HEIGHT, GAME_WIDTH, GAME_HEIGHT, IS_FULL_SCREEN)
{
    public const int GAME_WIDTH = 800;
    public const int GAME_HEIGHT = 600;
    public const int RESOLUTION_WIDTH = 1024;
    public const int RESOLUTION_HEIGHT = 768;
    public const bool IS_FULL_SCREEN = false;
    private PlayerEntity _player, _player1;
    private Level _currentLevel;
    private List<ActorEntity> _actorEntities;
    private List<Entity> _entities;
    private InputManager _input;
    private TextRuntime _debugWindow1, _debugWindow2;
    private CollisionComponent _collision;
    private static GumService GumService => GumService.Default;
    private int _numBackgroundsDrawn, _numEntitiesDrawn;

    private GameStateController _gameState;
    private CameraController _cameraController;
    private MenuManager _menuManager;

    protected override void Initialize()
    {
        _input = new InputManager();
        _input.ActionTriggered += OnActionTriggered;
        _collision = new CollisionComponent(new RectangleF(0, 0, GAME_WIDTH * 2, GAME_HEIGHT));

        _gameState = new GameStateController();
        _gameState.StateMachine.OnTransitioned(t =>
        {
            _menuManager.OnGameStateChanged();
            if (t.Destination == GameState.Playing && t.Source != GameState.Paused)
            {
                ResetGame();
            }
        });

        _menuManager = new MenuManager(_gameState, Exit);

        GumService.Initialize(this, DefaultVisualsVersion.V3);
        _debugWindow1 = new TextRuntime();
        _debugWindow1.AddToRoot();
        _debugWindow1.Visible = false;
        _debugWindow1.Anchor(Gum.Wireframe.Anchor.TopLeft);

        _debugWindow2 = new TextRuntime();
        _debugWindow2.AddToRoot();
        _debugWindow2.Visible = false;
        _debugWindow2.Anchor(Gum.Wireframe.Anchor.TopRight);
        _debugWindow2.Width = 200;
        _debugWindow2.Height = 200;
        _debugWindow2.X = -200;
        _debugWindow2.HorizontalAlignment = HorizontalAlignment.Right;

        _menuManager.BuildScreens();
        _menuManager.OnGameStateChanged();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _currentLevel = new Level1(Content, GAME_WIDTH, GAME_HEIGHT);

        AnimatedSprite playerSprite = PlayerSprite.GetPlayerSprite(Content);
        AnimatedSprite playerSprite1 = PlayerSprite.GetPlayerSprite(Content);
        _player = new PlayerEntity("player", new Vector2(100, 450), 2.0f, playerSprite);
        _player.Died += OnPlayerDied;
        _player1 = new PlayerEntity("player1", new Vector2(150, 500), 2.0f, playerSprite1);
        _actorEntities = [_player, _player1];
        _entities = [.. _actorEntities];

        _cameraController = new CameraController(_player, GAME_WIDTH, GAME_HEIGHT, GAME_WIDTH * 2);

        foreach (var entity in _actorEntities)
        {
            _collision.Insert(entity);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Mode = _gameState.State == GameState.Playing ? InputMode.Gameplay : InputMode.Menu;
        _input.Update(gameTime);

        if (_gameState.State == GameState.Playing)
        {
            _cameraController.Update(Camera);

            _player.MovementDirection = _input.MovementDirection;
            foreach (var entity in _actorEntities)
            {
                entity.MovementBounds = _currentLevel.MovementBounds;
            }

            _currentLevel.Update(gameTime);

            foreach (var entity in _entities)
            {
                entity.Update(gameTime);
            }
            _collision.Update(gameTime);
            foreach (var entity in _actorEntities)
            {
                entity.ClampToBounds();
            }
        }

        GumService.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _numBackgroundsDrawn = 0;
        _numEntitiesDrawn = 0;

        GraphicsDevice.Clear(Color.Black);

        if (_gameState.State is GameState.Playing or GameState.Paused or GameState.GameOver or GameState.LevelComplete)
        {
            SpriteBatch.Begin(transformMatrix: Camera.GetViewMatrix());

            _numBackgroundsDrawn = _currentLevel.Draw(SpriteBatch, Camera);

            var cameraBounds = Camera.BoundingRectangle;

            foreach (var entity in _actorEntities)
            {
                if (cameraBounds.Intersects(entity.Frame))
                {
                    entity.Draw(SpriteBatch);
                    _numEntitiesDrawn++;
                }
            }

            if (IsDebug)
            {
                foreach (var entity in _actorEntities)
                {
                    entity.DrawDebug(SpriteBatch);
                }
                _currentLevel.DrawDebug(SpriteBatch);
                _debugWindow1.Text = $"FPS: {FPSCounter.FramesPerSecond}\n" +
                                     $"State: {_gameState.State}\n" +
                                     $"Viewport: Virtual-{ViewportAdapter.VirtualWidth}x{ViewportAdapter.VirtualHeight} Actual-{ViewportAdapter.ViewportWidth}x{ViewportAdapter.ViewportHeight}\n" +
                                     $"Screen Buffer: {Graphics.PreferredBackBufferWidth}x{Graphics.PreferredBackBufferHeight}\n" +
                                     $"Window: {Window.ClientBounds.Width}x{Window.ClientBounds.Height}";
                _debugWindow2.Text = $"BGs draw: {_numBackgroundsDrawn}\n" +
                                     $"Ents draw: {_numEntitiesDrawn}";
            }
            SpriteBatch.End();
        }

        GumService.Draw();
        base.Draw(gameTime);
    }

    private void OnPlayerDied(object sender, EventArgs e)
    {
        _gameState.Fire(GameTrigger.PlayerDied);
    }

    private void OnActionTriggered(InputAction action)
    {
        switch (action)
        {
            case InputAction.Action1:
                if (_gameState.State == GameState.Playing) _player.Attack1();
                break;
            case InputAction.Action2:
                if (_gameState.State == GameState.Playing) _player.Attack2();
                break;
            case InputAction.Action3:
                if (_gameState.State == GameState.Playing) _player.Attack3();
                break;
            case InputAction.Back:
                _menuManager.HandleBack();
                break;
            case InputAction.Debug:
                ToggleDebug();
                break;
            case InputAction.Confirm:
                _menuManager.HandleConfirm();
                break;
            case InputAction.DebugKill:
                if (IsDebug && _gameState.State == GameState.Playing) _player?.TakeDamage(9999);
                break;
            case InputAction.DebugComplete:
                if (IsDebug && _gameState.State == GameState.Playing) _gameState.Fire(GameTrigger.CompleteLevel);
                break;
            case InputAction.MenuUp:
                _menuManager.HandleMenuNavigation(-1);
                break;
            case InputAction.MenuDown:
                _menuManager.HandleMenuNavigation(1);
                break;
        }
    }

    private void ToggleDebug()
    {
        IsDebug = !IsDebug;
        _debugWindow1.Visible = !_debugWindow1.Visible;
        _debugWindow2.Visible = !_debugWindow2.Visible;
    }

    private void ResetGame()
    {
        _player.Reset(new Vector2(100, 450));
        _player1.Reset(new Vector2(150, 500));
        _entities = [.. _actorEntities];
        _currentLevel = new Level1(Content, GAME_WIDTH, GAME_HEIGHT);
    }
}