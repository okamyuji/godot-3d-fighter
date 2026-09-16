using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Godot3dFighter.Core.Input;

/// <summary>直近64フレームの入力（ADR-0003）。Pushは新しい値を返し、自身は書き換えない。</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InputBuffer : IEquatable<InputBuffer>
{
    [InlineArray(Limits.InputBufferFrames)]
    private struct Slots
    {
        private byte _e0;
    }

    private Slots _slots;
    private byte _head;

    public InputBuffer Push(InputFrame frame)
    {
        var result = this;
        result._slots[result._head] = frame.Bits;
        result._head = (byte)((result._head + 1) % Limits.InputBufferFrames);
        return result;
    }

    /// <summary>0が最新。Limits.InputBufferFrames以上はArgumentOutOfRangeException。</summary>
    public readonly InputFrame At(int framesAgo)
    {
        if (framesAgo < 0 || framesAgo >= Limits.InputBufferFrames)
        {
            throw new ArgumentOutOfRangeException(nameof(framesAgo), framesAgo, "0以上InputBufferFrames未満の値が必要です。");
        }

        var index = (_head - 1 - framesAgo + (Limits.InputBufferFrames * 2)) % Limits.InputBufferFrames;
        return new InputFrame(_slots[index]);
    }

    public readonly bool Equals(InputBuffer other)
    {
        if (_head != other._head)
        {
            return false;
        }

        for (var i = 0; i < Limits.InputBufferFrames; i++)
        {
            if (_slots[i] != other._slots[i])
            {
                return false;
            }
        }

        return true;
    }

    public override readonly bool Equals(object? obj) => obj is InputBuffer other && Equals(other);

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_head);
        for (var i = 0; i < Limits.InputBufferFrames; i++)
        {
            hash.Add(_slots[i]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(InputBuffer left, InputBuffer right) => left.Equals(right);

    public static bool operator !=(InputBuffer left, InputBuffer right) => !left.Equals(right);
}
