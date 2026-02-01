using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Levels;

public class Level1 : Level
{
    public Level1(ContentManager content, int gameWidth, int gameHeight) : base(CreateBackgrounds(content, gameWidth, gameHeight))
    {
    }

    private static List<BackgroundEntity> CreateBackgrounds(ContentManager content, int gameWidth, int gameHeight)
    {
        Sprite background = new(content.Load<Texture2D>("backgrounds/background"));

        float bgCenterX = gameWidth / 2f;
        float bgCenterY = gameHeight / 2f;
        var bg1 = new BackgroundEntity("bg1", background, new Vector2(bgCenterX, bgCenterY), gameWidth, gameHeight);
        var bg2 = new BackgroundEntity("bg2", background, new Vector2(bgCenterX + gameWidth, bgCenterY), gameWidth, gameHeight);

        return [bg1, bg2];
    }
}
