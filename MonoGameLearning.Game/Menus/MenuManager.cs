using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using MonoGameLearning.Game.GameLoop;
using RenderingLibrary.Graphics;

namespace MonoGameLearning.Game.Menus;

public class MenuManager
{
    private static GumService GumService => GumService.Default;
    private readonly GameStateController _gameState;
    private readonly Action _exitGame;

    private ContainerRuntime _titleScreen, _pauseScreen, _gameOverScreen, _levelCompleteScreen;
    private int _menuIndex;
    private List<TextRuntime> _activeMenuItems;

    public MenuManager(GameStateController gameState, Action exitGame)
    {
        _gameState = gameState;
        _exitGame = exitGame;
    }

    public void BuildScreens()
    {
        _titleScreen = BuildScreen("BEAT 'EM UP", new Color(10, 15, 40), Color.Gold, ["Start Game", "Exit"]);
        _pauseScreen = BuildScreen("PAUSED", new Color(0, 0, 0, 180), Color.White, ["Resume", "Quit to Title"]);
        _gameOverScreen = BuildScreen("GAME OVER", new Color(60, 5, 5, 220), Color.Red, ["Retry", "Quit to Title"]);
        _levelCompleteScreen = BuildScreen("LEVEL COMPLETE!", new Color(20, 40, 10, 220), Color.Gold, ["Return to Title"]);
    }

    public void OnGameStateChanged()
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

    public void HandleBack()
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
                _exitGame();
                break;
        }
    }

    public void HandleMenuNavigation(int delta)
    {
        if (_gameState.State == GameState.Playing) return;
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
                else if (_menuIndex == 1) _exitGame();
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

    private void UpdateMenuCursor()
    {
        if (_activeMenuItems is null) return;
        for (int i = 0; i < _activeMenuItems.Count; i++)
        {
            _activeMenuItems[i].Text = (i == _menuIndex ? "> " : "  ") + _activeMenuItems[i].Text.TrimStart('>', ' ');
        }
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
}