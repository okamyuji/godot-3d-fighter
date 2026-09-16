using System;
using System.Collections.Generic;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>ダウン中の起き上がりの入力を判定する（ADR-0021）。</summary>
public static class WakeUp
{
    private static readonly MoveData TechCommand = new()
    {
        Name = "tech",
        Command = "P+K+G",
        Kind = MoveKind.Strike,
        Posture = Posture.Stand,
        Startup = 0,
        Active = 0,
        Recovery = 0,
        Tracking = 0,
        Height = HitHeight.High,
        Damage = 0,
        CounterDamage = 0,
        Hitstun = 0,
        CounterHitstun = 0,
        Blockstun = 0,
        Hitstop = 0,
        Knockdown = false,
        CounterKnockdown = false,
        HitsDown = false,
        Pushback = Godot3dFighter.Core.Math.Fix16.Zero,
        Motion = [],
        Windows = [],
    };

    private static readonly IReadOnlyList<MoveData> TechCommandList = [TechCommand];

    public static bool IsTechInputThisFrame(InputBuffer inputs, bool onLeftSide) =>
        CommandParser.TryFindMove(inputs, onLeftSide, TechCommandList, buffered: false, out _);

    public static WakeUpDecision Decide(
        in PlayerState player, InputBuffer inputs, bool onLeftSide, IReadOnlyList<MoveData> risingAttacks, Rules rules)
    {
        ArgumentNullException.ThrowIfNull(risingAttacks);
        ArgumentNullException.ThrowIfNull(rules);

        if (player.Flags.HasFlag(PlayerFlags.TechQueued))
        {
            return new WakeUpDecision(WakeUpAction.Tech, -1);
        }

        if (player.StateFrame < rules.DownMinFrames)
        {
            return new WakeUpDecision(WakeUpAction.None, -1);
        }

        var current = inputs.At(0);
        var previous = inputs.At(1);
        var digit = CommandParser.Digit(current, onLeftSide);
        var previousDigit = CommandParser.Digit(previous, onLeftSide);

        if (digit == 8 && previousDigit != 8)
        {
            return new WakeUpDecision(WakeUpAction.RollIn, -1);
        }

        if (digit is 1 or 2 or 3 && previousDigit is not (1 or 2 or 3))
        {
            return new WakeUpDecision(WakeUpAction.RollOut, -1);
        }

        if (IsFreshPress(current, previous, InputFrame.Punch) && TryFindRisingAttack(risingAttacks, InputFrame.Punch, out var punchIndex))
        {
            return new WakeUpDecision(WakeUpAction.RisingAttack, punchIndex);
        }

        if (IsFreshPress(current, previous, InputFrame.Kick) && TryFindRisingAttack(risingAttacks, InputFrame.Kick, out var kickIndex))
        {
            return new WakeUpDecision(WakeUpAction.RisingAttack, kickIndex);
        }

        if (IsFreshPress(current, previous, InputFrame.Guard))
        {
            return new WakeUpDecision(WakeUpAction.Rise, -1);
        }

        if (player.StateFrame >= rules.DownMaxFrames)
        {
            return new WakeUpDecision(WakeUpAction.Rise, -1);
        }

        return new WakeUpDecision(WakeUpAction.None, -1);
    }

    private static bool IsFreshPress(InputFrame current, InputFrame previous, byte flag) =>
        current.Has(flag) && !previous.Has(flag);

    private static bool TryFindRisingAttack(IReadOnlyList<MoveData> moves, byte buttonFlag, out int index)
    {
        var expectedCommand = buttonFlag == InputFrame.Punch ? "P" : "K";
        for (var i = 0; i < moves.Count; i++)
        {
            if (moves[i].Kind == MoveKind.RisingAttack && moves[i].Command == expectedCommand)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }
}
