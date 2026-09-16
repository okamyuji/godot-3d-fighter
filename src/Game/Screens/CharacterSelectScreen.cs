using Godot;

namespace Godot3dFighter.Game.Screens;

public sealed partial class CharacterSelectScreen : Control, IE2eScreen
{
    private Label? _stageLabel;
    private Label? _statusLabel;

    public string ScreenName => "CharacterSelect";

    public override void _Ready()
    {
        AddToGroup("e2e_screen");
        _stageLabel = GetNodeOrNull<Label>("%StageLabel");
        _statusLabel = GetNodeOrNull<Label>("%StatusLabel");
        Refresh();
    }

    public bool TryInvoke(string action)
    {
        var state = GetState();
        switch (action)
        {
            case "NextStage":
                state.NextStage();
                Refresh();
                return true;
            case "ConfirmP1":
                state.P1Confirmed = true;
                Refresh();
                TryStart(state);
                return true;
            case "ConfirmP2":
                state.P2Confirmed = true;
                Refresh();
                TryStart(state);
                return true;
            case "Back":
                GameState.GoTo(GetTree(), "res://scenes/Title.tscn");
                return true;
            default:
                return false;
        }
    }

    private void TryStart(GameState state)
    {
        if (!state.P1Confirmed || !state.P2Confirmed)
        {
            return;
        }

        GameState.GoTo(GetTree(), state.IsTraining ? "res://scenes/Training.tscn" : "res://scenes/Match.tscn");
    }

    private void Refresh()
    {
        var state = GetState();
        if (_stageLabel is not null)
        {
            _stageLabel.Text = "ステージ: " + state.StageName;
        }

        if (_statusLabel is not null)
        {
            _statusLabel.Text = "box vs box  P1:" + (state.P1Confirmed ? "決定" : "選択中")
                + "  P2:" + (state.P2Confirmed ? "決定" : "選択中");
        }
    }

    private GameState GetState() => GetNode<GameState>("/root/GameState");

    private void OnNextStagePressed() => TryInvoke("NextStage");

    private void OnConfirmP1Pressed() => TryInvoke("ConfirmP1");

    private void OnConfirmP2Pressed() => TryInvoke("ConfirmP2");

    private void OnBackPressed() => TryInvoke("Back");
}
