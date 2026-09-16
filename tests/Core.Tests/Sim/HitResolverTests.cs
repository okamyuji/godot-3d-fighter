using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class HitResolverTests
{
    private static HitCapsule Sphere(decimal x, decimal r) =>
        new(new Vec3Fix(Fix16.FromDecimal(x), Fix16.Zero, Fix16.Zero), new Vec3Fix(Fix16.FromDecimal(x), Fix16.Zero, Fix16.Zero), Fix16.FromDecimal(r));

    private static MoveData Jab(ushort startup = 5, ushort active = 3, HitHeight height = HitHeight.High) => new()
    {
        Name = "jab",
        Command = "P",
        Kind = MoveKind.Strike,
        Posture = Posture.Stand,
        Startup = startup,
        Active = active,
        Recovery = 5,
        Tracking = 2,
        Height = height,
        Damage = 10,
        CounterDamage = 15,
        Hitstun = 10,
        CounterHitstun = 15,
        Blockstun = 5,
        Hitstop = 6,
        Knockdown = false,
        CounterKnockdown = false,
        HitsDown = false,
        Pushback = Fix16.FromDecimal(0.2m),
        Motion = [],
        Windows =
        [
            new HitWindow { From = startup, To = (ushort)(startup + active - 1), Hit = [Sphere(0.7m, 0.2m)], Hurt = [] },
        ],
    };

    private static CharacterData Character() => new()
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
        StandHurtCapsules = [Sphere(0m, 0.3m)],
        CrouchHurtCapsules = [Sphere(0m, 0.2m)],
        DownHurtCapsules = [Sphere(0m, 0.2m)],
        Moves = [Jab()],
    };

    private static PlayerState Attacker(ushort stateFrame) => new()
    {
        State = StateKind.Attack,
        CurrentMove = 0,
        StateFrame = stateFrame,
        Position = new Vec3Fix(Fix16.Zero, Fix16.Zero, Fix16.Zero),
        Facing = new Angle16(0),
    };

    private static PlayerState Defender(StateKind state, ushort stateFrame = 0) => new()
    {
        State = state,
        CurrentMove = Limits.NoMove,
        StateFrame = stateFrame,
        Position = new Vec3Fix(Fix16.FromDecimal(0.7m), Fix16.Zero, Fix16.Zero),
        Facing = new Angle16(32768),
    };

    [Fact]
    public void DetectsHitWhenCapsulesOverlapAndDefenderIsIdle()
    {
        var attacker = Attacker(5);
        var defender = Defender(StateKind.Idle);

        var result = HitResolver.Detect(attacker, Character(), defender, Character());

        Assert.Equal(HitOutcomeKind.Hit, result.Kind);
    }

    [Fact]
    public void DetectsGuardWhenDefenderIsGuarding()
    {
        var attacker = Attacker(5);
        var defender = Defender(StateKind.Guard);

        var result = HitResolver.Detect(attacker, Character(), defender, Character());

        Assert.Equal(HitOutcomeKind.Guarded, result.Kind);
    }

    [Fact]
    public void DetectsCounterHitWhenDefenderIsBeforeItsOwnActiveFrames()
    {
        var attacker = Attacker(5);
        var defenderMove = Jab(startup: 20, active: 3);
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
            StandHurtCapsules = [Sphere(0m, 0.3m)],
            CrouchHurtCapsules = [Sphere(0m, 0.2m)],
            DownHurtCapsules = [Sphere(0m, 0.2m)],
            Moves = [defenderMove],
        };
        var defender = Defender(StateKind.Attack, stateFrame: 5) with { CurrentMove = 0 };

        var result = HitResolver.Detect(attacker, Character(), defender, defenderCharacter);

        Assert.Equal(HitOutcomeKind.CounterHit, result.Kind);
    }

    [Fact]
    public void HighAttackDoesNotHitCrouchingDefender()
    {
        var attacker = Attacker(5);
        var defender = Defender(StateKind.Crouch);

        var result = HitResolver.Detect(attacker, Character(), defender, Character());

        Assert.Equal(HitOutcomeKind.None, result.Kind);
    }

    [Fact]
    public void NoHitWhenCapsulesDoNotOverlap()
    {
        var attacker = Attacker(5);
        var farDefender = Defender(StateKind.Idle) with
        {
            Position = new Vec3Fix(Fix16.FromInt(10), Fix16.Zero, Fix16.Zero),
        };

        var result = HitResolver.Detect(attacker, Character(), farDefender, Character());

        Assert.Equal(HitOutcomeKind.None, result.Kind);
    }

    [Fact]
    public void NoHitOutsideTheActiveWindow()
    {
        var attacker = Attacker(1);
        var defender = Defender(StateKind.Idle);

        var result = HitResolver.Detect(attacker, Character(), defender, Character());

        Assert.Equal(HitOutcomeKind.None, result.Kind);
    }

    [Fact]
    public void NoHitWhenAttackerAlreadyHitThisMove()
    {
        var attacker = Attacker(5) with { Flags = Godot3dFighter.Core.State.PlayerFlags.HasHitThisMove };
        var defender = Defender(StateKind.Idle);

        var result = HitResolver.Detect(attacker, Character(), defender, Character());

        Assert.Equal(HitOutcomeKind.None, result.Kind);
    }

    [Fact]
    public void TradeGivesBothPlayersTheSameOutcomeRegardlessOfEvaluationOrder()
    {
        // 2人とも技の発生ちょうど（StateFrame==Startup）で攻撃判定が重なるため、
        // どちら向きに評価しても同じ結果（カウンターヒット）になる。
        var p1 = Attacker(5);
        var p2 = Attacker(5) with { Position = new Vec3Fix(Fix16.FromDecimal(0.7m), Fix16.Zero, Fix16.Zero), Facing = new Angle16(32768) };

        var p1AttacksP2 = HitResolver.Detect(p1, Character(), p2, Character());
        var p2AttacksP1 = HitResolver.Detect(p2, Character(), p1, Character());

        Assert.Equal(HitOutcomeKind.CounterHit, p1AttacksP2.Kind);
        Assert.Equal(HitOutcomeKind.CounterHit, p2AttacksP1.Kind);
    }

    [Fact]
    public void DownHitAppliesOnlyWhenMoveHitsDownAndNotYetTaken()
    {
        var character = CharacterWithHitsDownMove();
        var attacker = Attacker(5);
        var defender = Defender(StateKind.Down);

        var result = HitResolver.Detect(attacker, character, defender, character);

        Assert.Equal(HitOutcomeKind.DownHit, result.Kind);
    }

    [Fact]
    public void DownHitDoesNotApplyWhenMoveDoesNotHitDown()
    {
        var attacker = Attacker(5);
        var defender = Defender(StateKind.Down);

        var result = HitResolver.Detect(attacker, Character(), defender, Character());

        Assert.Equal(HitOutcomeKind.None, result.Kind);
    }

    [Fact]
    public void DownHitDoesNotApplyWhenAlreadyTakenThisKnockdown()
    {
        var character = CharacterWithHitsDownMove();
        var attacker = Attacker(5);
        var defender = Defender(StateKind.Down) with { Flags = Godot3dFighter.Core.State.PlayerFlags.DownHitTaken };

        var result = HitResolver.Detect(attacker, character, defender, character);

        Assert.Equal(HitOutcomeKind.None, result.Kind);
    }

    private static CharacterData CharacterWithHitsDownMove()
    {
        var move = new MoveData
        {
            Name = "jab",
            Command = "P",
            Kind = MoveKind.Strike,
            Posture = Posture.Stand,
            Startup = 5,
            Active = 3,
            Recovery = 5,
            Tracking = 2,
            Height = HitHeight.High,
            Damage = 10,
            CounterDamage = 15,
            Hitstun = 10,
            CounterHitstun = 15,
            Blockstun = 5,
            Hitstop = 6,
            Knockdown = false,
            CounterKnockdown = false,
            HitsDown = true,
            Pushback = Fix16.FromDecimal(0.2m),
            Motion = [],
            Windows = [new HitWindow { From = 5, To = 7, Hit = [Sphere(0.7m, 0.2m)], Hurt = [] }],
        };
        return new CharacterData
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
            StandHurtCapsules = [Sphere(0m, 0.3m)],
            CrouchHurtCapsules = [Sphere(0m, 0.2m)],
            DownHurtCapsules = [Sphere(0m, 0.2m)],
            Moves = [move],
        };
    }
}
