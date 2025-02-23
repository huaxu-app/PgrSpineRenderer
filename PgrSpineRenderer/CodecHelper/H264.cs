using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;

namespace PgrSpineRenderer.CodecHelper;

public class H264 : IRenderCodec
{
    public string HashName => "h264";
    public string Extension => "mp4";
    
    public void Apply(FFMpegArgumentOptions options, Vector2 size)
    {
        options
            .WithVideoCodec("libx264")
            .WithConstantRateFactor(23)
            .WithCustomArgument("-profile:v high -level 4.0")
            .ForcePixelFormat("yuv420p")
            .WithVideoFilters(f => f.Scale((int)size.X, (int)size.Y))
            .WithFastStart()
            .DisableChannel(Channel.Audio);
    }
}