using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Movement;

namespace MonoGameLearning.Core.Combat;

/// <summary>
/// Lazy pool for <see cref="ProjectileEntity"/>. Spawns happen from input handlers, which
/// run after <c>EntityService.ProcessPending</c>, so a projectile returned on the previous
/// frame is already deregistered before the next rent re-registers it.
/// </summary>
public class ProjectileService(EntityService entityService, HitboxService hitboxService, Func<ProjectileEntity> createProjectile)
{
    private readonly EntityService _entityService = entityService;
    private readonly HitboxService _hitboxService = hitboxService;
    private readonly Func<ProjectileEntity> _createProjectile = createProjectile;
    private readonly Stack<ProjectileEntity> _free = [];
    private readonly List<ProjectileEntity> _active = [];

    internal IReadOnlyList<ProjectileEntity> Active => _active;

    public ProjectileEntity Spawn(ThrowableWeaponDef def, Vector2 origin, FacingDirection facing, Faction faction)
    {
        var projectile = _free.Count > 0 ? _free.Pop() : _createProjectile();
        projectile.Launch(def, origin, facing, faction);
        _entityService.Register(projectile);
        _active.Add(projectile);
        return projectile;
    }

    /// <summary>Despawns every projectile spent by hit or range. Call after hit resolution.</summary>
    public void Update()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].Expired)
                Despawn(i);
        }
    }

    /// <summary>Drops all active state (level reset); pooled instances are discarded with it.</summary>
    public void Clear()
    {
        for (int i = 0; i < _active.Count; i++)
        {
            _hitboxService.Clear(_active[i]);
            _hitboxService.ClearAttackDedup(_active[i]);
        }
        _active.Clear();
        _free.Clear();
    }

    private void Despawn(int index)
    {
        var projectile = _active[index];
        _hitboxService.Clear(projectile);
        _hitboxService.ClearAttackDedup(projectile);
        _entityService.Destroy(projectile);
        _active.RemoveAt(index);
        _free.Push(projectile);
    }
}
