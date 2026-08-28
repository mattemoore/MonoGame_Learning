using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.UI;

public sealed class EnemyBar : UiBase
{
    private IDamageable? _hitTarget;
    private IDamageable? _proximityTarget;
    private IDamageable? _displayTarget;
    private float _lingerTimer;
    private float _deathLingerTimer;
    private bool _visible;
    private bool _isDeathLinger;

    public bool IsVisible => _visible;
    public IDamageable? DisplayTarget => _displayTarget;
    public bool IsDeathLinger => _isDeathLinger;

    public void SetProximityTarget(IDamageable? target) => _proximityTarget = target;

    private readonly SpriteFont _font;

    public EnemyBar(SpriteFont font) : base("enemyBar", Vector2.Zero, 0, 0)
    {
        _font = font;
    }

    public void OnHit(IDamageable enemy)
    {
        _hitTarget = enemy;
        _lingerTimer = HudLayoutConstants.ENEMY_BAR_LINGER_SECONDS;
        _isDeathLinger = false;
        _deathLingerTimer = 0f;
    }

    public void Reset()
    {
        _hitTarget = null;
        _proximityTarget = null;
        _displayTarget = null;
        _lingerTimer = 0f;
        _deathLingerTimer = 0f;
        _visible = false;
        _isDeathLinger = false;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_hitTarget is not null)
        {
            if (_hitTarget.IsAlive)
            {
                _lingerTimer = Math.Max(0f, _lingerTimer - dt);
                if (_lingerTimer <= 0f)
                {
                    _hitTarget = null;
                    _isDeathLinger = false;
                }
            }
            else
            {
                if (!_isDeathLinger)
                {
                    _deathLingerTimer = HudLayoutConstants.DEATH_LINGER_SECONDS;
                    _isDeathLinger = true;
                }
                _deathLingerTimer = Math.Max(0f, _deathLingerTimer - dt);
                if (_deathLingerTimer <= 0f)
                {
                    _hitTarget = null;
                    _isDeathLinger = false;
                }
            }
        }

        _displayTarget = null;
        _visible = false;

        if (_hitTarget is not null)
        {
            _displayTarget = _hitTarget;
            _visible = true;
        }
        else if (_proximityTarget is not null && _proximityTarget.IsAlive)
        {
            _displayTarget = _proximityTarget;
            _visible = true;
        }
    }

    public override void Render(RenderContext context)
    {
        if (!_visible || _displayTarget is null) return;

        var sb = context.SpriteBatch;
        float left = HudLayoutConstants.MARGIN;
        float top = HudLayoutConstants.MARGIN + HudLayoutConstants.MUGSHOT_SIZE + HudLayoutConstants.ENEMY_BAR_TEXT_OFFSET + HudLayoutConstants.PLAYER_BAR_HEIGHT + HudLayoutConstants.MARGIN;

        float mugSize = HudLayoutConstants.ENEMY_MUGSHOT_SIZE;
        var mugshotRect = new RectangleF(left, top, mugSize, mugSize);
        sb.FillRectangle(mugshotRect, Color.DarkGray);
        sb.DrawRectangle(mugshotRect, Color.White, 1f);

        string label = _displayTarget.Name;
        Vector2 labelSize = _font.MeasureString(label);
        sb.DrawString(_font, label,
            new Vector2(left + mugSize + 6f, top + mugSize / 2f - labelSize.Y / 2f),
            Color.White);

        float barTop = top + mugSize + 4f;
        float barWidth = HudLayoutConstants.ENEMY_BAR_WIDTH;
        float barHeight = HudLayoutConstants.ENEMY_BAR_HEIGHT;
        float healthFraction = (float)_displayTarget.Health / _displayTarget.MaxHealth;

        var barBgRect = new RectangleF(left, barTop, barWidth, barHeight);
        var barFgRect = new RectangleF(left, barTop, barWidth * healthFraction, barHeight);

        sb.FillRectangle(barBgRect, Color.DarkRed);
        sb.FillRectangle(barFgRect, Color.Red);
        sb.DrawRectangle(barBgRect, Color.White, 1f);

        if (_isDeathLinger)
        {
            float inset = 3f;
            var tl = new Vector2(mugshotRect.X + inset, mugshotRect.Y + inset);
            var br = new Vector2(mugshotRect.Right - inset, mugshotRect.Bottom - inset);
            var tr = new Vector2(mugshotRect.Right - inset, mugshotRect.Y + inset);
            var bl = new Vector2(mugshotRect.X + inset, mugshotRect.Bottom - inset);
            sb.DrawLine(tl, br, Color.Red, 2f);
            sb.DrawLine(tr, bl, Color.Red, 2f);
        }
    }

    public override void DrawDebug(DebugDrawContext context)
    {
        var sb = context.SpriteBatch;
        float left = HudLayoutConstants.MARGIN;
        float top = HudLayoutConstants.MARGIN + HudLayoutConstants.MUGSHOT_SIZE + HudLayoutConstants.ENEMY_BAR_TEXT_OFFSET + HudLayoutConstants.PLAYER_BAR_HEIGHT + HudLayoutConstants.MARGIN;

        sb.DrawRectangle(new RectangleF(left, top, HudLayoutConstants.ENEMY_BAR_WIDTH, HudLayoutConstants.ENEMY_BAR_HEIGHT), Color.Orange, 1f);
        string label = _displayTarget is not null
            ? $"Enemy HP:{_displayTarget.Health}/{_displayTarget.MaxHealth} linger:{_lingerTimer:F2} death:{_deathLingerTimer:F2}"
            : "No target";
        sb.DrawString(context.Font, label, new Vector2(left, top + 20f), Color.Orange);
    }
}