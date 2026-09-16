using Godot3dFighter.Core.Input;

namespace Godot3dFighter.Core.Sim;

/// <summary>横移動の入り判定（ADR-0015）。</summary>
public static class SidestepInput
{
    public static bool TryEnterIn(InputBuffer inputs, bool onLeftSide) =>
        CommandParser.Digit(inputs.At(0), onLeftSide) == 8
        && CommandParser.Digit(inputs.At(1), onLeftSide) == 5;

    public static bool TryEnterOut(InputBuffer inputs, bool onLeftSide)
    {
        if (CommandParser.Digit(inputs.At(0), onLeftSide) != 5)
        {
            return false;
        }

        var previous = CommandParser.Digit(inputs.At(1), onLeftSide);
        if (previous is not (1 or 2 or 3))
        {
            return false;
        }

        for (var f = 1; f <= Limits.TapFrames; f++)
        {
            var digit = CommandParser.Digit(inputs.At(f), onLeftSide);
            if (digit is not (1 or 2 or 3))
            {
                return false;
            }

            var before = CommandParser.Digit(inputs.At(f + 1), onLeftSide);
            if (before is not (1 or 2 or 3))
            {
                return true;
            }
        }

        return false;
    }
}
