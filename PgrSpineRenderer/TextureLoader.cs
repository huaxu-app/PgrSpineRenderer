using SkiaSharp;
using Spine;

namespace PgrSpineRenderer;

public class TextureLoader : Spine.TextureLoader
{
    public void Load(AtlasPage page, string path)
    {
        var bitmap = SKBitmap.Decode(path.ToLower());
        bitmap.SetImmutable();
        var image = SKImage.FromBitmap(bitmap);
        page.rendererObject = image;
    }

    public void Unload(object texture)
    {
        ((SKBitmap)texture).Dispose();
    }
}