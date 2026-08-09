using System;
using System.Collections.Generic;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Settings;
using MonoGameLearning.Core.UI;
using RenderingLibrary.Graphics;
using HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment;

namespace MonoGameLearning.Game.GameLoop;

public class MenuService
{
    private readonly GameStateService _gameState;
    private readonly Action _exitGame;
    private readonly Action<SfxId> _playSfx;
    private readonly Func<AudioSettings> _getAudioSettings;
    private readonly Action<AudioSettings> _setAudioSettings;

    private ContainerRuntime _titleScreen, _pauseScreen, _gameOverScreen, _levelCompleteScreen, _settingsScreen;
    private int _menuIndex;
    private List<TextRuntime> _activeMenuItems;
    private readonly GumUiService _gum;
    private GameState _previousState;

    private TextRuntime _resCursor, _resLabel, _resValue;
    private TextRuntime _sfxCursor, _sfxLabel, _sfxValue;
    private TextRuntime _musicCursor, _musicLabel, _musicValue;

    public MenuService(GameStateService gameState, Action exitGame, GumUiService gum,
        Action<SfxId> playSfx, Func<AudioSettings> getAudioSettings, Action<AudioSettings> setAudioSettings)
    {
        _gameState = gameState;
        _exitGame = exitGame;
        _gum = gum;
        _playSfx = playSfx;
        _getAudioSettings = getAudioSettings;
        _setAudioSettings = setAudioSettings;
    }

    public void BuildScreens()
    {
        _titleScreen = _gum.CreateScreen("BEAT 'EM UP", new Color(10, 15, 40), Color.Gold, ["Start Game", "Settings", "Exit"]);
        _pauseScreen = _gum.CreateScreen("PAUSED", new Color(0, 0, 0, 180), Color.White, ["Resume", "Settings", "Quit to Title"]);
        _gameOverScreen = _gum.CreateScreen("GAME OVER", new Color(60, 5, 5, 220), Color.Red, ["Retry", "Quit to Title"]);
        _levelCompleteScreen = _gum.CreateScreen("LEVEL COMPLETE!", new Color(20, 40, 10, 220), Color.Gold, ["Return to Title"]);
        BuildSettingsScreen();
    }

    public void OnGameStateChanged(GameState previousState)
    {
        if (_gameState.State == GameState.Settings)
            _previousState = previousState;

        _titleScreen.Visible = _gameState.State == GameState.TitleScreen;
        _pauseScreen.Visible = _gameState.State == GameState.Paused;
        _gameOverScreen.Visible = _gameState.State == GameState.GameOver;
        _levelCompleteScreen.Visible = _gameState.State == GameState.LevelComplete;
        _settingsScreen.Visible = _gameState.State == GameState.Settings;

        _activeMenuItems = _gameState.State switch
        {
            GameState.TitleScreen => [(TextRuntime)_titleScreen.Children[2], (TextRuntime)_titleScreen.Children[3], (TextRuntime)_titleScreen.Children[4]],
            GameState.Paused => [(TextRuntime)_pauseScreen.Children[2], (TextRuntime)_pauseScreen.Children[3], (TextRuntime)_pauseScreen.Children[4]],
            GameState.GameOver => [(TextRuntime)_gameOverScreen.Children[2], (TextRuntime)_gameOverScreen.Children[3]],
            GameState.LevelComplete => [(TextRuntime)_levelCompleteScreen.Children[2]],
            GameState.Settings => [_resValue, _sfxValue, _musicValue],
            _ => []
        };
        _menuIndex = 0;
        if (_gameState.State == GameState.Settings)
            UpdateSettingsDisplays();
        else
            UpdateMenuCursor();
    }

    public void HandleBack()
    {
        switch (_gameState.State)
        {
            case GameState.Playing:
            case GameState.Paused:
                _gameState.Fire(GameTrigger.PauseToggle);
                break;
            case GameState.TitleScreen:
                _exitGame();
                break;
            case GameState.Settings:
                if (_previousState == GameState.Paused)
                    _gameState.Fire(GameTrigger.PauseToggle);
                else
                    _gameState.Fire(GameTrigger.ReturnToTitle);
                break;
        }
    }

    public void HandleMenuNavigation(int delta)
    {
        if (_gameState.State == GameState.Playing) return;

        if (_activeMenuItems is not { Count: > 0 }) return;

        _menuIndex = Math.Clamp(_menuIndex + delta, 0, _activeMenuItems.Count - 1);
        _playSfx(SfxId.MenuNavigate);
        if (_gameState.State == GameState.Settings)
            UpdateSettingsDisplays();
        else
            UpdateMenuCursor();
    }

    public void HandleMenuAdjust(int delta)
    {
        if (_gameState.State != GameState.Settings) return;

        if (_menuIndex == 0)
        {
            // Resolution: cycle through available options
            var options = ResolutionSettings.AvailableResolutions;
            int currentIdx = -1;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Width == ResolutionSettings.Current.Width && options[i].Height == ResolutionSettings.Current.Height)
                {
                    currentIdx = i;
                    break;
                }
            }
            int newIdx = Math.Clamp(currentIdx + delta, 0, options.Count - 1);
            var selected = options[newIdx];
            ResolutionSettings.Save(selected);
            SettingsService.Apply(GameCore.Graphics, selected);
            UpdateSettingsDisplays();
        }
        else if (_menuIndex == 1)
        {
            // SFX volume
            var settings = _getAudioSettings();
            float vol = MathF.Round(Math.Clamp(settings.SfxVolume + delta * 0.05f, 0f, 1f) * 20f) / 20f;
            _setAudioSettings(new AudioSettings(vol, settings.MusicVolume));
            UpdateSettingsDisplays();
        }
        else if (_menuIndex == 2)
        {
            // Music volume
            var settings = _getAudioSettings();
            float vol = MathF.Round(Math.Clamp(settings.MusicVolume + delta * 0.05f, 0f, 1f) * 20f) / 20f;
            _setAudioSettings(new AudioSettings(settings.SfxVolume, vol));
            UpdateSettingsDisplays();
        }
    }

    public void HandleConfirm()
    {
        _playSfx(SfxId.MenuConfirm);

        switch (_gameState.State)
        {
            case GameState.TitleScreen:
                if (_menuIndex == 0) _gameState.Fire(GameTrigger.StartGame);
                else if (_menuIndex == 1) _gameState.Fire(GameTrigger.OpenSettings);
                else if (_menuIndex == 2) _exitGame();
                break;
            case GameState.Paused:
                if (_menuIndex == 0) _gameState.Fire(GameTrigger.PauseToggle);
                else if (_menuIndex == 1) _gameState.Fire(GameTrigger.OpenSettings);
                else if (_menuIndex == 2) _gameState.Fire(GameTrigger.ReturnToTitle);
                break;
            case GameState.GameOver:
                if (_menuIndex == 0) _gameState.Fire(GameTrigger.StartGame);
                else if (_menuIndex == 1) _gameState.Fire(GameTrigger.ReturnToTitle);
                break;
            case GameState.LevelComplete:
                if (_menuIndex == 0) _gameState.Fire(GameTrigger.ReturnToTitle);
                break;
            case GameState.Settings:
                ApplySelectedResolution();
                break;
        }
    }

    private void ApplySelectedResolution()
    {
        var selected = ResolutionSettings.Current;
        SettingsService.Apply(GameCore.Graphics, selected);
    }

    private void BuildSettingsScreen()
    {
        _settingsScreen = new ContainerRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Height = 0,
            Visible = false
        };
        _settingsScreen.AddToRoot();

        var bg = new RectangleRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Height = 0,
            IsFilled = true,
            FillColor = new Color(20, 20, 50, 230)
        };
        _settingsScreen.Children.Add(bg);

        var titleText = new TextRuntime
        {
            Text = "SETTINGS",
            X = 0,
            Y = -180,
            XOrigin = HorizontalAlignment.Center,
            YOrigin = VerticalAlignment.Center,
            XUnits = GeneralUnitType.PixelsFromMiddle,
            YUnits = GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontScale = 2.5f,
            Red = 200,
            Green = 200,
            Blue = 100
        };
        _settingsScreen.Children.Add(titleText);

        static TextRuntime MakeLabel(float y, string text, float x)
        {
            return new TextRuntime
            {
                Text = text,
                X = x, Y = y,
                XOrigin = HorizontalAlignment.Left,
                YOrigin = VerticalAlignment.Center,
                XUnits = GeneralUnitType.PixelsFromMiddle,
                YUnits = GeneralUnitType.PixelsFromMiddle,
                HorizontalAlignment = HorizontalAlignment.Left,
                FontScale = 1.4f,
                Red = 200, Green = 200, Blue = 200
            };
        }

        const float cursorX = -200;
        const float labelX = -180;
        const float valueX = 80;

        _resCursor = MakeLabel(-80, " ", cursorX);
        _resCursor.Red = 200; _resCursor.Green = 200; _resCursor.Blue = 0;
        _settingsScreen.Children.Add(_resCursor);
        _resLabel = MakeLabel(-80, "Resolution", labelX);
        _settingsScreen.Children.Add(_resLabel);
        _resValue = MakeLabel(-80, "", valueX);
        _resValue.HorizontalAlignment = HorizontalAlignment.Center;
        _resValue.XOrigin = HorizontalAlignment.Center;
        _settingsScreen.Children.Add(_resValue);

        _sfxCursor = MakeLabel(-30, " ", cursorX);
        _sfxCursor.Red = 200; _sfxCursor.Green = 200; _sfxCursor.Blue = 0;
        _settingsScreen.Children.Add(_sfxCursor);
        _sfxLabel = MakeLabel(-30, "SFX Volume", labelX);
        _settingsScreen.Children.Add(_sfxLabel);
        _sfxValue = MakeLabel(-30, "", valueX);
        _sfxValue.HorizontalAlignment = HorizontalAlignment.Center;
        _sfxValue.XOrigin = HorizontalAlignment.Center;
        _settingsScreen.Children.Add(_sfxValue);

        _musicCursor = MakeLabel(20, " ", cursorX);
        _musicCursor.Red = 200; _musicCursor.Green = 200; _musicCursor.Blue = 0;
        _settingsScreen.Children.Add(_musicCursor);
        _musicLabel = MakeLabel(20, "Music Volume", labelX);
        _settingsScreen.Children.Add(_musicLabel);
        _musicValue = MakeLabel(20, "", valueX);
        _musicValue.HorizontalAlignment = HorizontalAlignment.Center;
        _musicValue.XOrigin = HorizontalAlignment.Center;
        _settingsScreen.Children.Add(_musicValue);

        var navHint = new TextRuntime
        {
            Text = "Navigate: Arrow Keys    Adjust: Left/Right",
            X = 0, Y = 120,
            XOrigin = HorizontalAlignment.Center,
            YOrigin = VerticalAlignment.Center,
            XUnits = GeneralUnitType.PixelsFromMiddle,
            YUnits = GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontScale = 1f,
            Red = 140, Green = 140, Blue = 140
        };
        _settingsScreen.Children.Add(navHint);

        var escHint = new TextRuntime
        {
            Text = "ESC: Back",
            X = 0, Y = 200,
            XOrigin = HorizontalAlignment.Center,
            YOrigin = VerticalAlignment.Center,
            XUnits = GeneralUnitType.PixelsFromMiddle,
            YUnits = GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontScale = 1f,
            Red = 120, Green = 120, Blue = 120
        };
        _settingsScreen.Children.Add(escHint);
    }

    private void UpdateSettingsDisplays()
    {
        var res = ResolutionSettings.Current;
        var audio = _getAudioSettings();
        int sfxPct = (int)MathF.Round(audio.SfxVolume * 100f);
        int musicPct = (int)MathF.Round(audio.MusicVolume * 100f);
        int sfxBars = (int)MathF.Round(audio.SfxVolume * 10f);
        int musicBars = (int)MathF.Round(audio.MusicVolume * 10f);
        string sfxBarStr = new string('█', sfxBars) + new string('░', 10 - sfxBars);
        string musicBarStr = new string('█', musicBars) + new string('░', 10 - musicBars);

        _resCursor.Text = _menuIndex == 0 ? ">" : " ";
        _sfxCursor.Text = _menuIndex == 1 ? ">" : " ";
        _musicCursor.Text = _menuIndex == 2 ? ">" : " ";
        _resValue.Text = $"{res.Width}x{res.Height}";
        _sfxValue.Text = $"{sfxBarStr} {sfxPct}%";
        _musicValue.Text = $"{musicBarStr} {musicPct}%";
    }

    private void UpdateMenuCursor()
    {
        if (_activeMenuItems is null) return;
        for (int i = 0; i < _activeMenuItems.Count; i++)
        {
            _activeMenuItems[i].Text = (i == _menuIndex ? "> " : "  ") + _activeMenuItems[i].Text.TrimStart('>', ' ');
        }
    }
}