using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.State;

public sealed class PlayerStateTests
{
    private static PlayerState Sample() => new()
    {
        Position = new Vec3Fix(Fix16.FromInt(1), Fix16.Zero, Fix16.FromInt(2)),
        Health = 100,
        State = StateKind.Idle,
        CurrentMove = Limits.NoMove,
        Slot = 0,
    };

    [Fact]
    public void EqualsIsTrueForSameContent()
    {
        var a = Sample();
        var b = Sample();

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void EqualsIsFalseWhenHealthDiffers()
    {
        var a = Sample();
        var b = Sample() with { Health = 99 };

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void EqualsIsFalseWhenInputBufferDiffers()
    {
        var a = Sample();
        var b = Sample() with { Inputs = default(InputBuffer).Push(new InputFrame(InputFrame.Punch)) };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void EqualsIsFalseWhenComparedToNonPlayerState()
    {
        Assert.False(Sample().Equals("not a player state"));
    }
}
