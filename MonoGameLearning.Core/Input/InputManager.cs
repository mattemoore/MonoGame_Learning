using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;

namespace MonoGameLearning.Core.Input;

public enum InputAction
{
    Action1,
    Action2,
    Action3,
    Back,
    Debug,
    Confirm,
    DebugKill,
    DebugComplete,
    MenuUp,
    MenuDown
}

public enum InputMode
{
    Gameplay,
    Menu
}

public class InputManager
{
    public event Action<InputAction> ActionTriggered;
    public Vector2 MovementDirection { get; private set; }
    public InputMode Mode { get; set; } = InputMode.Gameplay;

    private readonly Dictionary<Keys, InputAction> _keyActions;
    private readonly Dictionary<Keys, Vector2> _movementKeys;
    private readonly Dictionary<Keys, InputAction> _menuKeys;

    public InputManager()
    {
        _keyActions = new()
        {
            { Keys.U, InputAction.Action1 },
            { Keys.I, InputAction.Action2 },
            { Keys.O, InputAction.Action3 },
            { Keys.Escape, InputAction.Back },
            { Keys.OemTilde, InputAction.Debug },
            { Keys.Enter, InputAction.Confirm },
            { Keys.Space, InputAction.Confirm },
            { Keys.K, InputAction.DebugKill },
            { Keys.C, InputAction.DebugComplete }
        };

        _movementKeys = new()
        {
            { Keys.W, new Vector2(0, -1) },
            { Keys.S, new Vector2(0, 1) },
            { Keys.A, new Vector2(-1, 0) },
            { Keys.D, new Vector2(1, 0) },
            { Keys.Up, new Vector2(0, -1) },
            { Keys.Down, new Vector2(0, 1) },
            { Keys.Left, new Vector2(-1, 0) },
            { Keys.Right, new Vector2(1, 0) }
        };

        _menuKeys = new()
        {
            { Keys.Up, InputAction.MenuUp },
            { Keys.Down, InputAction.MenuDown },
            { Keys.W, InputAction.MenuUp },
            { Keys.S, InputAction.MenuDown }
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
            foreach (var (key, action) in _menuKeys)
            {
                if (keyboardState.WasKeyPressed(key))
                {
                    ActionTriggered?.Invoke(action);
                }
            }
        }

        foreach (var (key, action) in _keyActions)
        {
            if (keyboardState.WasKeyPressed(key))
            {
                ActionTriggered?.Invoke(action);
            }
        }
    }
}