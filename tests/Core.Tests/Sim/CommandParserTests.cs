using System.Collections.Generic;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Sim;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class CommandParserTests
{
    private static MoveData Move(string name, string command, int index = 0) => new()
    {
        Name = name,
        Command = command,
        Kind = MoveKind.Strike,
        Posture = Posture.Stand,
        Startup = 5,
        Active = 2,
        Recovery = 5,
        Tracking = 2,
        Height = HitHeight.High,
        Damage = 10,
        CounterDamage = 10,
        Hitstun = 10,
        CounterHitstun = 10,
        Blockstun = 5,
        Hitstop = 3,
        Knockdown = false,
        CounterKnockdown = false,
        HitsDown = false,
        Pushback = Godot3dFighter.Core.Math.Fix16.Zero,
        Motion = [],
        Windows = [],
    };

    private static InputBuffer PushSequence(IEnumerable<byte> bitsInOrder)
    {
        var buffer = default(InputBuffer);
        foreach (var bits in bitsInOrder)
        {
            buffer = buffer.Push(new InputFrame(bits));
        }

        return buffer;
    }

    // Digitのテスト: 画面の左に立つプレイヤーは右が前。
    [Theory]
    [InlineData(0, true, 5)]
    [InlineData(InputFrame.Right, true, 6)]
    [InlineData(InputFrame.Left, true, 4)]
    [InlineData(InputFrame.Up, true, 8)]
    [InlineData(InputFrame.Down, true, 2)]
    [InlineData((byte)(InputFrame.Up | InputFrame.Right), true, 9)]
    [InlineData((byte)(InputFrame.Down | InputFrame.Right), true, 3)]
    [InlineData(InputFrame.Right, false, 4)]
    [InlineData(InputFrame.Left, false, 6)]
    public void DigitMatchesTenKeyLayout(byte bits, bool onLeftSide, int expected)
    {
        Assert.Equal(expected, CommandParser.Digit(new InputFrame(bits), onLeftSide));
    }

    [Fact]
    public void SingleButtonMoveMatchesOnThePressFrame()
    {
        var buffer = PushSequence([InputFrame.Punch]);
        var moves = new[] { Move("jab", "P") };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out var index);

        Assert.True(found);
        Assert.Equal(0, index);
    }

    [Fact]
    public void HoldingButtonDoesNotMatchOnTheSecondFrame()
    {
        var buffer = PushSequence([InputFrame.Punch, InputFrame.Punch]);
        var moves = new[] { Move("jab", "P") };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out _);

        Assert.False(found);
    }

    [Fact]
    public void TwoButtonComboMatchesWithinSimultaneousWindow()
    {
        // Gを先に押し続け、その1フレーム後にPを押す（SimultaneousPressFrames=2以内）。
        var buffer = PushSequence([InputFrame.Guard, (byte)(InputFrame.Guard | InputFrame.Punch)]);
        var moves = new[] { Move("throw", "P+G") };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out var index);

        Assert.True(found);
        Assert.Equal(0, index);
    }

    [Fact]
    public void ComboOutsideSimultaneousWindowDoesNotMatch()
    {
        // Pを押してから3フレーム後にKを押す（SimultaneousPressFrames=2を超える。GはPと違い猶予の対象外）。
        var buffer = PushSequence([InputFrame.Punch, 0, 0, InputFrame.Kick]);
        var moves = new[] { Move("pk", "P+K") };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out _);

        Assert.False(found);
    }

    [Fact]
    public void SingleDirectionMoveMatchesOnlyAtTheExactButtonFrame()
    {
        // 6を押してから3フレーム後にPを押す。1方向の技はボタンのフレームの方向が一致する必要がある。
        var buffer = PushSequence([InputFrame.Right, 0, 0, InputFrame.Punch]);
        var moves = new[] { Move("6p", "6P") };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out _);

        Assert.False(found);
    }

    [Fact]
    public void SingleDirectionMoveMatchesWhenDirectionHeldAtButtonFrame()
    {
        var buffer = PushSequence([InputFrame.Right, InputFrame.Right, (byte)(InputFrame.Right | InputFrame.Punch)]);
        var moves = new[] { Move("6p", "6P") };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out var index);

        Assert.True(found);
        Assert.Equal(0, index);
    }

    [Fact]
    public void QuarterCircleMoveMatchesWithinCommandWindow()
    {
        // 2(下)、3(下前)、6(前)と順に入れ、Pを押す。236Pは2→3→6の順で新しく入る必要がある。
        var buffer = PushSequence([InputFrame.Down, (byte)(InputFrame.Down | InputFrame.Right), InputFrame.Right, InputFrame.Punch]);
        var moves = new[] { Move("qcf", "236P") };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out var index);

        Assert.True(found);
        Assert.Equal(0, index);
    }

    [Fact]
    public void QuarterCircleMoveDoesNotMatchWhenDirectionsOutOfOrder()
    {
        var buffer = PushSequence([(byte)(InputFrame.Down | InputFrame.Right), InputFrame.Down, InputFrame.Punch]);
        var moves = new[] { Move("qcf", "236P") };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out _);

        Assert.False(found);
    }

    [Fact]
    public void DashMatchesOnSecondForwardTapWithoutButton()
    {
        var buffer = PushSequence([InputFrame.Right, 0, InputFrame.Right]);

        var found = CommandParser.TryFindDash(buffer, true, forward: true, out var judgeFramesAgo);

        Assert.True(found);
        Assert.Equal(0, judgeFramesAgo);
    }

    [Fact]
    public void HoldingForwardDoesNotTriggerDash()
    {
        var buffer = PushSequence([InputFrame.Right, InputFrame.Right, InputFrame.Right]);

        var found = CommandParser.TryFindDash(buffer, true, forward: true, out _);

        Assert.False(found);
    }

    [Fact]
    public void BufferedEntryMatchesEarlierButtonPress()
    {
        var buffer = PushSequence([0, 0, InputFrame.Punch]);
        var moves = new[] { Move("jab", "P") };

        var found = CommandParser.TryFindMove(buffer, true, moves, buffered: true, out var index);

        Assert.True(found);
        Assert.Equal(0, index);
    }

    [Fact]
    public void UnbufferedEntryDoesNotMatchEarlierButtonPress()
    {
        var buffer = PushSequence([InputFrame.Punch, 0, 0]);
        var moves = new[] { Move("jab", "P") };

        var found = CommandParser.TryFindMove(buffer, true, moves, buffered: false, out _);

        Assert.False(found);
    }

    [Fact]
    public void MoreDirectionsWinsOverFewerDirections()
    {
        var buffer = PushSequence([0, InputFrame.Right, (byte)(InputFrame.Right | InputFrame.Punch)]);
        var moves = new[] { Move("p", "P", 0), Move("6p", "6P", 1) };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out var index);

        Assert.True(found);
        Assert.Equal(1, index);
    }

    [Fact]
    public void MoreButtonsWinsWhenDirectionCountIsEqual()
    {
        var buffer = PushSequence([InputFrame.Guard, (byte)(InputFrame.Guard | InputFrame.Punch)]);
        var moves = new[] { Move("p", "P", 0), Move("throw", "P+G", 1) };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out var index);

        Assert.True(found);
        Assert.Equal(1, index);
    }

    [Fact]
    public void LowerMoveIndexWinsWhenDirectionsAndButtonsAreEqual()
    {
        var buffer = PushSequence([InputFrame.Punch]);
        var moves = new[] { Move("a", "P", 0), Move("b", "P", 1) };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out var index);

        Assert.True(found);
        Assert.Equal(0, index);
    }

    [Fact]
    public void NoMoveMatchesReturnsFalse()
    {
        var buffer = PushSequence([0]);
        var moves = new[] { Move("p", "P") };

        var found = CommandParser.TryFindMove(buffer, true, moves, false, out _);

        Assert.False(found);
    }
}
