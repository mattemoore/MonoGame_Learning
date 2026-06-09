using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities;

public class BackgroundEntity(string name, Sprite sprite, Vector2 position, int width, int height) : SpatialEntity(name, position, width, height)
{
    public RectangleF MovementBounds { get; set; } = new(position.X - (width / 2f), position.Y - (height / 2f) + (height * 0.6f), width, height * 0.4f);
    Sprite Sprite { get; init; } = sprite;

    public void Draw(SpriteBatch spriteBatch)
    {
        if (Sprite is not null)
            spriteBatch.Draw(Sprite, Frame.Position);
    }

    public override void DrawDebug(SpriteBatch spriteBatch)
    {
        spriteBatch.DrawRectangle(MovementBounds, Color.Yellow);
        base.DrawDebug(spriteBatch);

    }

    public override void Update(GameTime gameTime)
    {

    }
}