using System.Numerics;
using SkiaSharp;

namespace PgrSpineRenderer.Rendering;

/// <summary>
///     Writes a single rendered frame out as a webp poster image.
/// </summary>
public static class KeyframeWriter
{
    private const int Quality = 80;

    public static void Write(SkiaFrame frame, Vector2 targetSize, string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var info = new SKImageInfo((int)targetSize.X, (int)targetSize.Y, frame.Bitmap.ColorType,
            SKAlphaType.Premul);
        using var scaled = frame.Bitmap.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell))
                           ?? throw new InvalidOperationException(
                               $"Failed to scale keyframe to {info.Width}x{info.Height}");

        using var data = scaled.Encode(SKEncodedImageFormat.Webp, Quality)
                         ?? throw new InvalidOperationException("Skia has no webp encoder available");

        using var stream = File.Create(outputPath);
        data.SaveTo(stream);
    }
}