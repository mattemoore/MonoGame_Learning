using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Animation;

/// <summary>
/// Per-animation hand points for an actor: one <see cref="Vector2"/> per animation frame,
/// relative to the actor's <c>Position</c> (the sprite region center). The actor owns this
/// data; a held weapon only contributes its own grip offset when computing the overlay
/// anchor (<see cref="Combat.WeaponDef.ComputeAnchor"/>). Lookup is an allocation-free
/// linear scan, and the frame index wraps so a looping animation can pass its frame count.
/// </summary>
public sealed class HandAnchorTable
{
    private readonly (string Key, Vector2[] Frames)[] _entries;

    public HandAnchorTable(params (string Key, Vector2[] Frames)[] entries) => _entries = entries;

    public bool TryResolve(string? animationKey, int frameIndex, out Vector2 hand)
    {
        foreach (var (key, frames) in _entries)
        {
            if (key != animationKey || frames.Length == 0) continue;
            hand = frames[frameIndex % frames.Length];
            return true;
        }

        hand = Vector2.Zero;
        return false;
    }
}
