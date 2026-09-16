using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;

namespace Godot3dFighter.Core.Sim;

/// <summary>投げ抜けの入力を判定する（ADR-0019）。背中からの投げは抜けられない。</summary>
public static class ThrowEscapeResolver
{
    public static bool TryEscape(InputBuffer inputs, bool onLeftSide, MoveData throwMove)
    {
        ArgumentNullException.ThrowIfNull(throwMove);

        if (throwMove.ThrowSide == ThrowSide.Back)
        {
            return false;
        }

        return CommandParser.TryFindMove(inputs, onLeftSide, [throwMove], buffered: true, out _);
    }
}
