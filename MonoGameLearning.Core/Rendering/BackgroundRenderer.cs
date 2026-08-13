using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Rendering;

public class BackgroundRenderer(List<Sprite> sprites, int gameWidth, int gameHeight) : IRenderable
{
    public RectangleF Frame => new(0, 0, gameWidth * sprites.Count, gameHeight);
    public int LastFrameDrawCount { get; private set; }

    public void Render(RenderContext context)
    {
        var cameraBounds = context.Camera.BoundingRectangle;
        float bgY = gameHeight / 2f;
        int drawn = 0;
        for (int i = 0; i < sprites.Count; i++)
        {
            float x = i * gameWidth;
            var bgRect = new RectangleF(x, bgY - gameHeight / 2f, gameWidth, gameHeight);
            if (cameraBounds.Intersects(bgRect))
            {
                context.SpriteBatch.Draw(sprites[i], new Vector2(x, bgY - gameHeight / 2f));
                drawn++;
            }
        }
        LastFrameDrawCount = drawn;
    }

    public static BackgroundRenderer Create(ContentManager content, int gameWidth, int gameHeight, int backgroundCount)
    {
        var sprites = new List<Sprite>(backgroundCount);
        var tex = content.Load<Texture2D>("backgrounds/background1");
        for (int i = 0; i < backgroundCount; i++)
            sprites.Add(new Sprite(tex));
        return new(sprites, gameWidth, gameHeight);
    }
}