using System;
using System.Globalization;
using System.Linq;
using Godot;
using Godot3dFighter.Core;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Game.View;

/// <summary>試合画面のHUD（03-screens-and-e2e.md）。体力ゲージ、残り時間、勝ち数、決着の理由、カウンターヒットと壁やられの表示。</summary>
public sealed partial class FightHud : CanvasLayer
{
    private const int HitMessageFrames = 30;

    private readonly ProgressBar _p1Health = new();
    private readonly ProgressBar _p2Health = new();
    private readonly Label _timer = new();
    private readonly Label _round = new();
    private readonly Label _center = new();
    private readonly Label _hitMessage = new();
    private int _hitMessageFrames;
    private string _hitMessageText = "";

    public FightHud()
    {
        _p1Health.ShowPercentage = false;
        _p2Health.ShowPercentage = false;
        _p2Health.FillMode = (int)ProgressBar.FillModeEnum.EndToBegin;
        Place(_p1Health, 0.05f, 0.04f, 0.45f, 0.08f);
        Place(_p2Health, 0.55f, 0.04f, 0.95f, 0.08f);
        Place(Centered(_timer, 32), 0.45f, 0.02f, 0.55f, 0.1f);
        Place(Centered(_round, 16), 0.3f, 0.1f, 0.7f, 0.15f);
        Place(Centered(_center, 64), 0.2f, 0.4f, 0.8f, 0.55f);
        Place(Centered(_hitMessage, 28), 0.3f, 0.7f, 0.7f, 0.78f);
    }

    /// <summary>表示中の文字列を空白で連結したもの。E2EのexpectHudが読む。</summary>
    public string Text { get; private set; } = "";

    public void Apply(in MatchState state, MatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var p1 = state.GetPlayer(0);
        var p2 = state.GetPlayer(1);
        _p1Health.MaxValue = context.Rules.InitialHealth;
        _p1Health.Value = Math.Max(0, p1.Health);
        _p2Health.MaxValue = context.Rules.InitialHealth;
        _p2Health.Value = Math.Max(0, p2.Health);

        var seconds = (state.RoundTimerFrames + Limits.FramesPerSecond - 1) / Limits.FramesPerSecond;
        _timer.Text = seconds.ToString(CultureInfo.InvariantCulture);
        _round.Text = string.Format(CultureInfo.InvariantCulture, "ROUND {0}  {1}-{2}", state.RoundNumber, state.GetRoundWins(0), state.GetRoundWins(1));
        _center.Text = state.Phase is RoundPhase.RoundEnd or RoundPhase.MatchEnd ? ReasonText(state.LastRoundReason) : "";

        var message = HitMessage(p1.LastHitKind);
        if (message.Length == 0)
        {
            message = HitMessage(p2.LastHitKind);
        }

        if (message.Length > 0)
        {
            _hitMessageText = message;
            _hitMessageFrames = HitMessageFrames;
        }
        else if (_hitMessageFrames > 0)
        {
            _hitMessageFrames--;
            if (_hitMessageFrames == 0)
            {
                _hitMessageText = "";
            }
        }

        _hitMessage.Text = _hitMessageText;
        string[] parts = [$"P1:{p1.Health}", $"P2:{p2.Health}", $"TIME:{seconds}", _round.Text, _center.Text, _hitMessage.Text];
        Text = string.Join(' ', parts.Where(s => s.Length > 0));
    }

    private static string ReasonText(RoundEndReason reason) => reason switch
    {
        RoundEndReason.KnockOut => "KO",
        RoundEndReason.RingOut => "RING OUT",
        RoundEndReason.TimeUp => "TIME UP",
        _ => "",
    };

    private static string HitMessage(HitKind kind) => kind switch
    {
        HitKind.CounterHit => "COUNTER",
        HitKind.CounterWallHit => "COUNTER WALL",
        HitKind.WallHit => "WALL",
        _ => "",
    };

    private static Label Centered(Label label, int fontSize)
    {
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private void Place(Control control, float left, float top, float right, float bottom)
    {
        control.AnchorLeft = left;
        control.AnchorTop = top;
        control.AnchorRight = right;
        control.AnchorBottom = bottom;
        control.OffsetLeft = 0;
        control.OffsetTop = 0;
        control.OffsetRight = 0;
        control.OffsetBottom = 0;
        AddChild(control);
    }
}
