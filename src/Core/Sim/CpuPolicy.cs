using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>CPUが操作するプレイヤーの入力を試合状態から決める（ADR-0026）。MatchSimulatorは変えず、Game側が毎フレームこの入力をStepに渡す。</summary>
public static class CpuPolicy
{
    /// <summary>相手へ近づくのをやめて技を出す水平距離。boxのジャブ、ローキック、中段、投げがすべて届く。</summary>
    public static readonly Fix16 AttackDistance = Fix16.One;

    /// <summary>届く距離で技を選ぶ周期（フレーム）。決定フレーム以外は中立にして、ボタンを押した瞬間を作る。</summary>
    public const uint DecisionPeriod = 8;

    public static InputFrame Decide(in MatchState state, MatchContext context, int slot)
    {
        ArgumentNullException.ThrowIfNull(context);
        var opponentSlot = 1 - slot;
        var me = state.GetPlayer(slot);
        var opponent = state.GetPlayer(opponentSlot);

        if (GuardAgainst(opponent, context.Characters[opponentSlot]) is { } guard)
        {
            return guard;
        }

        var forward = Forward(state, slot);
        if (me.Position.HorizontalDistanceSquared(opponent.Position) > AttackDistance.RawSquared())
        {
            return new InputFrame(forward);
        }

        return Attack(state.FrameNumber, forward);
    }

    /// <summary>相手の打撃の段に合わせたガード。相手が打撃を出していなければnull。</summary>
    private static InputFrame? GuardAgainst(in PlayerState opponent, CharacterData character)
    {
        if (opponent.State != StateKind.Attack || opponent.CurrentMove == Limits.NoMove)
        {
            return null;
        }

        var move = character.Moves[opponent.CurrentMove];
        if (move.Kind == MoveKind.Throw)
        {
            return null;
        }

        return move.Height == HitHeight.Low
            ? new InputFrame((byte)(InputFrame.Guard | InputFrame.Down))
            : new InputFrame(InputFrame.Guard);
    }

    /// <summary>MatchSimulatorが行動の判断で読むのと同じ、更新前のCameraYawから画面上の前方向を決める。</summary>
    private static byte Forward(in MatchState state, int slot)
    {
        var p1OnLeft = CameraSide.IsP1OnLeft(state.GetPlayer(0).Position, state.GetPlayer(1).Position, state.CameraYaw);
        var onLeftSide = slot == 0 ? p1OnLeft : !p1OnLeft;
        return onLeftSide ? InputFrame.Right : InputFrame.Left;
    }

    private static InputFrame Attack(uint frameNumber, byte forward)
    {
        if (frameNumber % DecisionPeriod != 0)
        {
            return default;
        }

        return (frameNumber / DecisionPeriod % 4) switch
        {
            0 => new InputFrame(InputFrame.Punch),
            1 => new InputFrame(InputFrame.Kick),
            2 => new InputFrame((byte)(forward | InputFrame.Kick)),
            _ => new InputFrame((byte)(InputFrame.Punch | InputFrame.Guard)),
        };
    }
}
