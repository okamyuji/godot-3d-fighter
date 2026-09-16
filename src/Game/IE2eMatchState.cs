using Godot3dFighter.Core.State;

namespace Godot3dFighter.Game;

/// <summary>E2eRunnerがexpectPhaseとexpectRoundEndで参照する、試合画面の現在値（03-screens-and-e2e.md）。</summary>
public interface IE2eMatchState
{
    RoundPhase Phase { get; }

    RoundEndReason LastRoundReason { get; }

    byte LastRoundWinners { get; }
}
