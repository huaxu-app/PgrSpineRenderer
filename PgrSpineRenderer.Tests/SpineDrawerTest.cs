using FluentAssertions;
using NUnit.Framework;
using PgrSpineRenderer.Rendering;
using SkiaSharp;

namespace PgrSpineRenderer.Tests;

[TestFixture]
[TestOf(typeof(SpineDrawer))]
public class SpineDrawerTest
{
    /// <summary>
    ///     DrawJob takes the surface as a parameter, so a CPU raster surface works here and no GL
    ///     context is needed. An empty skeleton array means only what we pre-draw ends up in the frame.
    ///     Bgra8888 plus Premul is what the renderer's own surface resolves to, since Renderer builds
    ///     its SKImageInfo from width and height alone and those are the defaults it lands on.
    /// </summary>
    private static SkiaFrame RenderPreDrawn(SKColor color)
    {
        using var surface = SKSurface.Create(new SKImageInfo(4, 4, SKColorType.Bgra8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        using (var paint = new SKPaint { Color = color, BlendMode = SKBlendMode.Src })
        {
            surface.Canvas.DrawRect(new SKRect(0, 0, 4, 4), paint);
        }

        return SpineDrawer.DrawJob(surface, []);
    }

    [Test]
    public void FrameIsUnpremultiplied()
    {
        var frame = RenderPreDrawn(SKColors.Red.WithAlpha(128));

        frame.Bitmap.AlphaType.Should().Be(SKAlphaType.Unpremul);
    }

    /// <summary>
    ///     Bgra8888 puts the bytes down as blue, green, red, alpha, so red is index 2.
    ///     Indexing the wrong byte here would make these tests pass or fail on channel order
    ///     rather than on the premultiplied to straight conversion they are meant to check.
    /// </summary>
    private const int RedByte = 2;

    private const int AlphaByte = 3;

    [Test]
    public void FrameBytesAreStraightAlpha()
    {
        var frame = RenderPreDrawn(SKColors.Red.WithAlpha(128));

        frame.Bitmap.ColorType.Should().Be(SKColorType.Bgra8888);

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
    public void FrameFormatIsBgra()
    {
        var frame = RenderPreDrawn(SKColors.Red.WithAlpha(128));

        // The fix changes the alpha type only, so the format ffmpeg is told about is untouched.
        frame.Format.Should().Be("bgra");
    }
}