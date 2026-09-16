namespace Godot3dFighter.Core.Sim;

/// <summary>行動の判断の結果。MoveIndexはActionがMoveの時だけ意味を持つ。</summary>
public readonly record struct ActionDecision(SelectedAction Action, int MoveIndex);
