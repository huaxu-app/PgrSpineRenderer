using System.Numerics;
using Spine;

namespace PgrSpineRenderer.Rendering;

public interface IFrameRenderer
{
    public Task<SkiaFrame> Render(Vector2 canvasSize, Skeleton[] skeletons, CancellationToken token = default);
}