using System.Diagnostics;
using SkiaSharp;
using Spine;

namespace PgrSpineRenderer.Rendering;

public static class SpineDrawer
{
    private static readonly int[] QuadTriangles = [0, 1, 2, 2, 3, 0];

    public static SkiaFrame DrawJob(SKSurface surface, Skeleton[] skeletons)
    {
        Metrics.FramesRendered.Add(1);
        var stopwatch = Stopwatch.StartNew();
        foreach (var s in skeletons) Draw(surface.Canvas, s);
        surface.Flush();
        var image = surface.Snapshot();
        var frame = new SkiaFrame(ToStraightAlpha(image));
        image.Dispose();
        Metrics.FrameDrawTime.Record(stopwatch.ElapsedMilliseconds);
        return frame;
    }

    private static SKBitmap ToStraightAlpha(SKImage image)
    {
        var info = new SKImageInfo(image.Width, image.Height, image.ColorType, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);
        return !image.ReadPixels(info, bitmap.GetPixels(), info.RowBytes, 0, 0)
            ? throw new InvalidOperationException("Failed to read frame pixels out of the rendered image")
            : bitmap;
    }

    private static void Draw(SKCanvas canvas, Skeleton skeleton)
    {
        SKImage? lastTexture = null;
        var clipper = new SkeletonClipping();
        var skeletonColor = skeleton.GetColor();
        var paint = new SKPaint
        {
            IsAntialias = true
        };
        canvas.Save();

        foreach (var slot in skeleton.DrawOrder.AppliedPose)
        {
            var slotPose = slot.AppliedPose;
            var attachment = slotPose.Attachment;
            if (attachment is null) continue;

            SKImage? texture;
            var worldVertices = new float[8];
            float[] uvs;
            int[] triangles;
            Color32F attachmentColor;

            if (attachment is RegionAttachment regionAttachment)
            {
                var index = regionAttachment.Sequence.ResolveIndex(slotPose);
                texture = (regionAttachment.Sequence.GetRegion(index) as AtlasRegion)?.page.rendererObject as SKImage;
                uvs = regionAttachment.Sequence.GetUVs(index);
                attachmentColor = regionAttachment.GetColor();

                regionAttachment.ComputeWorldVertices(slot, regionAttachment.GetOffsets(slotPose), worldVertices, 0);
                triangles = QuadTriangles;
            }
            else if (attachment is MeshAttachment meshAttachment)
            {
                var index = meshAttachment.Sequence.ResolveIndex(slotPose);
                texture = (meshAttachment.Sequence.GetRegion(index) as AtlasRegion)?.page.rendererObject as SKImage;
                uvs = meshAttachment.Sequence.GetUVs(index);
                if (worldVertices.Length < uvs.Length) worldVertices = new float[uvs.Length];
                attachmentColor = meshAttachment.GetColor();

                meshAttachment.ComputeWorldVertices(skeleton, slot, worldVertices);
                triangles = meshAttachment.Triangles;
            }
            else if (attachment is ClippingAttachment clippingAttachment)
            {
                clipper.ClipStart(skeleton, slot, clippingAttachment);
                continue;
            }
            else
            {
                continue;
            }

            if (texture is null) continue;
            if (lastTexture != texture)
            {
                lastTexture = texture;
                paint.Shader = texture.ToShader();
            }


            if (clipper.IsClipping)
            {
                clipper.ClipTriangles(
                    worldVertices,
                    triangles,
                    triangles.Length,
                    uvs
                );
                worldVertices = clipper.ClippedVertices.ToArray();
                triangles = clipper.ClippedTriangles.ToArray();
                uvs = clipper.ClippedUVs.ToArray();
            }

            var textureWidth = texture.Width;
            var textureHeight = texture.Height;
            List<SKPoint> vertices = [];
            List<SKPoint> texturePoints = [];
            List<SKColor> colors = [];
            var indices = triangles.Select(x => (ushort)x).ToArray();

            var slotColor = slotPose.GetColor();
            var color = new SKColorF(
                skeletonColor.r * slotColor.r * attachmentColor.r,
                skeletonColor.g * slotColor.g * attachmentColor.g,
                skeletonColor.b * slotColor.b * attachmentColor.b,
                skeletonColor.a * slotColor.a * attachmentColor.a
            );

            for (var i = 0; i < Math.Min(worldVertices.Length, uvs.Length); i += 2)
            {
                vertices.Add(new SKPoint(worldVertices[i], worldVertices[i + 1]));
                texturePoints.Add(new SKPoint(textureWidth * uvs[i], textureHeight * uvs[i + 1]));
                colors.Add((SKColor)color);
            }

            // Determine and set correct blend mode
            paint.BlendMode = slot.Data.BlendMode switch
            {
                BlendMode.Screen => SKBlendMode.Screen,
                BlendMode.Additive => SKBlendMode.Plus,
                BlendMode.Multiply => SKBlendMode.Multiply,
                _ => SKBlendMode.SrcOver
            };


            var vert = SKVertices.CreateCopy(SKVertexMode.Triangles, vertices.ToArray(), texturePoints.ToArray(),
                colors.ToArray(), indices);
            canvas.DrawVertices(vert, SKBlendMode.Modulate, paint);
            vert.Dispose();

            clipper.ClipEnd(slot);
        }

        clipper.ClipEnd();
        canvas.Restore();
    }
}