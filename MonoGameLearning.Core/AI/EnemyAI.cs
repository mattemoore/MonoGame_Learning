using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace MonoGameLearning.Core.AI;

public class EnemyAI(float attackRange, float minChaseDistance)
{
    private const float AttackDelayDuration = 1.0f;
    private const float DirectionUpdateInterval = 0.35f;

    private const float SeekWeight = 1.0f;
    private const float SeparationRadius = 50f;
    private const float SeparationWeight = 1.5f;
    private const float AvoidRadius = 90f;
    private const float AvoidWeight = 3.0f;
    private const float BoundsMargin = 30f;
    private const float BoundsWeight = 2.0f;
    private const float MaxSteeringForce = 600f;
    private const float EpsilonSquared = 1e-6f;

    private float _directionUpdateTimer;
    private float _lastFacingX;
    private Vector2 _movementDirection;
    private bool _facingChanged;
    private float _newFacingX;
    private DominantForce _force;

    public float AttackCooldown { get; set; }
    private float AttackDelayTimer { get; set; }

    public AIUpdateResult Update(
        Vector2 selfPosition,
        float selfHalfWidth,
        float selfHalfHeight,
        in WorldSnapshot world,
        bool isIdleOrChasing,
        float deltaSeconds)
    {
        UpdateIdle(deltaSeconds);
        _facingChanged = false;
        _force = DominantForce.None;

        if (!isIdleOrChasing)
        {
            _movementDirection = Vector2.Zero;
            return new AIUpdateResult { Action = AIAction.None };
        }

        Vector2 toTarget = world.PlayerPosition - selfPosition;
        float distance = toTarget.Length();

        if (distance <= attackRange && AttackCooldown <= 0)
        {
            if (AttackDelayTimer <= 0)
                AttackDelayTimer = AttackDelayDuration;

            AttackDelayTimer -= deltaSeconds;

            Vector2 separate = ComputeSeparation(selfPosition, world.Enemies);
            _movementDirection = separate.LengthSquared() > EpsilonSquared ? Vector2.Normalize(separate) : Vector2.Zero;
            _force = DominantForce.Separate;

            if (AttackDelayTimer <= 0)
            {
                AttackDelayTimer = 0;
                return new AIUpdateResult { Action = AIAction.Attack, MovementDirection = _movementDirection, Force = _force };
            }
            return new AIUpdateResult { Action = AIAction.StopChase, MovementDirection = _movementDirection, Force = _force };
        }

        if (distance > attackRange)
        {
            AttackDelayTimer = 0;
            _directionUpdateTimer -= deltaSeconds;

            if (_directionUpdateTimer <= 0)
            {
                _directionUpdateTimer = DirectionUpdateInterval;
                _movementDirection = ComputeSteering(selfPosition, selfHalfWidth, selfHalfHeight, toTarget, distance, world);

                if (Math.Sign(_movementDirection.X) != Math.Sign(_lastFacingX))
                {
                    _lastFacingX = _movementDirection.X;
                    _newFacingX = _movementDirection.X;
                    _facingChanged = true;
                }
            }

            if (distance <= minChaseDistance)
                _movementDirection = Vector2.Zero;

            return new AIUpdateResult
            {
                Action = AIAction.StartChase,
                FacingChanged = _facingChanged,
                NewFacingX = _newFacingX,
                MovementDirection = _movementDirection,
                Force = _force
            };
        }

        _movementDirection = Vector2.Zero;
        return new AIUpdateResult { Action = AIAction.StopChase, MovementDirection = _movementDirection, Force = _force };
    }

    public void UpdateIdle(float deltaSeconds) =>
        AttackCooldown = Math.Max(0, AttackCooldown - deltaSeconds);

    private Vector2 ComputeSteering(
        Vector2 selfPosition,
        float selfHalfWidth,
        float selfHalfHeight,
        Vector2 toTarget,
        float distance,
        in WorldSnapshot world)
    {
        Vector2 steer = Vector2.Zero;
        float bestWeight = 0f;

        if (distance > minChaseDistance)
        {
            Vector2 seek = toTarget / distance * SeekWeight;
            steer += seek;
            bestWeight = SeekWeight;
            _force = DominantForce.Seek;
        }

        Vector2 separate = ComputeSeparation(selfPosition, world.Enemies);
        if (separate.LengthSquared() > EpsilonSquared)
        {
            steer += separate;
            if (SeparationWeight > bestWeight)
            {
                bestWeight = SeparationWeight;
                _force = DominantForce.Separate;
            }
        }

        Vector2 avoid = ComputeAvoidance(selfPosition, selfHalfWidth, selfHalfHeight, world.Props);
        if (avoid.LengthSquared() > EpsilonSquared)
        {
            steer += avoid;
            if (AvoidWeight > bestWeight)
            {
                bestWeight = AvoidWeight;
                _force = DominantForce.Avoid;
            }
        }

        Vector2 bounds = ComputeBoundsForce(selfPosition, world.WalkableBounds);
        if (bounds.LengthSquared() > EpsilonSquared)
        {
            steer += bounds;
            if (BoundsWeight > bestWeight)
            {
                bestWeight = BoundsWeight;
                _force = DominantForce.Bounds;
            }
        }

        float lengthSq = steer.LengthSquared();
        if (lengthSq > MaxSteeringForce * MaxSteeringForce)
            steer *= MaxSteeringForce / MathF.Sqrt(lengthSq);

        return lengthSq > EpsilonSquared ? steer / MathF.Sqrt(lengthSq) : Vector2.Zero;
    }

    private static Vector2 ComputeSeparation(
        Vector2 selfPosition,
        IReadOnlyList<ActorSnapshot> enemies)
    {
        Vector2 force = Vector2.Zero;
        float sepRadiusSq = SeparationRadius * SeparationRadius;
        if (enemies is null) return force;

        for (int i = 0; i < enemies.Count; i++)
        {
            var en = enemies[i];
            float dx = selfPosition.X - en.Position.X;
            float dy = selfPosition.Y - en.Position.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq < EpsilonSquared || distSq >= sepRadiusSq)
                continue;

            float dist = MathF.Sqrt(distSq);
            float strength = (SeparationRadius - dist) / SeparationRadius * SeparationWeight;
            force += new Vector2(dx / dist * strength, dy / dist * strength);
        }

        return force;
    }

    private static Vector2 ComputeAvoidance(
        Vector2 selfPosition,
        float selfHalfWidth,
        float selfHalfHeight,
        IReadOnlyList<ActorSnapshot> props)
    {
        Vector2 force = Vector2.Zero;
        float avoidRadiusSq = AvoidRadius * AvoidRadius;
        if (props is null) return force;

        for (int i = 0; i < props.Count; i++)
        {
            var prop = props[i];
            float dx = selfPosition.X - prop.Position.X;
            float dy = selfPosition.Y - prop.Position.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq >= avoidRadiusSq)
                continue;

            float dist = MathF.Sqrt(MathF.Max(distSq, EpsilonSquared));
            float overlapX = (selfHalfWidth + prop.HalfWidth) - MathF.Abs(dx);
            float overlapY = (selfHalfHeight + prop.HalfHeight) - MathF.Abs(dy);

            Vector2 pushDir;
            if (overlapX > 0 && overlapY > 0)
            {
                pushDir = overlapX < overlapY
                    ? new Vector2(MathF.Sign(dx), 0)
                    : new Vector2(0, MathF.Sign(dy));
            }
            else
            {
                pushDir = distSq > EpsilonSquared
                    ? new Vector2(dx, dy) / dist
                    : Vector2.Zero;
            }

            float strength = (AvoidRadius - dist) / AvoidRadius * AvoidWeight;
            force += pushDir * strength;
        }

        return force;
    }

    private static Vector2 ComputeBoundsForce(Vector2 selfPosition, RectangleF bounds)
    {
        Vector2 force = Vector2.Zero;

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return force;

        if (selfPosition.X < bounds.Left + BoundsMargin)
            force.X = (bounds.Left + BoundsMargin - selfPosition.X) / BoundsMargin * BoundsWeight;
        else if (selfPosition.X > bounds.Right - BoundsMargin)
            force.X = (bounds.Right - BoundsMargin - selfPosition.X) / BoundsMargin * BoundsWeight;

        if (selfPosition.Y < bounds.Top + BoundsMargin)
            force.Y = (bounds.Top + BoundsMargin - selfPosition.Y) / BoundsMargin * BoundsWeight;
        else if (selfPosition.Y > bounds.Bottom - BoundsMargin)
            force.Y = (bounds.Bottom - BoundsMargin - selfPosition.Y) / BoundsMargin * BoundsWeight;

        return force;
    }

    public void Reset()
    {
        AttackCooldown = 0;
        AttackDelayTimer = 0;
        _directionUpdateTimer = 0;
        _lastFacingX = 0;
        _movementDirection = Vector2.Zero;
        _facingChanged = false;
        _newFacingX = 0;
        _force = DominantForce.None;
    }
}