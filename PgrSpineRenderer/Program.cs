using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;

namespace PgrSpineRenderer;

internal static class Program
{
    private static readonly Codec LibVpxVp9 = FFMpeg.GetCodec("libvpx-vp9");
    
    private const float Fps = 30.0f;
    private static readonly Vector2 CanvasSize = new(1280, 720);

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

        var render = new Renderer(Fps, CanvasSize);
        foreach (var arg in args)
            render.AddSkeleton(arg);
        
        var baseName = Path.GetFileNameWithoutExtension(args[0]);
        
        foreach (var animation in render.Animations)
        {
            await render.GenerateVideo(animation, $"../output/{baseName}_{animation}.mp4", VideoCodec.LibX264);
        }
        
        Console.WriteLine("All animations rendered successfully");
    }

}