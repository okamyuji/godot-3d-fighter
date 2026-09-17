using System.Globalization;
using Godot;
using Godot3dFighter.Core;

namespace Godot3dFighter.Game.Screens;

public sealed partial class CharacterSelectScreen : Control, IE2eScreen
{
    /// <summary>対戦でP1が決定してからP2がCPUになるまでのフレーム数。この間にP2が決定すれば人間のP2になる。</summary>
    private const int CpuCountdownFrames = 3 * Limits.FramesPerSecond;

    private Label? _stageLabel;
    private Label? _statusLabel;
    private int _cpuCountdown = -1;

    public string ScreenName => "CharacterSelect";

    public override void _Ready()
    {
        AddToGroup("e2e_screen");
        _stageLabel = GetNodeOrNull<Label>("%StageLabel");
        _statusLabel = GetNodeOrNull<Label>("%StatusLabel");
        Refresh();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_cpuCountdown < 0)
        {
            return;
        }

        _cpuCountdown--;
        if (_cpuCountdown > 0)
        {
            Refresh();
            return;
        }

        var state = GetState();
        state.P2IsCpu = true;
        state.P2Confirmed = true;
        Start(state);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        System.ArgumentNullException.ThrowIfNull(@event);
        if (@event.IsActionPressed("p1_punch"))
        {
            TryInvoke("ConfirmP1");
        }
        else if (@event.IsActionPressed("p2_punch"))
        {
            TryInvoke("ConfirmP2");
        }
        else if (@event.IsActionPressed("p1_kick") || @event.IsActionPressed("p2_kick"))
        {
            TryInvoke("NextStage");
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            TryInvoke("Back");
        }
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
                if (state.IsTraining)
                {
                    state.P2Confirmed = true;
                }

                TryStart(state);
                return true;
            case "ConfirmP2":
                state.P2Confirmed = true;
                _cpuCountdown = -1;
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
        if (!state.P1Confirmed)
        {
            Refresh();
            return;
        }

        if (!state.P2Confirmed)
        {
            if (_cpuCountdown < 0)
            {
                _cpuCountdown = CpuCountdownFrames;
            }

            Refresh();
            return;
        }

        Start(state);
    }

    private void Start(GameState state)
    {
        _cpuCountdown = -1;
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
            _statusLabel.Text = "box vs box  P1:" + (state.P1Confirmed ? "決定" : "選択中") + "  P2:" + P2Status(state);
        }
    }

    private string P2Status(GameState state)
    {
        if (state.P2Confirmed)
        {
            return "決定";
        }

        if (_cpuCountdown < 0)
        {
            return "選択中";
        }

        var seconds = (_cpuCountdown + Limits.FramesPerSecond - 1) / Limits.FramesPerSecond;
        return "CPU（" + seconds.ToString(CultureInfo.InvariantCulture) + "秒後に開始、テンキー1で参加）";
    }

    private GameState GetState() => GetNode<GameState>("/root/GameState");

    private void OnNextStagePressed() => TryInvoke("NextStage");

    private void OnConfirmP1Pressed() => TryInvoke("ConfirmP1");

    private void OnConfirmP2Pressed() => TryInvoke("ConfirmP2");

    private void OnBackPressed() => TryInvoke("Back");
}
