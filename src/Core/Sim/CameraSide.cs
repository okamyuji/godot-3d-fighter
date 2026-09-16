using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Sim;

/// <summary>画面上の左右を決める向き（ADR-0016）。</summary>
public static class CameraSide
{
    public static Angle16 Update(Vec3Fix p1Position, Vec3Fix p2Position, Angle16 currentYaw)
    {
        if (p1Position.HorizontalDistanceSquared(p2Position) == 0)
        {
            return currentYaw;
        }

        var dx = p2Position.X - p1Position.X;
        var dz = p2Position.Z - p1Position.Z;
        var alpha = Trig.Atan2(Fix16.Zero - dz, dx);
        var alphaOpposite = new Angle16(unchecked((ushort)(alpha.Value + 32768)));

        var diffAlpha = unchecked((short)(alpha.Value - currentYaw.Value));
        var diffOpposite = unchecked((short)(alphaOpposite.Value - currentYaw.Value));

        var absAlpha = diffAlpha < 0 ? -diffAlpha : diffAlpha;
        var absOpposite = diffOpposite < 0 ? -diffOpposite : diffOpposite;

        return absOpposite < absAlpha ? alphaOpposite : alpha;
    }

    /// <summary>更新後のCameraYawからP1が画面の左に立つかを求める。</summary>
    public static bool IsP1OnLeft(Vec3Fix p1Position, Vec3Fix p2Position, Angle16 updatedYaw)
    {
        if (p1Position.HorizontalDistanceSquared(p2Position) == 0)
        {
            return true;
        }

        var dx = p2Position.X - p1Position.X;
        var dz = p2Position.Z - p1Position.Z;
        var alpha = Trig.Atan2(Fix16.Zero - dz, dx);
        return alpha.Value == updatedYaw.Value;
    }
}
