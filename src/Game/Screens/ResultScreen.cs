using System.Globalization;
using System.Text;
using Godot;

namespace Godot3dFighter.Game.Screens;

public sealed partial class ResultScreen : Control, IE2eScreen
{
    private Label? _resultLabel;

    public string ScreenName => "Result";

    public override void _Ready()
    {
        AddToGroup("e2e_screen");
        _resultLabel = GetNodeOrNull<Label>("%ResultLabel");
        GetNode<Button>("VBoxContainer/ConfirmButton").GrabFocus();
        Refresh();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        MenuKeys.Handle(this, @event);
    }

    public bool TryInvoke(string action)
    {
        if (action != "Confirm")
        {
            return false;
        }

        GameState.GoTo(GetTree(), "res://scenes/Title.tscn");
        return true;
    }

    private void Refresh()
    {
        if (_resultLabel is null)
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");
        var p1Wins = 0;
        var p2Wins = 0;
        var text = new StringBuilder();
        for (var i = 0; i < state.RoundHistory.Count; i++)
        {
            var (reason, winners) = state.RoundHistory[i];
            if ((winners & 1) != 0)
            {
                p1Wins++;
            }

            if ((winners & 2) != 0)
            {
                p2Wins++;
            }

            text.AppendLine(CultureInfo.InvariantCulture, $"round {i + 1}: {reason} winners={winners}");
        }

        var overall = p1Wins == p2Wins ? "DRAW" : p1Wins > p2Wins ? "P1 WINS" : "P2 WINS";
        text.Insert(0, overall + "\n");
        _resultLabel.Text = text.ToString();
    }

    private void OnConfirmPressed() => TryInvoke("Confirm");
}
