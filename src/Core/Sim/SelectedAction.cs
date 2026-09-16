namespace Godot3dFighter.Core.Sim;

/// <summary>行動の判断の結果（ADR-0017）。Sidestepは手順10（状態を持つ移動）で扱う。</summary>
public enum SelectedAction : byte
{
    None,
    Move,
    DashForward,
    DashBack,
    Guard,
    CrouchGuard,
    Crouch,
    WalkForward,
    WalkBack,
    Idle,
}
