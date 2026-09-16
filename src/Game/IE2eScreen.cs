namespace Godot3dFighter.Game;

/// <summary>各画面が実装する、主要導線の走破用の操作口（03-screens-and-e2e.md）。</summary>
public interface IE2eScreen
{
    string ScreenName { get; }

    /// <summary>画面にない操作ならfalse。</summary>
    bool TryInvoke(string action);
}
