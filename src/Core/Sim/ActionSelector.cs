using System;
using System.Collections.Generic;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;

namespace Godot3dFighter.Core.Sim;

/// <summary>行動できるプレイヤーが毎フレーム行う行動の判断（ADR-0017）。</summary>
public static class ActionSelector
{
    public static ActionDecision Select(InputBuffer inputs, bool onLeftSide, IReadOnlyList<MoveData> moves, bool buffered)
    {
        ArgumentNullException.ThrowIfNull(moves);

        if (CommandParser.TryFindMove(inputs, onLeftSide, moves, buffered, out var moveIndex))
        {
            return new ActionDecision(SelectedAction.Move, moveIndex);
        }

        if (CommandParser.TryFindDash(inputs, onLeftSide, forward: true, out _))
        {
            return new ActionDecision(SelectedAction.DashForward, -1);
        }

        if (CommandParser.TryFindDash(inputs, onLeftSide, forward: false, out _))
        {
            return new ActionDecision(SelectedAction.DashBack, -1);
        }

        var current = inputs.At(0);
        var guard = current.Has(InputFrame.Guard);
        var digit = CommandParser.Digit(current, onLeftSide);
        var down = digit is 1 or 2 or 3;

        if (guard)
        {
            return new ActionDecision(down ? SelectedAction.CrouchGuard : SelectedAction.Guard, -1);
        }

        if (down)
        {
            return new ActionDecision(SelectedAction.Crouch, -1);
        }

        return digit switch
        {
            6 => new ActionDecision(SelectedAction.WalkForward, -1),
            4 => new ActionDecision(SelectedAction.WalkBack, -1),
            _ => new ActionDecision(SelectedAction.Idle, -1),
        };
    }
}
