using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Sim;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class SidestepInputTests
{
    private static InputBuffer With(params byte[] bitsInOrder)
    {
        var buffer = default(InputBuffer);
        foreach (var bits in bitsInOrder)
        {
            buffer = buffer.Push(new InputFrame(bits));
        }

        return buffer;
    }

    [Fact]
    public void NeutralToUpEntersSidestepIn()
    {
        var buffer = With(0, InputFrame.Up);

        Assert.True(SidestepInput.TryEnterIn(buffer, true));
    }

    [Fact]
    public void HoldingUpDoesNotReenterSidestepIn()
    {
        var buffer = With(InputFrame.Up, InputFrame.Up);

        Assert.False(SidestepInput.TryEnterIn(buffer, true));
    }

    [Fact]
    public void DownReleasedWithinTapFramesEntersSidestepOut()
    {
        // 下を2フレーム入れてから中立へ戻す（TapFrames=6以内）。
        var buffer = With(InputFrame.Down, InputFrame.Down, 0);

        Assert.True(SidestepInput.TryEnterOut(buffer, true));
    }

    [Fact]
    public void DownReleasedAfterTapFramesDoesNotEnterSidestepOut()
    {
        var bits = new byte[9];
        for (var i = 0; i < 8; i++)
        {
            bits[i] = InputFrame.Down;
        }

        bits[8] = 0;
        var buffer = With(bits);

        Assert.False(SidestepInput.TryEnterOut(buffer, true));
    }

    [Fact]
    public void StillHoldingDownDoesNotEnterSidestepOut()
    {
        var buffer = With(InputFrame.Down, InputFrame.Down);

        Assert.False(SidestepInput.TryEnterOut(buffer, true));
    }

    [Fact]
    public void MovingThroughForwardBeforeNeutralDoesNotEnterSidestepOut()
    {
        // 下から中立を経ずに前へ動かした場合は横移動にならない。
        var buffer = With(InputFrame.Down, InputFrame.Right, 0);

        Assert.False(SidestepInput.TryEnterOut(buffer, true));
    }
}
