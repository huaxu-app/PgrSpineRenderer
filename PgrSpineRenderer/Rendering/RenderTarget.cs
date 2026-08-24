using System.Diagnostics;
using System.Numerics;
using SkiaSharp;
using Spine;

namespace PgrSpineRenderer.Rendering;

/// <summary>
///     A pair of GPU surfaces for one canvas/output size combination: skeletons are composited into a
///     supersampled, linear-light surface, then resolved down to the size ffmpeg is fed.
/// </summary>
internal sealed class RenderTarget : IDisposable
{
    private const int MaxSamples = 4;

    private const SKColorType BlendColorType = SKColorType.RgbaF16;

    private static readonly SKSamplingOptions ResolveSampling = new(SKCubicResampler.Mitchell);

    private readonly SKSurface _canvas;
    private readonly SKSurface? _output;
    private readonly SKRect _outputBounds;

    public RenderTarget(GRContext context, Vector2 canvasSize, Vector2 outputSize)
    {
        _canvas = CreateCanvas(context, canvasSize);
        _outputBounds = SKRect.Create(outputSize.X, outputSize.Y);

        if (outputSize == canvasSize) return;

        _output = Create(context, BlendInfo(outputSize), 0);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _output?.Dispose();
    }

    public SkiaFrame Render(Skeleton[] skeletons)
    {
        Metrics.FramesRendered.Add(1);
        var stopwatch = Stopwatch.StartNew();

        _canvas.Canvas.Clear();
        SpineDrawer.Draw(_canvas.Canvas, skeletons);
        _canvas.Flush();

        using var image = Resolve();
        var frame = SkiaFrame.FromImage(image);

        Metrics.FrameDrawTime.Record(stopwatch.ElapsedMilliseconds);
        return frame;
    }

    /// <summary>
    ///     Skia has no analytic antialiasing for vertex draws, so mesh and clipping edges rely on MSAA.
    /// </summary>
    private static SKSurface CreateCanvas(GRContext context, Vector2 size)
    {
        var info = BlendInfo(size);
        var samples = Math.Min(MaxSamples, context.GetMaxSurfaceSampleCount(info.ColorType));

        return SKSurface.Create(context, false, info, samples) ?? Create(context, info, 0);
    }

    private static SKSurface Create(GRContext context, SKImageInfo info, int samples)
    {
        return SKSurface.Create(context, false, info, samples)
               ?? throw new ApplicationException($"Failed to create a {info.Width}x{info.Height} render surface");
    }

    private static SKImageInfo BlendInfo(Vector2 size)
    {
        return new SKImageInfo((int)size.X, (int)size.Y, BlendColorType, SKAlphaType.Premul,
            SKColorSpace.CreateSrgbLinear());
    }

    /// <summary>
    ///     Downscaling has to happen on premultiplied data, otherwise transparent texels bleed into every
    ///     soft edge.
    /// </summary>
    private SKImage Resolve()
    {
        var image = _canvas.Snapshot();
        if (_output is null) return image;

        using (image)
        {
            _output.Canvas.Clear();
            _output.Canvas.DrawImage(image, _outputBounds, ResolveSampling);
            _output.Flush();
        }

        return _output.Snapshot();
    }
}