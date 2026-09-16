using Godot3dFighter.Core.Data;

namespace Godot3dFighter.Core.Sim;

/// <summary>攻撃1つが相手のやられ判定に当たったかの判定結果。</summary>
public readonly record struct HitOutcome(HitOutcomeKind Kind, MoveData? Move);
