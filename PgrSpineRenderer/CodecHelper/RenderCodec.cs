using System.Numerics;
using FFMpegCore;

namespace PgrSpineRenderer.CodecHelper;

public interface IRenderCodec
{
    string HashName { get; }
    string Extension { get; }
    
    void Apply(FFMpegArgumentOptions options, Vector2 size);
}