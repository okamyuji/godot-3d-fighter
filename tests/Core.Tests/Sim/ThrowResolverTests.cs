using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class ThrowResolverTests
{
    private static MoveData ThrowMove(ThrowSide side = ThrowSide.Front, decimal range = 1.0m, ushort startup = 6) => new()
    {
        Name = "throw",
        Command = "P+G",
        Kind = MoveKind.Throw,
        Posture = Posture.Stand,
        Startup = startup,
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
        ThrowRange = Fix16.FromDecimal(range),
        ThrowSide = side,
        ThrowEndOffset = new Vec3Fix(Fix16.FromInt(1), Fix16.Zero, Fix16.Zero),
    };

    private static CharacterData CharacterWith(MoveData move) => new()
    {
        Name = "c",
        WalkSpeed = Fix16.Zero,
        BackWalkSpeed = Fix16.Zero,
        DashSpeed = Fix16.Zero,
        DashFrames = 1,
        BackdashSpeed = Fix16.Zero,
        BackdashFrames = 1,
        DashCancelFrames = 0,
        SidestepSpeed = Fix16.Zero,
        SidestepFrames = 1,
        SidestepCancelFrame = 0,
        RollSpeed = Fix16.Zero,
        RollFrames = 1,
        BodyRadius = Fix16.FromDecimal(0.3m),
        StandHurtCapsules = [],
        CrouchHurtCapsules = [],
        DownHurtCapsules = [],
        Moves = [move],
    };

    private static PlayerState Attacker(ushort stateFrame, Angle16 facing) => new()
    {
        State = StateKind.Attack,
        CurrentMove = 0,
        StateFrame = stateFrame,
        Position = new Vec3Fix(Fix16.Zero, Fix16.Zero, Fix16.Zero),
        Facing = facing,
    };

    private static PlayerState Defender(StateKind state, Angle16 facing, decimal x = 0.7m) => new()
    {
        State = state,
        Position = new Vec3Fix(Fix16.FromDecimal(x), Fix16.Zero, Fix16.Zero),
        Facing = facing,
    };

    [Fact]
    public void SucceedsWhenInRangeFacingEachOtherAndThrowable()
    {
        var move = ThrowMove();
        var attacker = Attacker(6, new Angle16(0));
        var defender = Defender(StateKind.Idle, new Angle16(32768));

        var found = ThrowResolver.TryDetect(attacker, CharacterWith(move), defender, CharacterWith(move), out var result);

        Assert.True(found);
        Assert.Equal(move, result);
    }

    [Fact]
    public void FailsOutsideThrowRange()
    {
        var move = ThrowMove(range: 0.5m);
        var attacker = Attacker(6, new Angle16(0));
        var defender = Defender(StateKind.Idle, new Angle16(32768), x: 2m);

        var found = ThrowResolver.TryDetect(attacker, CharacterWith(move), defender, CharacterWith(move), out _);

        Assert.False(found);
    }

    [Fact]
    public void FailsWhenNotAtStartupFrame()
    {
        var move = ThrowMove(startup: 6);
        var attacker = Attacker(5, new Angle16(0));
        var defender = Defender(StateKind.Idle, new Angle16(32768));

        var found = ThrowResolver.TryDetect(attacker, CharacterWith(move), defender, CharacterWith(move), out _);

        Assert.False(found);
    }

    [Theory]
    [InlineData(StateKind.Crouch)]
    [InlineData(StateKind.CrouchGuard)]
    [InlineData(StateKind.Hitstun)]
    [InlineData(StateKind.Down)]
    [InlineData(StateKind.Dead)]
    [InlineData(StateKind.Throwing)]
    public void FailsWhenDefenderCannotBeThrown(StateKind defenderState)
    {
        var move = ThrowMove();
        var attacker = Attacker(6, new Angle16(0));
        var defender = Defender(defenderState, new Angle16(32768));

        var found = ThrowResolver.TryDetect(attacker, CharacterWith(move), defender, CharacterWith(move), out _);

        Assert.False(found);
    }

    [Fact]
    public void FrontThrowFailsWhenDefenderFacesAway()
    {
        var move = ThrowMove(side: ThrowSide.Front);
        var attacker = Attacker(6, new Angle16(0));
        var defender = Defender(StateKind.Idle, new Angle16(0));

        var found = ThrowResolver.TryDetect(attacker, CharacterWith(move), defender, CharacterWith(move), out _);

        Assert.False(found);
    }

    [Fact]
    public void BackThrowSucceedsWhenDefenderFacesAway()
    {
        var move = ThrowMove(side: ThrowSide.Back);
        var attacker = Attacker(6, new Angle16(0));
        var defender = Defender(StateKind.Idle, new Angle16(0));

        var found = ThrowResolver.TryDetect(attacker, CharacterWith(move), defender, CharacterWith(move), out _);

        Assert.True(found);
    }

    [Fact]
    public void BackThrowFailsWhenDefenderFacesAttacker()
    {
        var move = ThrowMove(side: ThrowSide.Back);
        var attacker = Attacker(6, new Angle16(0));
        var defender = Defender(StateKind.Idle, new Angle16(32768));

        var found = ThrowResolver.TryDetect(attacker, CharacterWith(move), defender, CharacterWith(move), out _);

        Assert.False(found);
    }

    [Fact]
    public void CrouchingAttackPostureCannotThrow()
    {
        var move = ThrowMove();
        var attacker = Attacker(6, new Angle16(0));
        var crouchMove = new MoveData
        {
            Name = "crouchAttack",
            Command = "K",
            Kind = MoveKind.Strike,
            Posture = Posture.Crouch,
            Startup = 5,
            Active = 2,
            Recovery = 5,
            Tracking = 2,
            Height = HitHeight.Low,
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
        var defenderCharacter = new CharacterData
        {
            Name = "d",
            WalkSpeed = Fix16.Zero,
            BackWalkSpeed = Fix16.Zero,
            DashSpeed = Fix16.Zero,
            DashFrames = 1,
            BackdashSpeed = Fix16.Zero,
            BackdashFrames = 1,
            DashCancelFrames = 0,
            SidestepSpeed = Fix16.Zero,
            SidestepFrames = 1,
            SidestepCancelFrame = 0,
            RollSpeed = Fix16.Zero,
            RollFrames = 1,
            BodyRadius = Fix16.FromDecimal(0.3m),
            StandHurtCapsules = [],
            CrouchHurtCapsules = [],
            DownHurtCapsules = [],
            Moves = [crouchMove],
        };
        var defender = Defender(StateKind.Attack, new Angle16(32768)) with { CurrentMove = 0 };

        var found = ThrowResolver.TryDetect(attacker, CharacterWith(move), defender, defenderCharacter, out _);

        Assert.False(found);
    }
}
