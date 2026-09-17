using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>投げの成立から終わりまでの状態の進み（ADR-0019、02-match-rules.mdの「投げの入力と終わり」）。</summary>
public static class ThrowSequence
{
    private const ushort HalfTurn = 32768;

    /// <summary>投げの成立。投げた側はThrowing、投げられた側はThrownで、向きは投げた側の逆。</summary>
    public static (PlayerState Thrower, PlayerState Thrown) Start(PlayerState thrower, PlayerState thrown) =>
        (thrower with { State = StateKind.Throwing, StateFrame = 0 },
            thrown with
            {
                State = StateKind.Thrown,
                StateFrame = 0,
                CurrentMove = Limits.NoMove,
                Facing = new Angle16(unchecked((ushort)(thrower.Facing.Value + HalfTurn))),
            });

    /// <summary>投げ抜けの確認。起点はPかKを最初に押した瞬間で、起点からSimultaneousPressFramesの間だけコマンドを確かめる。</summary>
    public static ThrowEscapeCheck CheckEscape(in PlayerState thrown, bool thrownOnLeftSide, MoveData throwMove)
    {
        if (thrown.Flags.HasFlag(PlayerFlags.ThrowEscapeTried))
        {
            return ThrowEscapeCheck.None;
        }

        var origin = FindOrigin(thrown.Inputs, thrown.StateFrame + Limits.BufferFrames);
        if (origin < 0)
        {
            return ThrowEscapeCheck.None;
        }

        if (ThrowEscapeResolver.TryEscape(thrown.Inputs, thrownOnLeftSide, throwMove))
        {
            return ThrowEscapeCheck.Escaped;
        }

        return origin >= Limits.SimultaneousPressFrames ? ThrowEscapeCheck.Tried : ThrowEscapeCheck.None;
    }

    /// <summary>投げ抜けの成立。2人をThrowEscapeにし、水平距離がThrowEscapeDistanceになるよう半分ずつ離して位置を確定する。</summary>
    public static (PlayerState Thrower, PlayerState Thrown) Escape(PlayerState thrower, PlayerState thrown, MatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var direction = MatchSimulator.HorizontalUnit(thrown.Position - thrower.Position, thrower.Facing);
        var distance = new Fix16((int)IntMath.Sqrt(thrower.Position.HorizontalDistanceSquared(thrown.Position)));
        var half = (context.Rules.ThrowEscapeDistance - distance) / Fix16.FromInt(2);
        if (half.Raw < 0)
        {
            half = Fix16.Zero;
        }

        var throwerPosition = PositionFinalizer.Finalize(
            thrower.Position - (direction * half), context.Characters[thrower.Slot].BodyRadius, context.Stage);
        var thrownPosition = PositionFinalizer.Finalize(
            thrown.Position + (direction * half), context.Characters[thrown.Slot].BodyRadius, context.Stage);

        return (
            thrower with { State = StateKind.ThrowEscape, StateFrame = 0, CurrentMove = Limits.NoMove, Position = throwerPosition },
            thrown with { State = StateKind.ThrowEscape, StateFrame = 0, CurrentMove = Limits.NoMove, Position = thrownPosition });
    }

    /// <summary>投げの終わり。ダメージ、ThrowEndOffsetの位置、投げた側への向き、ダウンか硬直。投げた側は技の硬直へ入る。</summary>
    public static (PlayerState Thrower, PlayerState Thrown) Finish(PlayerState thrower, PlayerState thrown, MoveData throwMove, MatchContext context)
    {
        ArgumentNullException.ThrowIfNull(throwMove);
        ArgumentNullException.ThrowIfNull(context);
        var endPosition = PositionFinalizer.Finalize(
            thrower.Position + Trig.Rotate(throwMove.ThrowEndOffset, thrower.Facing),
            context.Characters[thrown.Slot].BodyRadius,
            context.Stage);
        var health = thrown.Health - throwMove.Damage;
        if (context.Rules.Training && health < 1)
        {
            health = 1;
        }

        var landed = thrown with
        {
            Health = health,
            Position = endPosition,
            Facing = FacingToward(endPosition, thrower.Position, thrown.Facing),
            StateFrame = 0,
            LastHitKind = HitKind.Thrown,
        };
        landed = throwMove.Knockdown
            ? landed with { State = StateKind.Down, Flags = landed.Flags & ~PlayerFlags.DownHitTaken & ~PlayerFlags.TechQueued }
            : landed with { State = StateKind.Hitstun, StunFrames = throwMove.Hitstun };

        var recovering = thrower with { State = StateKind.Attack, StateFrame = (ushort)(throwMove.Startup + throwMove.Active) };
        return (recovering, landed);
    }

    private static Angle16 FacingToward(Vec3Fix self, Vec3Fix target, Angle16 fallback)
    {
        if (self.HorizontalDistanceSquared(target) == 0)
        {
            return fallback;
        }

        var dx = target.X - self.X;
        var dz = target.Z - self.Z;
        return Trig.Atan2(Fix16.Zero - dz, dx);
    }

    private static int FindOrigin(InputBuffer inputs, int windowStart)
    {
        for (var framesAgo = windowStart; framesAgo >= 0; framesAgo--)
        {
            if (IsPressEdge(inputs, framesAgo, InputFrame.Punch) || IsPressEdge(inputs, framesAgo, InputFrame.Kick))
            {
                return framesAgo;
            }
        }

        return -1;
    }

    private static bool IsPressEdge(InputBuffer inputs, int framesAgo, byte button) =>
        inputs.At(framesAgo).Has(button) && !inputs.At(framesAgo + 1).Has(button);
}
