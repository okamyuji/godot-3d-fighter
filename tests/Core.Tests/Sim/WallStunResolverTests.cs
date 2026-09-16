using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class WallStunResolverTests
{
    private static StageData WallSquare(decimal size = 5m) => new()
    {
        Name = "arena",
        Shape = RingShape.Square,
        Size = Fix16.FromDecimal(size),
        Edges = [EdgeKind.Wall, EdgeKind.Wall, EdgeKind.Wall, EdgeKind.Wall],
        StartDistance = Fix16.FromDecimal(2m),
    };

    private static Vec3Fix P(decimal x) => new(Fix16.FromDecimal(x), Fix16.Zero, Fix16.Zero);

    [Fact]
    public void BecomesWallStunWhenPushedBeyondTheWall()
    {
        var result = WallStunResolver.Resolve(P(6), Fix16.FromDecimal(0.5m), WallSquare(), PlayerFlags.None);

        Assert.True(result.BecameWallStun);
        Assert.True(result.NewFlags.HasFlag(PlayerFlags.WallHitTaken));
    }

    [Fact]
    public void DoesNotBecomeWallStunWhenWellInsideTheStage()
    {
        var result = WallStunResolver.Resolve(P(1), Fix16.FromDecimal(0.5m), WallSquare(), PlayerFlags.None);

        Assert.False(result.BecameWallStun);
    }

    [Fact]
    public void DoesNotFireTwiceInTheSameCombo()
    {
        var first = WallStunResolver.Resolve(P(6), Fix16.FromDecimal(0.5m), WallSquare(), PlayerFlags.None);
        var second = WallStunResolver.Resolve(P(6), Fix16.FromDecimal(0.5m), WallSquare(), first.NewFlags);

        Assert.True(first.BecameWallStun);
        Assert.False(second.BecameWallStun);
    }

    [Fact]
    public void RingOutEdgeNeverBecomesWallStun()
    {
        var stage = new StageData
        {
            Name = "default",
            Shape = RingShape.Square,
            Size = Fix16.FromDecimal(5m),
            Edges = [EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut],
            StartDistance = Fix16.FromDecimal(2m),
        };

        var result = WallStunResolver.Resolve(P(6), Fix16.FromDecimal(0.5m), stage, PlayerFlags.None);

        Assert.False(result.BecameWallStun);
    }
}
