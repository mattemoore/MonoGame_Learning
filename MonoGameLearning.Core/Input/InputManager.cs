using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;

namespace MonoGameLearning.Core.Input;

public class InputManager()
{
    public event EventHandler Action1Pressed, Action2Pressed, Action3Pressed;
    public event EventHandler BackPressed, DebugPressed;
    public Vector2 MovementDirection { get; private set; }

    public void Update(GameTime gameTime)
    {
        KeyboardExtended.Update();
        var keyboardState = KeyboardExtended.GetState();
        
        Vector2 newMovementDirection = Vector2.Zero;
        if (keyboardState.IsKeyDown(Keys.W)) newMovementDirection.Y -= 1;
        if (keyboardState.IsKeyDown(Keys.S)) newMovementDirection.Y += 1;
        if (keyboardState.IsKeyDown(Keys.A)) newMovementDirection.X -= 1;
        if (keyboardState.IsKeyDown(Keys.D)) newMovementDirection.X += 1;

        if (newMovementDirection != Vector2.Zero)
        {
            newMovementDirection.Normalize();
        }
        MovementDirection = newMovementDirection;

        if (keyboardState.WasKeyPressed(Keys.U)) Action1Pressed?.Invoke(this, EventArgs.Empty);
        else if (keyboardState.WasKeyPressed(Keys.I)) Action2Pressed?.Invoke(this, EventArgs.Empty);
        else if (keyboardState.WasKeyPressed(Keys.O)) Action3Pressed?.Invoke(this, EventArgs.Empty);
        else if (keyboardState.WasKeyPressed(Keys.Escape)) BackPressed?.Invoke(this, EventArgs.Empty);
        else if (keyboardState.WasKeyPressed(Keys.OemTilde)) DebugPressed?.Invoke(this, EventArgs.Empty);
    }
}
