using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Extensions.SkiaSharp;
using FFMpegCore.Pipes;
using ShellProgressBar;
using SkiaSharp;
using Spine;

namespace PgrSpineRenderer;

public class Renderer(float fps, Vector2 canvasSize)
{
    private readonly List<SkeletonData> _skeletonData = [];
    public List<string> Animations { get; private set; } = [];

    /// <summary>
    ///     Add a skeleton to the renderer. Takes a *partial* path to the skeleton files, without extension.
    /// </summary>
    /// <param name="path"></param>
    public void AddSkeleton(string path)
    {
        var atlasPath = File.Exists($"{path}.atlas") ? $"{path}.atlas" : $"{path}.atlas.txt";
        var atlas = new Atlas(atlasPath, new TextureLoader());
        var json = new SkeletonJson(atlas)
        {
            Scale = 0.5f
        };
        var skeletonData = json.ReadSkeletonData($"{path}.json");

        // No animations yet? Add them
        if (Animations.Count == 0)
            Animations = skeletonData.animations.Select(a => a.name).ToList();
        else if (Animations.Any(a => skeletonData.animations.All(s => s.name != a)))
            throw new RenderException($"The skeleton {path} does not contain all animations of the other skeletons");

        _skeletonData.Add(skeletonData);
    }

    public async Task GenerateVideo(string animationName, string outputPath, Codec codec)
    {
        if (Path.GetDirectoryName(outputPath) is var dir && !string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        var ok = await FFMpegArguments.FromPipeInput(
                new RawVideoPipeSource(Frames(animationName))
                {
                    FrameRate = fps
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

    /// <summary>
    /// Take the skeletons and return instances prepared for rendering.
    /// </summary>
    /// <returns></returns>
    private Skeleton[] Skeletons()
    {
        var scale = DetermineScale();
        return _skeletonData.Select(s =>
        {
            var skeleton = new Skeleton(s)
            {
                X = canvasSize.X / 2,
                Y = canvasSize.Y / 2,
                ScaleX = scale,
                // PGR flips the Y axis
                ScaleY = -scale
            };

            skeleton.UpdateWorldTransform();
            return skeleton;
        }).ToArray();
    }

    /// <summary>
    /// Generator for the frames of a specific animation.
    /// Initializes the skeletons and updates them for each frame,
    /// and then draws them onto a bitmap.
    /// </summary>
    /// <param name="animationName">Animation to use. Check <see cref="Animations"/> to see the options.</param>
    /// <returns>Generator full of <see cref="BitmapVideoFrameWrapper"/>'s you can pass into FFMPEG</returns>
    private IEnumerable<BitmapVideoFrameWrapper> Frames(string animationName)
    {
        var duration = 0f;
        var skeletons = Skeletons();
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
        
        using var progress = new ProgressBar(frames, $"Rendering '{animationName}'", new ProgressBarOptions
        {
            ForegroundColor = ConsoleColor.White,
            ForegroundColorDone = ConsoleColor.Green,
            ForegroundColorError = ConsoleColor.Red,
            ProgressBarOnBottom = true
        });

        for (var i = 0; i < frames; i++)
        {
            using var bitmap = new SKBitmap((int)canvasSize.X, (int)canvasSize.Y);
            using var canvas = new SKCanvas(bitmap);

            for (var j = 0; j < states.Length; j++)
            {
                var state = states[j];
                state.Update(frameTime);
                state.Apply(skeletons[j]);
                skeletons[j].UpdateWorldTransform();
                SpineDrawer.Draw(canvas, skeletons[j]);
            }

            yield return new BitmapVideoFrameWrapper(bitmap);
            progress.Tick();
        }
    }

    /// <summary>
    ///     Calculates the scale to fit all skeletons into the canvas.
    ///     Uses a similar approach to CSS 'contain: cover'
    /// </summary>
    /// <returns></returns>
    private float DetermineScale()
    {
        var scale = 1 / _skeletonData
            .Select(data => Math.Min(data.Width / 2 / canvasSize.X, data.Height / 2 / canvasSize.Y)).Max();
        return (float)Math.Ceiling(scale * 100) / 100;
    }
}