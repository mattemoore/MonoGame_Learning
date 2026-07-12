using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.UI;

public sealed class PlayerBar : UiBase
{
    private readonly IHudPlayerData _player;
    private readonly SpriteFont _font;
    private readonly string _initialStr;
    private string _label = "";
    private int _lastLives = -1;

    private static readonly Color MugshotColor = new(0.3f, 0.4f, 0.8f);

    public PlayerBar(IHudPlayerData player, SpriteFont font) : base("playerBar", Vector2.Zero, 0, 0)
    {
        _player = player;
        _font = font;
        char initial = player.Name.Length > 0 ? char.ToUpper(player.Name[0]) : '?';
        _initialStr = initial.ToString();
        _label = $"{player.Name}  ={player.Lives}";
    }

    public override void Update(GameTime gameTime)
    {
        if (_player.Lives != _lastLives)
        {
            _lastLives = _player.Lives;
            _label = $"{_player.Name}  ={_player.Lives}";
        }
    }

    public override void Render(RenderContext context)
    {
        var sb = context.SpriteBatch;
        float left = HudLayoutConstants.MARGIN;
        float top = HudLayoutConstants.MARGIN;

        var mugshotRect = new RectangleF(left, top, HudLayoutConstants.MUGSHOT_SIZE, HudLayoutConstants.MUGSHOT_SIZE + 6f);
        sb.FillRectangle(mugshotRect, MugshotColor);
        sb.DrawRectangle(mugshotRect, Color.White, 1f);
        Vector2 textSize = _font.MeasureString(_initialStr);
        sb.DrawString(_font, _initialStr,
            new Vector2(left + (HudLayoutConstants.MUGSHOT_SIZE - textSize.X) / 2f, top + HudLayoutConstants.MUGSHOT_SIZE / 2f - textSize.Y / 2f + 3f),
            Color.White);

        float labelLeft = left + HudLayoutConstants.MUGSHOT_SIZE + HudLayoutConstants.MARGIN;
        sb.DrawString(_font, _label,
            new Vector2(labelLeft, top), Color.White);

        float barTop = top + HudLayoutConstants.MUGSHOT_SIZE + 2f;
        float healthFraction = (float)_player.Health / _player.MaxHealth;
        var barBgRect = new RectangleF(left, barTop, HudLayoutConstants.HEALTH_BAR_WIDTH, HudLayoutConstants.PLAYER_BAR_HEIGHT);
        var barFgRect = new RectangleF(left, barTop, HudLayoutConstants.HEALTH_BAR_WIDTH * healthFraction, HudLayoutConstants.PLAYER_BAR_HEIGHT);

        sb.FillRectangle(barBgRect, Color.DarkRed);
        sb.FillRectangle(barFgRect, Color.Yellow);
        sb.DrawRectangle(barBgRect, Color.White, 1f);
    }

    public override void DrawDebug(DebugDrawContext context)
    {
        var sb = context.SpriteBatch;
        float left = HudLayoutConstants.MARGIN;
        float top = HudLayoutConstants.MARGIN;

        sb.DrawRectangle(new RectangleF(left, top, HudLayoutConstants.HEALTH_BAR_WIDTH, HudLayoutConstants.PLAYER_BAR_HEIGHT), Color.Cyan, 1f);
        sb.DrawString(context.Font, $"HP:{_player.Health}/{_player.MaxHealth} Inv:{_player.IsInvincible} Lives:{_player.Lives}", new Vector2(left, top + HudLayoutConstants.PLAYER_BAR_HEIGHT + 4f), Color.Cyan);
    }
}