using System;
using Godot3dFighter.Core.Math;
using Xunit;

namespace Godot3dFighter.Core.Tests.Math;

public sealed class Fix16Tests
{
    [Fact]
    public void ZeroIsRawZero()
    {
        Assert.Equal(0, Fix16.Zero.Raw);
    }

    [Fact]
    public void OneIsRaw65536()
    {
        Assert.Equal(65536, Fix16.One.Raw);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 65536)]
    [InlineData(-3, -196608)]
    public void FromIntScalesByOne(int value, int expectedRaw)
    {
        Assert.Equal(expectedRaw, Fix16.FromInt(value).Raw);
    }

    [Fact]
    public void FromIntThrowsOnOverflow()
    {
        Assert.Throws<OverflowException>(() => Fix16.FromInt(int.MaxValue / 65536 + 1));
    }

    [Fact]
    public void FromDecimalScalesByOne()
    {
        Assert.Equal(65536, Fix16.FromDecimal(1m).Raw);
    }

    [Fact]
    public void FromDecimalRoundsPositiveHalfAwayFromZero()
    {
        Assert.Equal(2, Fix16.FromDecimal(1.5m / 65536m).Raw);
    }

    [Fact]
    public void FromDecimalRoundsNegativeHalfAwayFromZero()
    {
        Assert.Equal(-2, Fix16.FromDecimal(-1.5m / 65536m).Raw);
    }

    [Fact]
    public void FromDecimalThrowsOnOverflow()
    {
        Assert.Throws<OverflowException>(() => Fix16.FromDecimal(1_000_000_000_000m));
    }

    [Fact]
    public void AdditionSumsRaw()
    {
        var result = Fix16.FromInt(1) + Fix16.FromInt(2);
        Assert.Equal(Fix16.FromInt(3), result);
    }

    [Fact]
    public void AdditionThrowsOnOverflow()
    {
        var a = new Fix16(int.MaxValue);
        var b = new Fix16(1);
        Assert.Throws<OverflowException>(() => a + b);
    }

    [Fact]
    public void SubtractionSubtractsRaw()
    {
        var result = Fix16.FromInt(5) - Fix16.FromInt(2);
        Assert.Equal(Fix16.FromInt(3), result);
    }

    [Fact]
    public void SubtractionThrowsOnOverflow()
    {
        var a = new Fix16(int.MinValue);
        var b = new Fix16(1);
        Assert.Throws<OverflowException>(() => a - b);
    }

    [Fact]
    public void MultiplicationIsExactForIntegerValues()
    {
        var result = Fix16.FromInt(3) * Fix16.FromInt(4);
        Assert.Equal(Fix16.FromInt(12), result);
    }

    [Fact]
    public void MultiplicationRoundsNegativeResultTowardNegativeInfinity()
    {
        // a = -1/65536, b = 3 + 1/65536。真の積は-3.0000152...で、
        // 負の無限大方向へ丸めると-4になる（ゼロ方向への丸めなら-3）。
        var a = new Fix16(-1);
        var b = new Fix16(196609);

        var result = a * b;

        Assert.Equal(-4, result.Raw);
    }

    [Fact]
    public void MultiplicationThrowsOnOverflow()
    {
        var a = new Fix16(int.MaxValue);
        var b = new Fix16(2 * 65536);
        Assert.Throws<OverflowException>(() => a * b);
    }

    [Fact]
    public void DivisionIsExactForIntegerValues()
    {
        var result = Fix16.FromInt(6) / Fix16.FromInt(3);
        Assert.Equal(Fix16.FromInt(2), result);
    }

    [Fact]
    public void DivisionTruncatesTowardZero()
    {
        // -7/3 = -2.333...。ゼロ方向への丸めなら-152917、
        // 負の無限大方向への丸めなら-152918。
        var a = new Fix16(-7);
        var b = new Fix16(3);

        var result = a / b;

        Assert.Equal(-152917, result.Raw);
    }

    [Fact]
    public void DivisionByZeroThrows()
    {
        var a = Fix16.FromInt(1);
        var zero = Fix16.Zero;
        Assert.Throws<DivideByZeroException>(() => a / zero);
    }

    [Fact]
    public void AbsOfNegativeIsPositive()
    {
        Assert.Equal(new Fix16(5), new Fix16(-5).Abs());
    }

    [Fact]
    public void AbsOfPositiveIsUnchanged()
    {
        Assert.Equal(new Fix16(5), new Fix16(5).Abs());
    }

    [Fact]
    public void AbsOfZeroIsZero()
    {
        Assert.Equal(Fix16.Zero, Fix16.Zero.Abs());
    }

    [Fact]
    public void AbsThrowsOnIntMinValueOverflow()
    {
        var value = new Fix16(int.MinValue);
        Assert.Throws<OverflowException>(() => value.Abs());
    }

    [Fact]
    public void ClampWithinRangeIsUnchanged()
    {
        var value = Fix16.FromInt(5);
        Assert.Equal(value, value.Clamp(Fix16.FromInt(0), Fix16.FromInt(10)));
    }

    [Fact]
    public void ClampBelowMinReturnsMin()
    {
        var value = Fix16.FromInt(-5);
        Assert.Equal(Fix16.FromInt(0), value.Clamp(Fix16.FromInt(0), Fix16.FromInt(10)));
    }

    [Fact]
    public void ClampAboveMaxReturnsMax()
    {
        var value = Fix16.FromInt(15);
        Assert.Equal(Fix16.FromInt(10), value.Clamp(Fix16.FromInt(0), Fix16.FromInt(10)));
    }

    [Fact]
    public void RawSquaredMultipliesRawByItself()
    {
        Assert.Equal(9L, new Fix16(3).RawSquared());
    }

    [Fact]
    public void RawSquaredHandlesValuesThatOverflowInt()
    {
        Assert.Equal(10_000_000_000L, new Fix16(100_000).RawSquared());
    }
}
