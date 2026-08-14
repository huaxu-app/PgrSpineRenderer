using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace PgrSpineRenderer.Tests;

[TestFixture]
[TestOf(typeof(AnimationSet))]
public class AnimationSetTest
{
    [Test]
    public void LayerSingleIdle()
    {
        var x = new AnimationSet();
        x.RegisterLayer(["idle"]);

        x.GetRenderSet().Should().Equal("idle");
        x.Resolve(["idle"], "idle").Should().Be("idle");
    }

    [Test]
    public void LayersAllIdle()
    {
        var x = new AnimationSet();
        x.RegisterLayer(["idle"]);
        x.RegisterLayer(["idle"]);

        x.GetRenderSet().Should().Equal("idle");
        x.Resolve(["idle"], "idle").Should().Be("idle");
    }

    [Test]
    public void LayersWithLipsync()
    {
        var x = new AnimationSet();
        List<string> l =
        [
            "A", "B", "C", "D", "E", "F", "G", "H", "X", "biyan", "biyan2", "biyanxiao", "chensi", "chensizhayan",
            "fennu", "fennuzhanyan", "idle", "idle2", "idlezhayan", "jingya", "jingyazhanyan", "weixiao",
            "weixiaozhayan", "zhoumei", "zhoumeizhayan"
        ];
        x.RegisterLayer(l);
        x.RegisterLayer(l);

        x.GetRenderSet().Should().Equal("biyan", "biyan2", "biyanxiao", "chensi", "chensizhayan", "fennu",
            "fennuzhanyan", "idle", "idle2", "idlezhayan", "jingya", "jingyazhanyan", "weixiao", "weixiaozhayan",
            "zhoumei", "zhoumeizhayan");
        x.Resolve(l, "biyan").Should().Be("biyan");
    }

    [Test]
    public void LayersWithFreeIdle()
    {
        var x = new AnimationSet();
        x.RegisterLayer(["idle"]);
        x.RegisterLayer(["alpha", "gamma"]);

        x.GetRenderSet().Should().Equal("alpha", "gamma");
        x.Resolve(["alpha", "gamma"], "alpha").Should().Be("alpha");
        x.Resolve(["idle"], "alpha").Should().Be("idle");
    }

    [Test]
    public void WeirdInconsistency()
    {
        var x = new AnimationSet();
        x.RegisterLayer(["idle"]);
        x.RegisterLayer(["idle", "alpha", "gamma"]);
        x.GetRenderSet().Should().Equal("idle", "alpha", "gamma");
    }

    [Test]
    public void StaticPoseOnOneLayerKeepsAnimation()
    {
        var x = new AnimationSet();
        x.RegisterLayer([("idle", 2f), ("alpha", 2f)]);
        x.RegisterLayer([("idle", 0f), ("alpha", 2f)]);

        x.GetRenderSet().Should().BeEquivalentTo("idle", "alpha");
    }

    [Test]
    public void EmptyOnEveryLayerDropsAnimation()
    {
        var x = new AnimationSet();
        x.RegisterLayer([("idle", 2f), ("Empty", 0f)]);
        x.RegisterLayer([("idle", 2f), ("Empty", 0f)]);

        x.GetRenderSet().Should().Equal("idle");
    }
}