using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.AI;

public readonly struct AIUpdateResult
{
    public AIAction Action { get; init; }
    public bool FacingChanged { get; init; }
    public float NewFacingX { get; init; }
    public Vector2 MovementDirection { get; init; }
    public DominantForce Force { get; init; }
}