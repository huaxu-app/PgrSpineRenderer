using System.Numerics;
using SkiaSharp;
using Spine;

namespace PgrSpineRenderer;

public class Renderer
{
    private readonly SKPaint _paint = new()
    {
        FilterQuality = SKFilterQuality.High
    };

    private SKImage? _lastTexture;

    private int[] QuadTriangles { get; } = [0, 1, 2, 1, 3, 2];


    public void Draw(SKCanvas canvas, Skeleton skeleton)
    {
        var clipper = new SkeletonClipping();
        canvas.Save();

        foreach (var slot in skeleton.DrawOrder)
        {
            var attachment = slot.Attachment;
            if (attachment is null) continue;

            SKImage? texture;
            AtlasRegion? region;
            var worldVertices = new float[8];
            float[] uvs;
            int[] triangles;
            Vector4 attachmentColor;

            if (attachment is RegionAttachment regionAttachment)
            {
                region = (regionAttachment.RendererObject as AtlasRegion)!;
                texture = region.page.rendererObject as SKImage;
                uvs = regionAttachment.UVs;
                attachmentColor = new Vector4(regionAttachment.R, regionAttachment.G, regionAttachment.B,
                    regionAttachment.A);

                regionAttachment.ComputeWorldVertices(slot.Bone, worldVertices, 0);
                triangles = QuadTriangles;
            }
            else if (attachment is MeshAttachment meshAttachment)
            {
                region = (meshAttachment.RendererObject as AtlasRegion)!;
                texture = region.page.rendererObject as SKImage;
                uvs = meshAttachment.UVs;
                if (worldVertices.Length < uvs.Length) worldVertices = new float[uvs.Length];
                attachmentColor = new Vector4(meshAttachment.R, meshAttachment.G, meshAttachment.B, meshAttachment.A);

                meshAttachment.ComputeWorldVertices(slot, worldVertices);
                triangles = meshAttachment.Triangles;
            }
            else if (attachment is ClippingAttachment clippingAttachment)
            {
                clipper.ClipStart(slot, clippingAttachment);
                continue;
            }
            else
            {
                throw new NotImplementedException(
                    $"Attachment type {attachment.GetType().FullName} not supported yet.");
            }

            if (texture is null) continue;
            if (_lastTexture != texture)
            {
                _lastTexture = texture;
                _paint.Shader = texture.ToShader();
            }


            if (clipper.IsClipping)
            {
                clipper.ClipTriangles(
                    worldVertices,
                    worldVertices.Length,
                    triangles,
                    triangles.Length,
                    uvs
                );
                worldVertices = clipper.ClippedVertices.Items;
                triangles = clipper.ClippedTriangles.Items;
                uvs = clipper.ClippedUVs.Items;
            }

            var textureWidth = texture.Width;
            var textureHeight = texture.Height;
            List<SKPoint> vertices = [];
            List<SKPoint> texturePoints = [];
            List<SKColor> colors = [];
            var indices = triangles.Select(x => (ushort)x).ToArray();

            var color = new SKColorF(
                skeleton.R * slot.R * attachmentColor.X,
                skeleton.G * slot.G * attachmentColor.Y,
                skeleton.B * slot.B * attachmentColor.Z,
                skeleton.A * slot.A * attachmentColor.W
            );

            for (var i = 0; i < worldVertices.Length; i += 2)
            {
                vertices.Add(new SKPoint(worldVertices[i], worldVertices[i + 1]));
                texturePoints.Add(new SKPoint(textureWidth * uvs[i], textureHeight * uvs[i + 1]));
                colors.Add((SKColor)color);
            }

            // Determine and set correct blend mode
            _paint.BlendMode = slot.data.BlendMode switch
            {
                BlendMode.Screen => SKBlendMode.Screen,
                BlendMode.Additive => SKBlendMode.Plus,
                BlendMode.Multiply => SKBlendMode.Multiply,
                _ => SKBlendMode.SrcOver
            };

            canvas.DrawVertices(
                SKVertexMode.Triangles,
                vertices.ToArray(),
                texturePoints.ToArray(),
                colors.ToArray(),
                SKBlendMode.Modulate,
                indices,
                _paint
            );

            clipper.ClipEnd(slot);
        }

        clipper.ClipEnd();
        canvas.Restore();
    }
}