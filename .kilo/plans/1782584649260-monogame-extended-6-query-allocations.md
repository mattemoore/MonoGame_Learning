# MGE v6 Post-Migration: QueryCollisionPairs Allocation Follow-Up

## Goal

Investigate and mitigate per-frame heap allocations from `CollisionWorld2D.QueryCollisionPairs()` in the gameplay hot path (`GameLoop.ResolveCollisions`), to comply with the zero-allocation hot-path requirement in AGENTS.md.

## Background

`ResolveCollisions()` calls `_collisionWorld.QueryCollisionPairs("actors", "props")` every frame. MGE v6's implementation internally allocates:

- `HashSet<ActorPairKey>` (per call)
- `CollisionPair2D` class instances (per colliding pair)
- `List<QuadtreeData>` (per actor in the broadphase query)

For a beat 'em up scene with ~15 enemies + props, this means several heap allocations per frame, which can cause GC-induced frame stutters.

## Investigation Steps

1. **Profile current allocation rate**: Run with `DOTNET_gcServer=1` and log GC pause times. Place a `GC.CollectionCount(0)` probe before/after a busy fight frame to measure gen-0 collections triggered by collision queries.
2. **Audit MGE v6 source** to confirm exact allocation sites and whether any pooling hooks exist (e.g., reusable `HashSet` parameter overloads, `IEqualityComparer<ActorPairKey>` caching opportunities, or `ArrayPool<T>` usage).
3. **Characterize impact**: For a worst-case frame with N actors + M props, compute `HashSet` capacity growth + `CollisionPair2D` count + `List` resizes.

## Mitigation Options (Ordered by Preference)

1. **Pool the HashSet** — If MGE v6 exposes an overload accepting a cached `HashSet<ActorPairKey>`, pass a pre-allocated instance and call `.Clear()` after each query.
2. **Wrap in a cached enumerator** — Maintain a persistent `HashSet` + result list as fields, wrap `QueryCollisionPairs` in a helper that reuses buffers. Requires access to internal types or an alternate code path.
3. **Pool `CollisionPair2D`** — Use `ObjectPool<CollisionPair2D>` with `Rent`/`Return` if `CollisionPair2D` is `sealed class` and the results are short-lived.
4. **Batched per-level allocation** — Pre-allocate worst-case capacity once per level load and reuse the set across all frames of that level.
5. **Upstream PR** — Submit a patch to MGE v6 adding pooling support (long-term).

## Success Criteria

- Zero heap allocations from collision resolution per frame after the fix (excluding unavoidable JIT-induced first-call allocs).
- All 216 existing tests still pass.
- No regression in collision behavior (actor-prop pushback, layer filtering).

## Out of Scope

- Allocation profiling of the rest of MGE v6 (e.g., `AnimatedSprite` controller events).
- Per-frame `Shape` property struct copies (`CollisionShape2D` + `BoundingBox2D` are structs, so no GC pressure — acceptable).
