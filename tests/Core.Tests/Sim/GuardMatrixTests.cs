using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class GuardMatrixTests
{
    [Theory]
    [InlineData(HitHeight.High, StateKind.Guard, true)]
    [InlineData(HitHeight.High, StateKind.CrouchGuard, false)]
    [InlineData(HitHeight.High, StateKind.Idle, false)]
    [InlineData(HitHeight.High, StateKind.Crouch, false)]
    [InlineData(HitHeight.Mid, StateKind.Guard, true)]
    [InlineData(HitHeight.Mid, StateKind.CrouchGuard, false)]
    [InlineData(HitHeight.Mid, StateKind.Idle, false)]
    [InlineData(HitHeight.Mid, StateKind.Crouch, false)]
    [InlineData(HitHeight.Low, StateKind.Guard, false)]
    [InlineData(HitHeight.Low, StateKind.CrouchGuard, true)]
    [InlineData(HitHeight.Low, StateKind.Idle, false)]
    [InlineData(HitHeight.Low, StateKind.Crouch, false)]
    public void MatchesTheTwelveCombinations(HitHeight height, StateKind defenderState, bool expectedGuarded)
    {
        Assert.Equal(expectedGuarded, GuardMatrix.IsGuarded(height, defenderState));
    }

    [Theory]
    [InlineData(HitHeight.High, StateKind.Blockstun, true)]
    [InlineData(HitHeight.Mid, StateKind.Blockstun, true)]
    [InlineData(HitHeight.Low, StateKind.Blockstun, false)]
    [InlineData(HitHeight.High, StateKind.CrouchBlockstun, false)]
    [InlineData(HitHeight.Mid, StateKind.CrouchBlockstun, false)]
    [InlineData(HitHeight.Low, StateKind.CrouchBlockstun, true)]
    public void TreatsBlockstunAsTheGuardThatCausedIt(HitHeight height, StateKind defenderState, bool expectedGuarded)
    {
        Assert.Equal(expectedGuarded, GuardMatrix.IsGuarded(height, defenderState));
    }

    [Fact]
    public void HighNeverHitsCrouchingOrCrouchGuardingDefendersEvenWhenCapsulesOverlap()
    {
        Assert.False(GuardMatrix.CanHit(HitHeight.High, StateKind.Crouch));
        Assert.False(GuardMatrix.CanHit(HitHeight.High, StateKind.CrouchGuard));
        Assert.False(GuardMatrix.CanHit(HitHeight.High, StateKind.CrouchBlockstun));
    }

    [Fact]
    public void MidAndLowCanHitCrouchingDefenders()
    {
        Assert.True(GuardMatrix.CanHit(HitHeight.Mid, StateKind.Crouch));
        Assert.True(GuardMatrix.CanHit(HitHeight.Low, StateKind.Crouch));
    }
}
