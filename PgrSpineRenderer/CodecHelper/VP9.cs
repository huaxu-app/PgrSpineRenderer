using System.IO.Pipelines;
using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;

namespace PgrSpineRenderer.CodecHelper;

public class VP9 : IRenderCodec
{
    public string HashName => "vp9";
    public string Extension => "vp9.webm";

    public void Apply(FFMpegArgumentOptions options, Vector2 size)
    {
        // based on
        // https://developers.google.com/media/vp9/settings/vod
        var bitrate = 1800;
        var minrate = 900;
        var maxrate = 2610;
        
        if (size.X < 1280 || size.Y < 720)
        {
            bitrate = 1024;
            minrate = 512;
            maxrate = 1485;
        }
        
        options
            .WithVideoCodec("vp9")
            .ForcePixelFormat("yuva420p")
            .WithVideoBitrate(bitrate)
            .WithCustomArgument($"-minrate {minrate}k -maxrate {maxrate}k")
            .WithConstantRateFactor(31)
            .WithCustomArgument("-tile-columns 2 -threads 4")
            .ForceFormat(VideoType.WebM)
            .WithVideoFilters(f =>
                f.Scale((int)size.X, (int)size.Y)
            )
            .DisableChannel(Channel.Audio);
    }
}