using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class MovementTests
{
    private static CharacterData Character() => new()
    {
        Name = "c",
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
        StandHurtCapsules = [],
        CrouchHurtCapsules = [],
        DownHurtCapsules = [],
        Moves = [],
    };

    [Fact]
    public void WalkMovesForwardAtWalkSpeed()
    {
        var player = new PlayerState { State = StateKind.Walk, Facing = new Angle16(0) };

        var velocity = Movement.ComputeVelocity(player, Character(), true, null);

        Assert.Equal(Fix16.FromDecimal(0.05m), velocity.X);
        Assert.Equal(Fix16.Zero, velocity.Z);
    }

    [Fact]
    public void BackWalkMovesBackwardAtBackWalkSpeed()
    {
        var player = new PlayerState { State = StateKind.BackWalk, Facing = new Angle16(0) };

        var velocity = Movement.ComputeVelocity(player, Character(), true, null);

        Assert.Equal(Fix16.Zero - Fix16.FromDecimal(0.04m), velocity.X);
    }

    [Fact]
    public void DashMovesForwardAtDashSpeed()
    {
        var player = new PlayerState { State = StateKind.Dash, Facing = new Angle16(0) };

        var velocity = Movement.ComputeVelocity(player, Character(), true, null);

        Assert.Equal(Fix16.FromDecimal(0.12m), velocity.X);
    }

    [Fact]
    public void IdleDoesNotMove()
    {
        var player = new PlayerState { State = StateKind.Idle, Facing = new Angle16(0) };

        var velocity = Movement.ComputeVelocity(player, Character(), true, null);

        Assert.Equal(default, velocity);
    }

    [Fact]
    public void HitstunDoesNotMove()
    {
        var player = new PlayerState { State = StateKind.Hitstun, Facing = new Angle16(0) };

        var velocity = Movement.ComputeVelocity(player, Character(), true, null);

        Assert.Equal(default, velocity);
    }

    [Fact]
    public void AttackUsesMotionSegmentCoveringCurrentStateFrame()
    {
        var move = new MoveData
        {
            Name = "m",
            Command = "P",
            Kind = MoveKind.Strike,
            Posture = Posture.Stand,
            Startup = 10,
            Active = 3,
            Recovery = 10,
            Tracking = 4,
            Height = HitHeight.High,
            Damage = 1,
            CounterDamage = 1,
            Hitstun = 1,
            CounterHitstun = 1,
            Blockstun = 1,
            Hitstop = 1,
            Knockdown = false,
            CounterKnockdown = false,
            HitsDown = false,
            Pushback = Fix16.Zero,
            Motion = [new MotionSegment(8, 12, Fix16.FromDecimal(0.02m))],
            Windows = [],
        };
        var player = new PlayerState { State = StateKind.Attack, StateFrame = 10, Facing = new Angle16(0) };

        var velocity = Movement.ComputeVelocity(player, Character(), true, move);

        Assert.Equal(Fix16.FromDecimal(0.02m), velocity.X);
    }

    [Fact]
    public void AttackOutsideMotionSegmentDoesNotMove()
    {
        var move = new MoveData
        {
            Name = "m",
            Command = "P",
            Kind = MoveKind.Strike,
            Posture = Posture.Stand,
            Startup = 10,
            Active = 3,
            Recovery = 10,
            Tracking = 4,
            Height = HitHeight.High,
            Damage = 1,
            CounterDamage = 1,
            Hitstun = 1,
            CounterHitstun = 1,
            Blockstun = 1,
            Hitstop = 1,
            Knockdown = false,
            CounterKnockdown = false,
            HitsDown = false,
            Pushback = Fix16.Zero,
            Motion = [new MotionSegment(8, 12, Fix16.FromDecimal(0.02m))],
            Windows = [],
        };
        var player = new PlayerState { State = StateKind.Attack, StateFrame = 20, Facing = new Angle16(0) };

        var velocity = Movement.ComputeVelocity(player, Character(), true, move);

        Assert.Equal(default, velocity);
    }
}
