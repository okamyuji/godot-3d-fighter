using System;
using System.Collections.Generic;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>1人の攻撃が1人のやられ判定に当たるかを判定する（ADR-0006、ADR-0018、ADR-0020、ADR-0021）。</summary>
public static class HitResolver
{
    public static HitOutcome Detect(
        in PlayerState attacker, CharacterData attackerCharacter,
        in PlayerState defender, CharacterData defenderCharacter)
    {
        ArgumentNullException.ThrowIfNull(attackerCharacter);
        ArgumentNullException.ThrowIfNull(defenderCharacter);

        if (attacker.State != StateKind.Attack || attacker.CurrentMove == Limits.NoMove)
        {
            return default;
        }

        if (attacker.Flags.HasFlag(PlayerFlags.HasHitThisMove))
        {
            return default;
        }

        var move = attackerCharacter.Moves[attacker.CurrentMove];
        if (move.Kind is not (MoveKind.Strike or MoveKind.RisingAttack))
        {
            return default;
        }

        var window = FindWindow(move.Windows, attacker.StateFrame);
        if (window is null || window.Hit.Count == 0)
        {
            return default;
        }

        if (defender.State is StateKind.Down or StateKind.RollIn or StateKind.RollOut)
        {
            return DetectDownHit(attacker, defender, defenderCharacter, window, move);
        }

        if (defender.State is StateKind.Throwing or StateKind.Thrown or StateKind.Dead)
        {
            return default;
        }

        var hurtCapsules = DefenderHurtCapsules(defender, defenderCharacter);
        if (!Overlaps(attacker.Position, attacker.Facing, window.Hit, defender.Position, defender.Facing, hurtCapsules))
        {
            return default;
        }

        return DetectStandingHit(defender, defenderCharacter, move);
    }

    private static HitOutcome DetectDownHit(
        in PlayerState attacker, in PlayerState defender, CharacterData defenderCharacter, HitWindow window, MoveData move)
    {
        if (!move.HitsDown || defender.Flags.HasFlag(PlayerFlags.DownHitTaken))
        {
            return default;
        }

        return Overlaps(attacker.Position, attacker.Facing, window.Hit, defender.Position, defender.Facing, defenderCharacter.DownHurtCapsules)
            ? new HitOutcome(HitOutcomeKind.DownHit, move)
            : default;
    }

    private static HitOutcome DetectStandingHit(in PlayerState defender, CharacterData defenderCharacter, MoveData move)
    {
        if (GuardMatrix.IsGuarded(move.Height, defender.State))
        {
            return new HitOutcome(HitOutcomeKind.Guarded, move);
        }

        if (!GuardMatrix.CanHit(move.Height, defender.State))
        {
            return default;
        }

        var isCounter = defender.State == StateKind.Attack
            && defender.CurrentMove != Limits.NoMove
            && IsBeforeActiveEnd(defenderCharacter.Moves[defender.CurrentMove], defender.StateFrame);

        return new HitOutcome(isCounter ? HitOutcomeKind.CounterHit : HitOutcomeKind.Hit, move);
    }

    private static bool IsBeforeActiveEnd(MoveData move, ushort stateFrame) =>
        stateFrame < move.Startup + move.Active;

    private static IReadOnlyList<HitCapsule> DefenderHurtCapsules(in PlayerState defender, CharacterData character) =>
        defender.State switch
        {
            StateKind.Crouch or StateKind.CrouchGuard or StateKind.CrouchBlockstun => character.CrouchHurtCapsules,
            StateKind.Attack => AttackHurtCapsules(defender, character),
            _ => character.StandHurtCapsules,
        };

    private static IReadOnlyList<HitCapsule> AttackHurtCapsules(in PlayerState defender, CharacterData character)
    {
        if (defender.CurrentMove == Limits.NoMove)
        {
            return character.StandHurtCapsules;
        }

        var move = character.Moves[defender.CurrentMove];
        var window = FindWindow(move.Windows, defender.StateFrame);
        if (window is not null && window.Hurt.Count > 0)
        {
            return window.Hurt;
        }

        return move.Posture == Posture.Crouch ? character.CrouchHurtCapsules : character.StandHurtCapsules;
    }

    private static HitWindow? FindWindow(IReadOnlyList<HitWindow> windows, ushort stateFrame)
    {
        foreach (var window in windows)
        {
            if (stateFrame >= window.From && stateFrame <= window.To)
            {
                return window;
            }
        }

        return null;
    }

    private static bool Overlaps(
        Vec3Fix attackerPosition, Angle16 attackerFacing, IReadOnlyList<HitCapsule> attackerCapsules,
        Vec3Fix defenderPosition, Angle16 defenderFacing, IReadOnlyList<HitCapsule> defenderCapsules)
    {
        foreach (var hit in attackerCapsules)
        {
            var worldHit = ToWorld(hit, attackerPosition, attackerFacing);
            foreach (var hurt in defenderCapsules)
            {
                var worldHurt = ToWorld(hurt, defenderPosition, defenderFacing);
                if (Collision.Overlaps(worldHit, worldHurt))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static HitCapsule ToWorld(HitCapsule local, Vec3Fix footPosition, Angle16 facing) =>
        new(footPosition + Trig.Rotate(local.A, facing), footPosition + Trig.Rotate(local.B, facing), local.Radius);
}
