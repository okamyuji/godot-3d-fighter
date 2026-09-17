using System.Runtime.CompilerServices;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.State;

public sealed class MatchStateLayoutTests
{
    [Fact]
    public void PlayerStateIs105Bytes()
    {
        Assert.Equal(105, Unsafe.SizeOf<PlayerState>());
    }

    [Fact]
    public void MatchStateIs230Bytes()
    {
        Assert.Equal(230, Unsafe.SizeOf<MatchState>());
    }

    private static MatchState Baseline()
    {
        var state = default(MatchState);
        state = state.WithPlayer(0, new PlayerState { Slot = 0 }).WithPlayer(1, new PlayerState { Slot = 1 });
        state = state.WithRoundWins(0, 1).WithRoundWins(1, 2);
        state.FrameNumber = 10;
        state.RngState = 20;
        state.RoundTimerFrames = 30;
        state.PhaseFrames = 40;
        state.CameraYaw = new Angle16(50);
        state.RoundNumber = 1;
        state.Phase = RoundPhase.Fight;
        state.LastRoundReason = RoundEndReason.None;
        state.LastRoundWinners = 0;
        return state;
    }

    [Fact]
    public void EqualsIsTrueForIdenticalStates() => Assert.Equal(Baseline(), Baseline());

    [Fact]
    public void EqualsIsFalseWhenAPlayerDiffers() =>
        Assert.NotEqual(Baseline(), Baseline().WithPlayer(0, new PlayerState { Slot = 0, Health = 1 }));

    [Fact]
    public void EqualsIsFalseWhenRoundWinsDiffer() =>
        Assert.NotEqual(Baseline(), Baseline().WithRoundWins(1, 9));

    [Fact]
    public void EqualsIsFalseWhenFrameNumberDiffers()
    {
        var other = Baseline();
        other.FrameNumber = 999;
        Assert.NotEqual(Baseline(), other);
    }

    [Fact]
    public void EqualsIsFalseWhenRngStateDiffers()
    {
        var other = Baseline();
        other.RngState = 999;
        Assert.NotEqual(Baseline(), other);
    }

    [Fact]
    public void EqualsIsFalseWhenRoundTimerFramesDiffers()
    {
        var other = Baseline();
        other.RoundTimerFrames = 999;
        Assert.NotEqual(Baseline(), other);
    }

    [Fact]
    public void EqualsIsFalseWhenPhaseFramesDiffers()
    {
        var other = Baseline();
        other.PhaseFrames = 999;
        Assert.NotEqual(Baseline(), other);
    }

    [Fact]
    public void EqualsIsFalseWhenCameraYawDiffers()
    {
        var other = Baseline();
        other.CameraYaw = new Angle16(999);
        Assert.NotEqual(Baseline(), other);
    }

    [Fact]
    public void EqualsIsFalseWhenRoundNumberDiffers()
    {
        var other = Baseline();
        other.RoundNumber = 9;
        Assert.NotEqual(Baseline(), other);
    }

    [Fact]
    public void EqualsIsFalseWhenPhaseDiffers()
    {
        var other = Baseline();
        other.Phase = RoundPhase.MatchEnd;
        Assert.NotEqual(Baseline(), other);
    }

    [Fact]
    public void EqualsIsFalseWhenLastRoundReasonDiffers()
    {
        var other = Baseline();
        other.LastRoundReason = RoundEndReason.KnockOut;
        Assert.NotEqual(Baseline(), other);
    }

    [Fact]
    public void EqualsIsFalseWhenLastRoundWinnersDiffers()
    {
        var other = Baseline();
        other.LastRoundWinners = 3;
        Assert.NotEqual(Baseline(), other);
    }

    [Fact]
    public void EqualsAgainstNonMatchStateObjectIsFalse() => Assert.False(Baseline().Equals((object)"not a match state"));

    [Fact]
    public void OperatorsMatchEquals()
    {
        var a = Baseline();
        var b = Baseline();
        var c = a.WithRoundWins(0, 9);
        Assert.True(a == b);
        Assert.True(a != c);
    }
}
