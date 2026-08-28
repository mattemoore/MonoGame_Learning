using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Core.UI;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class HudServiceTests
{
    private StubHudPlayerData _player;
    private HudService _hud;
    private TestEnemyEntity _enemy;

    [SetUp]
    public void Setup()
    {
        _player = new StubHudPlayerData();
        _hud = new HudService(_player, null!);
        _enemy = new TestEnemyEntity("testEnemy", Vector2.Zero);
    }

    private static GameTime Time(float seconds) =>
        new(TimeSpan.Zero, TimeSpan.FromSeconds(seconds));

    [Test]
    public void HudService_RootWidget_IsNotNull()
    {
        Assert.That(_hud.RootWidget, Is.Not.Null);
        Assert.That(_hud.RootWidget, Is.InstanceOf<UiBase>());
    }

    [Test]
    public void HudService_OnEnemyHit_SetsTarget()
    {
        _hud.OnEnemyHit(_enemy);
        _hud.RootWidget.Update(Time(0f));

        Assert.That(_hud.EnemyBarTarget, Is.EqualTo(_enemy));
        Assert.That(_hud.IsEnemyBarVisible, Is.True);
    }

    [Test]
    public void HudService_OnEnemyHit_AfterTimeout_Hides()
    {
        _hud.OnEnemyHit(_enemy);
        _hud.RootWidget.Update(Time(0f));
        Assert.That(_hud.IsEnemyBarVisible, Is.True);

        _hud.RootWidget.Update(Time(1.5f));

        Assert.That(_hud.IsEnemyBarVisible, Is.False);
    }

    [Test]
    public void HudService_OnEnemyHit_MultipleHitsResetsTimer()
    {
        _hud.OnEnemyHit(_enemy);
        _hud.RootWidget.Update(Time(1.0f));
        Assert.That(_hud.IsEnemyBarVisible, Is.True);

        _hud.OnEnemyHit(_enemy);
        _hud.RootWidget.Update(Time(1.0f));

        Assert.That(_hud.IsEnemyBarVisible, Is.True);

        _hud.RootWidget.Update(Time(1.0f));

        Assert.That(_hud.IsEnemyBarVisible, Is.False);
    }

    [Test]
    public void HudService_OnEnemyHit_DeathLinger_ShowsRedX()
    {
        _hud.OnEnemyHit(_enemy);
        _hud.RootWidget.Update(Time(0f));
        Assert.That(_hud.IsEnemyBarVisible, Is.True);

        _enemy.TakeDamage(new DamageInfo { Amount = 9999 });
        _hud.RootWidget.Update(Time(0f));

        Assert.That(_hud.IsEnemyBarVisible, Is.True, "Bar should stay visible during death linger");
        Assert.That(_hud.EnemyBarTarget, Is.EqualTo(_enemy), "Target should remain during death linger");
        Assert.That(_hud.IsDeathLinger, Is.True, "Should be in death linger state");
    }

    [Test]
    public void HudService_OnEnemyHit_DeathLinger_Expires()
    {
        _hud.OnEnemyHit(_enemy);
        _hud.RootWidget.Update(Time(0f));

        _enemy.TakeDamage(new DamageInfo { Amount = 9999 });

        _hud.RootWidget.Update(Time(1.5f));

        Assert.That(_hud.IsEnemyBarVisible, Is.False, "Bar should hide after death linger expires");
        Assert.That(_hud.EnemyBarTarget, Is.Null, "Target should be cleared after death linger");
        Assert.That(_hud.IsDeathLinger, Is.False, "Should not be in death linger after expiry");
    }

    [Test]
    public void HudService_OnEnemyHit_NewHitOverridesDeathLinger()
    {
        _hud.OnEnemyHit(_enemy);
        _hud.RootWidget.Update(Time(0f));

        _enemy.TakeDamage(new DamageInfo { Amount = 9999 });
        _hud.RootWidget.Update(Time(0f));
        Assert.That(_hud.IsDeathLinger, Is.True, "Should be in death linger");

        var enemy2 = new TestEnemyEntity("enemy2", new Vector2(100, 100));
        _hud.OnEnemyHit(enemy2);
        _hud.RootWidget.Update(Time(0f));

        Assert.That(_hud.EnemyBarTarget, Is.EqualTo(enemy2));
        Assert.That(_hud.IsDeathLinger, Is.False, "New hit should clear death linger state");
        Assert.That(_hud.IsEnemyBarVisible, Is.True);
    }

    [Test]
    public void HudService_OnEnemyHit_FromDifferentEnemy_SwitchesTarget()
    {
        var enemy2 = new TestEnemyEntity("enemy2", new Vector2(100, 100));

        _hud.OnEnemyHit(_enemy);
        _hud.RootWidget.Update(Time(0f));
        Assert.That(_hud.EnemyBarTarget, Is.EqualTo(_enemy));

        _hud.OnEnemyHit(enemy2);
        _hud.RootWidget.Update(Time(0f));

        Assert.That(_hud.EnemyBarTarget, Is.EqualTo(enemy2));
    }

    [Test]
    public void HudService_ProximityTarget_ShowsWhenNoActiveHit()
    {
        _hud.SetProximityTarget(_enemy);
        _hud.RootWidget.Update(Time(0f));

        Assert.That(_hud.IsEnemyBarVisible, Is.True);
        Assert.That(_hud.EnemyBarTarget, Is.EqualTo(_enemy));
    }

    [Test]
    public void HudService_ActiveHitTarget_PriorityOverProximity()
    {
        _hud.SetProximityTarget(_enemy);
        var enemy2 = new TestEnemyEntity("enemy2", new Vector2(100, 100));
        _hud.OnEnemyHit(enemy2);
        _hud.RootWidget.Update(Time(0f));

        Assert.That(_hud.EnemyBarTarget, Is.EqualTo(enemy2), "Hit target should take priority over proximity");
    }

    [Test]
    public void HudService_ProximityTarget_DeadTarget_Hides()
    {
        _hud.SetProximityTarget(_enemy);
        _hud.RootWidget.Update(Time(0f));
        Assert.That(_hud.IsEnemyBarVisible, Is.True);

        _enemy.TakeDamage(new DamageInfo { Amount = 9999 });
        _hud.RootWidget.Update(Time(0f));

        Assert.That(_hud.IsEnemyBarVisible, Is.False, "Proximity target that dies should hide immediately");
    }

    [Test]
    public void HudService_ProximityTarget_UpdatesEachFrame()
    {
        _hud.SetProximityTarget(_enemy);
        _hud.RootWidget.Update(Time(0f));
        Assert.That(_hud.EnemyBarTarget, Is.EqualTo(_enemy));

        var enemy2 = new TestEnemyEntity("enemy2", new Vector2(200, 200));
        _hud.SetProximityTarget(enemy2);
        _hud.RootWidget.Update(Time(0f));

        Assert.That(_hud.EnemyBarTarget, Is.EqualTo(enemy2), "Proximity target should update when GameLoop sets a new one");
    }

    [Test]
    public void HudService_ProximityTarget_Null_Hides()
    {
        _hud.SetProximityTarget(_enemy);
        _hud.RootWidget.Update(Time(0f));
        Assert.That(_hud.IsEnemyBarVisible, Is.True);

        _hud.SetProximityTarget(null!);
        _hud.RootWidget.Update(Time(0f));

        Assert.That(_hud.IsEnemyBarVisible, Is.False, "Null proximity target should hide bar");
    }

    [Test]
    public void HudService_RootWidget_IsScreenRenderableAndUpdatable()
    {
        Assert.That(_hud.RootWidget, Is.InstanceOf<IScreenRenderable>());
        Assert.That(_hud.RootWidget, Is.InstanceOf<IUpdatable>());
        Assert.That(_hud.RootWidget, Is.Not.InstanceOf<Entity>());
    }
}