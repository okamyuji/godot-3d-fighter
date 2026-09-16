using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class WakeUpTests
{
    private static InputBuffer With(params byte[] bitsInOrder)
    {
        var buffer = default(InputBuffer);
        foreach (var bits in bitsInOrder)
        {
            buffer = buffer.Push(new InputFrame(bits));
        }

        return buffer;
    }

    private static Rules Rules() => new()
    {
        RoundTimeSeconds = 60,
        RoundsToWin = 2,
        MaxRounds = 5,
        InitialHealth = 200,
        IntroFrames = 1,
        RoundEndFrames = 1,
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

    private static MoveData RisingPunch() => new()
    {
        Name = "risingP",
        Command = "P",
        Kind = MoveKind.RisingAttack,
        Posture = Posture.Stand,
        Startup = 8,
        Active = 4,
        Recovery = 20,
        Tracking = 0,
        Height = HitHeight.Mid,
        Damage = 10,
        CounterDamage = 10,
        Hitstun = 14,
        CounterHitstun = 14,
        Blockstun = 6,
        Hitstop = 5,
        Knockdown = false,
        CounterKnockdown = false,
        HitsDown = false,
        Pushback = Fix16.FromDecimal(0.2m),
        Motion = [],
        Windows = [],
    };

    [Fact]
    public void PKGSimultaneousWithinTechFramesQueuesTech()
    {
        var buffer = With((byte)(InputFrame.Punch | InputFrame.Kick | InputFrame.Guard));

        Assert.True(WakeUp.IsTechInputThisFrame(buffer, true));
    }

    [Fact]
    public void MissingOneButtonDoesNotQueueTech()
    {
        var buffer = With((byte)(InputFrame.Punch | InputFrame.Kick));

        Assert.False(WakeUp.IsTechInputThisFrame(buffer, true));
    }

    [Fact]
    public void TechQueuedFlagTakesPriorityOverEverythingElse()
    {
        var player = new PlayerState { StateFrame = 25, Flags = PlayerFlags.TechQueued };
        var buffer = With(0);

        var decision = WakeUp.Decide(player, buffer, true, [], Rules());

        Assert.Equal(WakeUpAction.Tech, decision.Action);
    }

    [Fact]
    public void BeforeDownMinFramesAcceptsNoInput()
    {
        var player = new PlayerState { StateFrame = 5 };
        var buffer = With(InputFrame.Guard);

        var decision = WakeUp.Decide(player, buffer, true, [], Rules());

        Assert.Equal(WakeUpAction.None, decision.Action);
    }

    [Fact]
    public void UpNewlyEnteredAfterDownMinFramesRollsIn()
    {
        var player = new PlayerState { StateFrame = 25 };
        var buffer = With(0, InputFrame.Up);

        var decision = WakeUp.Decide(player, buffer, true, [], Rules());

        Assert.Equal(WakeUpAction.RollIn, decision.Action);
    }

    [Fact]
    public void DownNewlyEnteredAfterDownMinFramesRollsOut()
    {
        var player = new PlayerState { StateFrame = 25 };
        var buffer = With(0, InputFrame.Down);

        var decision = WakeUp.Decide(player, buffer, true, [], Rules());

        Assert.Equal(WakeUpAction.RollOut, decision.Action);
    }

    [Fact]
    public void PunchPressedRisesWithAttackWhenMoveExists()
    {
        var player = new PlayerState { StateFrame = 25 };
        var buffer = With(0, InputFrame.Punch);
        var moves = new[] { RisingPunch() };

        var decision = WakeUp.Decide(player, buffer, true, moves, Rules());

        Assert.Equal(WakeUpAction.RisingAttack, decision.Action);
        Assert.Equal(0, decision.MoveIndex);
    }

    [Fact]
    public void GuardPressedRises()
    {
        var player = new PlayerState { StateFrame = 25 };
        var buffer = With(0, InputFrame.Guard);

        var decision = WakeUp.Decide(player, buffer, true, [], Rules());

        Assert.Equal(WakeUpAction.Rise, decision.Action);
    }

    [Fact]
    public void ReachingDownMaxFramesRisesWithoutInput()
    {
        var player = new PlayerState { StateFrame = 60 };
        var buffer = With(0);

        var decision = WakeUp.Decide(player, buffer, true, [], Rules());

        Assert.Equal(WakeUpAction.Rise, decision.Action);
    }

    [Fact]
    public void NoInputBeforeMaxFramesStaysNone()
    {
        var player = new PlayerState { StateFrame = 25 };
        var buffer = With(0);

        var decision = WakeUp.Decide(player, buffer, true, [], Rules());

        Assert.Equal(WakeUpAction.None, decision.Action);
    }
}
