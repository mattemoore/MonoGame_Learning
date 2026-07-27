using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Camera;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Components;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Core.Input;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Core.Settings;
using MonoGameLearning.Core.UI;
using MonoGameLearning.Game.Entities.GoIndicator;
using MonoGameLearning.Game.Entities.Player;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.Entities.Pickups;

namespace MonoGameLearning.Game.GameLoop;

public class GameLoop() : GameCore("Game Demo", RESOLUTION_WIDTH, RESOLUTION_HEIGHT, GAME_WIDTH, GAME_HEIGHT, IS_FULL_SCREEN)
{
    public const int GAME_WIDTH = 800;
    public const int GAME_HEIGHT = 600;
    private static readonly int RESOLUTION_WIDTH = ResolutionSettings.Load().Width;
    private static readonly int RESOLUTION_HEIGHT = ResolutionSettings.Load().Height;
    public const bool IS_FULL_SCREEN = false;
    private PlayerEntity _player;
    private Level _currentLevel;
    private EntityManager _entityManager;
    private InputManager _input;
    private int _numBackgroundsDrawn, _numEntitiesDrawn;

    private GameStateController _gameState;
    private CameraController _cameraController;
    private MenuManager _menuManager;
    private HitboxService _hitboxService;
    private SpriteFont _debugFont;
    private LevelDirector _levelDirector;
    private BackgroundRenderer _backgroundRenderer;
    private CollisionWorld2D _collisionWorld;
    private Dictionary<InputAction, Action> _actionHandlers;
    private AudioManager _audio;
    private GoIndicatorEntity _goIndicator;
    private HudService _hudService;

    protected override void Initialize()
    {
        _input = new InputManager();
        _input.ActionTriggered += OnActionTriggered;
        _audio = new AudioManager();
        SettingsService.LoadAudio();
        _audio.SfxVolume = SettingsService.AudioSettings.SfxVolume;
        _audio.MusicVolume = SettingsService.AudioSettings.MusicVolume;
        _hitboxService = new();

        _gameState = new GameStateController();
        _gameState.StateMachine.OnTransitioned(t =>
        {
            _menuManager.OnGameStateChanged(t.Source);
            if (t.Destination == GameState.Playing && t.Source != GameState.Paused)
                ResetGame();

            switch (t.Destination)
            {
                case GameState.TitleScreen:
                case GameState.Settings:
                    _audio.PlayMusic(MusicId.TitleMenu);
                    break;
                case GameState.Playing:
                    _audio.PlayMusic(MusicId.Gameplay);
                    break;
                case GameState.LevelComplete:
                    _audio.PlayMusic(MusicId.LevelComplete);
                    break;
                case GameState.Paused:
                    _audio.SetPaused(true);
                    break;
                case GameState.GameOver:
                    _audio.PlayMusic(null);
                    break;
            }

            if (t.Source == GameState.Paused && t.Destination != GameState.Paused)
                _audio.SetPaused(false);
        });

        _menuManager = new MenuManager(_gameState, Exit, Gum, _audio.PlaySfx, () => SettingsService.AudioSettings, settings =>
        {
            SettingsService.SaveAudio(settings);
            _audio.SfxVolume = settings.SfxVolume;
            _audio.MusicVolume = settings.MusicVolume;
        });

        base.Initialize();

        _menuManager.BuildScreens();
        _menuManager.OnGameStateChanged(GameState.TitleScreen);
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

        _audio.LoadContent(Content);

        // Play music for the initial state (OnTransitioned never fires for the starting state)
        _audio.PlayMusic(MusicId.TitleMenu);

        PlayerSprite.Load(Content);
        AnimatedSprite playerSprite = PlayerSprite.Create();
        _player = new PlayerEntity("player", new Vector2(100, 450), 2.0f, playerSprite, _audio);

        EnemySprite.Load(Content);
        OilDrumSprite.Load(Content);
        FoodPickupSprite.Load(Content);

        _player.Died += OnPlayerDied;
        _hudService = new HudService(_player, _debugFont);

        GoIndicatorSprite.Load(Content);

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
            [InputAction.MenuLeft] = () => _menuManager.HandleMenuAdjust(-1),
            [InputAction.MenuRight] = () => _menuManager.HandleMenuAdjust(1),
        };

        ReinitLevel();
        _goIndicator = new GoIndicatorEntity("goIndicator", GoIndicatorSprite.Texture);
        _entityManager.Register(_goIndicator);
    }

    protected override void Update(GameTime gameTime)
    {
        _audio.Update();

        _input.Mode = _gameState.State == GameState.Playing ? InputMode.Gameplay : InputMode.Menu;
        _input.Update(gameTime);

        _entityManager.ProcessPending();

        if (_gameState.State == GameState.Playing)
        {
            _levelDirector.Update(gameTime);
            if (_cameraController.WaveEndX is not null && _levelDirector.WaveEndX is null)
                _cameraController.OnWaveCleared();
            _cameraController.WaveEndX = _levelDirector.WaveEndX;
            _cameraController.Update(Camera);
            _player.MovementDirection = _input.MovementDirection;

            var movementBounds = CameraController.ComputeMovementBounds(
                Camera.Position.X,
                _currentLevel.MovementBounds,
                _levelDirector.WaveEndX);

            _levelDirector.PopulateSnapshots(movementBounds);

            _goIndicator.Visible = _levelDirector.ShowGoPrompt;

            var updatables = _entityManager.Updatables;
            for (int i = 0; i < updatables.Count; i++)
                updatables[i].Update(gameTime);

            var hitResults = _hitboxService.ResolveHits(_entityManager.All);
            foreach (var hit in hitResults)
            {
                var damageable = hit.Target;
                damageable.TakeDamage(new DamageInfo { Amount = hit.Damage, Knockdown = hit.Knockdown, Strength = hit.Strength, ImpactSfx = hit.ImpactSfx });
                if (damageable.Faction == Faction.Enemy)
                    _hudService.OnEnemyHit(damageable);
            }

            ResolveCollisions();
            ResolvePickupOverlaps();

            _hudService.SetProximityTarget(_entityManager.FindNearestAliveEnemy(_player.Position));

            var movables = _entityManager.Movables;
            for (int i = 0; i < movables.Count; i++)
            {
                var movable = movables[i];
                if (movable is IDamageable { IsAlive: false }) continue;
                movable.MovementBounds = movementBounds;
                Mover.ClampToBounds((IReadOnlyEntity)movable, movable.MovementBounds);
            }
        }

        Gum.Update(gameTime);
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
            _numBackgroundsDrawn = _backgroundRenderer.LastFrameDrawCount;

            var renderables = _entityManager.Renderables;
            _entityManager.SortRenderablesByY();
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
                {
                    if (drawable is IScreenRenderable) continue;
                    drawable.DrawDebug(debugCtx);
                }

                _levelDirector.DrawDebug(debugCtx);

                var waveStatus = _levelDirector.CurrentWaveIndex < _currentLevel.WaveDefs.Count
                    ? $"Wave: {_levelDirector.CurrentWaveIndex + 1}/{_currentLevel.WaveDefs.Count}"
                    : "All waves done";
                Gum.DebugOverlay.SetText(
                    $"FPS: {FPSCounter.FramesPerSecond}\n" +
                    $"State: {_gameState.State}\n" +
                    $"{waveStatus} | Active: {_levelDirector.ActiveEnemyCount} | Locked: {_levelDirector.IsScrollLocked}\n" +
                    $"Viewport: Virtual-{ViewportAdapter.VirtualWidth}x{ViewportAdapter.VirtualHeight} Actual-{ViewportAdapter.ViewportWidth}x{ViewportAdapter.ViewportHeight}\n" +
                    $"Screen Buffer: {Graphics.PreferredBackBufferWidth}x{Graphics.PreferredBackBufferHeight}\n" +
                    $"Window: {Window.ClientBounds.Width}x{Window.ClientBounds.Height}",
                    $"BGs draw: {_numBackgroundsDrawn}\nEnts draw: {_numEntitiesDrawn}");
            }
            SpriteBatch.End();

            SpriteBatch.Begin();
            var uiRenderCtx = new RenderContext(SpriteBatch, Camera);
            var screenRenderables = _entityManager.ScreenRenderables;
            for (int i = 0; i < screenRenderables.Count; i++)
                screenRenderables[i].Render(uiRenderCtx);

            if (IsDebug)
            {
                var uiDebugCtx = new DebugDrawContext(SpriteBatch, _debugFont);
                for (int i = 0; i < screenRenderables.Count; i++)
                {
                    if (screenRenderables[i] is IDebugDrawable dbg)
                        dbg.DrawDebug(uiDebugCtx);
                }
            }
            SpriteBatch.End();
        }

        Gum.Draw();
        base.Draw(gameTime);
    }

    // Separate from ResolvePickupOverlaps:
    //   - ResolveCollisions: quad-tree MTV pushback (actor-into-prop)
    //   - ResolvePickupOverlaps: manual AABB overlap, side-effect trigger (heal + destroy), no position correction
    private void ResolveCollisions()
    {
        _collisionWorld.RebuildDynamicLayers();

        foreach (var pair in _collisionWorld.QueryCollisionPairs("actors", "props"))
        {
            var actor = pair.First;
            var result = pair.FirstResult;
            if (!result.Intersects) continue;
            if (actor is Entity entity)
                entity.Position += result.MinimumTranslationVector;
        }
    }

    private void ResolvePickupOverlaps()
    {
        if (_player is not IDamageable { IsAlive: true }) return;

        var playerFrame = _player.Frame;
        var pickups = _entityManager.PickupCollidables;
        for (int i = 0; i < pickups.Count; i++)
        {
            var pickup = pickups[i];
            if (pickup is not Entity { } pickupEntity) continue;
            if (!playerFrame.Intersects(pickupEntity.Frame)) continue;
            if (pickup is not IPickup pickupInterface) continue;

            pickupInterface.OnPickup(_player);
            _audio.PlaySfx(SfxId.PickupHeal);
            _entityManager.Destroy(pickupEntity);
        }
    }

    private void OnPlayerDied(object sender, EventArgs e)
    {
        if (_player.Lives > 0)
        {
            _player.Lives--;
            RespawnPlayer();
        }
        else
        {
            _gameState.Fire(GameTrigger.PlayerDied);
        }
    }

    private const float SPAWN_BUFFER_X = 60f;
    private const float LEVEL_EDGE_BUFFER = 10f;

    internal static Vector2 ComputeRespawnPosition(float cameraX, RectangleF movementBounds, float walkableTopY)
    {
        float levelLeft = movementBounds.X;
        float levelRight = movementBounds.Right;
        float desiredX = cameraX + SPAWN_BUFFER_X;
        float clampedX = Math.Clamp(desiredX, levelLeft + LEVEL_EDGE_BUFFER, levelRight - LEVEL_EDGE_BUFFER);
        Debug.Assert(clampedX >= levelLeft + LEVEL_EDGE_BUFFER,
            "Respawn X clamped below level-left buffer — camera is before level start?");
        return new Vector2(clampedX, walkableTopY);
    }

    internal Vector2 ComputeRespawnPosition()
    {
        return ComputeRespawnPosition(GameCore.Camera?.Position.X ?? _currentLevel.MovementBounds.X, _currentLevel.MovementBounds, _currentLevel.WalkableTopY);
    }

    private void RespawnPlayer()
    {
        _player.Reset(ComputeRespawnPosition());
        _player.Respawn();
    }

    private void OnActionTriggered(InputAction action)
    {
        if (_actionHandlers.TryGetValue(action, out var handler))
            handler();
    }

    private void ToggleDebug()
    {
        IsDebug = !IsDebug;
        Gum.DebugOverlay.Visible = IsDebug;
    }

    private void ResetGame()
    {
        _hitboxService.ClearAll();
        _player.Lives = PlayerEntity.InitialLives;
        _player.Reset(new Vector2(100, 450));
        _entityManager.Clear();
        _hudService?.ClearTargetState();
        ReinitLevel();
        Camera.Position = Vector2.Zero;
    }

    private void ReinitLevel()
    {
        _currentLevel = new Level1(GAME_WIDTH, GAME_HEIGHT);
        _backgroundRenderer = _currentLevel.CreateBackgroundRenderer(Content);
        _collisionWorld = CreateCollisionWorld(_currentLevel.MovementBounds);
        _entityManager = new EntityManager(_collisionWorld)
        {
            HitboxService = _hitboxService
        };

        _entityManager.Register(_player);

        if (_goIndicator is not null)
            _entityManager.Register(_goIndicator);

        if (_hudService is not null)
            _entityManager.Register(_hudService.RootWidget);

        InitLevelSystems();
        _levelDirector.SpawnProps(_currentLevel.Props);
        _levelDirector.SpawnPickups(_currentLevel.Pickups);
    }

    private void InitLevelSystems()
    {
        _cameraController = new CameraController(_player, GAME_WIDTH, GAME_HEIGHT, _currentLevel.MovementBounds);

        _levelDirector = new LevelDirector(_entityManager, _currentLevel, _player, _audio);
        _levelDirector.LevelCompleted += () => _gameState.Fire(GameTrigger.CompleteLevel);
    }

    private static CollisionWorld2D CreateCollisionWorld(RectangleF bounds)
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(bounds.X, bounds.Y), new Vector2(bounds.Right, bounds.Bottom));
        var actorSpace = new QuadTreeSpace(bb);
        world.AddLayer("actors", new Layer(actorSpace));
        world.DisableCollisionBetweenLayers("actors", "actors");
        var propSpace = new QuadTreeSpace(bb);
        world.AddLayer("props", new Layer(propSpace));
        world.DisableCollisionBetweenLayers("props", "props");
        world.EnableCollisionBetweenLayers("actors", "props");
        var pickupSpace = new QuadTreeSpace(bb);
        world.AddLayer("pickups", new Layer(pickupSpace));
        world.DisableCollisionBetweenLayers("pickups", "pickups");
        world.EnableCollisionBetweenLayers("actors", "pickups");
        return world;
    }
}