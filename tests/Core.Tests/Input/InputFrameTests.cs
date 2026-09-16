using Godot3dFighter.Core.Input;
using Xunit;

namespace Godot3dFighter.Core.Tests.Input;

public sealed class InputFrameTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(InputFrame.Up)]
    [InlineData(InputFrame.Down)]
    [InlineData(InputFrame.Left)]
    [InlineData(InputFrame.Right)]
    [InlineData(InputFrame.Up | InputFrame.Left)]
    [InlineData(InputFrame.Up | InputFrame.Right)]
    [InlineData(InputFrame.Down | InputFrame.Left)]
    [InlineData(InputFrame.Down | InputFrame.Right)]
    [InlineData(InputFrame.Punch | InputFrame.Kick | InputFrame.Guard)]
    [InlineData(InputFrame.Up | InputFrame.Left | InputFrame.Punch | InputFrame.Kick | InputFrame.Guard)]
    public void ValidCombinationsAreNormalized(byte bits)
    {
        Assert.True(new InputFrame(bits).IsNormalized);
    }

    [Theory]
    [InlineData(InputFrame.Up | InputFrame.Down)]
    [InlineData(InputFrame.Left | InputFrame.Right)]
    [InlineData(InputFrame.Up | InputFrame.Down | InputFrame.Left | InputFrame.Right)]
    [InlineData(0x80)]
    [InlineData(0xFF)]
    public void InvalidCombinationsAreNotNormalized(byte bits)
    {
        Assert.False(new InputFrame(bits).IsNormalized);
    }

    [Theory]
    [InlineData(InputFrame.Up, InputFrame.Up, true)]
    [InlineData(InputFrame.Up, InputFrame.Down, false)]
    [InlineData((byte)(InputFrame.Punch | InputFrame.Guard), InputFrame.Punch, true)]
    [InlineData((byte)(InputFrame.Punch | InputFrame.Guard), InputFrame.Kick, false)]
    public void HasChecksSingleFlag(byte bits, byte flag, bool expected)
    {
        Assert.Equal(expected, new InputFrame(bits).Has(flag));
    }
}
