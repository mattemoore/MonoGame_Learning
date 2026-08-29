using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Camera;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Actor;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Input;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Core.Settings;
using MonoGameLearning.Core.StateMachines;
using MonoGameLearning.Core.UI;
using MonoGameLearning.Game.Entities.GoIndicator;
using MonoGameLearning.Game.Entities.Player;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Pickups;
using MonoGameLearning.Game.Entities.Props;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.Weapons;
using MonoGameLearning.Game.Audio;

namespace MonoGameLearning.Game.GameLoop;

public class GameLoop() : GameCore("Game Demo", RESOLUTION_WIDTH, RESOLUTION_HEIGHT, GAME_WIDTH, GAME_HEIGHT, IS_FULL_SCREEN)
{
    public const int GAME_WIDTH = 800;
    public const int GAME_HEIGHT = 600;
    private static readonly int RESOLUTION_WIDTH = SettingsService.LoadResolution().Width;
    private static readonly int RESOLUTION_HEIGHT = SettingsService.LoadResolution().Height;
    public const bool IS_FULL_SCREEN = false;
    private PlayerEntity _player;
    private Level _currentLevel;
    private EntityService _entityManager;
    private InputService _input;
    private int _numBackgroundsDrawn, _numEntitiesDrawn;

    private StateMachineController<GameState, GameTrigger> _gameState;
    private CameraService _cameraController;
    private MenuService _menuManager;
    private HitboxService _hitboxService;
    private SpriteFont _debugFont;
    private LevelDirector _levelDirector;
    private BackgroundRenderer _backgroundRenderer;
    private CollisionWorld2D _collisionWorld;
    private Dictionary<InputAction, Action> _actionHandlers;
    private AudioService _audio;
    private Action<SfxId> _playSfx;
    private GoIndicatorEntity _goIndicator;
    private HudService _hudService;
    private int _lives;
    private readonly List<IScreenRenderable> _screenRenderables = [];

    private static readonly StaticTextureAsset GoIndicatorTexture = new("images/arrow");
    private static readonly StaticTextureAsset FoodPickupTexture = new("images/apple-pickup");

    private static readonly string[] EnemyWarmUpKeys =
    [
        EnemySprite.AnimationIdle,
        EnemySprite.AnimationRun,
        EnemySprite.AnimationAttack1,
        EnemySprite.AnimationHurt,
        EnemySprite.AnimationFall,
        EnemySprite.AnimationDie,
        EnemySprite.AnimationGetUp,
    ];

    protected override void Initialize()
    {
        _input = new InputService();
        _input.ActionTriggered += OnActionTriggered;
        _audio = new AudioService();
        _playSfx = _audio.PlaySfx;
        SettingsService.LoadAudio();
        _audio.SfxVolume = SettingsService.AudioSettings.SfxVolume;
        _audio.MusicVolume = SettingsService.AudioSettings.MusicVolume;
        _hitboxService = new();

        _gameState = GameStateMachine.Create();
        _gameState.StateMachine.OnTransitioned(t =>
        {
            _menuManager.OnGameStateChanged(t.Source);
            if (t.Destination == GameState.Playing && t.Source != GameState.Paused)
                ResetGame();

            ApplyMusicForState(_audio, t.Source, t.Destination);
        });

        _menuManager = new MenuService(_gameState, Exit, _playSfx, () => SettingsService.AudioSettings, settings =>
        {
            SettingsService.SaveAudio(settings);
            _audio.SfxVolume = settings.SfxVolume;
            _audio.MusicVolume = settings.MusicVolume;
        }, Graphics);

        base.Initialize();

        _menuManager.BuildScreens();
        _menuManager.OnGameStateChanged(GameState.TitleScreen);
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

        _audio.LoadContent(Content, AudioManifest.SfxAssets, AudioManifest.MusicAssets);

        // Play music for the initial state (OnTransitioned never fires for the starting state)
        _audio.PlayMusic(MusicId.TitleMenu);

        PlayerSprite.Load(Content);
        AnimatedSprite playerSprite = PlayerSprite.Create();
        _player = new PlayerEntity("player", new Vector2(100, 450), 2.0f, playerSprite, _audio);

        EnemySprite.Load(Content);
        OilDrumSprite.Load(Content);
        FoodPickupTexture.Load(Content);
        BatWeapon.Load(Content);

        _player.Died += OnPlayerDied;
        _hudService = new HudService(_player, _debugFont, () => _lives);

        GoIndicatorTexture.Load(Content);

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
        _goIndicator = new GoIndicatorEntity(GoIndicatorTexture.Texture, () => new Point(ViewportAdapter.VirtualWidth, ViewportAdapter.VirtualHeight));
        _screenRenderables.Add(_goIndicator);
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
            _cameraController.Update(Camera);
            _player.MovementDirection = _input.MovementDirection;

            var movementBounds = CameraService.ComputeMovementBounds(
                Camera.Position.X,
                _currentLevel.MovementBounds,
                _levelDirector.WaveEndX);

            _levelDirector.PopulateSnapshots(movementBounds);

            _goIndicator.Visible = _levelDirector.ShowGoPrompt;

            var updatables = _entityManager.Updatables;
            for (int i = 0; i < updatables.Count; i++)
                updatables[i].Update(gameTime);

            for (int i = 0; i < _screenRenderables.Count; i++)
                if (_screenRenderables[i] is IUpdatable ui)
                    ui.Update(gameTime);

            var hitResults = _hitboxService.ResolveHits(_entityManager.All);
            foreach (var hit in hitResults)
            {
                if (hit.Target is not { } damageable) continue;
                damageable.TakeDamage(hit);
                if (damageable is CombatActorBase { Faction: Faction.Enemy })
                    _hudService.OnEnemyHit(damageable);
            }

            CollisionWorldFactory.ResolveActorPropCollisions(_collisionWorld);
            PickupService.ResolveOverlaps(_entityManager, _player, _playSfx);

            _hudService.SetProximityTarget(_entityManager.FindNearestAliveEnemy(_player.Position));

            var movables = _entityManager.Movables;
            for (int i = 0; i < movables.Count; i++)
            {
                var movable = movables[i];
                if (movable is IDamageable { IsAlive: false }) continue;
                movable.MovementBounds = movementBounds;
                Mover.ClampToBounds(movable, movable.MovementBounds);
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
                if (cameraBounds.Intersects(renderable.Frame))
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

            SpriteBatch.Begin(transformMatrix: ViewportAdapter.GetScaleMatrix());
            var uiRenderCtx = new RenderContext(SpriteBatch, Camera);
            var screenRenderables = _screenRenderables;
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

    private void OnPlayerDied(object sender, EventArgs e)
    {
        if (TryConsumeLife(ref _lives))
            RespawnPlayer();
        else
            _gameState.Fire(GameTrigger.PlayerDied);
    }

    internal static bool TryConsumeLife(ref int lives)
    {
        if (lives <= 0) return false;
        lives--;
        return true;
    }

    private const float SPAWN_BUFFER_X = 60f;
    private const float LEVEL_EDGE_BUFFER = 10f;
    private const int INITIAL_LIVES = 3;

    internal static void ApplyMusicForState(AudioService audio, GameState previous, GameState current)
    {
        if (previous == GameState.Paused && current != GameState.Paused)
            audio.SetPaused(false);

        switch (current)
        {
            case GameState.TitleScreen:
            case GameState.Settings:
                audio.PlayMusic(MusicId.TitleMenu);
                break;
            case GameState.Playing:
                audio.PlayMusic(MusicId.Gameplay);
                break;
            case GameState.LevelComplete:
                audio.PlayMusic(MusicId.LevelComplete);
                break;
            case GameState.Paused:
                audio.SetPaused(true);
                break;
            case GameState.GameOver:
                audio.PlayMusic(null);
                break;
        }
    }

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
        return ComputeRespawnPosition(Camera?.Position.X ?? _currentLevel.MovementBounds.X, _currentLevel.MovementBounds, _currentLevel.WalkableTopY);
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
        _lives = INITIAL_LIVES;
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
        _collisionWorld = CollisionWorldFactory.Create(_currentLevel.MovementBounds);
        _entityManager = new EntityService(_collisionWorld, _hitboxService);

        _screenRenderables.Clear();

        _entityManager.Register(_player);

        if (_goIndicator is not null)
            _screenRenderables.Add(_goIndicator);

        if (_hudService is not null)
            _screenRenderables.Add(_hudService.RootWidget);

        InitLevelSystems();
        _levelDirector.SpawnProps(_currentLevel.Props);
        _levelDirector.SpawnPickups(_currentLevel.Pickups);
    }

    private void InitLevelSystems()
    {
        _levelDirector = new LevelDirector(
            _entityManager,
            _currentLevel,
            _player,
            _audio,
            CreateProp,
            CreatePickup,
            BatWeapon.Get,
            CreateEnemy,
            ConfigureSpawnedEnemy,
            GetCameraView);

        _levelDirector.LevelCompleted += () => _gameState.Fire(GameTrigger.CompleteLevel);

        _cameraController = new CameraService(_player, GAME_WIDTH, GAME_HEIGHT, _currentLevel.MovementBounds, () => _levelDirector.WaveEndX);
    }

    private PropBase CreateProp(PropSpawnDef def) => new OilDrumEntity(def.Type, def.Position, 1.0f, OilDrumSprite.Create(), _audio, anchor: def.Anchor)
    {
        Drops = def.Drops
    };

    private Entity CreatePickup(PickupSpawnDef def) => def.Type switch
    {
        LevelContent.Food => new FoodPickupEntity(def.Type, def.Position, FoodPickupTexture.Texture),
        LevelContent.Bat => new WeaponPickupEntity(def.Type, def.Position, BatWeapon.Bat),
        _ => throw new ArgumentException($"Unknown pickup type: {def.Type}", nameof(def)),
    };

    private EnemyEntity CreateEnemy(string type, int index, Func<WorldSnapshot> getWorld)
    {
        var enemy = type switch
        {
            LevelContent.Grunt => new EnemyEntity($"grunt_pool_{index}", Vector2.Zero, 2.0f, EnemySprite.Create(), _audio, getWorld),
            _ => throw new ArgumentException($"Unknown enemy type: {type}", nameof(type)),
        };
        foreach (var key in EnemyWarmUpKeys)
            enemy.SpriteRenderer.SetAnimation(key);
        return enemy;
    }

    private void ConfigureSpawnedEnemy(EnemyEntity enemy, EnemySpawnDef def, FacingDirection initialFacing, MeleeWeaponDef weapon)
    {
        if (weapon is not null)
            enemy.EquipWeapon(weapon);

        // SpriteRenderer without an attached sprite (test enemies) → skip visual setup.
        if (enemy.SpriteRenderer.Sprite is not null)
        {
            enemy.Direction = initialFacing;
            enemy.SpriteRenderer.SetEffect(initialFacing == FacingDirection.Left
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None);

            Vector2 walkDir = initialFacing == FacingDirection.Left ? new Vector2(-1, 0) : new Vector2(1, 0);
            float halfW = enemy.Width * 0.5f;
            var view = GetCameraView();
            float targetX = initialFacing == FacingDirection.Left
                ? view.X + view.Width - halfW - 50f
                : view.X + halfW + 50f;
            enemy.SetSpawnWalkData(walkDir, targetX);
        }
    }

    private RectangleF GetCameraView() =>
        new(Camera.Position.X, 0, ViewportAdapter.VirtualWidth, ViewportAdapter.VirtualHeight);
}