using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class RoundResolverTests
{
    private static StageData Square() => new()
    {
        Name = "s",
        Shape = RingShape.Square,
        Size = Fix16.FromDecimal(5m),
        Edges = [EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut],
        StartDistance = Fix16.FromDecimal(2m),
    };

    private static PlayerState WithHealth(int health, decimal x = 0m) => new()
    {
        Health = health,
        Position = new Vec3Fix(Fix16.FromDecimal(x), Fix16.Zero, Fix16.Zero),
    };

    [Fact]
    public void NoOutcomeWhenBothAliveAndTimeRemains()
    {
        var outcome = RoundResolver.CheckRoundEnd(WithHealth(100), WithHealth(100), Square(), roundTimerFrames: 60);

        Assert.False(outcome.Decided);
    }

    [Fact]
    public void P1KnockOutGivesP2TheWin()
    {
        var outcome = RoundResolver.CheckRoundEnd(WithHealth(0), WithHealth(100), Square(), roundTimerFrames: 60);

        Assert.True(outcome.Decided);
        Assert.Equal(RoundEndReason.KnockOut, outcome.Reason);
        Assert.False(outcome.P1Won);
        Assert.True(outcome.P2Won);
    }

    [Fact]
    public void P1RingOutGivesP2TheWin()
    {
        var outcome = RoundResolver.CheckRoundEnd(WithHealth(50, x: 10m), WithHealth(50), Square(), roundTimerFrames: 60);

        Assert.True(outcome.Decided);
        Assert.Equal(RoundEndReason.RingOut, outcome.Reason);
        Assert.False(outcome.P1Won);
        Assert.True(outcome.P2Won);
    }

    [Fact]
    public void SimultaneousKnockOutGivesBothTheWin()
    {
        var outcome = RoundResolver.CheckRoundEnd(WithHealth(0), WithHealth(0), Square(), roundTimerFrames: 60);

        Assert.True(outcome.Decided);
        Assert.Equal(RoundEndReason.KnockOut, outcome.Reason);
        Assert.True(outcome.P1Won);
        Assert.True(outcome.P2Won);
    }

    [Fact]
    public void KnockOutTakesPriorityOverRingOutAsTheReason()
    {
        var outcome = RoundResolver.CheckRoundEnd(WithHealth(0), WithHealth(50, x: 10m), Square(), roundTimerFrames: 60);

        Assert.Equal(RoundEndReason.KnockOut, outcome.Reason);
    }

    [Fact]
    public void TimeUpGivesTheHealthierPlayerTheWin()
    {
        var outcome = RoundResolver.CheckRoundEnd(WithHealth(80), WithHealth(50), Square(), roundTimerFrames: 0);

        Assert.True(outcome.Decided);
        Assert.Equal(RoundEndReason.TimeUp, outcome.Reason);
        Assert.True(outcome.P1Won);
        Assert.False(outcome.P2Won);
    }

    [Fact]
    public void TimeUpWithEqualHealthGivesBothTheWin()
    {
        var outcome = RoundResolver.CheckRoundEnd(WithHealth(50), WithHealth(50), Square(), roundTimerFrames: 0);

        Assert.True(outcome.Decided);
        Assert.Equal(RoundEndReason.TimeUp, outcome.Reason);
        Assert.True(outcome.P1Won);
        Assert.True(outcome.P2Won);
    }

    [Theory]
    [InlineData(0, 0, 5, false)]
    [InlineData(2, 0, 5, true)]
    [InlineData(2, 1, 5, true)]
    [InlineData(1, 2, 5, true)]
    [InlineData(1, 1, 5, false)]
    [InlineData(1, 1, 5, true, 5)]
    public void MatchIsOverWhenAPlayerReachesRoundsToWinOrMaxRoundsIsHit(byte p1Wins, byte p2Wins, byte maxRounds, bool expected, byte roundNumber = 3)
    {
        var result = RoundResolver.IsMatchOver(p1Wins, p2Wins, roundNumber, roundsToWin: 2, maxRounds: maxRounds);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void MatchEndsAtMaxRoundsEvenWithoutAWinner()
    {
        var result = RoundResolver.IsMatchOver(p1Wins: 1, p2Wins: 1, roundNumber: 5, roundsToWin: 2, maxRounds: 5);

        Assert.True(result);
    }

    [Fact]
    public void MatchContinuesWhenNeitherConditionIsMet()
    {
        var result = RoundResolver.IsMatchOver(p1Wins: 1, p2Wins: 0, roundNumber: 2, roundsToWin: 2, maxRounds: 5);

        Assert.False(result);
    }
}
