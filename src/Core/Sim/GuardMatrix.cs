using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>段と守りの組み合わせから、ガードか命中かを決める（ADR-0018）。ガードで防いだ硬直は、防いだ時と同じガードとして扱う。</summary>
public static class GuardMatrix
{
    public static bool IsGuarded(HitHeight height, StateKind defenderState)
    {
        var crouching = defenderState is StateKind.CrouchGuard or StateKind.CrouchBlockstun;
        var standing = defenderState is StateKind.Guard or StateKind.Blockstun;
        if (!crouching && !standing)
        {
            return false;
        }

        return height switch
        {
            HitHeight.High => standing,
            HitHeight.Mid => standing,
            HitHeight.Low => crouching,
            _ => false,
        };
    }

    /// <summary>ガードしていなくても、段としゃがみの組み合わせで当たらない場合にfalseを返す。</summary>
    public static bool CanHit(HitHeight height, StateKind defenderState)
    {
        var crouching = defenderState is StateKind.Crouch or StateKind.CrouchGuard or StateKind.CrouchBlockstun;
        return height != HitHeight.High || !crouching;
    }
}
