using Godot;

namespace Godot3dFighter.Game.Screens;

public sealed partial class TitleScreen : Control, IE2eScreen
{
    public string ScreenName => "Title";

    public override void _Ready()
    {
        AddToGroup("e2e_screen");
        GetNode<Button>("VBoxContainer/VersusButton").GrabFocus();
    }

    /// <summary>W/Sで選択肢を移り、Jで決める。矢印キーとEnterはGodotの既定のフォーカス移動で動く。</summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        MenuKeys.Handle(this, @event);
    }

    public bool TryInvoke(string action)
    {
        switch (action)
        {
            case "Versus":
                GetState().IsTraining = false;
                GetState().ResetSelection();
                GameState.GoTo(GetTree(), "res://scenes/CharacterSelect.tscn");
                return true;
            case "Training":
                GetState().IsTraining = true;
                GetState().ResetSelection();
                GameState.GoTo(GetTree(), "res://scenes/CharacterSelect.tscn");
                return true;
            default:
                return false;
        }
    }

    private GameState GetState() => GetNode<GameState>("/root/GameState");

    private void OnVersusPressed() => TryInvoke("Versus");

    private void OnTrainingPressed() => TryInvoke("Training");
}
