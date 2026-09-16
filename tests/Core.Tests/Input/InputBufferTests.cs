using System;
using Godot3dFighter.Core.Input;
using Xunit;

namespace Godot3dFighter.Core.Tests.Input;

public sealed class InputBufferTests
{
    [Fact]
    public void AtZeroReturnsMostRecentlyPushedFrame()
    {
        var buffer = default(InputBuffer);
        buffer = buffer.Push(new InputFrame(InputFrame.Punch));

        Assert.Equal(new InputFrame(InputFrame.Punch), buffer.At(0));
    }

    [Fact]
    public void PushReturnsNewValueAndDoesNotMutateOriginal()
    {
        var original = default(InputBuffer);
        var pushed = original.Push(new InputFrame(InputFrame.Kick));

        Assert.Equal(new InputFrame(0), original.At(0));
        Assert.Equal(new InputFrame(InputFrame.Kick), pushed.At(0));
    }

    [Fact]
    public void AtReturnsFramesInPushOrder()
    {
        var buffer = default(InputBuffer);
        buffer = buffer.Push(new InputFrame(1));
        buffer = buffer.Push(new InputFrame(2));
        buffer = buffer.Push(new InputFrame(3));

        Assert.Equal(new InputFrame(3), buffer.At(0));
        Assert.Equal(new InputFrame(2), buffer.At(1));
        Assert.Equal(new InputFrame(1), buffer.At(2));
    }

    [Fact]
    public void RetainsExactlyTheLast64FramesAfter65Pushes()
    {
        var buffer = default(InputBuffer);
        for (var i = 0; i <= 64; i++)
        {
            buffer = buffer.Push(new InputFrame((byte)i));
        }

        Assert.Equal(new InputFrame(64), buffer.At(0));
        Assert.Equal(new InputFrame(1), buffer.At(63));
    }

    [Fact]
    public void At64ThrowsAfter65Pushes()
    {
        var buffer = default(InputBuffer);
        for (var i = 0; i <= 64; i++)
        {
            buffer = buffer.Push(new InputFrame((byte)i));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.At(64));
    }

    [Fact]
    public void AtNegativeThrows()
    {
        var buffer = default(InputBuffer);
        buffer = buffer.Push(new InputFrame(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.At(-1));
    }

    [Fact]
    public void EqualsIsTrueForSameContent()
    {
        var a = default(InputBuffer).Push(new InputFrame(1)).Push(new InputFrame(2));
        var b = default(InputBuffer).Push(new InputFrame(1)).Push(new InputFrame(2));

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void EqualsIsFalseForDifferentContent()
    {
        var a = default(InputBuffer).Push(new InputFrame(1));
        var b = default(InputBuffer).Push(new InputFrame(2));

        Assert.NotEqual(a, b);
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void EqualsIsFalseWhenComparedToNonInputBuffer()
    {
        var a = default(InputBuffer);

        Assert.False(a.Equals("not a buffer"));
    }
}
