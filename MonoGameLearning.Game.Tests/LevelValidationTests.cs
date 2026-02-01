using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Levels;
using NUnit.Framework;

namespace MonoGameLearning.Tests;

[TestFixture]
public class LevelValidationTests
{
    [Test]
    public void ValidateConnectivity_WithConnectedBackgrounds_ShouldPass()
    {
        // Arrange
        // Passing null for sprite since we only test bounds
        var bg1 = new BackgroundEntity("bg1", null, new Vector2(0, 0), 100, 100);
        // bg2 is exactly to the right of bg1. 
        // bg1 center (0,0), width 100. Right edge at +50.
        // bg2 starts at center (100, 0). Left edge at 100-50 = 50.
        // They touch at x=50.
        var bg2 = new BackgroundEntity("bg2", null, new Vector2(100, 0), 100, 100);

        var backgrounds = new List<BackgroundEntity> { bg1, bg2 };

        // Act & Assert
         var ex = Assert.Throws<InvalidOperationException>(() => Level.ValidateConnectivity(backgrounds));
        Assert.That(ex.Message, Contains.Substring("are not connected"));
   }

    [Test]
    public void ValidateConnectivity_WithDisconnectedBackgrounds_ShouldThrow()
    {
        // Arrange
        var bg1 = new BackgroundEntity("bg1", null, new Vector2(0, 0), 100, 100);
        // bg2 is far to the right, creating a gap.
        // bg1 right edge = 50.
        // bg2 center (200, 0). Left edge = 150.
        // Gap = 100.
        var bg2 = new BackgroundEntity("bg2", null, new Vector2(200, 0), 100, 100);

        var backgrounds = new List<BackgroundEntity> { bg1, bg2 };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => Level.ValidateConnectivity(backgrounds));
        Assert.That(ex.Message, Contains.Substring("are not connected"));
    }
}
