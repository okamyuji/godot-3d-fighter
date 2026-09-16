using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>投げの成立を判定する（ADR-0019）。</summary>
public static class ThrowResolver
{
    public static bool TryDetect(
        in PlayerState attacker, CharacterData attackerCharacter,
        in PlayerState defender, CharacterData defenderCharacter,
        out MoveData? move)
    {
        ArgumentNullException.ThrowIfNull(attackerCharacter);
        ArgumentNullException.ThrowIfNull(defenderCharacter);
        move = null;

        if (attacker.State != StateKind.Attack || attacker.CurrentMove == Limits.NoMove)
        {
            return false;
        }

        var candidate = attackerCharacter.Moves[attacker.CurrentMove];
        if (candidate.Kind != MoveKind.Throw)
        {
            return false;
        }

        if (attacker.StateFrame != candidate.Startup)
        {
            return false;
        }

        var distanceSquared = attacker.Position.HorizontalDistanceSquared(defender.Position);
        var rangeSquared = candidate.ThrowRange.RawSquared();
        if (distanceSquared > rangeSquared)
        {
            return false;
        }

        if (!IsThrowable(defender, defenderCharacter))
        {
            return false;
        }

        var facingDiff = unchecked((short)(attacker.Facing.Value - defender.Facing.Value));
        var absDiff = facingDiff < 0 ? -facingDiff : facingDiff;
        var facingEachOther = absDiff > 16384;
        var sideMatches = candidate.ThrowSide == ThrowSide.Front ? facingEachOther : !facingEachOther;
        if (!sideMatches)
        {
            return false;
        }

        move = candidate;
        return true;
    }

    private static bool IsThrowable(in PlayerState defender, CharacterData defenderCharacter)
    {
        switch (defender.State)
        {
            case StateKind.Idle:
            case StateKind.Walk:
            case StateKind.BackWalk:
            case StateKind.Guard:
            case StateKind.SidestepIn:
            case StateKind.SidestepOut:
            case StateKind.Dash:
            case StateKind.Backdash:
            case StateKind.Rise:
                return true;
            case StateKind.Attack:
                if (defender.CurrentMove == Limits.NoMove)
                {
                    return true;
                }

                return defenderCharacter.Moves[defender.CurrentMove].Posture != Posture.Crouch;
            default:
                return false;
        }
    }
}
