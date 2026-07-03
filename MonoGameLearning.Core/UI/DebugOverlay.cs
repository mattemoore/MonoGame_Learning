using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace MonoGameLearning.Core.UI;

public sealed class DebugOverlay
{
    private readonly TextRuntime _left, _right;

    public DebugOverlay()
    {
        _left = new TextRuntime();
        _left.AddToRoot();
        _left.Visible = false;
        _left.Anchor(Anchor.TopLeft);

        _right = new TextRuntime();
        _right.AddToRoot();
        _right.Visible = false;
        _right.Anchor(Anchor.TopRight);
        _right.Width = 200;
        _right.Height = 200;
        _right.X = -200;
        _right.HorizontalAlignment = HorizontalAlignment.Right;
    }

    public bool Visible
    {
        set
        {
            _left.Visible = value;
            _right.Visible = value;
        }
    }

    public void SetText(string left, string right)
    {
        _left.Text = left;
        _right.Text = right;
    }
}