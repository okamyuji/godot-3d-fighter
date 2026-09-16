using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Sim;

/// <summary>2人の体の押し合いと、その後の位置の確定（02-match-rules.mdの手順7）。</summary>
public static class BodyPush
{
    public static (Vec3Fix P1, Vec3Fix P2) Resolve(
        Vec3Fix p1BeforeMovement,
        Vec3Fix p2BeforeMovement,
        Vec3Fix p1AfterMovement,
        Vec3Fix p2AfterMovement,
        Fix16 p1Radius,
        Fix16 p2Radius,
        Angle16 cameraYaw,
        bool p1OnLeft,
        StageData stage,
        bool eitherThrowing)
    {
        if (eitherThrowing)
        {
            return (
                PositionFinalizer.Finalize(p1AfterMovement, p1Radius, stage),
                PositionFinalizer.Finalize(p2AfterMovement, p2Radius, stage));
        }

        var radiusSum = p1Radius + p2Radius;
        var (p1Pushed, p2Pushed) = PushApart(p1AfterMovement, p2AfterMovement, radiusSum, cameraYaw, p1OnLeft);

        var p1 = PositionFinalizer.Finalize(p1Pushed, p1Radius, stage);
        var p2 = PositionFinalizer.Finalize(p2Pushed, p2Radius, stage);
        var p1Changed = p1 != p1Pushed;
        var p2Changed = p2 != p2Pushed;

        if (p1Changed != p2Changed)
        {
            var radiusSumSquared = radiusSum.RawSquared();
            if (p1.HorizontalDistanceSquared(p2) < radiusSumSquared)
            {
                if (p1Changed)
                {
                    var pushed = PushAwayFully(p2, p1, radiusSum, cameraYaw, !p1OnLeft);
                    var refinalized = PositionFinalizer.Finalize(pushed, p2Radius, stage);
                    p2Changed = refinalized != pushed;
                    p2 = refinalized;
                }
                else
                {
                    var pushed = PushAwayFully(p1, p2, radiusSum, cameraYaw, p1OnLeft);
                    var refinalized = PositionFinalizer.Finalize(pushed, p1Radius, stage);
                    p1Changed = refinalized != pushed;
                    p1 = refinalized;
                }
            }
        }

        if (p1Changed && p2Changed)
        {
            return (p1BeforeMovement, p2BeforeMovement);
        }

        return (p1, p2);
    }

    private static (Vec3Fix P1, Vec3Fix P2) PushApart(Vec3Fix p1, Vec3Fix p2, Fix16 radiusSum, Angle16 cameraYaw, bool p1OnLeft)
    {
        var distanceSquared = p1.HorizontalDistanceSquared(p2);
        var radiusSumSquared = radiusSum.RawSquared();
        if (distanceSquared >= radiusSumSquared)
        {
            return (p1, p2);
        }

        var distance = new Fix16((int)IntMath.Sqrt(distanceSquared));
        var half = (radiusSum - distance) / Fix16.FromInt(2);

        var p1Direction = SeparationDirection(p2, p1, cameraYaw, p1OnLeft);
        var p2Direction = SeparationDirection(p1, p2, cameraYaw, !p1OnLeft);

        return (p1 + (p1Direction * half), p2 + (p2Direction * half));
    }

    private static Vec3Fix PushAwayFully(Vec3Fix moving, Vec3Fix fixedPoint, Fix16 radiusSum, Angle16 cameraYaw, bool movingOnLeft)
    {
        var distanceSquared = moving.HorizontalDistanceSquared(fixedPoint);
        var radiusSumSquared = radiusSum.RawSquared();
        if (distanceSquared >= radiusSumSquared)
        {
            return moving;
        }

        var distance = new Fix16((int)IntMath.Sqrt(distanceSquared));
        var shortfall = radiusSum - distance;
        var direction = SeparationDirection(fixedPoint, moving, cameraYaw, movingOnLeft);
        return moving + (direction * shortfall);
    }

    private static Vec3Fix SeparationDirection(Vec3Fix from, Vec3Fix toward, Angle16 cameraYaw, bool towardOnLeft)
    {
        var distanceSquared = toward.HorizontalDistanceSquared(from);
        if (distanceSquared == 0)
        {
            var angle = towardOnLeft
                ? new Angle16(unchecked((ushort)(cameraYaw.Value + 32768)))
                : cameraYaw;
            return Trig.Direction(angle);
        }

        var dx = toward.X - from.X;
        var dz = toward.Z - from.Z;
        var distance = new Fix16((int)IntMath.Sqrt(distanceSquared));
        return new Vec3Fix(dx / distance, Fix16.Zero, dz / distance);
    }
}
