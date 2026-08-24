using FFMpegCore.Pipes;
using SkiaSharp;

namespace PgrSpineRenderer.Rendering;

/// <summary>
///     Bastardised version of BitmapVideoFrameWrapper from FFMpegCore.Extensions.SkiaSharp,
///     but instead of keeping an SKBitmap, we clone the bytes so that ffmpeg doesn't cause SkiaSharp to explode.
///     This isn't great, but it seems to work, which is more than the other experiments.
/// </summary>
public class SkiaFrame(SKBitmap bitmap) : IVideoFrame, IDisposable
{
    private const SKColorType FrameColorType = SKColorType.Rgba8888;

    public SKBitmap Bitmap { get; } = bitmap;

    public void Dispose()
    {
        Bitmap.Dispose();
        GC.SuppressFinalize(this);
    }

    public int Width { get; } = bitmap.Width;

    public int Height { get; } = bitmap.Height;

    public string Format { get; } = ConvertStreamFormat(bitmap.ColorType);

    public void Serialize(Stream stream)
    {
        stream.Write(Bitmap.GetPixelSpan());
    }

    public Task SerializeAsync(Stream stream, CancellationToken token)
    {
        Serialize(stream);
        return Task.CompletedTask;
    }

    public static SkiaFrame FromImage(SKImage image)
    {
        var info = new SKImageInfo(image.Width, image.Height, FrameColorType, SKAlphaType.Unpremul,
            SKColorSpace.CreateSrgb());
        var bitmap = new SKBitmap(info);

        return !image.ReadPixels(info, bitmap.GetPixels(), info.RowBytes, 0, 0)
            ? throw new InvalidOperationException("Failed to read frame pixels out of the rendered image")
            : new SkiaFrame(bitmap);
    }

    private static string ConvertStreamFormat(SKColorType fmt)
    {
        return fmt switch
        {
            SKColorType.Rgb565 => "rgb565",
            SKColorType.Rgba8888 => "rgba",
            SKColorType.Rgb888x => "rgb",
            SKColorType.Bgra8888 => "bgra",
            SKColorType.Gray8 => "gray8",
            _ => throw new NotSupportedException($"Not supported pixel format {(object)fmt}")
        };
    }
}