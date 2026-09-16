namespace Godot3dFighter.Core.Input;

/// <summary>1フレーム分の入力（ADR-0003）。左右は画面上の絶対的な左右で、前後への変換はコマンド判定時に行う。</summary>
public readonly record struct InputFrame(byte Bits)
{
    public const byte Up = 1;
    public const byte Down = 2;
    public const byte Left = 4;
    public const byte Right = 8;
    public const byte Punch = 16;
    public const byte Kick = 32;
    public const byte Guard = 64;

    /// <summary>ビット7が0で、上下と左右がそれぞれ同時押しでない。</summary>
    public bool IsNormalized => (Bits & 0x80) == 0 && (Bits & 3) != 3 && (Bits & 12) != 12;

    public bool Has(byte flag) => (Bits & flag) == flag;
}
