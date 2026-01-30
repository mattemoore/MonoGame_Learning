using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Levels;

public class Level(List<BackgroundEntity> backgrounds)
{
    public List<BackgroundEntity> Backgrounds { get; } = backgrounds;

    public void Update(GameTime gameTime)
    {
        foreach (var bg in Backgrounds)
        {
            bg.Update(gameTime);
        }
    }

    public int Draw(SpriteBatch spriteBatch, OrthographicCamera camera)
    {
        int drawnCount = 0;
        var cameraBounds = camera.BoundingRectangle;
        foreach (var bg in Backgrounds)
        {
            if (cameraBounds.Intersects(bg.Frame))
            {
                bg.Draw(spriteBatch);
                drawnCount++;
            }
        }
        return drawnCount;
    }
}
