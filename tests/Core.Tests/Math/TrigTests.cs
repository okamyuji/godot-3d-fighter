using Godot3dFighter.Core.Math;
using Xunit;

namespace Godot3dFighter.Core.Tests.Math;

public sealed class TrigTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(16384, 65536)]
    [InlineData(32768, 0)]
    [InlineData(49152, -65536)]
    public void SinAtCardinalAngles(int angle, int expectedRaw)
    {
        Assert.Equal(expectedRaw, Trig.Sin(new Angle16((ushort)angle)).Raw);
    }

    [Theory]
    [InlineData(0, 65536)]
    [InlineData(16384, 0)]
    [InlineData(32768, -65536)]
    [InlineData(49152, 0)]
    public void CosAtCardinalAngles(int angle, int expectedRaw)
    {
        Assert.Equal(expectedRaw, Trig.Cos(new Angle16((ushort)angle)).Raw);
    }

    [Fact]
    public void SinSquaredPlusCosSquaredIsCloseToOneForEveryTableAngle()
    {
        for (var i = 0; i < 1024; i++)
        {
            var angle = new Angle16((ushort)(i * 64));
            var sin = Trig.Sin(angle);
            var cos = Trig.Cos(angle);

            var sumRaw = ((long)sin.Raw * sin.Raw + (long)cos.Raw * cos.Raw) >> 16;
            var diff = sumRaw - 65536;

            Assert.True(diff >= -3 && diff <= 3, $"index {i}: diff {diff}");
        }
    }
}
