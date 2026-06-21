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
    private CollisionComponent _collision;
    private static GumService GumService => GumService.Default;
    private int _numBackgroundsDrawn, _numEntitiesDrawn;

    private GameStateController _gameState;
    private CameraController _cameraController;
    private MenuManager _menuManager;
    private HitboxService _hitboxService;
    private SpriteFont _debugFont;

    protected override void Initialize()
    {
        _input = new InputManager();
        _input.ActionTriggered += OnActionTriggered;
        _collision = CreateCollisionComponent();
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

        _entityManager = new EntityManager(_collision);
        _entityManager.Register(_player);

        _player.Died += OnPlayerDied;

        RegisterEnemy("enemy1", new Vector2(500, 550));
        RegisterEnemy("enemy2", new Vector2(700, 550));
        RegisterOilDrum("can1", new Vector2(700, 450));
        RegisterOilDrum("can2", new Vector2(900, 450));
        RegisterOilDrum("can3", new Vector2(800, 450));

        AssignHitboxService();
        _cameraController = new CameraController(_player, GAME_WIDTH, GAME_HEIGHT, GAME_WIDTH * 2);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Mode = _gameState.State == GameState.Playing ? InputMode.Gameplay : InputMode.Menu;
        _input.Update(gameTime);

        _entityManager.ProcessPending();

        if (_gameState.State == GameState.Playing)
        {
            _cameraController.Update(Camera);
            _player.MovementDirection = _input.MovementDirection;

            foreach (var movable in _entityManager.Movables)
                movable.MovementBounds = _currentLevel.MovementBounds;

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

    private void RegisterEnemy(string name, Vector2 position)
    {
        var enemy = new EnemyEntity(name, position, 2.0f, EnemySprite.Create());
        enemy.Target = _player;
        enemy.Died += OnEnemyDied;
        _entityManager.Register(enemy);
    }

    private void OnEnemyDied(object sender, EventArgs e)
    {
        if (sender is not EnemyEntity enemy) return;
        enemy.Died -= OnEnemyDied;
        _entityManager.Destroy(enemy);
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

        _collision = CreateCollisionComponent();
        _entityManager.SetCollisionComponent(_collision);

        _entityManager.Register(_player);
        RegisterOilDrum("can1", new Vector2(700, 450));
        RegisterOilDrum("can2", new Vector2(900, 450));
        RegisterOilDrum("can3", new Vector2(800, 450));
        RegisterEnemy("enemy1", new Vector2(500, 550));
        RegisterEnemy("enemy2", new Vector2(700, 550));

        AssignHitboxService();
        _currentLevel = new Level1(Content, GAME_WIDTH, GAME_HEIGHT);
    }

    private void AssignHitboxService()
    {
        foreach (var provider in _entityManager.HitboxProviders)
            provider.HitboxService = _hitboxService;
    }

    private static CollisionComponent CreateCollisionComponent()
    {
        var cc = new CollisionComponent(new RectangleF(0, 0, GAME_WIDTH * 2, GAME_HEIGHT));
        cc.Add("actors", new MonoGame.Extended.Collisions.Layers.Layer(new MonoGame.Extended.Collisions.QuadTree.QuadTreeSpace(new RectangleF(0, 0, GAME_WIDTH * 2, GAME_HEIGHT))));
        return cc;
    }
}