using System.CommandLine;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using PgrSpineRenderer.CodecHelper;
using PgrSpineRenderer.Rendering;

namespace PgrSpineRenderer;

internal static class Program
{
    private static readonly Dictionary<CodecOption, IRenderCodec> Codecs = new()
    {
        { CodecOption.VP9, new VP9() },
        { CodecOption.H264, new H264() },
        { CodecOption.H264NV, new H264NV() },
        { CodecOption.Mov, new Mov() }
    };

    private const float DefaultFps = 30.0f;

    public static int Main(string[] args)
    {
        var rootCommand = new RootCommand("""
                                          Renders the animations of the specified index files to video.
                                          Animations will be written to the `render` directory in the same directory as the index file,
                                          with each animation being a separate video.

                                          A symbolic link to the default animation will be created as `_default.{ext}`,
                                          allowing for easy access to the default animation without knowing the name.

                                          When multiple index files are specified, they will be rendered in parallel,
                                          depending on the number of threads specified.

                                          By default the renderer will render to VP9 (webm) at 30fps.
                                          """);
        var fpsOption = new Option<float>("--fps")
        {
            Description = "The frames per second of the output video",
            DefaultValueFactory = _ => DefaultFps,
        };
        rootCommand.Options.Add(fpsOption);

        var codecOption = new Option<CodecOption>("--codec", "-c")
        {
            DefaultValueFactory = _ => CodecOption.VP9,
            Description = "The codec to use for the output videos. Defaults to 'vp9'"
        };
        rootCommand.Options.Add(codecOption);

        var encodeThreadsOption = new Option<int>("--encode-threads", "--et")
        {
            DefaultValueFactory = _ => 1,
            Description = "The number of threads to use for encoding. Might be capped by hardware limits (nvenc)"
        };
        rootCommand.Options.Add(encodeThreadsOption);

        var renderThreadsOption = new Option<int>("--render-threads", "--rt")
        {
            DefaultValueFactory = _ => 1,
            Description = "The number of threads to use for rendering"
        };

        rootCommand.Options.Add(renderThreadsOption);

        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Force rendering even if the index file has not changed"
        };
        rootCommand.Options.Add(forceOption);

        var linkDefaultOption = new Option<bool>("--link-default", "-l")
            { Description = "Create symlink for default animation" };
        rootCommand.Options.Add(linkDefaultOption);


        var indexArgument = new Argument<FileInfo[]>("indexes")
        {
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    result.AddError("You must specify at least one skeleton to render");
                    return [];
                }

                var files = new List<FileInfo>();
                foreach (var token in result.Tokens)
                {
                    if (!File.Exists(token.Value))
                    {
                        result.AddError($"File not found: {token.Value}");
                        continue;
                    }

                    files.Add(new FileInfo(token.Value));
                }

                if (files.Count == 0)
                    result.AddError("No valid files found");

                return files.ToArray();
            },
            Description = "The index files for the spines to render",
            Arity = ArgumentArity.OneOrMore
        };
        rootCommand.Arguments.Add(indexArgument);
        rootCommand.SetAction((result, token) => Handler(result.GetRequiredValue(indexArgument),
            result.GetRequiredValue(fpsOption),
            result.GetRequiredValue(codecOption),
            result.GetRequiredValue(forceOption),
            result.GetRequiredValue(encodeThreadsOption),
            result.GetRequiredValue(renderThreadsOption),
            result.GetRequiredValue(linkDefaultOption),
            token
        ));

        return rootCommand.Parse(args).Invoke();
    }


    private static async Task Handler(FileInfo[] indexFiles, float fps, CodecOption codecOption, bool forceOption,
        int encodeThreads, int renderThreads, bool linkDefaultAnimation, CancellationToken cancellationToken = default)
    {
        var codec = Codecs[codecOption];

        using var renderer = GetRenderer(renderThreads);

        await Parallel.ForEachAsync(indexFiles,
            new ParallelOptions { MaxDegreeOfParallelism = encodeThreads, CancellationToken = cancellationToken },
            async (indexFile, token) =>
            {
                var job = new RenderJob(indexFile, codec, renderer)
                    { Fps = fps, Force = forceOption, LinkDefaultAnimation = linkDefaultAnimation };
                await HandleIndex(job, token);
            });
    }

    private static Renderer GetRenderer(int threads)
    {
        var contexts = new IGLFWGraphicsContext[threads];
        // Create RenderThread amount of windows
        for (var i = 0; i < threads; i++)
        {
            var window = new NativeWindow(new NativeWindowSettings
            {
                StartVisible = false,
                StartFocused = false,
                Flags = ContextFlags.Offscreen,
                Title = $"PgrSpineRenderer{i}"
            });
            window.Context.MakeNoneCurrent();
            contexts[i] = window.Context ?? throw new ApplicationException("Render context is null");
        }

        return new Renderer(contexts);
    }

    private static async Task HandleIndex(RenderJob job,
        CancellationToken token = default)
    {
        if (!await job.ShouldRender()) return;

        var index = await JsonSerializer.DeserializeAsync(job.Index.OpenRead(), SerializerContext.Default.Index, token);
        if (index is null) return;

        var render = new SpineRenderer(job.Renderer, job.Fps, index.Size,
            new SpineRenderer.RendererSettings { Quirk = index.RenderQuirk });
        try
        {
            foreach (var skeleton in index.Spines)
                render.AddSkeleton(skeleton, job.IndexDir);
            foreach (var follower in index.BoneFollowers)
                render.AddBoneFollower(follower);
        }
        catch (Exception e)
        {
            // old 3.8 data will fail to parse.
            await Console.Error.WriteLineAsync($"Skipping {index.Name}: failed to load skeletons: {e.Message}");
            return;
        }

        var ok = true;
        foreach (var animation in render.Animations)
        {
            var outputPath = Path.Combine(job.OutputPath, $"{animation}.{job.Codec.Extension}");
            try
            {
                var sw = Stopwatch.StartNew();
                await render.GenerateVideo(animation, outputPath, job.Codec, token);
                Console.WriteLine($"Rendered {index.Name} - {animation} in {sw.ElapsedMilliseconds / 1000}s");
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync($"Failed to render {index.Name} - {animation}: {e}");
                ok = false;
            }
        }

        if (job.LinkDefaultAnimation && render.Animations.Count > 0)
        {
            var defaultAnimation = index.DefaultAnimation
                                   ?? render.Animations.Find(e => e == "idle")
                                   ?? render.Animations.First();
            var defaultPath = Path.Combine(job.OutputPath, $"_default.{job.Codec.Extension}");

            if (File.Exists(defaultPath))
                File.Delete(defaultPath);
            File.CreateSymbolicLink(defaultPath, $"{defaultAnimation}.{job.Codec.Extension}");
        }

        if (ok && render.Animations.Count > 0) await job.WriteSha256();
    }

    private struct RenderJob(
        FileInfo index,
        IRenderCodec codec,
        IFrameRenderer renderer)
    {
        public readonly FileInfo Index = index;
        public float Fps = 30;
        public readonly IRenderCodec Codec = codec;
        public readonly IFrameRenderer Renderer = renderer;
        public bool Force = false;
        public bool LinkDefaultAnimation = false;

        public string IndexDir => Path.GetRelativePath(Environment.CurrentDirectory, Index.DirectoryName ?? "");
        public string OutputPath => Path.Combine(IndexDir, "render");

        private string ShaPath => Path.Join(OutputPath, $"{Codec.HashName}.sha256");

        private string? _sha256;
        private string Sha256 => _sha256 ??= ComputeSha256Hash();

        private string ComputeSha256Hash()
        {
            using var sha256 = SHA256.Create();
            using var fileStream = File.OpenRead(Index.FullName);
            var hashBytes = sha256.ComputeHash(fileStream);
            return Convert.ToHexStringLower(hashBytes);
        }

        public async Task<bool> ShouldRender()
        {
            if (!File.Exists(ShaPath) || Force) return true;

            var oldSha256 = await File.ReadAllTextAsync(ShaPath);
            return Sha256 != oldSha256;
        }

        public async Task WriteSha256()
        {
            await File.WriteAllTextAsync(ShaPath, Sha256);
        }
    }

    private enum CodecOption
    {
        VP9,
        H264,
        H264NV,
        Mov
    }
}