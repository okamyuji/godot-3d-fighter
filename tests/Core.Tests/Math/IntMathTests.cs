using System;
using Godot3dFighter.Core.Math;
using Xunit;

namespace Godot3dFighter.Core.Tests.Math;

public sealed class IntMathTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(15, 3)]
    [InlineData(16, 4)]
    [InlineData(17, 4)]
    [InlineData(9_999_999_999L, 99999)]
    [InlineData(10_000_000_000L, 100000)]
    public void SqrtFloorsToIntegerRoot(long value, long expected)
    {
        Assert.Equal(expected, IntMath.Sqrt(value));
    }

    [Fact]
    public void SqrtOfLongMaxValueDoesNotThrow()
    {
        var result = IntMath.Sqrt(long.MaxValue);
        Assert.True(result * result <= long.MaxValue);
        Assert.True((result + 1) * (result + 1) < 0 || (result + 1) * (result + 1) > long.MaxValue);
    }

    [Fact]
    public void SqrtThrowsForNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IntMath.Sqrt(-1));
    }
}
