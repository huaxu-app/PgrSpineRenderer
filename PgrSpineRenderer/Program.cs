using System.Numerics;
using FFMpegCore;
using FFMpegCore.Extensions.SkiaSharp;
using FFMpegCore.Pipes;
using SkiaSharp;
using Spine;

namespace PgrSpineRenderer;

internal static class Program
{
    private const float Fps = 30.0f;
    private static readonly Vector2 CanvasSize = new(1920, 1080);

    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            var process = AppDomain.CurrentDomain.FriendlyName;
            Console.WriteLine($"usage: {process} <skeleton> [<skeleton>...]");
            Console.WriteLine("""

                              Renders the animations of the specified skeletons to webm videos.
                              Skeletons should be the path towards the .json and .atlas.txt files, without extension.
                              For example: 'luciyasairen' for 'luciyasairen.json' and 'luciyasairen.atlas.txt'

                              When multiple skeletons are specified, they will be rendered on top of each other.
                              The first skeleton will be the bottom layer, and the last one will be the top layer.
                              You can add as many skeletons as you want.

                              The output videos will be named after the animations contained within,
                              for example `idle.webm`.

                              """);


            return;
        }

        var data = args.Select(LoadSkeletonData).ToArray();

        var animations = data[0].animations;
        Console.WriteLine($"Found {animations.Count} animations:");
        foreach (var animation in animations)
        {
            Console.WriteLine($"- {animation.name}");
            await GenerateVideo(data, animation.name);
        }

        Console.WriteLine("All animations rendered successfully");
    }

    private static SkeletonData LoadSkeletonData(string skeletonName)
    {
        var atlas = new Atlas($"{skeletonName}.atlas.txt", new TextureLoader());
        var json = new SkeletonJson(atlas)
        {
            Scale = 0.5f
        };
        return json.ReadSkeletonData($"{skeletonName}.json");
    }

    private static async Task GenerateVideo(IEnumerable<SkeletonData> skeletonData, string animationName)
    {
        var skeletons = skeletonData.Select(s =>
        {
            var skeleton = new Skeleton(s);
            PositionSkeleton(skeleton, CanvasSize);
            return skeleton;
        }).ToArray();

        var outputPath = $"{animationName}.webm";
        var ok = await FFMpegArguments.FromPipeInput(
                new RawVideoPipeSource(GenerateFrames(skeletons, animationName, CanvasSize, Fps))
                {
                    FrameRate = Fps
                })
            .OutputToFile(outputPath, true, options => options.WithVideoCodec("libvpx-vp9"))
            .ProcessAsynchronously();
        if (!ok)
        {
            // Delete output file
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            Console.WriteLine($"Failed to render animation {animationName}");
            throw new Exception("Failed to render animation " + animationName);
        }
    }

    private static void PositionSkeleton(Skeleton skeleton, Vector2 canvas)
    {
        skeleton.X = canvas.X / 2;
        skeleton.Y = canvas.Y / 2;
        // PGR flips the Y axis
        skeleton.ScaleY = -1;
        skeleton.UpdateWorldTransform();
    }

    private static IEnumerable<BitmapVideoFrameWrapper> GenerateFrames(IReadOnlyList<Skeleton> skeletons,
        string animationName,
        Vector2 size,
        float fps)
    {
        var duration = 0f;
        var states = skeletons.Select(s =>
        {
            var state = new AnimationState(new AnimationStateData(s.data));
            var animation = s.data.animations.Find(m => m.name == animationName);
            state.SetAnimation(0, animation.name, false);
            if (animation.Duration > duration)
                duration = animation.Duration;
            return state;
        }).ToArray();

        var frames = (int)Math.Ceiling(duration * fps);
        var frameTime = 1.0f / fps;

        for (var i = 0; i < frames; i++)
        {
            using var bitmap = new SKBitmap((int)size.X, (int)size.Y);
            using var canvas = new SKCanvas(bitmap);

            for (var j = 0; j < states.Length; j++)
            {
                var state = states[j];
                state.Update(frameTime);
                state.Apply(skeletons[j]);
                skeletons[j].UpdateWorldTransform();
                Renderer.Draw(canvas, skeletons[j]);
            }

            yield return new BitmapVideoFrameWrapper(bitmap);
        }
    }
}