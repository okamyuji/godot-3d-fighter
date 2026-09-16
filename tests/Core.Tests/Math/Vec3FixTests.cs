using Godot3dFighter.Core.Math;
using Xunit;

namespace Godot3dFighter.Core.Tests.Math;

public sealed class Vec3FixTests
{
    [Fact]
    public void AdditionAddsEachComponent()
    {
        var a = new Vec3Fix(Fix16.FromInt(1), Fix16.FromInt(2), Fix16.FromInt(3));
        var b = new Vec3Fix(Fix16.FromInt(4), Fix16.FromInt(5), Fix16.FromInt(6));

        var result = a + b;

        Assert.Equal(new Vec3Fix(Fix16.FromInt(5), Fix16.FromInt(7), Fix16.FromInt(9)), result);
    }

    [Fact]
    public void SubtractionSubtractsEachComponent()
    {
        var a = new Vec3Fix(Fix16.FromInt(5), Fix16.FromInt(7), Fix16.FromInt(9));
        var b = new Vec3Fix(Fix16.FromInt(1), Fix16.FromInt(2), Fix16.FromInt(3));

        var result = a - b;

        Assert.Equal(new Vec3Fix(Fix16.FromInt(4), Fix16.FromInt(5), Fix16.FromInt(6)), result);
    }

    [Fact]
    public void ScalarMultiplicationScalesEachComponent()
    {
        var v = new Vec3Fix(Fix16.FromInt(1), Fix16.FromInt(2), Fix16.FromInt(3));

        var result = v * Fix16.FromInt(2);

        Assert.Equal(new Vec3Fix(Fix16.FromInt(2), Fix16.FromInt(4), Fix16.FromInt(6)), result);
    }

    [Fact]
    public void ClampAppliesToEachComponentIndependently()
    {
        var v = new Vec3Fix(Fix16.FromInt(-5), Fix16.FromInt(5), Fix16.FromInt(15));

        var result = v.Clamp(Fix16.FromInt(0), Fix16.FromInt(10));

        Assert.Equal(new Vec3Fix(Fix16.FromInt(0), Fix16.FromInt(5), Fix16.FromInt(10)), result);
    }

    [Fact]
    public void HorizontalDistanceSquaredIgnoresY()
    {
        var a = new Vec3Fix(Fix16.FromInt(0), Fix16.FromInt(100), Fix16.FromInt(0));
        var b = new Vec3Fix(Fix16.FromInt(3), Fix16.FromInt(-999), Fix16.FromInt(4));

        var result = a.HorizontalDistanceSquared(b);

        // 3-4-5の直角三角形。3²+4²=25 (Fix16なので65536²倍)
        Assert.Equal(25L * 65536L * 65536L, result);
    }

    [Fact]
    public void HorizontalDistanceSquaredIsZeroForSamePoint()
    {
        var a = new Vec3Fix(Fix16.FromInt(7), Fix16.FromInt(0), Fix16.FromInt(-2));

        Assert.Equal(0L, a.HorizontalDistanceSquared(a));
    }

    [Fact]
    public void HorizontalDotSumsXAndZProducts()
    {
        var p = new Vec3Fix(Fix16.FromInt(3), Fix16.FromInt(999), Fix16.FromInt(4));
        var n = new Vec3Fix(Fix16.FromInt(1), Fix16.FromInt(-999), Fix16.FromInt(0));

        var result = p.HorizontalDot(n);

        // 3*1 + 4*0 = 3 (Fix16なので65536²倍)
        Assert.Equal(3L * 65536L * 65536L, result);
    }
}
