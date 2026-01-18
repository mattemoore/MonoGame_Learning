using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities;

public class BackgroundEntity(string name, Sprite sprite, Vector2 position, int width, int height) : Entity(name, position, width, height)
{
    Sprite Sprite { get; init; } = sprite;

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(Sprite, Vector2.Zero);
    }

    public override void DrawDebug(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(Sprite, Position, 0, Vector2.One);
    }

    public override void Update(GameTime gameTime)
    {

    }
}