using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.Entities.Pickup;

public abstract class PickupBase : Entity, IUpdatable, IRenderable, IDebugDrawable, ICollisionActor, ICollisionLayer, IPickup
{
    public string LayerName => CollisionLayers.Pickups;

    private const int DefaultTextureSize = 32;

    private bool _isArcing;
    private float _arcT, _arcDuration, _arcPeak, _arcSpinTurns;
    private Vector2 _arcStart, _arcEnd;

    /// <summary>
    /// False while the pickup is mid-drop-arc, so a weapon knocked loose at the dropper's feet
    /// cannot be collected before it lands. Pickups that never arc are always collectible.
    /// </summary>
    public virtual bool CanBeCollectedBy(IDamageable target) => !_isArcing;

    /// <summary>Visual spin applied during a drop arc, in radians (0 for a resting pickup).</summary>
    public float Rotation { get; private set; }

    // Texture-based ctor — derives size from the texture (may be null in headless tests).
    protected PickupBase(string name, Vector2 position, Texture2D? texture)
        : this(name, position, texture?.Width ?? DefaultTextureSize, texture?.Height ?? DefaultTextureSize, texture) { }

    // Size-bearing overload for test doubles without a GraphicsDevice — Texture may be null (Render guards).
    protected PickupBase(string name, Vector2 position, int width, int height, Texture2D? texture)
        : base(name, position, width, height)
    {
        Texture = texture;
    }

    public int Id => GetHashCode();
    protected Texture2D? Texture { get; }

    public CollisionShape2D Shape => new(new BoundingBox2D(
        new Vector2(Frame.X, Frame.Y),
        new Vector2(Frame.Right, Frame.Bottom)));

    /// <summary>
    /// Starts a lightweight, code-driven toss from <paramref name="start"/> to
    /// <paramref name="end"/>: a parabolic hop of <paramref name="peakHeight"/> and
    /// <paramref name="spinTurns"/> full rotations over <paramref name="duration"/> seconds.
    /// The pickup is uncollectible until the arc completes. No new art or physics engine.
    /// </summary>
    public void BeginDropArc(Vector2 start, Vector2 end, float duration, float peakHeight, float spinTurns)
    {
        _arcStart = start;
        _arcEnd = end;
        _arcDuration = duration;
        _arcPeak = peakHeight;
        _arcSpinTurns = spinTurns;
        _arcT = 0f;
        _isArcing = true;
        Rotation = 0f;
        Position = start;
    }

    public void Update(GameTime gameTime)
    {
        if (!_isArcing) return;

        _arcT += (float)gameTime.ElapsedGameTime.TotalSeconds;
        float t = MathHelper.Clamp(_arcT / _arcDuration, 0f, 1f);
        var linear = Vector2.Lerp(_arcStart, _arcEnd, t);
        float lift = _arcPeak * (1f - (2f * t - 1f) * (2f * t - 1f)); // parabolic hop
        Position = new Vector2(linear.X, linear.Y - lift);
        Rotation = MathHelper.TwoPi * _arcSpinTurns * t;
        if (t >= 1f)
        {
            _isArcing = false;
            Position = _arcEnd;
            Rotation = 0f;
        }
    }

    public void Render(RenderContext context)
    {
        if (Texture is null) return;
        context.SpriteBatch.Draw(Texture, Position, null, Color.White,
            Rotation, new Vector2(Texture.Width / 2f, Texture.Height / 2f), 1f, SpriteEffects.None, 0f);
    }

    public void DrawDebug(DebugDrawContext context)
    {
        context.SpriteBatch.DrawRectangle(Frame, Color.Yellow);
    }

    public abstract void OnPickup(IDamageable target);
}
