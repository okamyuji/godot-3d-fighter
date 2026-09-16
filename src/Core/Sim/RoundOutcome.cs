using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

public readonly record struct RoundOutcome(bool Decided, RoundEndReason Reason, bool P1Won, bool P2Won);
