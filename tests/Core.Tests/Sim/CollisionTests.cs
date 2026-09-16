using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class CollisionTests
{
    private static Vec3Fix P(decimal x, decimal y, decimal z) =>
        new(Fix16.FromDecimal(x), Fix16.FromDecimal(y), Fix16.FromDecimal(z));

    [Fact]
    public void TwoSpheresOverlapWhenDistanceLessThanRadiusSum()
    {
        var a = new HitCapsule(P(0, 0, 0), P(0, 0, 0), Fix16.FromDecimal(1m));
        var b = new HitCapsule(P(1.5m, 0, 0), P(1.5m, 0, 0), Fix16.FromDecimal(1m));

        Assert.True(Collision.Overlaps(a, b));
    }

    [Fact]
    public void TwoSpheresTouchingExactlyAreOverlapping()
    {
        var a = new HitCapsule(P(0, 0, 0), P(0, 0, 0), Fix16.FromDecimal(1m));
        var b = new HitCapsule(P(2m, 0, 0), P(2m, 0, 0), Fix16.FromDecimal(1m));

        Assert.True(Collision.Overlaps(a, b));
    }

    [Fact]
    public void TwoSpheresJustBeyondRadiusSumDoNotOverlap()
    {
        var a = new HitCapsule(P(0, 0, 0), P(0, 0, 0), Fix16.FromDecimal(1m));
        var b = new HitCapsule(P(2m + 1m / 65536m, 0, 0), P(2m + 1m / 65536m, 0, 0), Fix16.FromDecimal(1m));

        Assert.False(Collision.Overlaps(a, b));
    }

    [Fact]
    public void ParallelSegmentsSideBySideOverlapWhenClose()
    {
        var a = new HitCapsule(P(0, 0, 0), P(0, 2m, 0), Fix16.FromDecimal(0.5m));
        var b = new HitCapsule(P(0.8m, 0, 0), P(0.8m, 2m, 0), Fix16.FromDecimal(0.5m));

        Assert.True(Collision.Overlaps(a, b));
    }

    [Fact]
    public void ParallelSegmentsSideBySideDoNotOverlapWhenFar()
    {
        var a = new HitCapsule(P(0, 0, 0), P(0, 2m, 0), Fix16.FromDecimal(0.5m));
        var b = new HitCapsule(P(1.5m, 0, 0), P(1.5m, 2m, 0), Fix16.FromDecimal(0.5m));

        Assert.False(Collision.Overlaps(a, b));
    }

    [Fact]
    public void PerpendicularSegmentsCrossingOverlap()
    {
        var a = new HitCapsule(P(-1m, 0, 0), P(1m, 0, 0), Fix16.FromDecimal(0.2m));
        var b = new HitCapsule(P(0, -1m, 0), P(0, 1m, 0), Fix16.FromDecimal(0.2m));

        Assert.True(Collision.Overlaps(a, b));
    }

    [Fact]
    public void EndpointToEndpointDistanceIsUsedWhenClosestPointsAreEndpoints()
    {
        // aは(0,0,0)-(1,0,0)、bは(3,0,0)-(4,0,0)。最も近い点はaの(1,0,0)とbの(3,0,0)で距離2。
        var a = new HitCapsule(P(0, 0, 0), P(1m, 0, 0), Fix16.FromDecimal(0.5m));
        var b = new HitCapsule(P(3m, 0, 0), P(4m, 0, 0), Fix16.FromDecimal(0.5m));

        Assert.False(Collision.Overlaps(a, b));
    }

    [Fact]
    public void OverlapsIsSymmetric()
    {
        var a = new HitCapsule(P(0, 0, 0), P(1m, 0.3m, 0), Fix16.FromDecimal(0.4m));
        var b = new HitCapsule(P(0.5m, -0.5m, 0.2m), P(0.9m, 1.2m, -0.3m), Fix16.FromDecimal(0.3m));

        Assert.Equal(Collision.Overlaps(a, b), Collision.Overlaps(b, a));
    }

    [Fact]
    public void DoesNotOverflowNearThePositionLimit()
    {
        // 座標の絶対値の上限1024m付近で判定しても例外を投げない（C-02の境界）。
        var a = new HitCapsule(P(-1020m, 500m, -300m), P(-1017m, 502m, -299m), Fix16.FromDecimal(1m));
        var b = new HitCapsule(P(1020m, -500m, 300m), P(1017m, -502m, 299m), Fix16.FromDecimal(1m));

        var result = Collision.Overlaps(a, b);

        Assert.False(result);
    }

    [Fact]
    public void DoesNotOverflowWhenBothCapsulesAreNearTheSamePositionLimit()
    {
        var a = new HitCapsule(P(1020m, 1020m, 1020m), P(1023m, 1021m, 1019m), Fix16.FromDecimal(2m));
        var b = new HitCapsule(P(1021m, 1022m, 1018m), P(1024m, 1023m, 1017m), Fix16.FromDecimal(2m));

        var result = Collision.Overlaps(a, b);

        Assert.True(result);
    }
}
