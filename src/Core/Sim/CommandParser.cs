using System;
using System.Collections.Generic;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;

namespace Godot3dFighter.Core.Sim;

/// <summary>入力履歴からコマンドの成立を判定する（ADR-0017）。</summary>
public static class CommandParser
{
    /// <summary>画面上の立ち位置から、入力をテンキーの方向の数字（1〜9）に変換する。</summary>
    public static int Digit(InputFrame frame, bool onLeftSide)
    {
        var v = (frame.Has(InputFrame.Up) ? 1 : 0) - (frame.Has(InputFrame.Down) ? 1 : 0);
        var right = frame.Has(InputFrame.Right) ? 1 : 0;
        var left = frame.Has(InputFrame.Left) ? 1 : 0;
        var h = onLeftSide ? right - left : left - right;
        return 5 + (3 * v) + h;
    }

    /// <summary>技データの一覧から、このフレームに成立する技を探す。C-21、C-22。</summary>
    public static bool TryFindMove(
        InputBuffer inputs, bool onLeftSide, IReadOnlyList<MoveData> moves, bool buffered, out int moveIndex)
    {
        ArgumentNullException.ThrowIfNull(moves);

        var searchDepth = buffered ? Limits.BufferFrames : 0;

        for (var judge = 0; judge <= searchDepth; judge++)
        {
            var bestIndex = -1;
            var bestDirectionCount = -1;
            var bestButtonCount = -1;

            for (var i = 0; i < moves.Count; i++)
            {
                if (!TryParseCommand(moves[i].Command, out var digits, out var buttons))
                {
                    continue;
                }

                if (!IsCompletionFrame(inputs, buttons, judge))
                {
                    continue;
                }

                if (!MatchesDirections(inputs, onLeftSide, digits, judge))
                {
                    continue;
                }

                var buttonCount = CountBits(buttons);
                if (digits.Length > bestDirectionCount
                    || (digits.Length == bestDirectionCount && buttonCount > bestButtonCount))
                {
                    bestIndex = i;
                    bestDirectionCount = digits.Length;
                    bestButtonCount = buttonCount;
                }
            }

            if (bestIndex >= 0)
            {
                moveIndex = bestIndex;
                return true;
            }
        }

        moveIndex = -1;
        return false;
    }

    /// <summary>ボタンを持たないダッシュのコマンド（66、44）が今のフレームで成立したかを探す。</summary>
    public static bool TryFindDash(InputBuffer inputs, bool onLeftSide, bool forward, out int judgeFramesAgo)
    {
        var digit = forward ? 6 : 4;
        var digits = new[] { digit, digit };
        if (MatchesDirections(inputs, onLeftSide, digits, 0))
        {
            judgeFramesAgo = 0;
            return true;
        }

        judgeFramesAgo = 0;
        return false;
    }

    private static bool TryParseCommand(string command, out int[] digits, out byte buttons)
    {
        var i = 0;
        var directionChars = new List<int>();
        while (i < command.Length && command[i] is >= '1' and <= '6')
        {
            directionChars.Add(command[i] - '0');
            i++;
        }

        digits = [.. directionChars];
        buttons = 0;
        while (i < command.Length)
        {
            buttons |= command[i] switch
            {
                'P' => InputFrame.Punch,
                'K' => InputFrame.Kick,
                'G' => InputFrame.Guard,
                _ => (byte)0,
            };
            i++;
            if (i < command.Length && command[i] == '+')
            {
                i++;
            }
        }

        return true;
    }

    private static bool IsCompletionFrame(InputBuffer inputs, byte requiredButtons, int framesAgo)
    {
        if (requiredButtons == 0)
        {
            return true;
        }

        var frame = inputs.At(framesAgo);
        if ((frame.Bits & requiredButtons) != requiredButtons)
        {
            return false;
        }

        var nonGuard = (byte)(requiredButtons & ~InputFrame.Guard);
        if (nonGuard == 0)
        {
            return IsFreshPress(inputs, requiredButtons, framesAgo);
        }

        var anyFreshHere = false;
        for (byte bit = 1; bit <= InputFrame.Guard; bit <<= 1)
        {
            if ((nonGuard & bit) == 0)
            {
                continue;
            }

            if (IsFreshPress(inputs, bit, framesAgo))
            {
                anyFreshHere = true;
            }

            if (!HasFreshPressWithin(inputs, bit, framesAgo, Limits.SimultaneousPressFrames))
            {
                return false;
            }
        }

        return anyFreshHere;
    }

    private static bool IsFreshPress(InputBuffer inputs, byte bit, int framesAgo) =>
        inputs.At(framesAgo).Has(bit) && !inputs.At(framesAgo + 1).Has(bit);

    private static bool HasFreshPressWithin(InputBuffer inputs, byte bit, int framesAgo, int window)
    {
        for (var f = framesAgo; f < framesAgo + window; f++)
        {
            if (IsFreshPress(inputs, bit, f))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesDirections(InputBuffer inputs, bool onLeftSide, int[] digits, int endFramesAgo)
    {
        if (digits.Length == 0)
        {
            return true;
        }

        if (digits.Length == 1)
        {
            return Digit(inputs.At(endFramesAgo), onLeftSide) == digits[0];
        }

        var searchStart = endFramesAgo;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var found = false;
            for (var f = searchStart; f < searchStart + Limits.CommandWindowFrames; f++)
            {
                if (IsNewlyEntered(inputs, onLeftSide, f, digits[i]))
                {
                    searchStart = f + 1;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNewlyEntered(InputBuffer inputs, bool onLeftSide, int framesAgo, int targetDigit) =>
        Digit(inputs.At(framesAgo), onLeftSide) == targetDigit
        && Digit(inputs.At(framesAgo + 1), onLeftSide) != targetDigit;

    private static int CountBits(byte value)
    {
        var count = 0;
        for (var bit = 1; bit <= InputFrame.Guard; bit <<= 1)
        {
            if ((value & bit) != 0)
            {
                count++;
            }
        }

        return count;
    }
}
