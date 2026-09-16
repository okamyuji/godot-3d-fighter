namespace Godot3dFighter.Core.Math;

/// <summary>16bitの角度。1周を65536とする（ADR-0014）。加減算はushortの桁あふれをそのまま使う。</summary>
public readonly record struct Angle16(ushort Value)
{
    public static Angle16 operator +(Angle16 a, Angle16 b) =>
        new(unchecked((ushort)(a.Value + b.Value)));

    public static Angle16 Add(Angle16 a, Angle16 b) => a + b;

    public static Angle16 operator -(Angle16 a, Angle16 b) =>
        new(unchecked((ushort)(a.Value - b.Value)));

    public static Angle16 Subtract(Angle16 a, Angle16 b) => a - b;
}
