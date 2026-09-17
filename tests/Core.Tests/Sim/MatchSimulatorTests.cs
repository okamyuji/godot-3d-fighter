using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class MatchSimulatorTests
{
    private static MoveData Jab() => new()
    {
        Name = "jab",
        Command = "P",
        Kind = MoveKind.Strike,
        Posture = Posture.Stand,
        Startup = 5,
        Active = 3,
        Recovery = 8,
        Tracking = 2,
        Height = HitHeight.High,
        Damage = 10,
        CounterDamage = 15,
        Hitstun = 12,
        CounterHitstun = 16,
        Blockstun = 6,
        Hitstop = 4,
        Knockdown = false,
        CounterKnockdown = false,
        HitsDown = false,
        Pushback = Fix16.FromDecimal(0.2m),
        Motion = [],
        Windows =
        [
            new HitWindow
            {
                From = 5,
                To = 7,
                Hit = [new HitCapsule(new Vec3Fix(Fix16.FromDecimal(0.5m), Fix16.FromDecimal(1m), Fix16.Zero), new Vec3Fix(Fix16.FromDecimal(0.9m), Fix16.FromDecimal(1m), Fix16.Zero), Fix16.FromDecimal(0.15m))],
                Hurt = [],
            },
        ],
    };

    private static MoveData Launcher() => new()
    {
        Name = "launcher",
        Command = "6K",
        Kind = MoveKind.Strike,
        Posture = Posture.Stand,
        Startup = 5,
        Active = 3,
        Recovery = 8,
        Tracking = 2,
        Height = HitHeight.Mid,
        Damage = 10,
        CounterDamage = 15,
        Hitstun = 12,
        CounterHitstun = 16,
        Blockstun = 6,
        Hitstop = 4,
        Knockdown = true,
        CounterKnockdown = true,
        HitsDown = false,
        Pushback = Fix16.FromDecimal(0.2m),
        Motion = [],
        Windows =
        [
            new HitWindow
            {
                From = 5,
                To = 7,
                Hit = [new HitCapsule(new Vec3Fix(Fix16.FromDecimal(0.5m), Fix16.FromDecimal(1m), Fix16.Zero), new Vec3Fix(Fix16.FromDecimal(0.9m), Fix16.FromDecimal(1m), Fix16.Zero), Fix16.FromDecimal(0.15m))],
                Hurt = [],
            },
        ],
    };

    private static CharacterData Character() => new()
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
        Moves = [Jab(), Launcher()],
    };

    private static StageData Stage() => new()
    {
        Name = "default",
        Shape = RingShape.Square,
        Size = Fix16.FromDecimal(5m),
        Edges = [EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut],
        StartDistance = Fix16.FromDecimal(2m),
    };

    private static StageData WallStage() => new()
    {
        Name = "wall",
        Shape = RingShape.Square,
        Size = Fix16.FromDecimal(5m),
        Edges = [EdgeKind.Wall, EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut],
        StartDistance = Fix16.FromDecimal(2m),
    };

    private static Rules Rules() => new()
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
        Training = false,
    };

    private static MatchContext Context() => new()
    {
        Characters = [Character(), Character()],
        Stage = Stage(),
        Rules = Rules(),
    };

    private static MatchState EnterFight(MatchContext context)
    {
        var state = MatchSimulator.CreateMatch(context);
        for (var i = 0; i < context.Rules.IntroFrames; i++)
        {
            state = MatchSimulator.Step(state, default, default, context);
        }

        return state;
    }

    [Fact]
    public void CreateMatchStartsInIntroWithFullHealth()
    {
        var state = MatchSimulator.CreateMatch(Context());

        Assert.Equal(RoundPhase.Intro, state.Phase);
        Assert.Equal(100, state.GetPlayer(0).Health);
        Assert.Equal(100, state.GetPlayer(1).Health);
        Assert.Equal(1u, state.RngState);
    }

    [Fact]
    public void IntroAdvancesToFightAfterIntroFrames()
    {
        var context = Context();
        var state = EnterFight(context);

        Assert.Equal(RoundPhase.Fight, state.Phase);
    }

    [Fact]
    public void ThrowsOnNonNormalizedInput()
    {
        var context = Context();
        var state = EnterFight(context);

        Assert.Throws<ArgumentException>(() =>
            MatchSimulator.Step(state, new InputFrame((byte)(InputFrame.Up | InputFrame.Down)), default, context));
    }

    [Fact]
    public void WalkingForwardMovesTowardTheOpponent()
    {
        var context = Context();
        var state = EnterFight(context);
        var startX = state.GetPlayer(0).Position.X;

        state = MatchSimulator.Step(state, new InputFrame(InputFrame.Right), default, context);

        Assert.True(state.GetPlayer(0).Position.X.Raw > startX.Raw);
    }

    [Fact]
    public void IdenticalInputSequencesProduceIdenticalStatesAndHashes()
    {
        var context = Context();
        InputFrame[] p1Inputs = [new(InputFrame.Right), new(InputFrame.Right), new(InputFrame.Punch), default, default];
        InputFrame[] p2Inputs = [default, default, default, default, default];

        MatchState Run()
        {
            var state = EnterFight(context);
            for (var i = 0; i < p1Inputs.Length; i++)
            {
                state = MatchSimulator.Step(state, p1Inputs[i], p2Inputs[i], context);
            }

            return state;
        }

        var a = Run();
        var b = Run();

        Assert.Equal(a, b);
        Assert.Equal(StateHash.Compute(a), StateHash.Compute(b));
    }

    [Fact]
    public void JabConnectingDealsDamageAndPutsDefenderInHitstun()
    {
        var context = Context();
        var state = EnterFight(context);

        // P1をP2に近づける（P2は+X側）。
        for (var i = 0; i < 20 && state.GetPlayer(0).Position.HorizontalDistanceSquared(state.GetPlayer(1).Position) > 0; i++)
        {
            state = MatchSimulator.Step(state, new InputFrame(InputFrame.Right), default, context);
        }

        state = MatchSimulator.Step(state, new InputFrame(InputFrame.Punch), default, context);
        var startingHealth = state.GetPlayer(1).Health;

        // 発生5フレーム待つ。
        for (var i = 0; i < 6; i++)
        {
            state = MatchSimulator.Step(state, default, default, context);
        }

        Assert.True(state.GetPlayer(1).Health < startingHealth);
        Assert.Equal(StateKind.Hitstun, state.GetPlayer(1).State);
    }

    [Fact]
    public void ZeroHealthEndsTheRoundWithAWinner()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(1, state.GetPlayer(1) with { Health = 0 });

        state = MatchSimulator.Step(state, default, default, context);

        Assert.Equal(RoundPhase.RoundEnd, state.Phase);
        Assert.Equal(RoundEndReason.KnockOut, state.LastRoundReason);
        Assert.Equal(1, state.LastRoundWinners);
    }

    [Fact]
    public void ResetPositionsRestoresHealthAndStartingPositions()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with { Health = 10, State = StateKind.Hitstun });
        state = state.WithPlayer(1, state.GetPlayer(1) with { Health = 5, State = StateKind.Down });

        state = MatchSimulator.ResetPositions(state, context);

        Assert.Equal(context.Rules.InitialHealth, state.GetPlayer(0).Health);
        Assert.Equal(context.Rules.InitialHealth, state.GetPlayer(1).Health);
        Assert.Equal(StateKind.Idle, state.GetPlayer(0).State);
        Assert.Equal(StateKind.Idle, state.GetPlayer(1).State);
        Assert.True(state.GetPlayer(0).Position.X.Raw < state.GetPlayer(1).Position.X.Raw);
    }

    [Fact]
    public void TrainingModeDoesNotEndTheRoundOnZeroHealth()
    {
        var context = Context();
        var trainingContext = new MatchContext
        {
            Characters = context.Characters,
            Stage = context.Stage,
            Rules = new Rules
            {
                RoundTimeSeconds = context.Rules.RoundTimeSeconds,
                RoundsToWin = context.Rules.RoundsToWin,
                MaxRounds = context.Rules.MaxRounds,
                InitialHealth = context.Rules.InitialHealth,
                IntroFrames = context.Rules.IntroFrames,
                RoundEndFrames = context.Rules.RoundEndFrames,
                TechFrames = context.Rules.TechFrames,
                TechRecoveryFrames = context.Rules.TechRecoveryFrames,
                DownMinFrames = context.Rules.DownMinFrames,
                DownMaxFrames = context.Rules.DownMaxFrames,
                RiseFrames = context.Rules.RiseFrames,
                ThrowEscapeFrames = context.Rules.ThrowEscapeFrames,
                ThrowEscapeRecoveryFrames = context.Rules.ThrowEscapeRecoveryFrames,
                ThrowEscapeDistance = context.Rules.ThrowEscapeDistance,
                WallStunFrames = context.Rules.WallStunFrames,
                Seed = context.Rules.Seed,
                Training = true,
            },
        };
        var state = EnterFight(trainingContext);
        state = state.WithPlayer(1, state.GetPlayer(1) with { Health = 0, State = StateKind.Hitstun, StunFrames = 1 });

        state = MatchSimulator.Step(state, default, default, trainingContext);

        Assert.Equal(RoundPhase.Fight, state.Phase);
    }

    [Fact]
    public void KnockdownMoveDealsDamageAndKnocksDefenderDown()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with
        {
            Position = new Vec3Fix(Fix16.Zero, Fix16.Zero, Fix16.Zero),
            State = StateKind.Attack,
            CurrentMove = 1,
            StateFrame = 4,
        });
        state = state.WithPlayer(1, state.GetPlayer(1) with
        {
            Position = new Vec3Fix(Fix16.FromDecimal(1m), Fix16.Zero, Fix16.Zero),
        });
        var startingHealth = state.GetPlayer(1).Health;

        state = MatchSimulator.Step(state, default, default, context);

        Assert.True(state.GetPlayer(1).Health < startingHealth);
        Assert.Equal(StateKind.Down, state.GetPlayer(1).State);
    }

    [Fact]
    public void HitPushedIntoAWallCausesWallStun()
    {
        var context = new MatchContext { Characters = [Character(), Character()], Stage = WallStage(), Rules = Rules() };
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with
        {
            Position = new Vec3Fix(Fix16.FromDecimal(4.3m), Fix16.Zero, Fix16.Zero),
            State = StateKind.Attack,
            CurrentMove = 0,
            StateFrame = 4,
        });
        state = state.WithPlayer(1, state.GetPlayer(1) with
        {
            Position = new Vec3Fix(Fix16.FromDecimal(4.6m), Fix16.Zero, Fix16.Zero),
        });

        state = MatchSimulator.Step(state, default, default, context);

        Assert.Equal(StateKind.WallStun, state.GetPlayer(1).State);
        Assert.True(state.GetPlayer(1).Flags.HasFlag(PlayerFlags.WallHitTaken));
    }

    [Fact]
    public void DownPlayerRollsInOnFreshUpInput()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with { State = StateKind.Down, StateFrame = 25 });

        state = MatchSimulator.Step(state, new InputFrame(InputFrame.Up), default, context);

        Assert.Equal(StateKind.RollIn, state.GetPlayer(0).State);
    }

    [Fact]
    public void DownPlayerRollsOutOnFreshDownInput()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with { State = StateKind.Down, StateFrame = 25 });

        state = MatchSimulator.Step(state, new InputFrame(InputFrame.Down), default, context);

        Assert.Equal(StateKind.RollOut, state.GetPlayer(0).State);
    }

    [Fact]
    public void DownPlayerRisesOnFreshGuardPress()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with { State = StateKind.Down, StateFrame = 25 });

        state = MatchSimulator.Step(state, new InputFrame(InputFrame.Guard), default, context);

        Assert.Equal(StateKind.Rise, state.GetPlayer(0).State);
    }

    [Fact]
    public void DownPlayerStaysDownBeforeDownMinFrames()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with { State = StateKind.Down, StateFrame = 5 });

        state = MatchSimulator.Step(state, new InputFrame(InputFrame.Up), default, context);

        Assert.Equal(StateKind.Down, state.GetPlayer(0).State);
    }

    [Fact]
    public void DownPlayerTechQueuedWithoutDirectionEntersTech()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with { State = StateKind.Down, StateFrame = 3 });

        state = MatchSimulator.Step(
            state, new InputFrame((byte)(InputFrame.Punch | InputFrame.Kick | InputFrame.Guard)), default, context);

        Assert.Equal(StateKind.Tech, state.GetPlayer(0).State);
    }

    [Fact]
    public void GuardInputEntersGuardState()
    {
        var context = Context();
        var state = EnterFight(context);

        state = MatchSimulator.Step(state, new InputFrame(InputFrame.Guard), default, context);

        Assert.Equal(StateKind.Guard, state.GetPlayer(0).State);
    }

    [Fact]
    public void GuardWithDownInputEntersCrouchGuardState()
    {
        var context = Context();
        var state = EnterFight(context);

        state = MatchSimulator.Step(state, new InputFrame((byte)(InputFrame.Guard | InputFrame.Down)), default, context);

        Assert.Equal(StateKind.CrouchGuard, state.GetPlayer(0).State);
    }

    [Fact]
    public void DownInputAloneEntersCrouchState()
    {
        var context = Context();
        var state = EnterFight(context);

        state = MatchSimulator.Step(state, new InputFrame(InputFrame.Down), default, context);

        Assert.Equal(StateKind.Crouch, state.GetPlayer(0).State);
    }

    [Fact]
    public void BackwardInputEntersBackWalkState()
    {
        var context = Context();
        var state = EnterFight(context);

        state = MatchSimulator.Step(state, new InputFrame(InputFrame.Left), default, context);

        Assert.Equal(StateKind.BackWalk, state.GetPlayer(0).State);
    }

    [Theory]
    [InlineData(StateKind.Dash, (ushort)16)]
    [InlineData(StateKind.Backdash, (ushort)18)]
    [InlineData(StateKind.SidestepIn, (ushort)18)]
    [InlineData(StateKind.SidestepOut, (ushort)18)]
    [InlineData(StateKind.ThrowEscape, (ushort)20)]
    [InlineData(StateKind.Tech, (ushort)20)]
    public void TimedStateReturnsToIdleAfterItsDuration(StateKind state, ushort frames)
    {
        var context = Context();
        var matchState = EnterFight(context);
        matchState = matchState.WithPlayer(0, matchState.GetPlayer(0) with { State = state, StateFrame = frames });

        matchState = MatchSimulator.Step(matchState, default, default, context);

        Assert.Equal(StateKind.Idle, matchState.GetPlayer(0).State);
    }

    [Fact]
    public void HitstunEndsWhenStunFramesReachZero()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with { State = StateKind.Hitstun, StunFrames = 1 });

        state = MatchSimulator.Step(state, default, default, context);

        Assert.Equal(StateKind.Idle, state.GetPlayer(0).State);
    }

    [Fact]
    public void RollInEndsInRiseAfterRollFrames()
    {
        var context = Context();
        var state = EnterFight(context);
        state = state.WithPlayer(0, state.GetPlayer(0) with { State = StateKind.RollIn, StateFrame = 24 });

        state = MatchSimulator.Step(state, default, default, context);

        Assert.Equal(StateKind.Rise, state.GetPlayer(0).State);
    }
}
