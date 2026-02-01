using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Levels;

public abstract class Level
{
    public List<BackgroundEntity> Backgrounds { get; }
    public RectangleF MovementBounds { get; }

    protected Level(List<BackgroundEntity> backgrounds)
    {
        Backgrounds = backgrounds;

        ValidateConnectivity(Backgrounds);

        if (Backgrounds.Count > 0)
        {
            MovementBounds = Backgrounds.Select(b => b.MovementBounds).Aggregate(RectangleF.Union);
        }
    }

    public static void ValidateConnectivity(List<BackgroundEntity> backgrounds)
    {
        for (int i = 0; i < backgrounds.Count - 1; i++)
        {
            var b1 = backgrounds[i];
            var b2 = backgrounds[i + 1];

            // Inflate slightly to allow touching edges to count as connected
            var checkBounds = b1.MovementBounds;
            checkBounds.Inflate(0.1f, 0.1f);

            if (!checkBounds.Intersects(b2.MovementBounds))
            {
                throw new System.InvalidOperationException($"Backgrounds at index {i} ('{b1.Name}') and {i + 1} ('{b2.Name}') are not connected. Player movement would be broken.");
            }
        }
    }

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

    public virtual void DrawDebug(SpriteBatch spriteBatch)
    {
        foreach (var bg in Backgrounds)
        {
            bg.DrawDebug(spriteBatch);
        }
    }
}
