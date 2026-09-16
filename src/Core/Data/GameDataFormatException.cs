using System;

namespace Godot3dFighter.Core.Data;

/// <summary>技データ、ステージデータ、規則データの形式が不正な時に投げる。</summary>
public sealed class GameDataFormatException : Exception
{
    public GameDataFormatException()
    {
    }

    public GameDataFormatException(string message)
        : base(message)
    {
    }

    public GameDataFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
