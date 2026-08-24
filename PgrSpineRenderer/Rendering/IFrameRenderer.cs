using System.Numerics;
using Spine;

namespace PgrSpineRenderer.Rendering;

public interface IFrameRenderer
{
    public Task<SkiaFrame> Render(Vector2 canvasSize, Vector2 outputSize, Skeleton[] skeletons,
        CancellationToken token = default);
}