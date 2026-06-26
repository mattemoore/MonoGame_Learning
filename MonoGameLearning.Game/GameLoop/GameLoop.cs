using System;
using Gum.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Core.GameCore;
using MonoGameLearning.Core.Input;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Player;
using MonoGameLearning.Game.Entities.Props;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.GameLoop;

public class GameLoop() : GameCore("Game Demo", RESOLUTION_WIDTH, RESOLUTION_HEIGHT, GAME_WIDTH, GAME_HEIGHT, IS_FULL_SCREEN)
{
    public const int GAME_WIDTH = 800;
    public const int GAME_HEIGHT = 600;
    public const int RESOLUTION_WIDTH = 1024;
    public const int RESOLUTION_HEIGHT = 768;
    public const bool IS_FULL_SCREEN = false;
    private PlayerEntity _player;
    private Level _currentLevel;
    private EntityManager _entityManager;
    private InputManager _input;
    private TextRuntime _debugWindow1, _debugWindow2;
    private CollisionComponent _collision = null!;
    private static GumService GumService => GumService.Default;
    private int _numBackgroundsDrawn, _numEntitiesDrawn;

    private GameStateController _gameState;
    private CameraController _cameraController;
    private MenuManager _menuManager;
    private HitboxService _hitboxService;
    private SpriteFont _debugFont;
    private LevelDirector _levelDirector;

    protected override void Initialize()
    {
        _input = new InputManager();
        _input.ActionTriggered += OnActionTriggered;
        _hitboxService = new();

        _gameState = new GameStateController();
        _gameState.StateMachine.OnTransitioned(t =>
        {
            _menuManager.OnGameStateChanged();
            if (t.Destination == GameState.Playing && t.Source != GameState.Paused)
                ResetGame();
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
        _debugWindow2.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Right;

        _menuManager.BuildScreens();
        _menuManager.OnGameStateChanged();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

        _currentLevel = new Level1(Content, GAME_WIDTH, GAME_HEIGHT);

        PlayerSprite.Load(Content);
        AnimatedSprite playerSprite = PlayerSprite.Create();
        _player = new PlayerEntity("player", new Vector2(100, 450), 2.0f, playerSprite);

        EnemySprite.Load(Content);
        OilDrumSprite.Load(Content);

        _collision = CreateCollisionComponent(_currentLevel.MovementBounds);
        _entityManager = new EntityManager(_collision);
        _entityManager.Register(_player);

        _player.Died += OnPlayerDied;

        RegisterOilDrum("can1", new Vector2(700, 450));
        RegisterOilDrum("can2", new Vector2(900, 450));
        RegisterOilDrum("can3", new Vector2(800, 450));

        AssignHitboxService();
        InitLevelSystems();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Mode = _gameState.State == GameState.Playing ? InputMode.Gameplay : InputMode.Menu;
        _input.Update(gameTime);

        _entityManager.ProcessPending();

        if (_gameState.State == GameState.Playing)
        {
            _levelDirector.Update(gameTime);
            _player.MovementDirection = _input.MovementDirection;

            var fa = _levelDirector.CurrentFightArea;

            if (fa.HasValue)
            {
                _cameraController.LeftBound = _levelDirector.PersistentCameraCenter;
                _cameraController.RightBound = _levelDirector.PersistentCameraCenter;
            }
            else
            {
                _cameraController.LeftBound = _levelDirector.PersistentCameraCenter;
                _cameraController.RightBound = null;
            }

            _cameraController.Update(Camera);

            foreach (var movable in _entityManager.Movables)
            {
                var bounds = _currentLevel.MovementBounds;
                if (fa.HasValue)
                {
                    bounds.X = fa.Value.X;
                    bounds.Width = Math.Max(fa.Value.Width, _player.Position.X - fa.Value.X);
                }
                else if (_levelDirector.PersistentCameraCenter.HasValue)
                {
                    float leftEdge = _levelDirector.PersistentCameraCenter.Value - GAME_WIDTH / 2f;
                    bounds.X = leftEdge;
                    bounds.Width = _currentLevel.MovementBounds.Right - leftEdge;
                }
                movable.MovementBounds = bounds;
            }

            foreach (var updatable in _entityManager.Updatables)
                updatable.Update(gameTime);

            var hitResults = _hitboxService.ResolveHits(_entityManager.All);
            foreach (var hit in hitResults)
            {
                if (hit.Target is IDamageable damageable)
                    damageable.TakeDamage(new DamageInfo { Amount = hit.Damage, Knockdown = hit.Knockdown, Strength = hit.Strength });
            }

            _collision.Update(gameTime);
            foreach (var movable in _entityManager.Movables)
                Core.Entities.Helpers.Mover.ClampToBounds((Entity)movable, movable.MovementBounds);
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

            var renderCtx = new RenderContext(SpriteBatch, Camera);
            var cameraBounds = Camera.BoundingRectangle;

            _numBackgroundsDrawn = _currentLevel.Draw(renderCtx);

            foreach (var renderable in _entityManager.Renderables)
            {
                if (cameraBounds.Intersects(((Entity)renderable).Frame))
                {
                    renderable.Render(renderCtx);
                    _numEntitiesDrawn++;
                }
            }

            if (IsDebug)
            {
                var debugCtx = new DebugDrawContext(SpriteBatch, _debugFont);
                foreach (var drawable in _entityManager.DebugDrawables)
                    drawable.DrawDebug(debugCtx);
                _currentLevel.DrawDebug(debugCtx);

                foreach (var wave in _currentLevel.WaveDefs)
                    SpriteBatch.DrawLine(wave.TriggerX, 0, wave.TriggerX, GAME_HEIGHT, Color.Cyan * 0.4f, 2f);

                SpriteBatch.DrawLine(_currentLevel.EndTriggerX, 0, _currentLevel.EndTriggerX, GAME_HEIGHT, Color.Orange * 0.4f, 2f);

                if (_levelDirector.CurrentFightArea.HasValue)
                {
                    var fightRight = _levelDirector.CurrentFightArea.Value.X + _levelDirector.CurrentFightArea.Value.Width;
                    var rect = new RectangleF(_currentLevel.MovementBounds.Left, 0, fightRight - _currentLevel.MovementBounds.Left, GAME_HEIGHT);
                    SpriteBatch.DrawRectangle(rect, Color.Yellow * 0.3f, 2f);
                }

                var waveStatus = _levelDirector.CurrentWaveIndex < _currentLevel.WaveDefs.Count
                    ? $"Wave: {_levelDirector.CurrentWaveIndex + 1}/{_currentLevel.WaveDefs.Count}"
                    : "All waves done";
                _debugWindow1.Text = $"FPS: {FPSCounter.FramesPerSecond}\n" +
                                     $"State: {_gameState.State}\n" +
                                     $"{waveStatus} | Active: {_levelDirector.ActiveEnemyCount} | Locked: {_levelDirector.IsScrollLocked}\n" +
                                     $"Viewport: Virtual-{ViewportAdapter.VirtualWidth}x{ViewportAdapter.VirtualHeight} Actual-{ViewportAdapter.ViewportWidth}x{ViewportAdapter.ViewportHeight}\n" +
                                     $"Screen Buffer: {Graphics.PreferredBackBufferWidth}x{Graphics.PreferredBackBufferHeight}\n" +
                                     $"Window: {Window.ClientBounds.Width}x{Window.ClientBounds.Height}";
                _debugWindow2.Text = $"BGs draw: {_numBackgroundsDrawn}\n" +
                                     $"Ents draw: {_numEntitiesDrawn}";
            }
            SpriteBatch.End();

            if (_levelDirector.ShowGoPrompt)
            {
                SpriteBatch.Begin();
                var goText = "GO ->";
                float scale = 3f;
                var textSize = _debugFont.MeasureString(goText) * scale;
                SpriteBatch.DrawString(_debugFont, goText,
                    new Vector2(GAME_WIDTH - textSize.X - 20, GAME_HEIGHT / 2f - textSize.Y / 2f),
                    Color.LimeGreen, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                SpriteBatch.End();
            }
        }

        GumService.Draw();
        base.Draw(gameTime);
    }

    private void OnPlayerDied(object sender, EventArgs e)
    {
        _gameState.Fire(GameTrigger.PlayerDied);
    }

    private void RegisterOilDrum(string name, Vector2 position)
    {
        var drum = new OilDrumEntity(name, position, 1.0f, OilDrumSprite.Create());
        drum.Destroyed += OnOilDrumDestroyed;
        _entityManager.Register(drum);
    }

    private void OnOilDrumDestroyed(Entity drum)
    {
        if (drum is OilDrumEntity oilDrum)
            oilDrum.Destroyed -= OnOilDrumDestroyed;
        _entityManager.Destroy(drum);
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
                if (IsDebug && _gameState.State == GameState.Playing) _player?.TakeDamage(new DamageInfo { Amount = 9999 });
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
        _hitboxService.ClearAll();
        _player.Reset(new Vector2(100, 450));

        _entityManager.Clear();

        _currentLevel = new Level1(Content, GAME_WIDTH, GAME_HEIGHT);
        _collision = CreateCollisionComponent(_currentLevel.MovementBounds);
        _entityManager.SetCollisionComponent(_collision);

        _entityManager.Register(_player);
        RegisterOilDrum("can1", new Vector2(700, 450));
        RegisterOilDrum("can2", new Vector2(900, 450));
        RegisterOilDrum("can3", new Vector2(800, 450));

        AssignHitboxService();
        InitLevelSystems();
        Camera.Position = Vector2.Zero;
    }

    private void InitLevelSystems()
    {
        _cameraController = new CameraController(_player, GAME_WIDTH, GAME_HEIGHT, _currentLevel.MovementBounds);

        _levelDirector = new LevelDirector(_entityManager, _currentLevel, _player);
        _levelDirector.LevelCompleted += () => _gameState.Fire(GameTrigger.CompleteLevel);
    }

    private void AssignHitboxService()
    {
        foreach (var provider in _entityManager.HitboxProviders)
            provider.HitboxService = _hitboxService;
    }

    private static CollisionComponent CreateCollisionComponent(RectangleF bounds)
    {
        var cc = new CollisionComponent(bounds);
        cc.Add("actors", new MonoGame.Extended.Collisions.Layers.Layer(new MonoGame.Extended.Collisions.QuadTree.QuadTreeSpace(bounds)));
        return cc;
    }
}
