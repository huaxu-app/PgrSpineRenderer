using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;

namespace PgrSpineRenderer.CodecHelper;

public class Mov : IRenderCodec
{
    public string HashName => "mov";
    public string Extension => "mov";

    public void Apply(FFMpegArgumentOptions options, Vector2 size)
    {
        options
            .WithVideoCodec("qtrle")
            .DisableChannel(Channel.Audio);
    }
}