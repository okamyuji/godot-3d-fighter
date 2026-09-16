using System;

namespace Godot3dFighter.Core.Math;

/// <summary>
/// Q16.16固定小数点数。Rawは実数値の65536倍。
/// 乗算は負の無限大方向へ、除算はゼロ方向へ丸まる（ADR-0001）。
/// プロジェクトのCheckForOverflowUnderflow設定により、int同士の加減乗算とintへの
/// 明示キャストはあふれるとOverflowExceptionを投げる。
/// </summary>
public readonly record struct Fix16(int Raw)
{
    public const int Shift = 16;

    public static readonly Fix16 Zero = new(0);
    public static readonly Fix16 One = new(1 << Shift);

    public static Fix16 FromInt(int v) => new(v * One.Raw);

    public static Fix16 FromDecimal(decimal v)
    {
        var scaled = decimal.Round(v * One.Raw, 0, MidpointRounding.AwayFromZero);
        return new Fix16((int)scaled);
    }

    public static Fix16 operator +(Fix16 a, Fix16 b) => new(a.Raw + b.Raw);

    public static Fix16 Add(Fix16 a, Fix16 b) => a + b;

    public static Fix16 operator -(Fix16 a, Fix16 b) => new(a.Raw - b.Raw);

    public static Fix16 Subtract(Fix16 a, Fix16 b) => a - b;

    public static Fix16 operator *(Fix16 a, Fix16 b) =>
        new((int)(((long)a.Raw * b.Raw) >> Shift));

    public static Fix16 Multiply(Fix16 a, Fix16 b) => a * b;

    public static Fix16 operator /(Fix16 a, Fix16 b) =>
        new((int)(((long)a.Raw << Shift) / b.Raw));

    public static Fix16 Divide(Fix16 a, Fix16 b) => a / b;

    public Fix16 Abs() => Raw < 0 ? new(-Raw) : this;

    public Fix16 Clamp(Fix16 min, Fix16 max) =>
        Raw < min.Raw ? min : Raw > max.Raw ? max : this;

    public long RawSquared() => (long)Raw * Raw;
}
