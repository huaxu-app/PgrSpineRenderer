using System.Numerics;
using Spine;

namespace PgrSpineRenderer;

public class SpineBoneOffsetTracker(Bone target)
{
    private Vector2 _lastPosition = new(target.WorldX, target.WorldY);

    public void Update()
    {
        _lastPosition = CurrentPosition;
    }

    private Vector2 CurrentPosition => new(target.WorldX, target.WorldY);
    public Vector2 Offset => CurrentPosition - _lastPosition;
}