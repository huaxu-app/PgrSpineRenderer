using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Channels;
using OpenTK.Windowing.Desktop;
using ShellProgressBar;
using SkiaSharp;
using Spine;

namespace PgrSpineRenderer.Rendering;

public class Renderer: IFrameRenderer, IDisposable
{
    private readonly List<Thread> _workerThreads = new();
    private readonly Channel<WorkItem> _workChannel = Channel.CreateUnbounded<WorkItem>();
    private readonly AutoResetEvent _workAvailable = new(false);
    private volatile bool _isRunning = true;
    private readonly object _syncLock = new();

    public Renderer(IEnumerable<IGLFWGraphicsContext> graphicsContexts)
    {
        foreach (var graphicsContext in graphicsContexts)
        {
            var t = new Thread(() => ThreadStart(graphicsContext)) { IsBackground = true };
            t.Start();
            _workerThreads.Add(t);
        }
    }

    private void ThreadStart(IGLFWGraphicsContext graphicsContext)
    {
        graphicsContext.MakeCurrent();
        var context = GRContext.CreateGl();
        Dictionary<Vector2, SKSurface> surfaces = new();
            
        while (_isRunning)
        {
            while (_workChannel.Reader.TryRead(out var workItem))
            {
                try
                {
                    if (!surfaces.TryGetValue(workItem.CanvasSize, out var surface))
                    {
                        surface = SKSurface.Create(context, false, new SKImageInfo((int)workItem.CanvasSize.X, (int)workItem.CanvasSize.Y));
                        surfaces[workItem.CanvasSize] = surface;
                    }

                    surface.Canvas.Clear();
                    var result = SpineDrawer.DrawJob(surface, workItem.Skeletons);
                        
                    workItem.CompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    workItem.CompletionSource.SetException(ex);
                }
            }
        }
    }
    
    public async Task<SkiaFrame> Render(Vector2 canvasSize, Skeleton[] skeletons, CancellationToken token = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("Worker has been disposed");
        
        var workItem = new WorkItem(canvasSize, skeletons);
        await _workChannel.Writer.WriteAsync(workItem, token);
        _workAvailable.Set();

        var sw = Stopwatch.StartNew();
        var i = await workItem.CompletionSource.Task;
        Metrics.FrameTotalTime.Record(sw.ElapsedMilliseconds);
        return i;
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (!_isRunning) return;
            _isRunning = false;
            _workAvailable.Set(); // Wake up worker thread to exit
            foreach (var workerThread in _workerThreads)
            {
                workerThread.Join();
            }
            
            _workAvailable.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    private class WorkItem(Vector2 canvasSize, Skeleton[] skeletons)
    {
        public Vector2 CanvasSize { get; } = canvasSize;
        public Skeleton[] Skeletons { get; } = skeletons;
        public TaskCompletionSource<SkiaFrame> CompletionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    
}