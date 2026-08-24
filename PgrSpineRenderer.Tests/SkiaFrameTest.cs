using FluentAssertions;
using NUnit.Framework;
using PgrSpineRenderer.Rendering;
using SkiaSharp;

namespace PgrSpineRenderer.Tests;

[TestFixture]
[TestOf(typeof(SkiaFrame))]
public class SkiaFrameTest
{
    /// <summary>
    ///     FromImage takes the image as a parameter, so a CPU raster surface works here and no GL
    ///     context is needed.
    /// </summary>
    private static SkiaFrame RenderPreDrawn(SKColor color)
    {
        using var surface = SKSurface.Create(new SKImageInfo(4, 4, SKColorType.Bgra8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        using (var paint = new SKPaint())
        {
            paint.Color = color;
            paint.BlendMode = SKBlendMode.Src;
            surface.Canvas.DrawRect(new SKRect(0, 0, 4, 4), paint);
        }

        using var image = surface.Snapshot();
        return SkiaFrame.FromImage(image);
    }

    [Test]
    public void FrameIsUnpremultiplied()
    {
        var frame = RenderPreDrawn(SKColors.Red.WithAlpha(128));

        frame.Bitmap.AlphaType.Should().Be(SKAlphaType.Unpremul);
    }

    /// <summary>
    ///     Rgba8888 puts the bytes down as red, green, blue, alpha, so red is index 0.
    ///     Indexing the wrong byte here would make these tests pass or fail on channel order
    ///     rather than on the premultiplied to straight conversion they are meant to check.
    /// </summary>
    private const int RedByte = 0;

    private const int AlphaByte = 3;

    [Test]
    public void FrameBytesAreStraightAlpha()
    {
        var frame = RenderPreDrawn(SKColors.Red.WithAlpha(128));

        frame.Bitmap.ColorType.Should().Be(SKColorType.Rgba8888);

        // Straight alpha keeps red at full intensity next to alpha 128.
        // Premultiplied would roughly halve it, which is what ffmpeg would then read as too dark.
        var bytes = frame.Bitmap.GetPixelSpan();
        bytes[RedByte].Should().BeGreaterThan(250);
        bytes[AlphaByte].Should().BeInRange(126, 130);
    }

    [Test]
    public void OpaquePixelsAreUnchanged()
    {
        var frame = RenderPreDrawn(SKColors.Red);

        var bytes = frame.Bitmap.GetPixelSpan();
        bytes[RedByte].Should().Be(255);
        bytes[AlphaByte].Should().Be(255);
    }

    [Test]
    public void FrameFormatIsRgba()
    {
        var frame = RenderPreDrawn(SKColors.Red.WithAlpha(128));

        frame.Format.Should().Be("rgba");
    }
}