using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>状態からこのフレームの移動量を決める（02-match-rules.mdの手順6）。</summary>
public static class Movement
{
    public static Vec3Fix ComputeVelocity(in PlayerState player, CharacterData character, bool onLeftSide, MoveData? currentMove)
    {
        ArgumentNullException.ThrowIfNull(character);

        var forward = Trig.Direction(player.Facing);

        return player.State switch
        {
            StateKind.Walk => forward * character.WalkSpeed,
            StateKind.BackWalk => forward * (Fix16.Zero - character.BackWalkSpeed),
            StateKind.Dash => forward * character.DashSpeed,
            StateKind.Backdash => forward * (Fix16.Zero - character.BackdashSpeed),
            StateKind.SidestepIn => SidestepVelocity(player, character, onLeftSide, into: true),
            StateKind.SidestepOut => SidestepVelocity(player, character, onLeftSide, into: false),
            StateKind.RollIn => RollVelocity(player, character, onLeftSide, into: true),
            StateKind.RollOut => RollVelocity(player, character, onLeftSide, into: false),
            StateKind.Attack => AttackVelocity(player, forward, currentMove),
            _ => default,
        };
    }

    private static Vec3Fix SidestepVelocity(in PlayerState player, CharacterData character, bool onLeftSide, bool into)
    {
        var direction = LateralDirection(player.Facing, onLeftSide, into);
        return direction * character.SidestepSpeed;
    }

    private static Vec3Fix RollVelocity(in PlayerState player, CharacterData character, bool onLeftSide, bool into)
    {
        var direction = LateralDirection(player.Facing, onLeftSide, into);
        return direction * character.RollSpeed;
    }

    /// <summary>画面の奥（into）か手前へ向かう向き。ADR-0016の横移動の向き。</summary>
    private static Vec3Fix LateralDirection(Angle16 facing, bool onLeftSide, bool into)
    {
        var deep = onLeftSide == into;
        var offset = deep ? 16384 : unchecked((ushort)(-16384));
        return Trig.Direction(new Angle16(unchecked((ushort)(facing.Value + offset))));
    }

    private static Vec3Fix AttackVelocity(in PlayerState player, Vec3Fix forward, MoveData? currentMove)
    {
        if (currentMove is null)
        {
            return default;
        }

        foreach (var segment in currentMove.Motion)
        {
            if (player.StateFrame >= segment.From && player.StateFrame <= segment.To)
            {
                return forward * segment.ForwardPerFrame;
            }
        }

        return default;
    }
}
