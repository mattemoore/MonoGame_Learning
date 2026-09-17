using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Game.Weapons;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class ProjectileServiceTests
{
    private static readonly RectangleF Bounds = new(0, 0, 2000, 600);

    private static EntityService CreateManager(HitboxService hitboxService) =>
        new(CollisionWorldFactory.Create(Bounds), hitboxService);

    private static GameTime Seconds(float seconds) => new(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));

    private static void RegisterKnifeHitbox(HitboxService service, ProjectileEntity projectile) =>
        service.RegisterHitbox(projectile, projectile.Position, Faction.Player, KnifeWeapon.Knife.ProjectileHitbox,
            KnifeWeapon.Knife.Damage, KnifeWeapon.Knife.Knockdown, KnifeWeapon.Knife.Strength,
            KnifeWeapon.Knife.ImpactSfx, FacingDirection.Right);

    // --- Hitbox pipeline ---

    [Test]
    public void ProjectileHitbox_ReportsProjectileAsSource()
    {
        var service = new HitboxService();
        var projectile = new ProjectileEntity();
        projectile.Launch(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);
        RegisterKnifeHitbox(service, projectile);

        var enemy = new StubCombatActor("enemy", new Vector2(10, 0), 20, 20, Faction.Enemy);
        var hits = service.ResolveHits([enemy]);

        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Source, Is.SameAs(projectile));
        Assert.That(hits[0].Amount, Is.EqualTo(KnifeWeapon.Knife.Damage));
    }

    [Test]
    public void ProjectileHitbox_DoesNotHitSameFaction()
    {
        var service = new HitboxService();
        var projectile = new ProjectileEntity();
        projectile.Launch(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);
        RegisterKnifeHitbox(service, projectile);

        var ally = new StubCombatActor("ally", new Vector2(10, 0), 20, 20, Faction.Player);
        var hits = service.ResolveHits([ally]);

        Assert.That(hits, Is.Empty);
    }

    [Test]
    public void ProjectileHitbox_HitsATargetOnlyOnce()
    {
        var service = new HitboxService();
        var projectile = new ProjectileEntity();
        projectile.Launch(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);
        RegisterKnifeHitbox(service, projectile);

        var enemy = new StubCombatActor("enemy", new Vector2(10, 0), 20, 20, Faction.Enemy);
        Assert.That(service.ResolveHits([enemy]), Has.Count.EqualTo(1));
        Assert.That(service.ResolveHits([enemy]), Is.Empty, "A projectile must not hit the same target twice");
    }

    [Test]
    public void ProjectileHitbox_HitsPropEvenThoughNeutral()
    {
        var service = new HitboxService();
        var projectile = new ProjectileEntity();
        projectile.Launch(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);
        RegisterKnifeHitbox(service, projectile);

        var prop = new TestPropEntity("prop", new Vector2(10, 0));
        var hits = service.ResolveHits([prop]);

        Assert.That(hits, Has.Count.EqualTo(1));
    }

    // --- Pooling ---

    [Test]
    public void Spawn_RegistersProjectile_AndActiveReflectsIt()
    {
        var hitbox = new HitboxService();
        var manager = CreateManager(hitbox);
        var service = new ProjectileService(manager, hitbox, () => new ProjectileEntity());

        var projectile = service.Spawn(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);

        Assert.That(service.Active, Has.Count.EqualTo(1));
        Assert.That(manager.All, Does.Contain(projectile));
        Assert.That(projectile.HitboxService, Is.SameAs(hitbox), "EntityService must inject the hitbox service");
    }

    [Test]
    public void Update_KeepsMidFlightProjectileActive()
    {
        var hitbox = new HitboxService();
        var manager = CreateManager(hitbox);
        var service = new ProjectileService(manager, hitbox, () => new ProjectileEntity());
        var projectile = service.Spawn(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);

        projectile.Update(Seconds(0.05f));
        service.Update();

        Assert.That(service.Active, Has.Count.EqualTo(1));
        Assert.That(hitbox.GetActiveHitboxBounds(projectile), Has.Count.EqualTo(1));
    }

    [Test]
    public void Despawn_ClearsHitboxAndDedup_AndReusesInstance()
    {
        var hitbox = new HitboxService();
        var manager = CreateManager(hitbox);
        var service = new ProjectileService(manager, hitbox, () => new ProjectileEntity());
        var projectile = service.Spawn(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);

        projectile.Update(Seconds(0.05f));
        var enemy = new StubCombatActor("enemy", new Vector2(10, 0), 20, 20, Faction.Enemy);
        hitbox.ResolveHits([enemy]); // populates the owner dedup set

        projectile.MarkHit();
        service.Update();

        Assert.That(service.Active, Is.Empty);
        Assert.That(hitbox.GetActiveHitboxBounds(projectile), Is.Empty, "Returned projectiles must not leak hitboxes");

        manager.ProcessPending();
        var reused = service.Spawn(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);
        Assert.That(reused, Is.SameAs(projectile), "The pool must reuse the returned instance");
    }

    [Test]
    public void RangeExpired_Projectile_DoesNotLinger_AndIsReused()
    {
        var hitbox = new HitboxService();
        var manager = CreateManager(hitbox);
        var service = new ProjectileService(manager, hitbox, () => new ProjectileEntity());
        var projectile = service.Spawn(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);

        projectile.Update(Seconds(1f)); // past MaxRange
        Assert.That(projectile.Expired, Is.True);

        service.Update();
        Assert.That(service.Active, Is.Empty);

        manager.ProcessPending();
        var reused = service.Spawn(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);
        Assert.That(reused, Is.SameAs(projectile));
    }

    [Test]
    public void Clear_DropsActiveAndHitboxes()
    {
        var hitbox = new HitboxService();
        var manager = CreateManager(hitbox);
        var service = new ProjectileService(manager, hitbox, () => new ProjectileEntity());
        var projectile = service.Spawn(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);
        projectile.Update(Seconds(0.05f));

        service.Clear();

        Assert.That(service.Active, Is.Empty);
        Assert.That(hitbox.GetActiveHitboxBounds(projectile), Is.Empty);
    }
}
