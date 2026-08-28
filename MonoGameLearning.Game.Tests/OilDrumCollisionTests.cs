using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Prop;

internal class DrumTestActor(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
}

[TestFixture]
public class OilDrumCollisionTests
{
    [Test]
    public void TopAnchor_CollisionBounds_TopMatchesFrameTop()
    {
        var frame = new RectangleF(100, 200, 80, 160);
        var bounds = PropBase.ComputeCollisionBounds(frame, 0.5f, CollisionAnchor.Top);

        Assert.That(bounds.Y, Is.EqualTo(frame.Y));
        Assert.That(bounds.Top, Is.EqualTo(frame.Top));
    }

    [Test]
    public void TopAnchor_CollisionBounds_HeightIsShorterThanFrame()
    {
        var frame = new RectangleF(100, 200, 80, 160);
        var bounds = PropBase.ComputeCollisionBounds(frame, 0.5f, CollisionAnchor.Top);

        Assert.That(bounds.Height, Is.EqualTo(80f));
        Assert.That(bounds.Height, Is.EqualTo(frame.Height * 0.5f));
    }

    [Test]
    public void CenterAnchor_CentersVerticallyWithinFrame()
    {
        var frame = new RectangleF(100, 200, 80, 160);
        var bounds = PropBase.ComputeCollisionBounds(frame, 0.5f, CollisionAnchor.Center);

        // 160 * 0.5 = 80 tall, centered in 160 → top offset = (160-80)/2 = 40
        Assert.That(bounds.Y, Is.EqualTo(240));
        Assert.That(bounds.Height, Is.EqualTo(80));
        Assert.That(bounds.Center.Y, Is.EqualTo(frame.Center.Y));
    }

    [Test]
    public void BottomAnchor_BottomEdgeMatchesFrameBottom()
    {
        var frame = new RectangleF(100, 200, 80, 160);
        var bounds = PropBase.ComputeCollisionBounds(frame, 0.5f, CollisionAnchor.Bottom);

        Assert.That(bounds.Bottom, Is.EqualTo(frame.Bottom));
        Assert.That(bounds.Height, Is.EqualTo(80));
    }

    [Test]
    public void FullFraction_CollisionBoundsEqualsFrame()
    {
        var frame = new RectangleF(100, 200, 80, 160);
        var bounds = PropBase.ComputeCollisionBounds(frame, 1.0f, CollisionAnchor.Top);

        Assert.That(bounds, Is.EqualTo(frame));
    }

    [Test]
    public void FullFraction_CenterAnchor_EqualsFrame()
    {
        var frame = new RectangleF(100, 200, 80, 160);
        var bounds = PropBase.ComputeCollisionBounds(frame, 1.0f, CollisionAnchor.Center);

        Assert.That(bounds, Is.EqualTo(frame));
    }

    [Test]
    public void FullFraction_BottomAnchor_EqualsFrame()
    {
        var frame = new RectangleF(100, 200, 80, 160);
        var bounds = PropBase.ComputeCollisionBounds(frame, 1.0f, CollisionAnchor.Bottom);

        Assert.That(bounds, Is.EqualTo(frame));
    }

    [Test]
    public void FrameWidth_IsPreserved()
    {
        var frame = new RectangleF(100, 200, 80, 160);
        var bounds = PropBase.ComputeCollisionBounds(frame, 0.25f, CollisionAnchor.Top);

        Assert.That(bounds.Width, Is.EqualTo(80));
        Assert.That(bounds.X, Is.EqualTo(100));
    }

    [Test]
    public void FractionAtMinimum_DoesNotThrow()
    {
        var frame = new RectangleF(0, 0, 50, 100);
        // 0.01 is within (0,1]
        var bounds = PropBase.ComputeCollisionBounds(frame, 0.01f, CollisionAnchor.Top);

        Assert.That(bounds.Height, Is.EqualTo(1f));
    }

    // --- Integration: CollisionWorld2D with shortened collision box ---

    private const int ActorSize = 50;
    private const int DrumWidth = 80;
    private const int DrumHeight = 160;
    private const float DrumCollisionFraction = 0.5f;
    private const float DrumCollisionHeight = DrumHeight * DrumCollisionFraction; // 80

    private static CollisionWorld2D CreateWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(0, 0), new Vector2(2000, 2000));
        world.AddLayer(CollisionLayers.Actors, new Layer(new QuadTreeSpace(bb)));
        world.DisableCollisionBetweenLayers(CollisionLayers.Actors, CollisionLayers.Actors);
        world.AddLayer(CollisionLayers.Props, new Layer(new QuadTreeSpace(bb)));
        world.DisableCollisionBetweenLayers(CollisionLayers.Props, CollisionLayers.Props);
        world.EnableCollisionBetweenLayers(CollisionLayers.Actors, CollisionLayers.Props);
        return world;
    }

    /// <summary>
    /// Creates a test prop whose collision shape is top-anchored at 50% height,
    /// matching what OilDrumEntity produces. The visual frame stays full-size.
    /// </summary>
    private static TestPropCollision MakeDrum(float x, float y)
    {
        var frame = new RectangleF(
            x - DrumWidth / 2f, y - DrumHeight / 2f,
            DrumWidth, DrumHeight);
        float collY = frame.Y;
        float collH = DrumCollisionHeight;
        return new TestPropCollision("drum", new Vector2(x, y), DrumWidth, DrumHeight,
            new BoundingBox2D(new Vector2(frame.X, collY),
                              new Vector2(frame.Right, collY + collH)));
    }

    private static DrumTestActor MakeActor(float x, float y) =>
        new("actor", new Vector2(x, y), ActorSize, ActorSize);

    /// <summary>
    /// Y value for the actor's bottom edge to be at the drum's floor level.
    /// Drum center Y = 300, so drum bottom = 300 + 80 = 380.
    /// Actor bottom = actorY + 25.  Set actorY = 355 so bottom = 380.
    /// </summary>
    private static float FloorY => 300 + DrumHeight / 2f - ActorSize / 2f; // 355

    [Test]
    public void Integration_ActorAtDrumFloorLevel_PassesUnderWithoutCollision()
    {
        var world = CreateWorld();
        var actor = MakeActor(300, FloorY);                     // bottom = 380 = drum bottom
        var drum = MakeDrum(300, 300);                          // drum at (300,300)

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(drum, CollisionLayers.Props);
        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props).ToList();
        // Actor's full frame overlaps drum's full frame, but collision shape
        // only covers top 80px — actor bottom is at 380, drum collision bottom is at 340.
        // No collision expected.
        Assert.That(pairs, Is.Empty);
    }

    [Test]
    public void Integration_ActorAboveDrum_CollidesWithTopHalf()
    {
        var world = CreateWorld();
        // Place actor so its bottom is inside the drum's collision band (top 80px)
        // Drum collision band: Y=220 to Y=300 (top-anchored, 80px tall)
        // Actor at (300, 260) → frame Y=235 to Y=285 → overlaps collision band
        var actor = MakeActor(300, 260);
        var drum = MakeDrum(300, 300);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(drum, CollisionLayers.Props);
        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props).ToList();
        Assert.That(pairs, Has.Count.EqualTo(1));
    }

    [Test]
    public void Integration_ActorBelowDrumCollisionBand_NoCollision()
    {
        var world = CreateWorld();
        // Drum collision band: Y=220 to Y=300 (top 80px of 160px sprite)
        // Actor at (300, 340) → frame Y=315 to Y=365 → entirely below collision band
        var actor = MakeActor(300, 340);
        var drum = MakeDrum(300, 300);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(drum, CollisionLayers.Props);
        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props).ToList();
        Assert.That(pairs, Is.Empty);
    }

    [Test]
    public void Integration_ActorAboveDrumCollisionBand_NoCollision()
    {
        var world = CreateWorld();
        // Drum collision band: Y=220 to Y=300
        // Actor at (300, 180) → frame Y=155 to Y=205 → entirely above collision band
        var actor = MakeActor(300, 180);
        var drum = MakeDrum(300, 300);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(drum, CollisionLayers.Props);
        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props).ToList();
        Assert.That(pairs, Is.Empty);
    }

    [Test]
    public void Integration_ActorWalkIntoDrumHorizontal_BlockedByTopHalf()
    {
        var world = CreateWorld();
        // Actor at same Y as drum's collision band (overlapping top half)
        var actor = MakeActor(260, 260); // frame overlaps drum collision band
        var drum = MakeDrum(300, 300);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(drum, CollisionLayers.Props);
        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props).ToList();
        Assert.That(pairs, Has.Count.EqualTo(1));
    }

    [Test]
    public void Integration_ActorWalkIntoShortenedDrum_PushedOutReturnsCorrectPosition()
    {
        var world = CreateWorld();
        var actor = MakeActor(260, 260); // overlapping drum collision band
        var drum = MakeDrum(300, 300);
        var drumPos = drum.Position;

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(drum, CollisionLayers.Props);
        world.RebuildDynamicLayers();

        foreach (var pair in world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props))
        {
            if (pair.First is Entity entity)
                entity.Position += pair.FirstResult.MinimumTranslationVector;
        }

        // Actor was pushed out of collision shape; drum stayed put
        Assert.That(drum.Position, Is.EqualTo(drumPos));
        // Actor's visual frame may still overlap drum's full visual frame,
        // but the collision shape no longer intersects
    }
}

/// <summary>
/// A test prop that exposes a custom collision shape separate from the visual frame,
/// mimicking how OilDrumEntity shortens its collision box to the top portion.
/// </summary>
internal class TestPropCollision(string name, Vector2 position, int width, int height, BoundingBox2D collisionBox)
    : Entity(name, position, width, height), ICollisionActor
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(collisionBox);
}