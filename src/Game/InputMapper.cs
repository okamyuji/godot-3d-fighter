using Godot;
using Godot3dFighter.Core.Input;

namespace Godot3dFighter.Game;

/// <summary>GodotのInputMapからプレイヤーごとの正規化済みInputFrameを作る（03-screens-and-e2e.md）。</summary>
public static class InputMapper
{
    private static readonly InputFrame?[] Overrides = new InputFrame?[3];

    /// <summary>E2eRunnerがシナリオの入力をキーボードの代わりに与えるための上書き。nullで解除する。</summary>
    public static void SetOverride(int playerNumber, InputFrame? frame) => Overrides[playerNumber] = frame;

    public static InputFrame Read(int playerNumber)
    {
        if (Overrides[playerNumber] is { } overridden)
        {
            return overridden;
        }

        var prefix = $"p{playerNumber}_";
        byte bits = 0;

        var up = Input.IsActionPressed(prefix + "up");
        var down = Input.IsActionPressed(prefix + "down");
        var left = Input.IsActionPressed(prefix + "left");
        var right = Input.IsActionPressed(prefix + "right");

        if (up && !down)
        {
            bits |= InputFrame.Up;
        }
        else if (down && !up)
        {
            bits |= InputFrame.Down;
        }

        if (left && !right)
        {
            bits |= InputFrame.Left;
        }
        else if (right && !left)
        {
            bits |= InputFrame.Right;
        }

        if (Input.IsActionPressed(prefix + "punch"))
        {
            bits |= InputFrame.Punch;
        }

        if (Input.IsActionPressed(prefix + "kick"))
        {
            bits |= InputFrame.Kick;
        }

        if (Input.IsActionPressed(prefix + "guard"))
        {
            bits |= InputFrame.Guard;
        }

        return new InputFrame(bits);
    }
}
