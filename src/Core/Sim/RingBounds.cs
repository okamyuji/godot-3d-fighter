using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Sim;

/// <summary>足元の座標とリングの縁の関係（ADR-0002）。</summary>
public static class RingBounds
{
    private static readonly Vec3Fix Origin = new(Fix16.Zero, Fix16.Zero, Fix16.Zero);

    public static bool IsOut(Vec3Fix footPosition, StageData stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Shape == RingShape.Circle)
        {
            if (stage.Edges[0] != EdgeKind.RingOut)
            {
                return false;
            }

            var distanceSquared = footPosition.HorizontalDistanceSquared(Origin);
            var sizeSquared = (long)stage.Size.Raw * stage.Size.Raw;
            return distanceSquared > sizeSquared;
        }

        var boundary = (long)stage.Size.Raw << Fix16.Shift;
        for (var i = 0; i < stage.Edges.Count; i++)
        {
            if (stage.Edges[i] != EdgeKind.RingOut)
            {
                continue;
            }

            var normal = EdgeNormal(stage.Shape, i);
            if (footPosition.HorizontalDot(normal) > boundary)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>体の外周が壁を越えないよう足元を押し戻す。壁でない縁は変えない。</summary>
    public static Vec3Fix PushInsideWalls(Vec3Fix footPosition, Fix16 bodyRadius, StageData stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        var limit = stage.Size - bodyRadius;

        if (stage.Shape == RingShape.Circle)
        {
            return PushInsideCircle(footPosition, limit, stage.Edges[0]);
        }

        var boundary = (long)limit.Raw << Fix16.Shift;
        var result = footPosition;
        for (var i = 0; i < stage.Edges.Count; i++)
        {
            if (stage.Edges[i] != EdgeKind.Wall)
            {
                continue;
            }

            var normal = EdgeNormal(stage.Shape, i);
            var dot = result.HorizontalDot(normal);
            if (dot <= boundary)
            {
                continue;
            }

            var excess = new Fix16((int)((dot - boundary) >> Fix16.Shift));
            result -= normal * excess;
        }

        return result;
    }

    private static Vec3Fix PushInsideCircle(Vec3Fix footPosition, Fix16 limit, EdgeKind edge)
    {
        if (edge != EdgeKind.Wall)
        {
            return footPosition;
        }

        var distanceSquared = footPosition.HorizontalDistanceSquared(Origin);
        var limitSquared = (long)limit.Raw * limit.Raw;
        if (distanceSquared <= limitSquared)
        {
            return footPosition;
        }

        var distance = new Fix16((int)IntMath.Sqrt(distanceSquared));
        var ratio = limit / distance;
        return new Vec3Fix(footPosition.X * ratio, footPosition.Y, footPosition.Z * ratio);
    }

    private static Vec3Fix EdgeNormal(RingShape shape, int index)
    {
        var step = shape == RingShape.Square ? 16384 : 8192;
        return Trig.Direction(new Angle16(unchecked((ushort)(index * step))));
    }
}
