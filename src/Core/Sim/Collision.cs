using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Sim;

/// <summary>2つのカプセルが重なるかを判定する（ADR-0006）。途中の積はInt128で持つ。</summary>
public static class Collision
{
    private const long One = 65536;

    public static bool Overlaps(HitCapsule c1, HitCapsule c2)
    {
        var d1 = c1.B - c1.A;
        var d2 = c2.B - c2.A;
        var w = c1.A - c2.A;

        var a = Dot(d1, d1);
        var e = Dot(d2, d2);
        var f = Dot(d2, w);

        long s;
        long t;

        if (a == 0 && e == 0)
        {
            s = 0;
            t = 0;
        }
        else if (a == 0)
        {
            s = 0;
            t = ClampToUnit(Divide((Int128)f << 16, e));
        }
        else
        {
            var c = Dot(d1, w);
            if (e == 0)
            {
                t = 0;
                s = ClampToUnit(Divide((Int128)(-c) << 16, a));
            }
            else
            {
                var b = Dot(d1, d2);
                var denom = (Int128)a * e - (Int128)b * b;
                s = denom != 0
                    ? ClampToUnit(Divide((((Int128)b * f) - ((Int128)c * e)) << 16, denom))
                    : 0;
                t = Divide(((Int128)b * s) + ((Int128)f << 16), e);
                if (t < 0)
                {
                    t = 0;
                    s = ClampToUnit(Divide((Int128)(-c) << 16, a));
                }
                else if (t > One)
                {
                    t = One;
                    s = ClampToUnit(Divide(((Int128)b - c) << 16, a));
                }
            }
        }

        var closest1 = c1.A + ScaleShift(d1, s);
        var closest2 = c2.A + ScaleShift(d2, t);
        var diff = closest1 - closest2;
        var distanceSquared = Dot(diff, diff);

        var radiusSum = (long)c1.Radius.Raw + c2.Radius.Raw;
        var radiusSumSquared = radiusSum * radiusSum;

        return distanceSquared <= radiusSumSquared;
    }

    private static long Dot(Vec3Fix a, Vec3Fix b) =>
        ((long)a.X.Raw * b.X.Raw) + ((long)a.Y.Raw * b.Y.Raw) + ((long)a.Z.Raw * b.Z.Raw);

    private static long ClampToUnit(long v) => v < 0 ? 0 : v > One ? One : v;

    private static long Divide(Int128 numerator, Int128 denominator) => (long)(numerator / denominator);

    private static Vec3Fix ScaleShift(Vec3Fix v, long fraction) =>
        new(
            new Fix16((int)(((long)v.X.Raw * fraction) >> 16)),
            new Fix16((int)(((long)v.Y.Raw * fraction) >> 16)),
            new Fix16((int)(((long)v.Z.Raw * fraction) >> 16)));
}
