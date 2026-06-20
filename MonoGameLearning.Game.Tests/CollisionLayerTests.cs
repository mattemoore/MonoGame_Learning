using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Tests;

public class TestProp(string name, Vector2 position, int width, int height)
    : PropEntity(name, position, width, height)
{
    public override void TakeDamage(int amount, bool knockdown = false) { }
}

[TestFixture]
public class CollisionLayerTests
{
    private const int EntitySize = 50;

    private static RectangleF Bounds => new(0, 0, 2000, 2000);

    private static TestActorEntity MakeActor(float x, float y) =>
        new("actor", new Vector2(x, y), EntitySize, EntitySize);

    private static TestProp MakeProp(float x, float y) =>
        new("prop", new Vector2(x, y), EntitySize, EntitySize);

    private static CollisionComponent CreateCollision()
    {
        var cc = new CollisionComponent(Bounds);
        cc.Add("actors", new Layer(new QuadTreeSpace(Bounds)));
        return cc;
    }

    [Test]
    public void ActorActor_SameLayer_PassThrough()
    {
        var cc = CreateCollision();
        var a1 = MakeActor(100, 100);
        var a2 = MakeActor(110, 100);

        cc.Insert(a1);
        cc.Insert(a2);

        var pos1 = a1.Position;
        var pos2 = a2.Position;

        cc.Update(new GameTime());

        Assert.That(a1.Position, Is.EqualTo(pos1));
        Assert.That(a2.Position, Is.EqualTo(pos2));
    }

    [Test]
    public void ActorProp_ActorPushedOutOfProp()
    {
        var cc = CreateCollision();
        var actor = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        cc.Insert(actor);
        cc.Insert(prop);

        var propPos = prop.Position;

        cc.Update(new GameTime());

        Assert.That(actor.Position, Is.Not.EqualTo(new Vector2(100, 100)));
        Assert.That(prop.Position, Is.EqualTo(propPos));
    }

    [Test]
    public void EnemyProp_EnemyBlockedByProp()
    {
        var cc = CreateCollision();
        var enemy = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        cc.Insert(enemy);
        cc.Insert(prop);

        var propPos = prop.Position;

        cc.Update(new GameTime());

        Assert.That(enemy.Position, Is.Not.EqualTo(new Vector2(100, 100)));
        Assert.That(prop.Position, Is.EqualTo(propPos));
    }

    [Test]
    public void Prop_Prop_NoMovement()
    {
        var cc = CreateCollision();
        var p1 = MakeProp(100, 100);
        var p2 = MakeProp(110, 100);

        cc.Insert(p1);
        cc.Insert(p2);

        var pos1 = p1.Position;
        var pos2 = p2.Position;

        cc.Update(new GameTime());

        Assert.That(p1.Position, Is.EqualTo(pos1));
        Assert.That(p2.Position, Is.EqualTo(pos2));
    }

    [Test]
    public void ActorProp_ActorFullySeparated()
    {
        var cc = CreateCollision();
        var actor = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        cc.Insert(actor);
        cc.Insert(prop);

        cc.Update(new GameTime());

        Assert.That(actor.Frame.Intersects(prop.Frame), Is.False);
        Assert.That(prop.Position, Is.EqualTo(new Vector2(110, 100)));
    }
}