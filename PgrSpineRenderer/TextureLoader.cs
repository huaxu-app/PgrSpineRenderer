using SkiaSharp;
using Spine;

namespace PgrSpineRenderer;

public class TextureLoader : Spine.TextureLoader
{
    public void Load(AtlasPage page, string path)
    {
        var bitmap = SKBitmap.Decode(path);
        if (bitmap == null)
            throw new Exception($"Failed to decode image: {path}");
        bitmap.SetImmutable();
        var image = SKImage.FromBitmap(bitmap);
        if (image == null)
            throw new Exception($"Failed to load image: {path}");
        page.rendererObject = image;
    }

    public void Unload(object texture)
    {
        ((SKBitmap)texture).Dispose();
    }
}