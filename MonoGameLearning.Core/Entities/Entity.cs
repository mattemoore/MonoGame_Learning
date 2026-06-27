using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace MonoGameLearning.Core.Entities;

public abstract class Entity(string name, Vector2 position, int width, int height)
{
    public Vector2 Position { get; set; } = position;
    public int Width { get; init; } = width;
    public int Height { get; init; } = height;
    public string Name { get; } = name;

    private RectangleF _frame;
    private bool _frameDirty = true;
    private Vector2 _lastFramePosition;
    private int _lastFrameWidth;
    private int _lastFrameHeight;

    public RectangleF Frame
    {
        get
        {
            if (!_frameDirty && _lastFramePosition == Position && _lastFrameWidth == Width && _lastFrameHeight == Height)
                return _frame;

            _frame = new(
                Position.X - (Width / 2f),
                Position.Y - (Height / 2f),
                Width,
                Height
            );
            _frameDirty = false;
            _lastFramePosition = Position;
            _lastFrameWidth = Width;
            _lastFrameHeight = Height;
            return _frame;
        }
    }
}