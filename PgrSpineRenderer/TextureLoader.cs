using SkiaSharp;
using Spine;

namespace PgrSpineRenderer;

public class TextureLoader : Spine.TextureLoader
{
    public void Load(AtlasPage page, string path)
    {
        using var bitmap = SKBitmap.Decode(path);
        if (bitmap == null)
            throw new Exception($"Failed to decode image: {path}");

        bitmap.SetImmutable();

        page.rendererObject = SKImage.FromBitmap(bitmap)
                              ?? throw new Exception($"Failed to load image: {path}");
    }

    public void Unload(object texture)
    {
        ((SKImage)texture).Dispose();
    }
}