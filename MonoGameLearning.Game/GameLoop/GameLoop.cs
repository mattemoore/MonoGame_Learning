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
    private ContainerRuntime _titleScreen, _pauseScreen, _gameOverScreen, _levelCompleteScreen;
    private int _menuIndex;
    private List<TextRuntime> _activeMenuItems;

    protected override void Initialize()
    {
        _input = new InputManager();
        _input.Action1Pressed += (_, _) => { if (_gameState.State == GameState.Playing) _player.Attack1(); };
        _input.Action2Pressed += (_, _) => { if (_gameState.State == GameState.Playing) _player.Attack2(); };
        _input.Action3Pressed += (_, _) => { if (_gameState.State == GameState.Playing) _player.Attack3(); };
        _input.BackPressed += (_, _) => OnBackPressed();
        _input.DebugPressed += (_, _) => ToggleDebug();
        _input.MenuNavigated += dir => OnMenuNavigated(dir);
        _input.ConfirmPressed += (_, _) => OnConfirmPressed();
        _input.DebugKillPressed += (_, _) => { if (IsDebug && _gameState.State == GameState.Playing) _player.TakeDamage(9999); };
        _input.DebugCompletePressed += (_, _) => { if (IsDebug && _gameState.State == GameState.Playing) _gameState.Fire(GameTrigger.CompleteLevel); };
        _collision = new CollisionComponent(new RectangleF(0, 0, GAME_WIDTH, GAME_HEIGHT));

        _gameState = new GameStateController();
        _gameState.StateMachine.OnTransitioned(t =>
        {
            OnGameStateChanged();
            if (t.Destination == GameState.Playing && t.Source != GameState.Paused)
            {
                ResetGame();
            }
        });

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

        BuildScreens();
        OnGameStateChanged();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _currentLevel = new Level1(Content, GAME_WIDTH, GAME_HEIGHT);

        AnimatedSprite playerSprite = PlayerSprite.GetPlayerSprite(Content);
        AnimatedSprite playerSprite1 = PlayerSprite.GetPlayerSprite(Content);
        _player = new PlayerEntity("player", new Vector2(100, 450), 2.0f, playerSprite);
        _player.Died += (_, _) => _gameState.Fire(GameTrigger.PlayerDied);
        _player1 = new PlayerEntity("player1", new Vector2(150, 500), 2.0f, playerSprite1);
        _actorEntities = [_player, _player1];
        _entities = [.. _actorEntities];

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
            float totalLevelWidth = GAME_WIDTH * 2;
            float minX = GAME_WIDTH / 2f;
            float maxX = totalLevelWidth - (GAME_WIDTH / 2f);
            float clampedX = Math.Clamp(_player.Position.X, minX, maxX);
            Camera.LookAt(new Vector2(clampedX, GAME_HEIGHT / 2f));

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

    // --- Input handlers ---

    private void OnBackPressed()
    {
        switch (_gameState.State)
        {
            case GameState.Playing:
                _gameState.Fire(GameTrigger.PauseToggle);
                break;
            case GameState.Paused:
                _gameState.Fire(GameTrigger.PauseToggle);
                break;
            case GameState.TitleScreen:
                Exit();
                break;
        }
    }

    private void OnMenuNavigated(Vector2 direction)
    {
        if (_gameState.State == GameState.Playing) return;
        if (_activeMenuItems is not { Count: > 0 }) return;

        int delta = direction.Y < 0 ? -1 : direction.Y > 0 ? 1 : 0;
        if (delta == 0) return;

        _menuIndex = Math.Clamp(_menuIndex + delta, 0, _activeMenuItems.Count - 1);
        UpdateMenuCursor();
    }

    private void OnConfirmPressed()
    {
        switch (_gameState.State)
        {
            case GameState.TitleScreen:
                if (_menuIndex == 0) _gameState.Fire(GameTrigger.StartGame);
                else if (_menuIndex == 1) Exit();
                break;
            case GameState.Paused:
                if (_menuIndex == 0) _gameState.Fire(GameTrigger.PauseToggle);
                else if (_menuIndex == 1) _gameState.Fire(GameTrigger.ReturnToTitle);
                break;
            case GameState.GameOver:
                if (_menuIndex == 0) _gameState.Fire(GameTrigger.StartGame);
                else if (_menuIndex == 1) _gameState.Fire(GameTrigger.ReturnToTitle);
                break;
            case GameState.LevelComplete:
                if (_menuIndex == 0) _gameState.Fire(GameTrigger.ReturnToTitle);
                break;
        }
    }

    // --- State change handler ---

    private void OnGameStateChanged()
    {
        _titleScreen.Visible = _gameState.State == GameState.TitleScreen;
        _pauseScreen.Visible = _gameState.State == GameState.Paused;
        _gameOverScreen.Visible = _gameState.State == GameState.GameOver;
        _levelCompleteScreen.Visible = _gameState.State == GameState.LevelComplete;

        _activeMenuItems = _gameState.State switch
        {
            GameState.TitleScreen => [(TextRuntime)_titleScreen.Children[2], (TextRuntime)_titleScreen.Children[3]],
            GameState.Paused => [(TextRuntime)_pauseScreen.Children[2], (TextRuntime)_pauseScreen.Children[3]],
            GameState.GameOver => [(TextRuntime)_gameOverScreen.Children[2], (TextRuntime)_gameOverScreen.Children[3]],
            GameState.LevelComplete => [(TextRuntime)_levelCompleteScreen.Children[2]],
            _ => []
        };
        _menuIndex = 0;
        UpdateMenuCursor();
    }

    private void UpdateMenuCursor()
    {
        if (_activeMenuItems is null) return;
        for (int i = 0; i < _activeMenuItems.Count; i++)
        {
            _activeMenuItems[i].Text = (i == _menuIndex ? "> " : "  ") + _activeMenuItems[i].Text.TrimStart('>', ' ');
        }
    }

    // --- Screen builders ---

    private void BuildScreens()
    {
        _titleScreen = BuildScreen("BEAT 'EM UP", new Color(10, 15, 40), Color.Gold, ["Start Game", "Exit"]);
        _pauseScreen = BuildScreen("PAUSED", new Color(0, 0, 0, 180), Color.White, ["Resume", "Quit to Title"]);
        _gameOverScreen = BuildScreen("GAME OVER", new Color(60, 5, 5, 220), Color.Red, ["Retry", "Quit to Title"]);
        _levelCompleteScreen = BuildScreen("LEVEL COMPLETE!", new Color(20, 40, 10, 220), Color.Gold, ["Return to Title"]);
    }

    private static ContainerRuntime BuildScreen(string title, Color bgColor, Color titleColor, string[] options)
    {
        var container = new ContainerRuntime { WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent, Width = 0, HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent, Height = 0, Visible = false };
        container.AddToRoot();

        var bg = new ColoredRectangleRuntime { WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent, Width = 0, HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent, Height = 0, Color = bgColor };
        container.Children.Add(bg);

        var titleText = new TextRuntime { Text = title, X = 0, Y = -80, XOrigin = HorizontalAlignment.Center, YOrigin = VerticalAlignment.Center, XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle, YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle, HorizontalAlignment = HorizontalAlignment.Center, FontScale = 3f, Red = titleColor.R, Green = titleColor.G, Blue = titleColor.B };
        container.Children.Add(titleText);

        float yOffset = 0;
        foreach (var option in options)
        {
            var item = new TextRuntime { Text = "  " + option, X = 0, Y = yOffset, XOrigin = HorizontalAlignment.Center, YOrigin = VerticalAlignment.Center, XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle, YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle, HorizontalAlignment = HorizontalAlignment.Center, FontScale = 1.5f, Red = 220, Green = 220, Blue = 220 };
            container.Children.Add(item);
            yOffset += 40;
        }

        return container;
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
        _currentLevel = new Level1(Content, GAME_WIDTH, GAME_HEIGHT);
    }
}
