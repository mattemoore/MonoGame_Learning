using Gum;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;
using HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment;

namespace MonoGameLearning.Core.UI;

public class GumManager
{
    private static GumService Svc => GumService.Default;

    public DebugOverlay DebugOverlay { get; private set; }

    public GumManager() => DebugOverlay = null!;

    public void Initialize(Microsoft.Xna.Framework.Game game)
    {
        Svc.Initialize(game);
        ShapeRenderer.Self.Initialize();
        Svc.EnableExpandToWindow(1f);
        DebugOverlay = new DebugOverlay();
    }

    public void Update(GameTime gameTime) => Svc.Update(gameTime);

    public void Draw() => Svc.Draw();

    public ContainerRuntime CreateScreen(string title, Color bgColor, Color titleColor, string[] options)
    {
        var container = new ContainerRuntime
        {
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            Width = 0,
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            Height = 0,
            Visible = false
        };
        container.AddToRoot();

        var bg = new RectangleRuntime
        {
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            Width = 0,
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            Height = 0,
            IsFilled = true,
            StrokeWidth = 0,
            FillColor = bgColor
        };
        container.Children.Add(bg);

        var titleText = new TextRuntime
        {
            Text = title,
            X = 0,
            Y = -80,
            XOrigin = HorizontalAlignment.Center,
            YOrigin = VerticalAlignment.Center,
            XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontScale = 3f,
            Red = titleColor.R,
            Green = titleColor.G,
            Blue = titleColor.B
        };
        container.Children.Add(titleText);

        float yOffset = 0;
        foreach (var option in options)
        {
            var item = new TextRuntime
            {
                Text = "  " + option,
                X = 0,
                Y = yOffset,
                XOrigin = HorizontalAlignment.Center,
                YOrigin = VerticalAlignment.Center,
                XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
                YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontScale = 1.5f,
                Red = 220,
                Green = 220,
                Blue = 220
            };
            container.Children.Add(item);
            yOffset += 40;
        }

        return container;
    }
}