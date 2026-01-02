using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Input;

namespace MonoGameLearning.Input;

public class InputManager
{
    public event EventHandler Action1Pressed, Action2Pressed, Action3Pressed;
    public event EventHandler BackPressed;
    public Vector2 MovementDirection { get; private set; }

    public InputManager()
    {

    }

    public void Update(GameTime gameTime)
    {
        KeyboardExtended.Update();
        KeyboardStateExtended keyboardStateExtended = KeyboardExtended.GetState();
        Vector2 newMovementDirection = Vector2.Zero;
        if (keyboardStateExtended.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W)) newMovementDirection.Y -= 1;
        if (keyboardStateExtended.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S)) newMovementDirection.Y += 1;
        if (keyboardStateExtended.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A)) newMovementDirection.X -= 1;
        if (keyboardStateExtended.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D)) newMovementDirection.X += 1;

        // Normalize so diagonal movement isn't faster (1.41x speed)
        if (newMovementDirection != Vector2.Zero)
        {
            newMovementDirection.Normalize();
        }
        MovementDirection = newMovementDirection;

        if (keyboardStateExtended.WasKeyPressed(Microsoft.Xna.Framework.Input.Keys.U))
        {
            Action1Pressed?.Invoke(this, EventArgs.Empty);
        }
        else if (keyboardStateExtended.WasKeyPressed(Microsoft.Xna.Framework.Input.Keys.I))
        {
            Action2Pressed?.Invoke(this, EventArgs.Empty);
        }
        else if (keyboardStateExtended.WasKeyPressed(Microsoft.Xna.Framework.Input.Keys.O))
        {
            Action3Pressed?.Invoke(this, EventArgs.Empty);
        }
        else if (keyboardStateExtended.WasKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
        {
            BackPressed?.Invoke(this, EventArgs.Empty);
        }
    }
}
