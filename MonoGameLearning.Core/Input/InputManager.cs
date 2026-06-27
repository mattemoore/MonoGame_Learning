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

    private readonly List<(HashSet<Keys> keys, InputAction action, InputMode? mode)> _bindings;
    private readonly List<(HashSet<Keys> keys, Vector2 direction)> _movementBindings;

    public InputManager()
    {
        _bindings =
        [
            (new() { Keys.U }, InputAction.Action1, InputMode.Gameplay),
            (new() { Keys.I }, InputAction.Action2, InputMode.Gameplay),
            (new() { Keys.O }, InputAction.Action3, InputMode.Gameplay),
            (new() { Keys.Escape }, InputAction.Back, null),
            (new() { Keys.OemTilde }, InputAction.Debug, null),
            (new() { Keys.Enter, Keys.Space }, InputAction.Confirm, null),
            (new() { Keys.K }, InputAction.DebugKill, InputMode.Gameplay),
            (new() { Keys.C }, InputAction.DebugComplete, InputMode.Gameplay),
            (new() { Keys.Up, Keys.W }, InputAction.MenuUp, InputMode.Menu),
            (new() { Keys.Down, Keys.S }, InputAction.MenuDown, InputMode.Menu),
        ];

        _movementBindings =
        [
            (new() { Keys.W, Keys.Up }, new Vector2(0, -1)),
            (new() { Keys.S, Keys.Down }, new Vector2(0, 1)),
            (new() { Keys.A, Keys.Left }, new Vector2(-1, 0)),
            (new() { Keys.D, Keys.Right }, new Vector2(1, 0)),
        ];
    }

    public void Update(GameTime gameTime)
    {
        KeyboardExtended.Update();
        var keyboardState = KeyboardExtended.GetState();

        Vector2 newMovementDirection = Vector2.Zero;
        foreach (var (keys, direction) in _movementBindings)
        {
            foreach (var key in keys)
            {
                if (keyboardState.IsKeyDown(key))
                {
                    newMovementDirection += direction;
                    break;
                }
            }
        }

        if (newMovementDirection != Vector2.Zero)
            newMovementDirection.Normalize();
        MovementDirection = Mode == InputMode.Gameplay ? newMovementDirection : Vector2.Zero;

        foreach (var (keys, action, mode) in _bindings)
        {
            if (mode is not null && mode != Mode) continue;
            foreach (var key in keys)
            {
                if (keyboardState.WasKeyPressed(key))
                {
                    ActionTriggered?.Invoke(action);
                    break;
                }
            }
        }
    }
}