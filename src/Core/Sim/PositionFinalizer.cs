using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Sim;

/// <summary>位置を書き換えた直後に必ず適用する順序（02-match-rules.mdの「位置の確定」）。</summary>
public static class PositionFinalizer
{
    public static Vec3Fix Finalize(Vec3Fix position, Fix16 bodyRadius, StageData stage)
    {
        var pushed = RingBounds.PushInsideWalls(position, bodyRadius, stage);
        var limit = Fix16.FromInt(Limits.PositionLimitMeters);
        return pushed.Clamp(Fix16.Zero - limit, limit);
    }
}
