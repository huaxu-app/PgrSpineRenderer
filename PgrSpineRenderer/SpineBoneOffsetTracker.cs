using System.Numerics;
using Spine;

namespace PgrSpineRenderer;

public class SpineBoneOffsetTracker(Bone target)
{
    private Vector2 _lastPosition = new(target.AppliedPose.WorldX, target.AppliedPose.WorldY);

    private Vector2 CurrentPosition => new(target.AppliedPose.WorldX, target.AppliedPose.WorldY);
    public Vector2 Offset => CurrentPosition - _lastPosition;

    public void Update()
    {
        _lastPosition = CurrentPosition;
    }
}