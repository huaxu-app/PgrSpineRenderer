using System.Diagnostics.Metrics;

namespace PgrSpineRenderer;

internal static class Metrics
{
    private static readonly Meter Meter = new("PgrSpineRenderer");
    public static readonly Histogram<long> FrameDrawTime = Meter.CreateHistogram<long>("frame_draw_time", "ms", "Time taken to draw a frame");
    public static readonly Histogram<long> FrameTotalTime = Meter.CreateHistogram<long>("frame_total_time", "ms", "Time taken to draw a frame");
    public static readonly Counter<int> FramesRendered = Meter.CreateCounter<int>("frames_rendered_total", "{frames}", "Number of frames rendered");
}