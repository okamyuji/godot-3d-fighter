using Godot;
using Godot3dFighter.Core.Input;

namespace Godot3dFighter.Game;

/// <summary>GodotのInputMapからプレイヤーごとの正規化済みInputFrameを作る（03-screens-and-e2e.md）。</summary>
public static class InputMapper
{
    public static InputFrame Read(int playerNumber)
    {
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
