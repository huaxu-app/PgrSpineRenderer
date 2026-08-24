using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;

namespace PgrSpineRenderer.CodecHelper;

public class H264NV : IRenderCodec
{
    private const int Crf = 23;

    // nvenc needs roughly 60% more bits than x264 for the same SSIM, so its scale does not line up
    private const int NvencQuality = 29;

    private const int NvencMaxDimension = 4096;

    public string HashName => "h264";
    public string Extension => "mp4";

    public void Apply(FFMpegArgumentOptions options, Vector2 size)
    {
        var nvenc = size.X <= NvencMaxDimension && size.Y <= NvencMaxDimension;
        if (!nvenc)
            Console.Error.WriteLine("Resolution is too high for NVENC, falling back to libx264");

        options
            .WithVideoCodec(nvenc ? "h264_nvenc" : "libx264")
            .WithCustomArgument("-profile:v high")
            .ForcePixelFormat("yuv420p")
            .WithFastStart()
            .DisableChannel(Channel.Audio);

        // nvenc has no CRF, given one it silently falls back to its default bitrate
        if (nvenc)
            options.WithCustomArgument($"-rc vbr -cq {NvencQuality} -b:v 0 -preset p6 -tune hq");
        else
            options.WithConstantRateFactor(Crf);
    }
}