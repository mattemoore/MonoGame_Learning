using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;

namespace MonoGameLearning.Core.Input;

public enum InputMode
{
    Gameplay,
    Menu
}

public class InputManager
{
    public event EventHandler Action1Pressed, Action2Pressed, Action3Pressed;
    public event EventHandler BackPressed, DebugPressed, ConfirmPressed;
    public event EventHandler DebugKillPressed, DebugCompletePressed;
    public event Action<Vector2> MenuNavigated;
    public Vector2 MovementDirection { get; private set; }
    public InputMode Mode { get; set; } = InputMode.Gameplay;

    private readonly Dictionary<Keys, Action> _keyActions;
    private readonly Dictionary<Keys, Vector2> _movementKeys;
    private readonly Dictionary<Keys, Vector2> _menuKeys;

    public InputManager()
    {
        _keyActions = new()
        {
            { Keys.U, () => Action1Pressed?.Invoke(this, EventArgs.Empty) },
            { Keys.I, () => Action2Pressed?.Invoke(this, EventArgs.Empty) },
            { Keys.O, () => Action3Pressed?.Invoke(this, EventArgs.Empty) },
            { Keys.Escape, () => BackPressed?.Invoke(this, EventArgs.Empty) },
            { Keys.OemTilde, () => DebugPressed?.Invoke(this, EventArgs.Empty) },
            { Keys.Enter, () => ConfirmPressed?.Invoke(this, EventArgs.Empty) },
            { Keys.Space, () => ConfirmPressed?.Invoke(this, EventArgs.Empty) },
            { Keys.K, () => DebugKillPressed?.Invoke(this, EventArgs.Empty) },
            { Keys.C, () => DebugCompletePressed?.Invoke(this, EventArgs.Empty) }
        };

        _movementKeys = new()
        {
            { Keys.W, new Vector2(0, -1) },
            { Keys.S, new Vector2(0, 1) },
            { Keys.A, new Vector2(-1, 0) },
            { Keys.D, new Vector2(1, 0) }
        };

        _menuKeys = new()
        {
            { Keys.Up, new Vector2(0, -1) },
            { Keys.Down, new Vector2(0, 1) }
        };
    }

    public void Update(GameTime gameTime)
    {
        KeyboardExtended.Update();
        var keyboardState = KeyboardExtended.GetState();

        if (Mode == InputMode.Gameplay)
        {
            Vector2 newMovementDirection = Vector2.Zero;
            foreach (var (key, direction) in _movementKeys)
            {
                if (keyboardState.IsKeyDown(key))
                {
                    newMovementDirection += direction;
                }
            }

            if (newMovementDirection != Vector2.Zero)
            {
                newMovementDirection.Normalize();
            }
            MovementDirection = newMovementDirection;
        }
        else
        {
            MovementDirection = Vector2.Zero;
        }

        if (Mode == InputMode.Menu)
        {
            foreach (var (key, direction) in _menuKeys)
            {
                if (keyboardState.WasKeyPressed(key))
                {
                    MenuNavigated?.Invoke(direction);
                }
            }
        }

        foreach (var (key, action) in _keyActions)
        {
            if (keyboardState.WasKeyPressed(key))
            {
                action();
            }
        }
    }
}
