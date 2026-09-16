using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Xunit;

namespace Godot3dFighter.Core.Tests.Data;

public sealed class RingBoundsTests
{
    private static Vec3Fix P(decimal x, decimal z) => new(Fix16.FromDecimal(x), Fix16.Zero, Fix16.FromDecimal(z));

    private static StageData Circle(EdgeKind edge, decimal size = 5m) => new()
    {
        Name = "circle",
        Shape = RingShape.Circle,
        Size = Fix16.FromDecimal(size),
        Edges = [edge],
        StartDistance = Fix16.FromDecimal(2m),
    };

    private static StageData Square(EdgeKind edge, decimal size = 5m) => new()
    {
        Name = "square",
        Shape = RingShape.Square,
        Size = Fix16.FromDecimal(size),
        Edges = [edge, edge, edge, edge],
        StartDistance = Fix16.FromDecimal(2m),
    };

    private static StageData Octagon(EdgeKind edge, decimal size = 5m) => new()
    {
        Name = "octagon",
        Shape = RingShape.Octagon,
        Size = Fix16.FromDecimal(size),
        Edges = [edge, edge, edge, edge, edge, edge, edge, edge],
        StartDistance = Fix16.FromDecimal(2m),
    };

    [Fact]
    public void CircleFootOnBoundaryIsNotOut()
    {
        var stage = Circle(EdgeKind.RingOut);
        Assert.False(RingBounds.IsOut(P(5m, 0), stage));
    }

    [Fact]
    public void CircleFootJustBeyondBoundaryIsOut()
    {
        var stage = Circle(EdgeKind.RingOut);
        Assert.True(RingBounds.IsOut(P(5m + 1m / 65536m, 0), stage));
    }

    [Fact]
    public void CircleFootInsideIsNotOut()
    {
        var stage = Circle(EdgeKind.RingOut);
        Assert.False(RingBounds.IsOut(P(3m, 3m), stage));
    }

    [Fact]
    public void CircleWallEdgeNeverReportsOut()
    {
        var stage = Circle(EdgeKind.Wall);
        Assert.False(RingBounds.IsOut(P(100m, 100m), stage));
    }

    [Fact]
    public void SquareFootOnRightEdgeIsNotOut()
    {
        var stage = Square(EdgeKind.RingOut);
        Assert.False(RingBounds.IsOut(P(5m, 0), stage));
    }

    [Fact]
    public void SquareFootBeyondRightEdgeIsOut()
    {
        var stage = Square(EdgeKind.RingOut);
        Assert.True(RingBounds.IsOut(P(5m + 1m / 65536m, 0), stage));
    }

    [Fact]
    public void SquareFootBeyondLeftEdgeIsOut()
    {
        // 辺2（i=2、180度）の外向きはTrig.Direction(180度)=(-1,0,0)で-X側。
        var stage = Square(EdgeKind.RingOut);
        Assert.True(RingBounds.IsOut(P(-5.1m, 0), stage));
    }

    [Fact]
    public void SquareCornerIsNotOutWhenWithinBothEdges()
    {
        var stage = Square(EdgeKind.RingOut);
        Assert.False(RingBounds.IsOut(P(5m, 5m), stage));
    }

    [Fact]
    public void OctagonFootBeyondRightEdgeIsOut()
    {
        var stage = Octagon(EdgeKind.RingOut);
        Assert.True(RingBounds.IsOut(P(5m + 1m / 65536m, 0), stage));
    }

    [Fact]
    public void OctagonFootInsideIsNotOut()
    {
        var stage = Octagon(EdgeKind.RingOut);
        Assert.False(RingBounds.IsOut(P(3m, 3m), stage));
    }

    [Fact]
    public void PushInsideWallsDoesNotMoveWhenAlreadyInside()
    {
        var stage = Square(EdgeKind.Wall);
        var pos = P(1m, 1m);

        var result = RingBounds.PushInsideWalls(pos, Fix16.FromDecimal(0.3m), stage);

        Assert.Equal(pos, result);
    }

    [Fact]
    public void PushInsideWallsPushesBackOnCircle()
    {
        var stage = Circle(EdgeKind.Wall, 5m);
        var pos = P(6m, 0);

        var result = RingBounds.PushInsideWalls(pos, Fix16.FromDecimal(0.5m), stage);

        // limit = 5 - 0.5 = 4.5m
        Assert.Equal(Fix16.FromDecimal(4.5m), result.X);
        Assert.Equal(Fix16.Zero, result.Z);
    }

    [Fact]
    public void PushInsideWallsPushesBackOnSquareEdge()
    {
        var stage = Square(EdgeKind.Wall, 5m);
        var pos = P(6m, 1m);

        var result = RingBounds.PushInsideWalls(pos, Fix16.FromDecimal(0.5m), stage);

        Assert.Equal(Fix16.FromDecimal(4.5m), result.X);
        Assert.Equal(Fix16.FromDecimal(1m), result.Z);
    }

    [Fact]
    public void PushInsideWallsDoesNothingWhenEdgeIsRingOut()
    {
        var stage = Square(EdgeKind.RingOut);
        var pos = P(6m, 0);

        var result = RingBounds.PushInsideWalls(pos, Fix16.FromDecimal(0.5m), stage);

        Assert.Equal(pos, result);
    }

    [Fact]
    public void PushInsideWallsHandlesCornerBeyondTwoEdges()
    {
        var stage = Square(EdgeKind.Wall, 5m);
        var pos = P(6m, 6m);

        var result = RingBounds.PushInsideWalls(pos, Fix16.FromDecimal(0.5m), stage);

        Assert.False(RingBounds.IsOut(result, Square(EdgeKind.RingOut, 4.5m)));
    }
}
