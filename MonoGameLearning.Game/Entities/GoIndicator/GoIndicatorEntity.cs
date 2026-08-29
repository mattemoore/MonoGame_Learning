using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Core.UI;

namespace MonoGameLearning.Game.Entities.GoIndicator;

public class GoIndicatorEntity(Texture2D texture, Func<Point> getViewportSize)
    : UiBase
{
    private readonly Texture2D _texture = texture;
    private readonly Func<Point> _getViewportSize = getViewportSize;

    private float _flashTimer;
    private float _flashAlpha = 1f;
    private float _pulseScale = 1f;

    public const float SCALE = 0.3f;
    public const float PULSE_AMPLITUDE = 0.04f;
    public const int MARGIN = 20;
    public const float FLASH_PERIOD = 0.8f;

    public static Vector2 ComputeAnchorPosition(int virtualWidth, int virtualHeight, float textureWidth, float scale, float margin)
    {
        float scaledWidth = textureWidth * scale;
        return new Vector2(virtualWidth - scaledWidth / 2f - margin, virtualHeight / 2f);
    }

    public override void Update(GameTime gameTime)
    {
        if (!Visible) return;
        var vp = _getViewportSize();
        Position = ComputeAnchorPosition(vp.X, vp.Y, _texture.Width, SCALE, MARGIN);

        _flashTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _flashAlpha = 0.3f + (MathF.Sin(_flashTimer * MathF.PI / FLASH_PERIOD * 2f) + 1f) * 0.35f;
        _pulseScale = 1f + MathF.Sin(_flashTimer * MathF.PI / FLASH_PERIOD * 2f) * PULSE_AMPLITUDE;
    }

    public override void Render(RenderContext context)
    {
        if (!Visible) return;
        var origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
        float renderScale = SCALE * _pulseScale;
        float scaledW = _texture.Width * renderScale;
        float scaledH = _texture.Height * renderScale;

        var destRect = new RectangleF(
            Position.X - scaledW / 2f,
            Position.Y - scaledH / 2f,
            scaledW,
            scaledH);
        var glowColor = Color.LimeGreen * (_flashAlpha * 0.2f);
        context.SpriteBatch.FillRectangle(destRect, glowColor);

        var tint = Color.LimeGreen * _flashAlpha;
        context.SpriteBatch.Draw(_texture, Position, null, tint, 0f, origin, renderScale, SpriteEffects.None, 0f);
    }

    public override void DrawDebug(DebugDrawContext context)
    {
        if (!Visible) return;
        float renderScale = SCALE * _pulseScale;
        float scaledW = _texture.Width * renderScale;
        float scaledH = _texture.Height * renderScale;
        var bounds = new RectangleF(
            Position.X - scaledW / 2f,
            Position.Y - scaledH / 2f,
            scaledW,
            scaledH);
        context.SpriteBatch.DrawRectangle(bounds, Color.LimeGreen, 1f);
        context.SpriteBatch.DrawString(context.Font, $"[GO] alpha={_flashAlpha:F2} scale={_pulseScale:F2}", Position + new Vector2(0, -scaledH / 2f - 16f), Color.LimeGreen);
    }
}