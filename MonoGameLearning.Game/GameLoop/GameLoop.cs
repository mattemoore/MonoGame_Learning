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
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Core.GameCore;
using MonoGameLearning.Core.Input;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Player;
using MonoGameLearning.Game.Entities.Props;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.Rendering;
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
    private BackgroundRenderer _backgroundRenderer;
    private Dictionary<InputAction, Action> _actionHandlers;

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

        PlayerSprite.Load(Content);
        AnimatedSprite playerSprite = PlayerSprite.Create();
        _player = new PlayerEntity("player", new Vector2(100, 450), 2.0f, playerSprite);

        EnemySprite.Load(Content);
        OilDrumSprite.Load(Content);

        _player.Died += OnPlayerDied;

        _actionHandlers = new()
        {
            [InputAction.Action1] = () => { if (_gameState.State == GameState.Playing) _player.Attack(_player.Attack1Move); },
            [InputAction.Action2] = () => { if (_gameState.State == GameState.Playing) _player.Attack(_player.Attack2Move); },
            [InputAction.Action3] = () => { if (_gameState.State == GameState.Playing) _player.Attack(_player.Attack3Move); },
            [InputAction.Back] = () => _menuManager.HandleBack(),
            [InputAction.Debug] = ToggleDebug,
            [InputAction.Confirm] = () => _menuManager.HandleConfirm(),
            [InputAction.DebugKill] = () => { if (IsDebug && _gameState.State == GameState.Playing) _player?.TakeDamage(new DamageInfo { Amount = 9999 }); },
            [InputAction.DebugComplete] = () => { if (IsDebug && _gameState.State == GameState.Playing) _gameState.Fire(GameTrigger.CompleteLevel); },
            [InputAction.MenuUp] = () => _menuManager.HandleMenuNavigation(-1),
            [InputAction.MenuDown] = () => _menuManager.HandleMenuNavigation(1),
        };

        ReinitLevel();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Mode = _gameState.State == GameState.Playing ? InputMode.Gameplay : InputMode.Menu;
        _input.Update(gameTime);

        _entityManager.ProcessPending();

        if (_gameState.State == GameState.Playing)
        {
            _levelDirector.Update(gameTime);
            _cameraController.WaveEndX = _levelDirector.WaveEndX;
            _cameraController.Update(Camera);
            _player.MovementDirection = _input.MovementDirection;

            // indexed for loop to avoid heap-allocated IEnumerator<T> from IReadOnlyList<T>
            var updatables = _entityManager.Updatables;
            for (int i = 0; i < updatables.Count; i++)
                updatables[i].Update(gameTime);

            var hitResults = _hitboxService.ResolveHits(_entityManager.All);
            foreach (var hit in hitResults)
            {
                if (hit.Target is IDamageable damageable)
                    damageable.TakeDamage(new DamageInfo { Amount = hit.Damage, Knockdown = hit.Knockdown, Strength = hit.Strength });
            }

            _collision.Update(gameTime);

            var movementBounds = CameraController.ComputeMovementBounds(
                Camera.Position.X,
                _currentLevel.MovementBounds,
                _levelDirector.WaveEndX);
            // indexed for loop to avoid heap-allocated IEnumerator<T> from IReadOnlyList<T>
            var movables = _entityManager.Movables;
            for (int i = 0; i < movables.Count; i++)
            {
                var movable = movables[i];
                movable.MovementBounds = movementBounds;
                Core.Entities.Helpers.Mover.ClampToBounds((Entity)movable, movable.MovementBounds);
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

            var renderCtx = new RenderContext(SpriteBatch, Camera);
            var cameraBounds = Camera.BoundingRectangle;

            _backgroundRenderer.Render(renderCtx);
            _backgroundRenderer.Render(renderCtx);
            _numBackgroundsDrawn = _backgroundRenderer.LastFrameDrawCount;

            // indexed for loop to avoid heap-allocated IEnumerator<T> from IReadOnlyList<T>
            var renderables = _entityManager.Renderables;
            for (int i = 0; i < renderables.Count; i++)
            {
                var renderable = renderables[i];
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

                foreach (var wave in _currentLevel.WaveDefs)
                    SpriteBatch.DrawLine(wave.TriggerX, 0, wave.TriggerX, ViewportAdapter.VirtualHeight, Color.Cyan * 0.4f, 2f);

                SpriteBatch.DrawLine(_currentLevel.EndTriggerX, 0, _currentLevel.EndTriggerX, ViewportAdapter.VirtualHeight, Color.Orange * 0.4f, 2f);

                if (_levelDirector.IsScrollLocked)
                {
                    SpriteBatch.DrawLine(_levelDirector.WaveTriggerX!.Value, 0, _levelDirector.WaveTriggerX.Value, ViewportAdapter.VirtualHeight, Color.Cyan * 0.7f, 2f);
                    SpriteBatch.DrawLine(_levelDirector.WaveEndX!.Value, 0, _levelDirector.WaveEndX.Value, ViewportAdapter.VirtualHeight, Color.Yellow * 0.7f, 2f);
                }

                SpriteBatch.DrawLine(0, _currentLevel.WalkableTopY, _currentLevel.MovementBounds.Right, _currentLevel.WalkableTopY, Color.Lime * 0.5f, 2f);

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
                    new Vector2(ViewportAdapter.VirtualWidth - textSize.X - 20, ViewportAdapter.VirtualHeight / 2f - textSize.Y / 2f),
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

    private void RegisterOilDrum(PropSpawnDef prop)
    {
        var drum = new OilDrumEntity(prop.Type, prop.Position, 1.0f, OilDrumSprite.Create());
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
        if (_actionHandlers.TryGetValue(action, out var handler))
            handler();
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
        ReinitLevel();
        Camera.Position = Vector2.Zero;
    }

    private void ReinitLevel()
    {
        _currentLevel = new Level1(GAME_WIDTH, GAME_HEIGHT);
        _backgroundRenderer = _currentLevel.CreateBackgroundRenderer(Content);
        _collision = CreateCollisionComponent(_currentLevel.MovementBounds);

        if (_entityManager is null)
            _entityManager = new EntityManager(_collision);
        else
            _entityManager.SetCollisionComponent(_collision);

        _entityManager.Register(_player);

        foreach (var prop in _currentLevel.Props)
            RegisterOilDrum(prop);

        AssignHitboxService();
        InitLevelSystems();
    }

    private void InitLevelSystems()
    {
        _cameraController = new CameraController(_player, GAME_WIDTH, GAME_HEIGHT, _currentLevel.MovementBounds);

        _levelDirector = new LevelDirector(_entityManager, _currentLevel, _player, GAME_WIDTH, GAME_HEIGHT);
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