using Godot3dFighter.Core.Math;
using Xunit;

namespace Godot3dFighter.Core.Tests.Math;

public sealed class Atan2Tests
{
    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 16384)]
    [InlineData(1, 1, 8192)]
    [InlineData(0, -1, 32768)]
    [InlineData(-1, 0, 49152)]
    public void ReturnsAngleForCardinalAndDiagonalPoints(int y, int x, int expected)
    {
        var result = Trig.Atan2(Fix16.FromInt(y), Fix16.FromInt(x));

        Assert.Equal((ushort)expected, result.Value);
    }

    [Fact]
    public void OriginReturnsZero()
    {
        var result = Trig.Atan2(Fix16.Zero, Fix16.Zero);

        Assert.Equal((ushort)0, result.Value);
    }

    [Theory]
    [InlineData(1, -1, 24576)]
    [InlineData(-1, -1, 40960)]
    [InlineData(-1, 1, 57344)]
    public void ReturnsAngleForOtherQuadrantDiagonals(int y, int x, int expected)
    {
        var result = Trig.Atan2(Fix16.FromInt(y), Fix16.FromInt(x));

        Assert.Equal((ushort)expected, result.Value);
    }
}
