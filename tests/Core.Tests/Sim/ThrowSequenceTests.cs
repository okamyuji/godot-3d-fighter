using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

/// <summary>投げの成立から終わりまでをMatchSimulator.Stepで通して確かめる（ADR-0019、02-match-rules.mdの「投げの入力と終わり」）。</summary>
public sealed class ThrowSequenceTests
{
    private const ushort Startup = 6;
    private const ushort Active = 1;
    private const ushort Recovery = 20;
    private const int Damage = 25;

    private static MoveData ThrowMove(bool knockdown = true, ushort hitstun = 0) => new()
    {
        Name = "throw",
        Command = "P+G",
        Kind = MoveKind.Throw,
        Posture = Posture.Stand,
        Startup = Startup,
        Active = Active,
        Recovery = Recovery,
        Tracking = 0,
        Height = HitHeight.Mid,
        Damage = Damage,
        CounterDamage = Damage,
        Hitstun = hitstun,
        CounterHitstun = hitstun,
        Blockstun = 0,
        Hitstop = 0,
        Knockdown = knockdown,
        CounterKnockdown = knockdown,
        HitsDown = false,
        Pushback = Fix16.Zero,
        Motion = [],
        Windows = [],
        ThrowRange = Fix16.FromDecimal(1.0m),
        ThrowSide = ThrowSide.Front,
        ThrowEndOffset = new Vec3Fix(Fix16.FromInt(1), Fix16.Zero, Fix16.Zero),
    };

    private static CharacterData Character(MoveData move) => new()
    {
        Name = "box",
        WalkSpeed = Fix16.FromDecimal(0.05m),
        BackWalkSpeed = Fix16.FromDecimal(0.04m),
        DashSpeed = Fix16.FromDecimal(0.12m),
        DashFrames = 16,
        BackdashSpeed = Fix16.FromDecimal(0.1m),
        BackdashFrames = 18,
        DashCancelFrames = 4,
        SidestepSpeed = Fix16.FromDecimal(0.06m),
        SidestepFrames = 18,
        SidestepCancelFrame = 10,
        RollSpeed = Fix16.FromDecimal(0.06m),
        RollFrames = 24,
        BodyRadius = Fix16.FromDecimal(0.3m),
        StandHurtCapsules = [new HitCapsule(new Vec3Fix(Fix16.Zero, Fix16.FromDecimal(0.2m), Fix16.Zero), new Vec3Fix(Fix16.Zero, Fix16.FromDecimal(1.5m), Fix16.Zero), Fix16.FromDecimal(0.3m))],
        CrouchHurtCapsules = [new HitCapsule(new Vec3Fix(Fix16.Zero, Fix16.FromDecimal(0.3m), Fix16.Zero), new Vec3Fix(Fix16.Zero, Fix16.FromDecimal(0.8m), Fix16.Zero), Fix16.FromDecimal(0.35m))],
        DownHurtCapsules = [new HitCapsule(new Vec3Fix(Fix16.FromDecimal(-0.8m), Fix16.FromDecimal(0.15m), Fix16.Zero), new Vec3Fix(Fix16.FromDecimal(0.8m), Fix16.FromDecimal(0.15m), Fix16.Zero), Fix16.FromDecimal(0.2m))],
        Moves = [move],
    };

    private static Rules Rules(bool training) => new()
    {
        RoundTimeSeconds = 60,
        RoundsToWin = 2,
        MaxRounds = 5,
        InitialHealth = 100,
        IntroFrames = 3,
        RoundEndFrames = 3,
        TechFrames = 6,
        TechRecoveryFrames = 20,
        DownMinFrames = 20,
        DownMaxFrames = 60,
        RiseFrames = 24,
        ThrowEscapeFrames = 10,
        ThrowEscapeRecoveryFrames = 20,
        ThrowEscapeDistance = Fix16.FromDecimal(1.2m),
        WallStunFrames = 40,
        Seed = 1,
        Training = training,
    };

    private static MatchContext Context(MoveData? move = null, bool training = false) => new()
    {
        Characters = [Character(move ?? ThrowMove()), Character(move ?? ThrowMove())],
        Stage = new StageData
        {
            Name = "default",
            Shape = RingShape.Square,
            Size = Fix16.FromDecimal(5m),
            Edges = [EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut],
            StartDistance = Fix16.FromDecimal(2m),
        },
        Rules = Rules(training),
    };

    /// <summary>P1を原点、P2を+X側0.8mに置き、thrower側を投げの発生1フレーム前のAttackにする。次のStepで投げが成立する。</summary>
    private static MatchState Arrange(MatchContext context, int thrower = 0, ushort startupFramesBefore = 1)
    {
        var state = MatchSimulator.CreateMatch(context);
        for (var i = 0; i < context.Rules.IntroFrames; i++)
        {
            state = MatchSimulator.Step(state, default, default, context);
        }

        state = state.WithPlayer(0, state.GetPlayer(0) with { Position = new Vec3Fix(Fix16.Zero, Fix16.Zero, Fix16.Zero) });
        state = state.WithPlayer(1, state.GetPlayer(1) with { Position = new Vec3Fix(Fix16.FromDecimal(0.8m), Fix16.Zero, Fix16.Zero) });
        state = state.WithPlayer(thrower, state.GetPlayer(thrower) with
        {
            State = StateKind.Attack,
            CurrentMove = 0,
            StateFrame = (ushort)(Startup - startupFramesBefore),
        });
        return state;
    }

    private static MatchState StepNeutral(MatchState state, MatchContext context, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            state = MatchSimulator.Step(state, default, default, context);
        }

        return state;
    }

    private static InputFrame PunchGuard => new((byte)(InputFrame.Punch | InputFrame.Guard));

    [Fact]
    public void ThrowSuccessPutsThrowerInThrowingAndVictimInThrown()
    {
        var context = Context();
        var state = Arrange(context);

        state = MatchSimulator.Step(state, default, default, context);

        var thrower = state.GetPlayer(0);
        var victim = state.GetPlayer(1);
        Assert.Equal(StateKind.Throwing, thrower.State);
        Assert.Equal(0, thrower.StateFrame);
        Assert.Equal(StateKind.Thrown, victim.State);
        Assert.Equal(0, victim.StateFrame);
        Assert.Equal(new Angle16(unchecked((ushort)(thrower.Facing.Value + 32768))), victim.Facing);
        Assert.Equal(Limits.NoMove, victim.CurrentMove);
        Assert.Equal(100, victim.Health);
    }

    [Fact]
    public void VictimStaysThrownAndUnhurtUntilTheFrameBeforeThrowEscapeFrames()
    {
        var context = Context();
        var state = Arrange(context);

        state = StepNeutral(state, context, 1 + context.Rules.ThrowEscapeFrames - 1);

        Assert.Equal(StateKind.Thrown, state.GetPlayer(1).State);
        Assert.Equal(StateKind.Throwing, state.GetPlayer(0).State);
        Assert.Equal(100, state.GetPlayer(1).Health);
    }

    [Fact]
    public void ThrowFinishesWithDamageEndOffsetFacingAndKnockdown()
    {
        var context = Context();
        var state = Arrange(context);

        state = StepNeutral(state, context, 1 + context.Rules.ThrowEscapeFrames);

        var thrower = state.GetPlayer(0);
        var victim = state.GetPlayer(1);
        Assert.Equal(100 - Damage, victim.Health);
        Assert.Equal(StateKind.Down, victim.State);
        Assert.Equal(0, victim.StateFrame);
        Assert.Equal(HitKind.Thrown, victim.LastHitKind);
        Assert.Equal(Fix16.FromInt(1).Raw, victim.Position.X.Raw);
        Assert.Equal(0, victim.Position.Z.Raw);
        Assert.Equal(new Angle16(32768), victim.Facing);
        Assert.Equal(StateKind.Attack, thrower.State);
        Assert.Equal(Startup + Active, thrower.StateFrame);
        Assert.Equal(0, thrower.CurrentMove);
    }

    [Fact]
    public void ThrowWithoutKnockdownPutsVictimInHitstun()
    {
        var context = Context(ThrowMove(knockdown: false, hitstun: 15));
        var state = Arrange(context);

        state = StepNeutral(state, context, 1 + context.Rules.ThrowEscapeFrames);

        Assert.Equal(StateKind.Hitstun, state.GetPlayer(1).State);
        Assert.Equal(15, state.GetPlayer(1).StunFrames);
        Assert.Equal(100 - Damage, state.GetPlayer(1).Health);
    }

    [Fact]
    public void ThrowerReturnsToIdleAfterRecovery()
    {
        var context = Context();
        var state = Arrange(context);

        state = StepNeutral(state, context, 1 + context.Rules.ThrowEscapeFrames + Recovery);

        Assert.Equal(StateKind.Idle, state.GetPlayer(0).State);
        Assert.Equal(Limits.NoMove, state.GetPlayer(0).CurrentMove);
    }

    [Fact]
    public void ThrowFromP2MirrorsThrowFromP1()
    {
        var context = Context();
        var state = Arrange(context, thrower: 1);

        state = StepNeutral(state, context, 1 + context.Rules.ThrowEscapeFrames);

        var victim = state.GetPlayer(0);
        Assert.Equal(StateKind.Down, victim.State);
        Assert.Equal(100 - Damage, victim.Health);
        Assert.Equal(Fix16.FromDecimal(0.8m).Raw - Fix16.FromInt(1).Raw, victim.Position.X.Raw);
        Assert.Equal(new Angle16(0), victim.Facing);
        Assert.Equal(StateKind.Attack, state.GetPlayer(1).State);
        Assert.Equal(Startup + Active, state.GetPlayer(1).StateFrame);
    }

    [Fact]
    public void PunchAndGuardDuringThrownEscapesAndSeparatesToThrowEscapeDistance()
    {
        var context = Context();
        var state = Arrange(context);
        state = MatchSimulator.Step(state, default, default, context);

        state = MatchSimulator.Step(state, default, PunchGuard, context);

        Assert.Equal(StateKind.ThrowEscape, state.GetPlayer(0).State);
        Assert.Equal(StateKind.ThrowEscape, state.GetPlayer(1).State);
        Assert.Equal(0, state.GetPlayer(1).StateFrame);
        Assert.Equal(100, state.GetPlayer(1).Health);
        Assert.Equal(Limits.NoMove, state.GetPlayer(0).CurrentMove);
        Assert.Equal(
            context.Rules.ThrowEscapeDistance.RawSquared(),
            state.GetPlayer(0).Position.HorizontalDistanceSquared(state.GetPlayer(1).Position));
    }

    [Fact]
    public void EscapeOnTheFrameThrowingReachesThrowEscapeFramesStillEscapes()
    {
        var context = Context();
        var state = Arrange(context);
        state = StepNeutral(state, context, 1 + context.Rules.ThrowEscapeFrames - 1);

        state = MatchSimulator.Step(state, default, PunchGuard, context);

        Assert.Equal(StateKind.ThrowEscape, state.GetPlayer(1).State);
        Assert.Equal(100, state.GetPlayer(1).Health);
    }

    [Fact]
    public void WrongButtonStartsTheGraceAndLaterPunchGuardCannotEscape()
    {
        var context = Context();
        var state = Arrange(context);
        state = MatchSimulator.Step(state, default, default, context);

        state = MatchSimulator.Step(state, default, new InputFrame(InputFrame.Kick), context);
        state = MatchSimulator.Step(state, default, default, context);
        Assert.False(state.GetPlayer(1).Flags.HasFlag(PlayerFlags.ThrowEscapeTried));
        state = MatchSimulator.Step(state, default, default, context);
        Assert.True(state.GetPlayer(1).Flags.HasFlag(PlayerFlags.ThrowEscapeTried));
        Assert.Equal(StateKind.Thrown, state.GetPlayer(1).State);

        state = MatchSimulator.Step(state, default, PunchGuard, context);

        Assert.Equal(StateKind.Thrown, state.GetPlayer(1).State);
        state = StepNeutral(state, context, context.Rules.ThrowEscapeFrames - 4);
        Assert.Equal(StateKind.Down, state.GetPlayer(1).State);
        Assert.Equal(100 - Damage, state.GetPlayer(1).Health);
    }

    [Fact]
    public void GuardAloneIsNotAnEscapeOrigin()
    {
        var context = Context();
        var state = Arrange(context);
        state = MatchSimulator.Step(state, default, default, context);

        state = MatchSimulator.Step(state, default, new InputFrame(InputFrame.Guard), context);
        state = StepNeutral(state, context, 2);
        Assert.False(state.GetPlayer(1).Flags.HasFlag(PlayerFlags.ThrowEscapeTried));
        state = MatchSimulator.Step(state, default, PunchGuard, context);

        Assert.Equal(StateKind.ThrowEscape, state.GetPlayer(1).State);
    }

    [Fact]
    public void PunchGuardBufferedBeforeSuccessEscapesOnTheSuccessFrame()
    {
        var context = Context();
        var state = Arrange(context, startupFramesBefore: 4);

        state = MatchSimulator.Step(state, default, PunchGuard, context);
        state = StepNeutral(state, context, 2);
        Assert.Equal(StateKind.Attack, state.GetPlayer(0).State);
        state = MatchSimulator.Step(state, default, default, context);

        Assert.Equal(StateKind.ThrowEscape, state.GetPlayer(0).State);
        Assert.Equal(StateKind.ThrowEscape, state.GetPlayer(1).State);
    }

    [Fact]
    public void TrainingThrowLeavesOneHealthAndTheRoundContinues()
    {
        var context = Context(training: true);
        var state = Arrange(context);
        state = state.WithPlayer(1, state.GetPlayer(1) with { Health = 10 });

        state = StepNeutral(state, context, 1 + context.Rules.ThrowEscapeFrames);

        Assert.Equal(1, state.GetPlayer(1).Health);
        Assert.Equal(StateKind.Down, state.GetPlayer(1).State);
        Assert.Equal(RoundPhase.Fight, state.Phase);
    }

    [Fact]
    public void ThrowKnockoutEndsTheRoundForTheThrower()
    {
        var context = Context();
        var state = Arrange(context);
        state = state.WithPlayer(1, state.GetPlayer(1) with { Health = 20 });

        state = StepNeutral(state, context, 1 + context.Rules.ThrowEscapeFrames);

        Assert.Equal(RoundPhase.RoundEnd, state.Phase);
        Assert.Equal(RoundEndReason.KnockOut, state.LastRoundReason);
        Assert.Equal(1, state.LastRoundWinners);
    }
}
