using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class CpuPolicyTests
{
    private const int Cpu = 1;
    private const byte JabIndex = 0;
    private const byte LowKickIndex = 1;
    private const byte LauncherIndex = 2;
    private const byte ThrowIndex = 3;

    [Fact]
    public void ThrowsOnNullContext()
    {
        var state = default(MatchState);

        Assert.Throws<ArgumentNullException>(() => CpuPolicy.Decide(state, null!, Cpu));
    }

    [Theory]
    [InlineData(HitHeight.High, InputFrame.Guard)]
    [InlineData(HitHeight.Mid, InputFrame.Guard)]
    [InlineData(HitHeight.Low, InputFrame.Guard | InputFrame.Down)]
    public void GuardsByTheHeightOfTheOpponentsStrikeEvenOnADecisionFrame(HitHeight height, int expectedBits)
    {
        var context = Context();
        var moveIndex = height switch
        {
            HitHeight.High => JabIndex,
            HitHeight.Mid => LauncherIndex,
            _ => LowKickIndex,
        };
        var state = Attacking(InRange(context, frameNumber: 0), moveIndex);

        Assert.Equal(new InputFrame((byte)expectedBits), CpuPolicy.Decide(state, context, Cpu));
    }

    [Fact]
    public void DoesNotGuardAgainstAThrow()
    {
        var context = Context();
        var state = Attacking(InRange(context, frameNumber: 1), ThrowIndex);

        Assert.Equal(default, CpuPolicy.Decide(state, context, Cpu));
    }

    [Fact]
    public void DoesNotGuardWhenTheAttackerHasNoMove()
    {
        var context = Context();
        var state = Attacking(InRange(context, frameNumber: 1), Limits.NoMove);

        Assert.Equal(default, CpuPolicy.Decide(state, context, Cpu));
    }

    [Fact]
    public void WalksLeftTowardTheOpponentWhenFarOnTheRight()
    {
        var context = Context();
        var state = MatchSimulator.CreateMatch(context);

        Assert.Equal(new InputFrame(InputFrame.Left), CpuPolicy.Decide(state, context, Cpu));
    }

    [Fact]
    public void WalksRightTowardTheOpponentWhenFarOnTheLeft()
    {
        var context = Context();
        var state = Swapped(MatchSimulator.CreateMatch(context));

        Assert.Equal(new InputFrame(InputFrame.Right), CpuPolicy.Decide(state, context, Cpu));
    }

    [Fact]
    public void WalksTowardTheOpponentAsPlayerOne()
    {
        var context = Context();
        var state = MatchSimulator.CreateMatch(context);

        Assert.Equal(new InputFrame(InputFrame.Right), CpuPolicy.Decide(state, context, 0));
    }

    [Theory]
    [InlineData(0u, InputFrame.Punch)]
    [InlineData(8u, InputFrame.Kick)]
    [InlineData(16u, InputFrame.Left | InputFrame.Kick)]
    [InlineData(24u, InputFrame.Punch | InputFrame.Guard)]
    [InlineData(32u, InputFrame.Punch)]
    public void CyclesThroughJabLowKickLauncherAndThrowOnDecisionFrames(uint frameNumber, int expectedBits)
    {
        var context = Context();
        var state = InRange(context, frameNumber);

        Assert.Equal(new InputFrame((byte)expectedBits), CpuPolicy.Decide(state, context, Cpu));
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(7u)]
    [InlineData(9u)]
    public void WaitsBetweenDecisionFramesWhenInRange(uint frameNumber)
    {
        var context = Context();
        var state = InRange(context, frameNumber);

        Assert.Equal(default, CpuPolicy.Decide(state, context, Cpu));
    }

    [Fact]
    public void AttacksAtExactlyTheAttackDistance()
    {
        var context = Context();
        var state = AtDistance(MatchSimulator.CreateMatch(context), CpuPolicy.AttackDistance);

        Assert.Equal(new InputFrame(InputFrame.Punch), CpuPolicy.Decide(state, context, Cpu));
    }

    [Fact]
    public void WalksOneRawUnitBeyondTheAttackDistance()
    {
        var context = Context();
        var state = AtDistance(MatchSimulator.CreateMatch(context), new Fix16(CpuPolicy.AttackDistance.Raw + 1));

        Assert.Equal(new InputFrame(InputFrame.Left), CpuPolicy.Decide(state, context, Cpu));
    }

    [Fact]
    public void KnocksOutAnIdleOpponentWithNormalizedInputs()
    {
        var context = Context(initialHealth: 24);
        var state = MatchSimulator.CreateMatch(context);

        for (var i = 0; i < 600 && state.Phase != RoundPhase.RoundEnd; i++)
        {
            var cpu = CpuPolicy.Decide(state, context, Cpu);
            Assert.True(cpu.IsNormalized);
            state = MatchSimulator.Step(state, default, cpu, context);
        }

        Assert.Equal(RoundPhase.RoundEnd, state.Phase);
        Assert.Equal(RoundEndReason.KnockOut, state.LastRoundReason);
        Assert.Equal(2, state.LastRoundWinners);
    }

    private static MatchState InRange(MatchContext context, uint frameNumber)
    {
        var state = AtDistance(MatchSimulator.CreateMatch(context), Fix16.FromDecimal(0.8m));
        state.FrameNumber = frameNumber;
        return state;
    }

    /// <summary>P1を原点、P2を+X側のdistanceに置く。画面の左右は変わらない。</summary>
    private static MatchState AtDistance(MatchState state, Fix16 distance)
    {
        state = state.WithPlayer(0, state.GetPlayer(0) with { Position = new Vec3Fix(Fix16.Zero, Fix16.Zero, Fix16.Zero) });
        return state.WithPlayer(1, state.GetPlayer(1) with { Position = new Vec3Fix(distance, Fix16.Zero, Fix16.Zero) });
    }

    private static MatchState Swapped(MatchState state)
    {
        var p1Position = state.GetPlayer(0).Position;
        var p2Position = state.GetPlayer(1).Position;
        state = state.WithPlayer(0, state.GetPlayer(0) with { Position = p2Position });
        return state.WithPlayer(1, state.GetPlayer(1) with { Position = p1Position });
    }

    private static MatchState Attacking(MatchState state, byte moveIndex) =>
        state.WithPlayer(0, state.GetPlayer(0) with { State = StateKind.Attack, CurrentMove = moveIndex });

    private static MoveData Strike(string name, string command, HitHeight height, Fix16 y, bool knockdown) => new()
    {
        Name = name,
        Command = command,
        Kind = MoveKind.Strike,
        Posture = height == HitHeight.Low ? Posture.Crouch : Posture.Stand,
        Startup = 10,
        Active = 3,
        Recovery = 12,
        Tracking = 4,
        Height = height,
        Damage = 12,
        CounterDamage = 15,
        Hitstun = 18,
        CounterHitstun = 22,
        Blockstun = 10,
        Hitstop = 6,
        Knockdown = knockdown,
        CounterKnockdown = knockdown,
        HitsDown = false,
        Pushback = Fix16.FromDecimal(0.25m),
        Motion = [],
        Windows =
        [
            new HitWindow
            {
                From = 10,
                To = 12,
                Hit = [new HitCapsule(new Vec3Fix(Fix16.FromDecimal(0.3m), y, Fix16.Zero), new Vec3Fix(Fix16.FromDecimal(0.8m), y, Fix16.Zero), Fix16.FromDecimal(0.15m))],
                Hurt = [],
            },
        ],
    };

    private static MoveData Throw() => new()
    {
        Name = "throw",
        Command = "P+G",
        Kind = MoveKind.Throw,
        Posture = Posture.Stand,
        Startup = 6,
        Active = 1,
        Recovery = 20,
        Tracking = 0,
        Height = HitHeight.Mid,
        Damage = 25,
        CounterDamage = 25,
        Hitstun = 0,
        CounterHitstun = 0,
        Blockstun = 0,
        Hitstop = 0,
        Knockdown = true,
        CounterKnockdown = true,
        HitsDown = false,
        Pushback = Fix16.Zero,
        Motion = [],
        Windows = [],
        ThrowRange = Fix16.One,
        ThrowSide = ThrowSide.Front,
        ThrowEndOffset = new Vec3Fix(Fix16.One, Fix16.Zero, Fix16.Zero),
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
        Moves =
        [
            Strike("jab", "P", HitHeight.High, Fix16.FromDecimal(1.3m), knockdown: false),
            Strike("lowKick", "K", HitHeight.Low, Fix16.FromDecimal(0.15m), knockdown: false),
            Strike("launcher", "6K", HitHeight.Mid, Fix16.FromDecimal(1.0m), knockdown: true),
            Throw(),
        ],
    };

    private static MatchContext Context(int initialHealth = 100) => new()
    {
        Characters = [Character(), Character()],
        Stage = new StageData
        {
            Name = "default",
            Shape = RingShape.Square,
            Size = Fix16.FromDecimal(5m),
            Edges = [EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut, EdgeKind.RingOut],
            StartDistance = Fix16.FromDecimal(2m),
        },
        Rules = new Rules
        {
            RoundTimeSeconds = 60,
            RoundsToWin = 2,
            MaxRounds = 5,
            InitialHealth = initialHealth,
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
        },
    };
}
