using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;

namespace MonoGameLearning.Core.Input;

public class InputManager
{
    public event EventHandler Action1Pressed, Action2Pressed, Action3Pressed;
    public event EventHandler BackPressed, DebugPressed;
    public Vector2 MovementDirection { get; private set; }

    private readonly Dictionary<Keys, Action> _keyActions;
    private readonly Dictionary<Keys, Vector2> _movementKeys;

    public InputManager()
    {
        _keyActions = new()
        {
            { Keys.U, () => Action1Pressed?.Invoke(this, EventArgs.Empty) },
            { Keys.I, () => Action2Pressed?.Invoke(this, EventArgs.Empty) },
            { Keys.O, () => Action3Pressed?.Invoke(this, EventArgs.Empty) },
            { Keys.Escape, () => BackPressed?.Invoke(this, EventArgs.Empty) },
            { Keys.OemTilde, () => DebugPressed?.Invoke(this, EventArgs.Empty) }
        };

        _movementKeys = new()
        {
            { Keys.W, new Vector2(0, -1) },
            { Keys.S, new Vector2(0, 1) },
            { Keys.A, new Vector2(-1, 0) },
            { Keys.D, new Vector2(1, 0) }
        };
    }

    public void Update(GameTime gameTime)
    {
        KeyboardExtended.Update();
        var keyboardState = KeyboardExtended.GetState();

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

        foreach (var (key, action) in _keyActions)
        {
            if (keyboardState.WasKeyPressed(key))
            {
                action();
            }
        }
    }
}
