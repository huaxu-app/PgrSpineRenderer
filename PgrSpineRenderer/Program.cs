using System.CommandLine;
using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;

namespace PgrSpineRenderer;

internal static class Program
{
    private static readonly Dictionary<CodecOption, (Codec codec, string extension)> Codecs = new()
    {
        {CodecOption.VP9, (FFMpeg.GetCodec("libvpx-vp9"), "webm")},
        {CodecOption.H264, (VideoCodec.LibX264, "mp4")},
    };
    
    private const float DefaultFps = 30.0f;
    private static readonly Vector2 DefaultCanvasSize = new(1920, 1080);

    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("""
                                          Renders the animations of the specified skeletons to webm videos.
                                          Skeletons should be the path towards the .json and .atlas.txt files, without extension.
                                          For example: 'luciyasairen' for 'luciyasairen.json' and 'luciyasairen.atlas.txt'

                                          When multiple skeletons are specified, they will be rendered on top of each other.
                                          The first skeleton will be the bottom layer, and the last one will be the top layer.
                                          You can add as many skeletons as you want.
                                          
                                          The output videos will be named after the animations contained within,
                                          for example `idle.webm`.
                                          """);
        var fpsOption = new Option<float>("--fps", () => DefaultFps, "The frames per second of the output video");
        rootCommand.AddOption(fpsOption);

        var widthOption = new Option<int>("--width", () => (int)DefaultCanvasSize.X, "The width of the output video");
        widthOption.AddAlias("-w");
        rootCommand.AddOption(widthOption);
        var heightOption = new Option<int>("--height", () => (int)DefaultCanvasSize.Y, "The height of the output video");
        heightOption.AddAlias("-h");
        rootCommand.AddOption(heightOption);
        
        var outputDirOption = new Option<string>("--output-dir", () => "", "The directory where the output videos will be saved. Defaults to the current directory");
        outputDirOption.AddAlias("-o");
        rootCommand.AddOption(outputDirOption);
        
        var codecOption = new Option<CodecOption>("--codec", () => CodecOption.VP9, "The codec to use for the output videos. Defaults to 'vp9'");
        codecOption.AddAlias("-c");
        rootCommand.AddOption(codecOption);
        
        var skeletonArguments = new Argument<string[]>("skeletons", (result) =>
        {
            if (result.Tokens.Count == 0)
            {
                result.ErrorMessage = "You must specify at least one skeleton to render";
                return [];
            }

            foreach (var token in result.Tokens)
            {
                if (!token.Value.StartsWith('-')) continue; 
                // Option
                result.ErrorMessage = $"Invalid option: {token.Value}";
                return [];
            }

            return result.Tokens.Select(t => t.Value).ToArray();
        }, description: "The skeletons to render")
        {
            Arity = ArgumentArity.OneOrMore
        };
        rootCommand.AddArgument(skeletonArguments);
        rootCommand.SetHandler(Handler, skeletonArguments, fpsOption, widthOption, heightOption, outputDirOption, codecOption);
        
        return await rootCommand.InvokeAsync(args);
    }
    
    private static async Task Handler(string[] skeletons, float fps, int width, int height, string outputDir, CodecOption codecOption)
    {
        var render = new Renderer(fps, new Vector2(width, height));
        foreach (var skeleton in skeletons)
            render.AddSkeleton(skeleton);
        
        var baseName = Path.GetFileNameWithoutExtension(skeletons[0]);
        var (codec, extension) = Codecs[codecOption];
        
        foreach (var animation in render.Animations)
        {
            var outputPath = Path.Combine(outputDir, $"{baseName}_{animation}.{extension}");
            await render.GenerateVideo(animation, outputPath, codec);
        }
        
        Console.WriteLine("All animations rendered successfully");
    }

    private enum CodecOption
    {
        VP9,
        H264
    }
    
}