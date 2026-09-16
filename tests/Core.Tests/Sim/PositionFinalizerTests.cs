using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class PositionFinalizerTests
{
    private static StageData Square(EdgeKind edge, decimal size = 5m) => new()
    {
        Name = "square",
        Shape = RingShape.Square,
        Size = Fix16.FromDecimal(size),
        Edges = [edge, edge, edge, edge],
        StartDistance = Fix16.FromDecimal(2m),
    };

    [Fact]
    public void PushesInsideWallThenClampsToPositionLimit()
    {
        var stage = Square(EdgeKind.Wall);
        var pos = new Vec3Fix(Fix16.FromInt(6), Fix16.Zero, Fix16.Zero);

        var result = PositionFinalizer.Finalize(pos, Fix16.FromDecimal(0.5m), stage);

        Assert.Equal(Fix16.FromDecimal(4.5m), result.X);
    }

    [Fact]
    public void ClampsToPositionLimitEvenOnRingOutStage()
    {
        var stage = Square(EdgeKind.RingOut, size: 5m);
        var pos = new Vec3Fix(Fix16.FromInt(2000), Fix16.Zero, Fix16.FromInt(-2000));

        var result = PositionFinalizer.Finalize(pos, Fix16.FromDecimal(0.3m), stage);

        Assert.Equal(Fix16.FromInt(Limits.PositionLimitMeters), result.X);
        Assert.Equal(Fix16.FromInt(-Limits.PositionLimitMeters), result.Z);
    }
}
