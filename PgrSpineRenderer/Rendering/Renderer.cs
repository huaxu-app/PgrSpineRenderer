using System.Diagnostics;
using System.Numerics;
using System.Threading.Channels;
using OpenTK.Windowing.Desktop;
using SkiaSharp;
using Spine;

namespace PgrSpineRenderer.Rendering;

public class Renderer : IFrameRenderer, IDisposable
{
    private readonly object _syncLock = new();
    private readonly AutoResetEvent _workAvailable = new(false);
    private readonly Channel<WorkItem> _workChannel = Channel.CreateUnbounded<WorkItem>();
    private readonly List<Thread> _workerThreads = new();
    private volatile bool _isRunning = true;

    public Renderer(IEnumerable<IGLFWGraphicsContext> graphicsContexts)
    {
        foreach (var graphicsContext in graphicsContexts)
        {
            var t = new Thread(() => ThreadStart(graphicsContext)) { IsBackground = true };
            t.Start();
            _workerThreads.Add(t);
        }
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (!_isRunning) return;
            _isRunning = false;
            _workAvailable.Set(); // Wake up worker thread to exit
            foreach (var workerThread in _workerThreads) workerThread.Join();

            _workAvailable.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public async Task<SkiaFrame> Render(Vector2 canvasSize, Vector2 outputSize, Skeleton[] skeletons,
        CancellationToken token = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("Worker has been disposed");

        var workItem = new WorkItem(canvasSize, outputSize, skeletons);
        await _workChannel.Writer.WriteAsync(workItem, token);
        _workAvailable.Set();

        var sw = Stopwatch.StartNew();
        var i = await workItem.CompletionSource.Task;
        Metrics.FrameTotalTime.Record(sw.ElapsedMilliseconds);
        return i;
    }

    private void ThreadStart(IGLFWGraphicsContext graphicsContext)
    {
        graphicsContext.MakeCurrent();
        var context = GRContext.CreateGl();
        if (context is null) throw new ApplicationException("Context is null");
        Dictionary<(Vector2 canvas, Vector2 output), RenderTarget> targets = new();

        while (_isRunning)
        while (_workChannel.Reader.TryRead(out var workItem))
            try
            {
                var key = (workItem.CanvasSize, workItem.OutputSize);
                if (!targets.TryGetValue(key, out var target))
                {
                    target = new RenderTarget(context, workItem.CanvasSize, workItem.OutputSize);
                    targets[key] = target;
                }

                workItem.CompletionSource.SetResult(target.Render(workItem.Skeletons));
            }
            catch (Exception ex)
            {
                workItem.CompletionSource.SetException(ex);
            }

        foreach (var target in targets.Values) target.Dispose();
    }

    private class WorkItem(Vector2 canvasSize, Vector2 outputSize, Skeleton[] skeletons)
    {
        public Vector2 CanvasSize { get; } = canvasSize;
        public Vector2 OutputSize { get; } = outputSize;
        public Skeleton[] Skeletons { get; } = skeletons;

        public TaskCompletionSource<SkiaFrame> CompletionSource { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}