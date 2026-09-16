using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class BodyPushTests
{
    private static Vec3Fix P(decimal x, decimal z) => new(Fix16.FromDecimal(x), Fix16.Zero, Fix16.FromDecimal(z));

    private static StageData WideStage() => new()
    {
        Name = "wide",
        Shape = RingShape.Square,
        Size = Fix16.FromDecimal(100m),
        Edges = [EdgeKind.Wall, EdgeKind.Wall, EdgeKind.Wall, EdgeKind.Wall],
        StartDistance = Fix16.FromDecimal(2m),
    };

    private static long DistSq(Vec3Fix a, Vec3Fix b) => a.HorizontalDistanceSquared(b);

    [Fact]
    public void NoOverlapLeavesPositionsUnchanged()
    {
        var a = P(0, 0);
        var b = P(2, 0);
        var radius = Fix16.FromDecimal(0.3m);

        var (rA, rB) = BodyPush.Resolve(a, b, a, b, radius, radius, new Angle16(0), true, WideStage(), eitherThrowing: false);

        Assert.Equal(a, rA);
        Assert.Equal(b, rB);
    }

    [Fact]
    public void OverlapPushesBothApartByHalfTheShortfall()
    {
        var a = P(0, 0);
        var b = P(0.4m, 0);
        var radius = Fix16.FromDecimal(0.3m);

        var (rA, rB) = BodyPush.Resolve(a, b, a, b, radius, radius, new Angle16(0), true, WideStage(), eitherThrowing: false);

        var radiusSumSq = (0.6m * 0.6m);
        var resultDistSq = DistSq(rA, rB);
        Assert.True(resultDistSq >= radiusSumSq * 65536m * 65536m - 4);
    }

    [Fact]
    public void ThrowingBypassesPushButStillFinalizes()
    {
        var a = P(0, 0);
        var b = P(0.1m, 0);
        var radius = Fix16.FromDecimal(0.5m);

        var (rA, rB) = BodyPush.Resolve(a, b, a, b, radius, radius, new Angle16(0), true, WideStage(), eitherThrowing: true);

        Assert.Equal(a, rA);
        Assert.Equal(b, rB);
    }

    [Fact]
    public void ResultingDistanceIsNeverBelowRadiusSumMinusOneRaw()
    {
        var radius = Fix16.FromDecimal(0.5m);
        var radiusSum = radius + radius;
        var radiusSumSq = radiusSum.RawSquared();

        for (var dx = -3; dx <= 3; dx++)
        {
            var a = P(0, 0);
            var b = new Vec3Fix(new Fix16(dx * 10000), Fix16.Zero, Fix16.Zero);

            var (rA, rB) = BodyPush.Resolve(a, b, a, b, radius, radius, new Angle16(0), true, WideStage(), eitherThrowing: false);

            var resultDistSq = DistSq(rA, rB);
            var minAllowed = (long)(radiusSum.Raw - 1) * (radiusSum.Raw - 1);
            Assert.True(resultDistSq >= minAllowed, $"dx={dx}: distSq={resultDistSq} min={minAllowed} radiusSumSq={radiusSumSq}");
        }
    }
}
