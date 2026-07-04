using System;
using System.Collections.Generic;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core;
using MonoGameLearning.Core.Settings;
using MonoGameLearning.Core.UI;
using RenderingLibrary.Graphics;
using HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment;

namespace MonoGameLearning.Game.GameLoop;

public class MenuManager(GameStateController gameState, Action exitGame, GumManager gum)
{
    private readonly GameStateController _gameState = gameState;
    private readonly Action _exitGame = exitGame;

    private ContainerRuntime _titleScreen, _pauseScreen, _gameOverScreen, _levelCompleteScreen, _settingsScreen;
    private int _menuIndex;
    private List<TextRuntime> _activeMenuItems;
    private List<ResolutionSetting> _resolutionOptions;
    private int _resolutionIndex;
    private TextRuntime _currentResolutionLabel;
    private List<TextRuntime> _resolutionItems;

    public void BuildScreens()
    {
        _titleScreen = gum.CreateScreen("BEAT 'EM UP", new Color(10, 15, 40), Color.Gold, ["Start Game", "Settings", "Exit"]);
        _pauseScreen = gum.CreateScreen("PAUSED", new Color(0, 0, 0, 180), Color.White, ["Resume", "Settings", "Quit to Title"]);
        _gameOverScreen = gum.CreateScreen("GAME OVER", new Color(60, 5, 5, 220), Color.Red, ["Retry", "Quit to Title"]);
        _levelCompleteScreen = gum.CreateScreen("LEVEL COMPLETE!", new Color(20, 40, 10, 220), Color.Gold, ["Return to Title"]);
        BuildSettingsScreen();
    }

    public void OnGameStateChanged()
    {
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
            GameState.Settings => BuildResolutionItems(),
            _ => []
        };
        _menuIndex = 0;
        _resolutionIndex = 0;
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
                _gameState.Fire(GameTrigger.ReturnToTitle);
                break;
        }
    }

    public void HandleMenuNavigation(int delta)
    {
        if (_gameState.State == GameState.Playing) return;

        if (_gameState.State == GameState.Settings)
        {
            if (_resolutionOptions is not { Count: > 0 }) return;
            _resolutionIndex = Math.Clamp(_resolutionIndex + delta, 0, _resolutionOptions.Count - 1);
            UpdateResolutionDisplay();
            return;
        }

        if (_activeMenuItems is not { Count: > 0 }) return;

        _menuIndex = Math.Clamp(_menuIndex + delta, 0, _activeMenuItems.Count - 1);
        UpdateMenuCursor();
    }

    public void HandleConfirm()
    {
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
        if (_resolutionOptions is not { Count: > 0 }) return;
        if (_resolutionIndex < 0 || _resolutionIndex >= _resolutionOptions.Count) return;

        var selected = _resolutionOptions[_resolutionIndex];
        ResolutionSettings.Save(selected);
        SettingsService.Apply(GameCore.Graphics, selected);

        _currentResolutionLabel.Text = $"Current: {selected.Width}x{selected.Height}";
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
            Text = "RESOLUTION",
            X = 0,
            Y = -120,
            XOrigin = HorizontalAlignment.Center,
            YOrigin = VerticalAlignment.Center,
            XUnits = GeneralUnitType.PixelsFromMiddle,
            YUnits = GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontScale = 2f,
            Red = 200,
            Green = 200,
            Blue = 100
        };
        _settingsScreen.Children.Add(titleText);

        var hintText = new TextRuntime
        {
            Text = "Select and confirm to apply",
            X = 0,
            Y = -80,
            XOrigin = HorizontalAlignment.Center,
            YOrigin = VerticalAlignment.Center,
            XUnits = GeneralUnitType.PixelsFromMiddle,
            YUnits = GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontScale = 1f,
            Red = 180,
            Green = 180,
            Blue = 180
        };
        _settingsScreen.Children.Add(hintText);

        _currentResolutionLabel = new TextRuntime
        {
            Text = "",
            X = 0,
            Y = -40,
            XOrigin = HorizontalAlignment.Center,
            YOrigin = VerticalAlignment.Center,
            XUnits = GeneralUnitType.PixelsFromMiddle,
            YUnits = GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontScale = 1f,
            Red = 100,
            Green = 200,
            Blue = 255
        };
        _settingsScreen.Children.Add(_currentResolutionLabel);

        var navHint = new TextRuntime
        {
            Text = "ESC: Back",
            X = 0,
            Y = 200,
            XOrigin = HorizontalAlignment.Center,
            YOrigin = VerticalAlignment.Center,
            XUnits = GeneralUnitType.PixelsFromMiddle,
            YUnits = GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontScale = 1f,
            Red = 120,
            Green = 120,
            Blue = 120
        };
        _settingsScreen.Children.Add(navHint);

        _resolutionOptions = [.. ResolutionSettings.AvailableResolutions];
        _resolutionItems = [];
        float yOffset = -10;
        for (int i = 0; i < _resolutionOptions.Count; i++)
        {
            var opt = _resolutionOptions[i];
            var item = new TextRuntime
            {
                Text = $"  {opt.Width}x{opt.Height}",
                X = 0,
                Y = yOffset,
                XOrigin = HorizontalAlignment.Center,
                YOrigin = VerticalAlignment.Center,
                XUnits = GeneralUnitType.PixelsFromMiddle,
                YUnits = GeneralUnitType.PixelsFromMiddle,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontScale = 1.2f,
                Red = 200,
                Green = 200,
                Blue = 200
            };
            _settingsScreen.Children.Add(item);
            _resolutionItems.Add(item);
            yOffset += 30;
        }
    }

    private List<TextRuntime> BuildResolutionItems()
    {
        int currentIdx = _resolutionOptions.FindIndex(r => r.Width == ResolutionSettings.Current.Width && r.Height == ResolutionSettings.Current.Height);
        _resolutionIndex = currentIdx >= 0 ? currentIdx : 0;

        _currentResolutionLabel.Text = $"Current: {ResolutionSettings.Current.Width}x{ResolutionSettings.Current.Height}";

        UpdateResolutionDisplay();
        return _resolutionItems;
    }

    private void UpdateResolutionDisplay()
    {
        for (int i = 0; i < _resolutionOptions.Count && i < _resolutionItems.Count; i++)
        {
            if (_resolutionItems[i] is not TextRuntime text) continue;
            var opt = _resolutionOptions[i];
            text.Text = (i == _resolutionIndex ? "> " : "  ") + $"{opt.Width}x{opt.Height}";
        }
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