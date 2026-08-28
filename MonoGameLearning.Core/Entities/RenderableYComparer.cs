using System.Collections.Generic;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.Entities;

internal readonly struct RenderableYComparer : IComparer<IRenderable>
{
    public int Compare(IRenderable? x, IRenderable? y)
    {
        if (x is null || y is null) return 0;
        float diff = x.Frame.Center.Y - y.Frame.Center.Y;
        return diff < 0 ? -1 : diff > 0 ? 1 : 0;
    }
}