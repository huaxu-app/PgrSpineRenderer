using System.Collections.Concurrent;
using System.Numerics;
using FFMpegCore;
using FFMpegCore.Pipes;
using PgrSpineRenderer.CodecHelper;
using PgrSpineRenderer.Index;
using PgrSpineRenderer.Rendering;
using Spine;

namespace PgrSpineRenderer;

public class SpineRenderer
{
    private readonly AnimationSet _animationSet = new();
    private readonly List<BoneTrackerInfo> _boneFollowers = [];
    private readonly Vector2 _canvasSize;
    private readonly float _fps;

    private readonly IFrameRenderer _frameRenderer;
    private readonly RendererSettings _settings;
    private readonly List<(Entry entry, SkeletonData data)> _skeletonData = [];

    public SpineRenderer(IFrameRenderer frameRenderer, float fps, Vector2? canvasSize, RendererSettings settings)
    {
        _frameRenderer = frameRenderer;
        _fps = fps;
        _settings = settings;
        _canvasSize = (canvasSize ?? new Vector2(1920, 1080)) * settings.Scale;
        if (settings.Quirk is not null) Console.WriteLine("Using render quirk " + settings.Quirk);
    }

    public List<string> Animations => _animationSet.GetRenderSet();

    private Vector2 OutputSize
    {
        get
        {
            var size = _canvasSize / _settings.Scale;
            return new Vector2((int)size.X & ~1, (int)size.Y & ~1);
        }
    }

    /// <summary>
    ///     Add a skeleton to the renderer. Takes a *partial* path to the skeleton files, without extension.
    /// </summary>
    public void AddSkeleton(Entry skeleton, string path)
    {
        var rawPath = Path.Join(path, skeleton.Name);
        var isBinary = rawPath.EndsWith(".skel");
        path = RemoveFromEnd(rawPath, ".skel");

        var atlasPath = File.Exists($"{path}.atlas") ? $"{path}.atlas" : $"{path}.atlas.txt";
        var atlas = new Atlas(atlasPath, new TextureLoader());

        var skeletonData = isBinary
            ? new SkeletonBinary(atlas).ReadSkeletonData(rawPath)
            : new SkeletonJson(atlas).ReadSkeletonData($"{path}.json");

        _animationSet.RegisterLayer(LayeredAnimations.Flatten(skeletonData));
        _skeletonData.Add((skeleton, skeletonData));
    }

    public void AddBoneFollower(BoneFollower meta)
    {
        var sourceIndex = _skeletonData.FindIndex(s => s.entry.Name == meta.Skeleton);
        if (sourceIndex == -1)
            throw new ArgumentException($"Skeleton {meta.Skeleton} not found");

        foreach (var target in meta.Spines)
        {
            var targetIndex = _skeletonData.FindIndex(s => s.entry.Name == target);
            if (targetIndex == -1)
                throw new ArgumentException($"Skeleton target {target} not found");

            _boneFollowers.Add(new BoneTrackerInfo
            {
                Bone = meta.Bone,
                SourceIndex = sourceIndex,
                TargetIndex = targetIndex
            });
        }
    }

    public async Task GenerateVideo(string animationName, string outputPath, IRenderCodec codec,
        CancellationToken token = default)
    {
        if (Path.GetDirectoryName(outputPath) is var dir && !string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var sink = new BlockingCollection<SkiaFrame>(5);

        // Spawn nonblocking
        var ok = true;
        var ffmpegThread = Task.Run(() =>
        {
            try
            {
                ok = FFMpegArguments.FromPipeInput(new RawVideoPipeSource(Drain(sink.GetConsumingEnumerable()))
                    {
                        FrameRate = _fps
                    })
                    .OutputToFile(outputPath, true, options => codec.Apply(options, OutputSize))
                    .ProcessSynchronously();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to render video: " + e);
                sink.CompleteAdding();
                ok = false;
            }
        }, token);

        try
        {
            await GenerateFrames(animationName, sink, token);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync("Failed to generate frames: " + e);
        }
        finally
        {
            sink.CompleteAdding();
            await ffmpegThread;

            // If ffmpeg bailed out the pipe stopped consuming, so anything still queued is ours to free.
            foreach (var leftover in sink) leftover.Dispose();
        }

        if (ok && !token.IsCancellationRequested) return;

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        throw new Exception("Failed to render video");
    }

    public async Task GenerateKeyframe(string animationName, string outputPath, CancellationToken token = default)
    {
        var (skeletons, states, _) = InitAnimation(animationName);
        var followers = BoneFollowers(skeletons);

        for (var j = 0; j < states.Length; j++)
        {
            var skeleton = skeletons[j];
            states[j].Update(0);
            states[j].Apply(skeleton);
            skeleton.UpdateWorldTransform(Physics.Update);

            if (!followers.TryGetValue(j, out var follower)) continue;

            skeleton.X += follower.Offset.X;
            skeleton.Y += follower.Offset.Y;
            skeleton.UpdateWorldTransform(Physics.Pose);
        }

        using var frame = await _frameRenderer.Render(_canvasSize, OutputSize, skeletons, token);
        KeyframeWriter.Write(frame, OutputSize, outputPath);
    }

    private static IEnumerable<IVideoFrame> Drain(IEnumerable<SkiaFrame> frames)
    {
        foreach (var frame in frames)
            using (frame)
            {
                yield return frame;
            }
    }

    /// <summary>
    ///     Take the skeletons and return instances prepared for rendering.
    /// </summary>
    /// <returns></returns>
    private Skeleton[] Skeletons()
    {
        return
        [
            .. _skeletonData.Select(pair =>
            {
                var scale = pair.entry.Scale * _settings.Scale;
                var position = _canvasSize * (Vector2.One - pair.entry.Pivot) +
                               pair.entry.Position.GetValueOrDefault() * new Vector2(1, -1) * _settings.Scale;

                var skeleton = new Skeleton(pair.data)
                {
                    X = position.X,
                    Y = position.Y,
                    ScaleX = scale,
                    // PGR flips the Y axis
                    ScaleY = scale * -1
                };

                skeleton.UpdateWorldTransform(Physics.Update);
                return skeleton;
            })
        ];
    }

    /// <summary>
    ///     Build the skeletons and animation states for a named animation, stepped to t=0,
    ///     and work out how long the animation runs for.
    /// </summary>
    private (Skeleton[] Skeletons, AnimationState[] States, float Duration) InitAnimation(string animationName)
    {
        var duration = 0f;
        var skeletons = Skeletons();

        var states = skeletons.Select(s =>
        {
            var state = new AnimationState(new AnimationStateData(s.Data));
            var available = LayeredAnimations.Flatten(s.Data).Select(m => m.Name);
            var layerAnimationName = _animationSet.Resolve(available, animationName);

            // Layered skeletons play their body, face and mouth parts together, one per track.
            var tracks = LayeredAnimations.Tracks(s.Data, layerAnimationName);
            for (var track = 0; track < tracks.Count; track++)
                state.SetAnimation(track, tracks[track].Name, true);

            // The body track is the one that dictates how long we render for.
            var animation = tracks[0];

            if (duration == 0)
                duration = animation.Duration;
            // If we're in short mode, we want the shortest animation, not the longest
            else if (_settings.Quirk == RenderQuirk.Short && animation.Duration < duration && animation.Duration > 0)
                duration = animation.Duration;
            // By default, we want the longest animation
            else if (animation.Duration > duration) duration = animation.Duration;

            state.Update(0);
            state.Apply(s);
            s.UpdateWorldTransform(Physics.Update);
            return state;
        }).ToArray();

        return duration == 0
            ? throw new EmptyAnimationException($"Animation {animationName} is empty")
            : (skeletons, states, duration);
    }

    private Dictionary<int, SpineBoneOffsetTracker> BoneFollowers(Skeleton[] skeletons)
    {
        var followers = new Dictionary<int, SpineBoneOffsetTracker>();
        foreach (var follower in _boneFollowers)
        {
            var tracker = SpineBoneOffsetTracker.Resolve(skeletons[follower.SourceIndex],
                skeletons[follower.TargetIndex], follower.Bone);
            if (tracker is null) continue;

            followers[follower.TargetIndex] = tracker;
        }

        return followers;
    }

    /// <summary>
    ///     Generator for the frames of a specific animation.
    ///     Initializes the skeletons and updates them for each frame,
    ///     and then draws them onto a bitmap.
    /// </summary>
    private async Task GenerateFrames(string animationName, BlockingCollection<SkiaFrame> sink,
        CancellationToken token = default)
    {
        var (skeletons, states, duration) = InitAnimation(animationName);

        var followers = BoneFollowers(skeletons);

        var frames = (int)Math.Ceiling(duration * _fps);
        var frameTime = 1.0f / _fps;

        for (var i = 0; i < frames; i++)
        {
            if (token.IsCancellationRequested) break;

            for (var j = 0; j < states.Length; j++)
            {
                var state = states[j];
                // First loop won't get any delta time
                state.Update(i == 0 ? 0 : frameTime);

                var skeleton = skeletons[j];
                state.Apply(skeleton);
                skeleton.UpdateWorldTransform(Physics.Update);

                if (!followers.TryGetValue(j, out var follower)) continue;

                // The offset is measured against this frame's pose, so the skeleton has to be posed
                // first. Re-pose after moving it, without stepping physics a second time.
                skeleton.X += follower.Offset.X;
                skeleton.Y += follower.Offset.Y;
                skeleton.UpdateWorldTransform(Physics.Pose);
            }

            var frame = await _frameRenderer.Render(_canvasSize, OutputSize, skeletons, token);
            if (sink.IsAddingCompleted) break;
            try
            {
                sink.Add(frame, token);
            }
            catch (InvalidOperationException)
            {
                // ffmpeg finished or failed and completed the sink; stop producing quietly
                // so its error is the one that surfaces, not a cascade.
                break;
            }
        }
    }

    private static string RemoveFromEnd(string s, string suffix)
    {
        return s.EndsWith(suffix) ? s[..^suffix.Length] : s;
    }

    private struct BoneTrackerInfo
    {
        public string Bone;
        public int SourceIndex;
        public int TargetIndex;
    }

    public struct RendererSettings()
    {
        public RenderQuirk? Quirk = null;
        public int Scale = 2;
    }
}