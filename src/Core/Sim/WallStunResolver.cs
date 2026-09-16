using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>押し離した後に壁やられになるかを判定する（ADR-0024）。1回の連続技で1回だけ起きる。</summary>
public static class WallStunResolver
{
    public static WallStunResult Resolve(Vec3Fix pushedBackPosition, Fix16 bodyRadius, StageData stage, PlayerFlags currentFlags)
    {
        var finalPosition = RingBounds.PushInsideWalls(pushedBackPosition, bodyRadius, stage);
        var blockedByWall = finalPosition != pushedBackPosition;
        var alreadyTaken = currentFlags.HasFlag(PlayerFlags.WallHitTaken);

        if (blockedByWall && !alreadyTaken)
        {
            return new WallStunResult(true, currentFlags | PlayerFlags.WallHitTaken, finalPosition);
        }

        return new WallStunResult(false, currentFlags, finalPosition);
    }
}
