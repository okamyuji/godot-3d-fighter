using System;
using Godot;

namespace Godot3dFighter.Game;

/// <summary>フォーカスで選ぶメニュー画面のキーボード操作（03-screens-and-e2e.md）。P1のW/Sで選択肢を移り、P1かP2のパンチでフォーカス中のボタンを押す。</summary>
public static class MenuKeys
{
    public static void Handle(Control screen, InputEvent @event)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(@event);
        var focused = screen.GetViewport().GuiGetFocusOwner();
        if (focused is null)
        {
            return;
        }

        if (@event.IsActionPressed("p1_punch") || @event.IsActionPressed("p2_punch"))
        {
            (focused as BaseButton)?.EmitSignal(BaseButton.SignalName.Pressed);
        }
        else if (@event.IsActionPressed("p1_up"))
        {
            focused.FindPrevValidFocus()?.GrabFocus();
        }
        else if (@event.IsActionPressed("p1_down"))
        {
            focused.FindNextValidFocus()?.GrabFocus();
        }
    }
}
