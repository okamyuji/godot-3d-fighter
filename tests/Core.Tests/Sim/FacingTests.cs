using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class FacingTests
{
    private static Vec3Fix P(decimal x, decimal z) => new(Fix16.FromDecimal(x), Fix16.Zero, Fix16.FromDecimal(z));

    private static MoveData MoveWithTracking(ushort startup, ushort tracking) => new()
    {
        Name = "m",
        Command = "P",
        Kind = MoveKind.Strike,
        Posture = Posture.Stand,
        Startup = startup,
        Active = 2,
        Recovery = 5,
        Tracking = tracking,
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
        Motion = [],
        Windows = [],
    };

    [Fact]
    public void IdleFacesTheOpponent()
    {
        var player = new PlayerState { State = StateKind.Idle, Facing = new Angle16(0) };

        var result = Facing.Update(player, P(0, 0), P(-1, 0), null);

        // 自分が(-1,0)、相手が(0,0)。相手は自分の+X側なので0度を向く。
        Assert.Equal((ushort)0, result.Value);
    }

    [Fact]
    public void AttackBeforeTrackingLimitStillFacesTheOpponent()
    {
        var move = MoveWithTracking(startup: 10, tracking: 4);
        var player = new PlayerState { State = StateKind.Attack, StateFrame = 2, Facing = new Angle16(32768) };

        var result = Facing.Update(player, P(1, 0), P(0, 0), move);

        Assert.Equal((ushort)0, result.Value);
    }

    [Fact]
    public void AttackAtOrAfterTrackingLimitDoesNotChangeFacing()
    {
        var move = MoveWithTracking(startup: 10, tracking: 4);
        var player = new PlayerState { State = StateKind.Attack, StateFrame = 4, Facing = new Angle16(32768) };

        var result = Facing.Update(player, P(1, 0), P(0, 0), move);

        Assert.Equal(player.Facing, result);
    }

    [Fact]
    public void HitstunDoesNotChangeFacing()
    {
        var player = new PlayerState { State = StateKind.Hitstun, Facing = new Angle16(12345) };

        var result = Facing.Update(player, P(1, 0), P(0, 0), null);

        Assert.Equal(player.Facing, result);
    }

    [Fact]
    public void SameHorizontalPositionDoesNotChangeFacing()
    {
        var player = new PlayerState { State = StateKind.Idle, Facing = new Angle16(999) };

        var result = Facing.Update(player, P(0, 0), P(0, 0), null);

        Assert.Equal(player.Facing, result);
    }
}
