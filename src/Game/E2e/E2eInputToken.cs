using System;
using Godot3dFighter.Core.Input;

namespace Godot3dFighter.Game.E2e;

/// <summary>シナリオのframes手順にある入力表記（テンキー数字とP、K、G）をInputFrameへ変換する（03-screens-and-e2e.md）。</summary>
public static class E2eInputToken
{
    public static InputFrame Parse(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        byte bits = 0;
        var index = 0;

        if (index < token.Length && token[index] is >= '1' and <= '9')
        {
            bits |= DirectionBits(token[index]);
            index++;
        }

        for (; index < token.Length; index++)
        {
            bits |= (byte)(token[index] switch
            {
                'P' => InputFrame.Punch,
                'K' => InputFrame.Kick,
                'G' => InputFrame.Guard,
                '+' => 0,
                _ => throw new FormatException($"入力表記'{token}'の文字'{token[index]}'が不明です。"),
            });
        }

        return new InputFrame(bits);
    }

    private static byte DirectionBits(char digit) => (byte)(digit switch
    {
        '1' => InputFrame.Down | InputFrame.Left,
        '2' => InputFrame.Down,
        '3' => InputFrame.Down | InputFrame.Right,
        '4' => InputFrame.Left,
        '5' => 0,
        '6' => InputFrame.Right,
        '7' => InputFrame.Up | InputFrame.Left,
        '8' => InputFrame.Up,
        '9' => InputFrame.Up | InputFrame.Right,
        _ => throw new FormatException($"テンキーの数字'{digit}'が不明です。"),
    });
}
