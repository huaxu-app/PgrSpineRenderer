using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;

namespace PgrSpineRenderer.CodecHelper;

public class H264NV : IRenderCodec
{
    public string HashName => "h264";
    public string Extension => "mp4";

    public void Apply(FFMpegArgumentOptions options, Vector2 size)
    {
        var codec = "h264_nvenc";
        if (size.X > 4096 || size.Y > 4096)
        {
            Console.Error.WriteLine("Resolution is too high for NVENC, falling back to libx264");
            codec = "libx264";
        }
        
        options
            .WithVideoCodec(codec)
            .WithConstantRateFactor(23)
            .WithCustomArgument("-profile:v high -level 4.0")
            .ForcePixelFormat("yuv420p")
            .WithVideoFilters(f => f.Scale((int)size.X, (int)size.Y))
            .WithFastStart()
            .DisableChannel(Channel.Audio);
    }
}