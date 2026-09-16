using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>相手の方を向き直す処理（ADR-0015）。</summary>
public static class Facing
{
    public static Angle16 Update(in PlayerState player, Vec3Fix opponentPosition, Vec3Fix selfPosition, MoveData? currentMove)
    {
        if (!IsTracking(player, currentMove))
        {
            return player.Facing;
        }

        if (selfPosition.HorizontalDistanceSquared(opponentPosition) == 0)
        {
            return player.Facing;
        }

        var dx = opponentPosition.X - selfPosition.X;
        var dz = opponentPosition.Z - selfPosition.Z;
        return Trig.Atan2(Fix16.Zero - dz, dx);
    }

    private static bool IsTracking(in PlayerState player, MoveData? currentMove) => player.State switch
    {
        StateKind.Idle or StateKind.Walk or StateKind.BackWalk or StateKind.Crouch
            or StateKind.Guard or StateKind.CrouchGuard or StateKind.SidestepIn or StateKind.SidestepOut
            or StateKind.Dash or StateKind.Backdash or StateKind.Rise => true,
        StateKind.Attack => currentMove is not null && player.StateFrame < currentMove.Tracking,
        _ => false,
    };
}
