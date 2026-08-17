using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Spine;

namespace PgrSpineRenderer.Tests;

[TestFixture]
[TestOf(typeof(LayeredAnimations))]
public class LayeredAnimationsTest
{
    private static SkeletonData Data(params (string Name, float Duration)[] animations)
    {
        var data = new SkeletonData();
        foreach (var (name, duration) in animations)
            data.Animations.Add(new Animation(name) { Duration = duration });
        return data;
    }

    [Test]
    public void PlainSkeletonPassesThrough()
    {
        var data = Data(("idle", 2), ("weixiao", 1));

        LayeredAnimations.Flatten(data).Should().Equal(("idle", 2f), ("weixiao", 1f));
        LayeredAnimations.Tracks(data, "idle").Select(a => a.Name).Should().Equal("idle");
    }

    [Test]
    public void FoldersAreNotLayers()
    {
        // uinewbietasksailika organises its animations in folders, but they're whole animations
        var data = Data(("idle", 2), ("huaixiao/huaixiao_1", 1), ("shengqi/shengqi_1", 1));

        LayeredAnimations.Flatten(data).Select(a => a.Name)
            .Should().Equal("idle", "huaixiao/huaixiao_1", "shengqi/shengqi_1");
        LayeredAnimations.Tracks(data, "huaixiao/huaixiao_1").Select(a => a.Name)
            .Should().Equal("huaixiao/huaixiao_1");
    }

    [Test]
    public void LayeredSkeletonFlattensToBody()
    {
        var data = Data(
            ("By/idle", 2), ("Fa/idle", 2), ("Mo/idle", 0),
            ("By/weixiao", 1), ("Fa/weixiao", 1), ("Mo/weixiao", 0),
            ("Mo/A", 0), ("Mo/X", 0));

        LayeredAnimations.Flatten(data).Should().Equal(("idle", 2f), ("weixiao", 1f));
    }

    [Test]
    public void LayeredSkeletonPlaysBodyFaceAndMouth()
    {
        var data = Data(("By/idle", 2), ("Fa/idle", 2), ("Mo/idle", 0));

        LayeredAnimations.Tracks(data, "idle").Select(a => a.Name)
            .Should().Equal("By/idle", "Fa/idle", "Mo/idle");
    }

    [Test]
    public void MissingPartsAreSkipped()
    {
        var data = Data(("By/idle", 2), ("Mo/idle", 0));

        LayeredAnimations.Tracks(data, "idle").Select(a => a.Name)
            .Should().Equal("By/idle", "Mo/idle");
    }
}
