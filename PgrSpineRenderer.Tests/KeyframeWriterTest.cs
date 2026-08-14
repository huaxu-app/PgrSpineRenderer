using System;
using System.IO;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using PgrSpineRenderer.Rendering;
using SkiaSharp;

namespace PgrSpineRenderer.Tests;

[TestFixture]
[TestOf(typeof(KeyframeWriter))]
public class KeyframeWriterTest
{
    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "keyframe-test-" + Guid.NewGuid().ToString("n"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private string _dir = null!;

    /// <summary>
    ///     A 100x50 bitmap: left half opaque red, right half fully transparent.
    /// </summary>
    private static SkiaFrame MakeFrame()
    {
        var bitmap = new SKBitmap(new SKImageInfo(100, 50, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            using var paint = new SKPaint { Color = SKColors.Red };
            canvas.DrawRect(new SKRect(0, 0, 50, 50), paint);
        }

        return new SkiaFrame(bitmap);
    }

    [Test]
    public void WritesAFileAtTheTargetSize()
    {
        var path = Path.Combine(_dir, "idle.webp");

        KeyframeWriter.Write(MakeFrame(), new Vector2(50, 25), path);

        File.Exists(path).Should().BeTrue();
        using var decoded = SKBitmap.Decode(path);
        decoded.Should().NotBeNull();
        decoded.Width.Should().Be(50);
        decoded.Height.Should().Be(25);
    }

    [Test]
    public void PreservesAlpha()
    {
        var path = Path.Combine(_dir, "idle.webp");

        KeyframeWriter.Write(MakeFrame(), new Vector2(50, 25), path);

        using var decoded = SKBitmap.Decode(path);
        // Left quarter is inside the red block, right quarter is inside the transparent block.
        decoded.GetPixel(10, 12).Alpha.Should().BeGreaterThan(200);
        decoded.GetPixel(40, 12).Alpha.Should().BeLessThan(50);
    }

    [Test]
    public void CreatesTheOutputDirectory()
    {
        var path = Path.Combine(_dir, "nested", "idle.webp");

        KeyframeWriter.Write(MakeFrame(), new Vector2(50, 25), path);

        File.Exists(path).Should().BeTrue();
    }

    [Test]
    public void KeepsSemiTransparentColourFromAnUnpremultipliedFrame()
    {
        var path = Path.Combine(_dir, "idle.webp");
        var bitmap = new SKBitmap(new SKImageInfo(100, 50, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
            bitmap.SetPixel(x, y, new SKColor(255, 0, 0, 128));

        KeyframeWriter.Write(new SkiaFrame(bitmap), new Vector2(50, 25), path);

        using var decoded = SKBitmap.Decode(path);
        var pixel = decoded.GetPixel(25, 12);
        // Straight red at half alpha stays full-intensity red, it does not come out at ~128.
        pixel.Red.Should().BeGreaterThan(240);
        pixel.Green.Should().BeLessThan(15);
        pixel.Blue.Should().BeLessThan(15);
        pixel.Alpha.Should().BeInRange(120, 136);
    }

    /// <summary>
    ///     A 100x50 bitmap of deterministic noise. Flat colour blocks compress to almost nothing
    ///     either way, so only noise tells a lossy encode apart from a lossless one.
    /// </summary>
    private static SKBitmap MakeNoiseBitmap()
    {
        var bitmap = new SKBitmap(new SKImageInfo(100, 50, SKColorType.Rgba8888, SKAlphaType.Premul));
        var random = new Random(1234);
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
            bitmap.SetPixel(x, y, new SKColor((byte)random.Next(256), (byte)random.Next(256),
                (byte)random.Next(256), 255));

        return bitmap;
    }

    [Test]
    public void EncodesLossy()
    {
        var path = Path.Combine(_dir, "idle.webp");
        var bitmap = MakeNoiseBitmap();

        // Same size in and out, so the written file and the reference below encode the same pixels.
        KeyframeWriter.Write(new SkiaFrame(bitmap), new Vector2(bitmap.Width, bitmap.Height), path);

        using var lossless = new MemoryStream();
        bitmap.PeekPixels()
            .Encode(lossless, new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100))
            .Should().BeTrue();

        // On noise a lossless encode is several times the size of a quality-80 lossy one.
        // If the writer ever silently falls back to lossless, the two converge and this fails.
        new FileInfo(path).Length.Should().BeLessThan(lossless.Length / 2);
    }
}