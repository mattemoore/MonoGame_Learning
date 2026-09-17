using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.AnimatedSprites;

/// <summary>
/// Knife atlas: a single held pose plus a looping flight run. The held pose serves the
/// carry overlay; the flight run is played by <c>ProjectileEntity</c> while the knife is
/// airborne. It is never driven through an <c>AnimatedSprite</c>/<c>AnimationController</c>:
/// pooled projectiles allocate nothing per spawn, and <c>SetFrame</c> would not refresh
/// <c>TextureRegion</c>, so the flight regions are indexed directly by the projectile's own
/// timer (see <c>ThrowableWeaponDef.ProjectileFrameCount</c>).
/// </summary>
public static class KnifeSprite
{
    public const string HoldRegion = "knife-hold-00";
    public const string FlyPrefix = "knife-fly";
    public const int FlyFrameCount = 7;
    public const float FlyFrameDuration = 0.1f;

    private static readonly SpriteSheetAsset Asset = new("knife", "images/knife");

    public static SpriteSheet Sheet => Asset.Sheet;

    public static void Load(ContentManager content) => Asset.Load(content);
}
