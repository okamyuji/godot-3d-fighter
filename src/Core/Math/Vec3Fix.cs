namespace Godot3dFighter.Core.Math;

/// <summary>
/// キャラ座標かワールド座標の3成分。ADR-0002の規約でYが上、Xが右、Zが手前。
/// </summary>
public readonly record struct Vec3Fix(Fix16 X, Fix16 Y, Fix16 Z)
{
    public static Vec3Fix operator +(Vec3Fix a, Vec3Fix b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vec3Fix Add(Vec3Fix a, Vec3Fix b) => a + b;

    public static Vec3Fix operator -(Vec3Fix a, Vec3Fix b) =>
        new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vec3Fix Subtract(Vec3Fix a, Vec3Fix b) => a - b;

    public static Vec3Fix operator *(Vec3Fix v, Fix16 scalar) =>
        new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    public static Vec3Fix Multiply(Vec3Fix v, Fix16 scalar) => v * scalar;

    public Vec3Fix Clamp(Fix16 min, Fix16 max) =>
        new(X.Clamp(min, max), Y.Clamp(min, max), Z.Clamp(min, max));

    /// <summary>XとZだけの距離の二乗。long（Q32.32）。</summary>
    public long HorizontalDistanceSquared(Vec3Fix other)
    {
        var dx = X - other.X;
        var dz = Z - other.Z;
        return dx.RawSquared() + dz.RawSquared();
    }

    /// <summary>XとZだけの内積。long（Q32.32）。</summary>
    public long HorizontalDot(Vec3Fix n) =>
        (long)X.Raw * n.X.Raw + (long)Z.Raw * n.Z.Raw;
}
