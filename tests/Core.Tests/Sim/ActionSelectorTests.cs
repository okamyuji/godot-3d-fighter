using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Sim;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class ActionSelectorTests
{
    private static MoveData Jab() => new()
    {
        Name = "jab",
        Command = "P",
        Kind = MoveKind.Strike,
        Posture = Posture.Stand,
        Startup = 5,
        Active = 2,
        Recovery = 5,
        Tracking = 2,
        Height = HitHeight.High,
        Damage = 10,
        CounterDamage = 10,
        Hitstun = 10,
        CounterHitstun = 10,
        Blockstun = 5,
        Hitstop = 3,
        Knockdown = false,
        CounterKnockdown = false,
        HitsDown = false,
        Pushback = Godot3dFighter.Core.Math.Fix16.Zero,
        Motion = [],
        Windows = [],
    };

    private static InputBuffer With(params byte[] bitsInOrder)
    {
        var buffer = default(InputBuffer);
        foreach (var bits in bitsInOrder)
        {
            buffer = buffer.Push(new InputFrame(bits));
        }

        return buffer;
    }

    [Fact]
    public void SelectsMoveOverDash()
    {
        var buffer = With(InputFrame.Right, InputFrame.Punch);
        var moves = new[] { Jab() };

        var decision = ActionSelector.Select(buffer, true, moves, buffered: false);

        Assert.Equal(SelectedAction.Move, decision.Action);
        Assert.Equal(0, decision.MoveIndex);
    }

    [Fact]
    public void SelectsDashOverGuard()
    {
        var buffer = With(0, InputFrame.Right, 0, InputFrame.Right);
        var moves = System.Array.Empty<MoveData>();

        var decision = ActionSelector.Select(buffer, true, moves, buffered: false);

        Assert.Equal(SelectedAction.DashForward, decision.Action);
    }

    [Fact]
    public void SelectsCrouchGuardWhenGuardAndDownArePressed()
    {
        var buffer = With((byte)(InputFrame.Guard | InputFrame.Down));
        var moves = System.Array.Empty<MoveData>();

        var decision = ActionSelector.Select(buffer, true, moves, buffered: false);

        Assert.Equal(SelectedAction.CrouchGuard, decision.Action);
    }

    [Fact]
    public void SelectsStandingGuardWhenOnlyGuardIsPressed()
    {
        var buffer = With(InputFrame.Guard);
        var moves = System.Array.Empty<MoveData>();

        var decision = ActionSelector.Select(buffer, true, moves, buffered: false);

        Assert.Equal(SelectedAction.Guard, decision.Action);
    }

    [Fact]
    public void SelectsCrouchWhenOnlyDownIsPressed()
    {
        var buffer = With(InputFrame.Down);
        var moves = System.Array.Empty<MoveData>();

        var decision = ActionSelector.Select(buffer, true, moves, buffered: false);

        Assert.Equal(SelectedAction.Crouch, decision.Action);
    }

    [Fact]
    public void SelectsWalkForwardWhenRightIsHeldOnLeftSide()
    {
        var buffer = With(InputFrame.Right);
        var moves = System.Array.Empty<MoveData>();

        var decision = ActionSelector.Select(buffer, true, moves, buffered: false);

        Assert.Equal(SelectedAction.WalkForward, decision.Action);
    }

    [Fact]
    public void SelectsWalkBackWhenLeftIsHeldOnLeftSide()
    {
        var buffer = With(InputFrame.Left);
        var moves = System.Array.Empty<MoveData>();

        var decision = ActionSelector.Select(buffer, true, moves, buffered: false);

        Assert.Equal(SelectedAction.WalkBack, decision.Action);
    }

    [Fact]
    public void SelectsIdleWhenNoInput()
    {
        var buffer = With(0);
        var moves = System.Array.Empty<MoveData>();

        var decision = ActionSelector.Select(buffer, true, moves, buffered: false);

        Assert.Equal(SelectedAction.Idle, decision.Action);
    }
}
