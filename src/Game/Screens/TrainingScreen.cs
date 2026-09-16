using Godot3dFighter.Core;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Game.Screens;

public sealed partial class TrainingScreen : FightScreenBase, IE2eScreen
{
    private enum DummyMode
    {
        Stand,
        Crouch,
        GuardAll,
    }

    private DummyMode _mode = DummyMode.Stand;

    public string ScreenName => "Training";

    public override void _Ready()
    {
        base._Ready();
        AddToGroup("e2e_screen");
    }

    public bool TryInvoke(string action)
    {
        switch (action)
        {
            case "ResetPositions":
                State = MatchSimulator.ResetPositions(State, Context);
                return true;
            case "DummyStand":
                _mode = DummyMode.Stand;
                return true;
            case "DummyCrouch":
                _mode = DummyMode.Crouch;
                return true;
            case "DummyGuardAll":
                _mode = DummyMode.GuardAll;
                return true;
            case "Exit":
                GameState.GoTo(GetTree(), "res://scenes/Title.tscn");
                return true;
            default:
                return false;
        }
    }

    protected override InputFrame ReadP2Input() => _mode switch
    {
        DummyMode.Crouch => new InputFrame(InputFrame.Down),
        DummyMode.GuardAll => GuardAllInput(),
        _ => default,
    };

    private InputFrame GuardAllInput()
    {
        var p1 = State.GetPlayer(0);
        if (p1.State != StateKind.Attack || p1.CurrentMove == Limits.NoMove)
        {
            return new InputFrame(InputFrame.Guard);
        }

        var move = Context.Characters[0].Moves[p1.CurrentMove];
        return move.Kind == MoveKind.Strike && move.Height == HitHeight.Low
            ? new InputFrame((byte)(InputFrame.Guard | InputFrame.Down))
            : new InputFrame(InputFrame.Guard);
    }

    protected override void OnRoundEnd()
    {
        // トレーニングは決着を記録しない（rules-training.jsonのtraining=trueで決着自体が起きない）。
    }

    private void OnResetPositionsPressed() => TryInvoke("ResetPositions");

    private void OnDummyStandPressed() => TryInvoke("DummyStand");

    private void OnDummyCrouchPressed() => TryInvoke("DummyCrouch");

    private void OnDummyGuardAllPressed() => TryInvoke("DummyGuardAll");

    private void OnExitPressed() => TryInvoke("Exit");
}
