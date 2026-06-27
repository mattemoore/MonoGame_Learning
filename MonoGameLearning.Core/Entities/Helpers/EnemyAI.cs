using System;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities.Helpers;

public enum AIAction { None, StartChase, StopChase, Attack }

public class EnemyAI(float attackRange, float minChaseDistance)
{
    private const float AttackDelayDuration = 1.0f;
    private const float DirectionUpdateInterval = 0.35f;

    private float _directionUpdateTimer;
    private float _lastFacingX;

    public float AttackCooldown { get; set; }
    public float AttackDelayTimer { get; set; }
    public Vector2 MovementDirection { get; private set; }
    public bool FacingChanged { get; private set; }
    public float NewFacingX { get; private set; }

    public AIAction Update(Vector2 selfPosition, Vector2 targetPosition, bool isIdleOrChasing, float deltaSeconds)
    {
        AttackCooldown = Math.Max(0, AttackCooldown - deltaSeconds);

        Vector2 toTarget = targetPosition - selfPosition;
        float distance = toTarget.Length();
        FacingChanged = false;

        if (!isIdleOrChasing)
        {
            MovementDirection = Vector2.Zero;
            return AIAction.None;
        }

        if (distance <= attackRange && AttackCooldown <= 0)
        {
            MovementDirection = Vector2.Zero;
            if (AttackDelayTimer <= 0)
                AttackDelayTimer = AttackDelayDuration;

            AttackDelayTimer -= deltaSeconds;
            if (AttackDelayTimer <= 0)
            {
                AttackDelayTimer = 0;
                return AIAction.Attack;
            }
            return AIAction.StopChase;
        }

        if (distance > attackRange)
        {
            AttackDelayTimer = 0;
            _directionUpdateTimer -= deltaSeconds;
            if (_directionUpdateTimer <= 0)
            {
                _directionUpdateTimer = DirectionUpdateInterval;
                toTarget /= distance;
                MovementDirection = Mover.PreventDiagonal(toTarget);
                if (Math.Sign(MovementDirection.X) != Math.Sign(_lastFacingX))
                {
                    _lastFacingX = MovementDirection.X;
                    NewFacingX = MovementDirection.X;
                    FacingChanged = true;
                }
            }

            if (distance <= minChaseDistance)
                MovementDirection = Vector2.Zero;

            return AIAction.StartChase;
        }

        MovementDirection = Vector2.Zero;
        return AIAction.StopChase;
    }

    public void Reset()
    {
        AttackCooldown = 0;
        AttackDelayTimer = 0;
        _directionUpdateTimer = 0;
        _lastFacingX = 0;
        MovementDirection = Vector2.Zero;
    }
}