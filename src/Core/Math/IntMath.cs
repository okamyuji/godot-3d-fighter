using System;

namespace Godot3dFighter.Core.Math;

/// <summary>Coreで使う整数演算の補助関数。System.Mathの代わり（ADR-0001、ADR-0009）。</summary>
public static class IntMath
{
    /// <summary>0以上のlongの平方根を、小数点以下を切り捨てたlongで返す。</summary>
    public static long Sqrt(long v)
    {
        if (v < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(v), v, "0以上の値が必要です。");
        }

        if (v == 0)
        {
            return 0;
        }

        var x = v;
        // x + 1はx==long.MaxValueであふれるため、その時だけ別の式にする。
        var y = x == long.MaxValue ? x / 2 + 1 : (x + 1) / 2;
        while (y < x)
        {
            x = y;
            y = (x + v / x) / 2;
        }

        return x;
    }
}
