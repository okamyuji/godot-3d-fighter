namespace Godot3dFighter.Core.Sim;

/// <summary>投げ抜けの確認の結果。Triedは猶予が過ぎて以後の入力では抜けられないことを示す。</summary>
public enum ThrowEscapeCheck : byte
{
    None,
    Escaped,
    Tried,
}
