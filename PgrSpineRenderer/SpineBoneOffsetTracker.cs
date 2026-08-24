using System.Numerics;
using Spine;

namespace PgrSpineRenderer;

public class SpineBoneOffsetTracker(Bone source, Bone target)
{
    public Vector2 Offset => WorldPosition(source) - WorldPosition(target);

    public static SpineBoneOffsetTracker? Resolve(Skeleton sourceSkeleton, Skeleton targetSkeleton, string boneName)
    {
        var source = sourceSkeleton.FindBone(boneName);
        if (source is null) return null;

        for (var bone = source; bone is not null; bone = bone.Parent)
        {
            var match = targetSkeleton.FindBone(bone.Data.Name);
            if (match is not null) return new SpineBoneOffsetTracker(bone, match);
        }

        // Every skeleton has a root, so the walk normally terminates well before here.
        return new SpineBoneOffsetTracker(source, targetSkeleton.RootBone);
    }

    private static Vector2 WorldPosition(Bone bone)
    {
        return new Vector2(bone.AppliedPose.WorldX, bone.AppliedPose.WorldY);
    }
}