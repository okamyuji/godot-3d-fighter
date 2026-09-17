using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Game.Screens;

public sealed partial class MatchScreen : FightScreenBase, IE2eScreen
{
    public string ScreenName => "Match";

    public override void _Ready()
    {
        base._Ready();
        AddToGroup("e2e_screen");
    }

    public bool TryInvoke(string action) => false;

    /// <summary>P2がCPUなら、キーボードとE2Eの上書きを読まずにCpuPolicyの入力を使う。</summary>
    protected override InputFrame ReadP2Input() =>
        GetGameState().P2IsCpu ? CpuPolicy.Decide(State, Context, 1) : InputMapper.Read(2);

    protected override void OnAfterStep()
    {
        if (State.Phase == RoundPhase.MatchEnd && State.PhaseFrames >= Context.Rules.RoundEndFrames)
        {
            GameState.GoTo(GetTree(), "res://scenes/Result.tscn");
        }
    }
}
