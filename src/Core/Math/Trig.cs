namespace Godot3dFighter.Core.Math;

/// <summary>
/// コミット済みのsin表とatan表を使う三角関数（ADR-0014）。Coreでは`System.Math`を使わない。
/// </summary>
public static class Trig
{
    private static readonly Angle16 QuarterTurn = new(16384);
    private static readonly Angle16 HalfTurn = new(32768);
    private static readonly Angle16 FullTurn = new(0);

    public static Fix16 Sin(Angle16 angle) => new(SinTable.Values[angle.Value >> 6]);

    public static Fix16 Cos(Angle16 angle) => Sin(angle + QuarterTurn);

    /// <summary>0以上の角度（Q1相当）をatan表から求める。0以上90度（16384）以下を返す。</summary>
    private static int AbsoluteAngle(Fix16 ay, Fix16 ax)
    {
        var swap = ay.Raw > ax.Raw;
        var lo = swap ? ax : ay;
        var hi = swap ? ay : ax;
        var ratio = lo / hi;
        var baseAngle = AtanTable.Values[ratio.Raw >> 8];
        return swap ? QuarterTurn.Value - baseAngle : baseAngle;
    }

    public static Angle16 Atan2(Fix16 y, Fix16 x)
    {
        if (x.Raw == 0 && y.Raw == 0)
        {
            return FullTurn;
        }

        var angle = AbsoluteAngle(y.Abs(), x.Abs());
        var xNeg = x.Raw < 0;
        var yNeg = y.Raw < 0;

        var result = (xNeg, yNeg) switch
        {
            (false, false) => angle,
            (true, false) => HalfTurn.Value - angle,
            (true, true) => HalfTurn.Value + angle,
            (false, true) => 65536 - angle,
        };

        return new Angle16(unchecked((ushort)result));
    }

    /// <summary>キャラ座標（前が+X、右が+Z、上が+Y）の点をワールド座標の向きへ回す。</summary>
    public static Vec3Fix Rotate(Vec3Fix local, Angle16 facing)
    {
        var s = Sin(facing);
        var c = Cos(facing);
        var worldX = local.X * c + local.Z * s;
        var worldZ = local.Z * c - local.X * s;
        return new Vec3Fix(worldX, local.Y, worldZ);
    }

    /// <summary>向きベクトル(cos a, 0, -sin a)。</summary>
    public static Vec3Fix Direction(Angle16 a) => new(Cos(a), Fix16.Zero, Fix16.Zero - Sin(a));
}
