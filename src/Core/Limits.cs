namespace Godot3dFighter.Core;

/// <summary>格闘コアの定数と上限。値と理由はdocs/design/01-data-structures.mdの「定数と上限」を正とする。</summary>
public static class Limits
{
    public const int FramesPerSecond = 60;
    public const int InputBufferFrames = 64;
    public const int PlayerCount = 2;
    public const int CommandWindowFrames = 10;
    public const int SimultaneousPressFrames = 2;
    public const int BufferFrames = 5;
    public const int TapFrames = 6;
    public const int MaxCommandDirections = 4;
    public const int MaxCommandButtons = 3;
    public const int MaxHitCapsulesPerWindow = 4;
    public const int MaxHurtCapsulesPerWindow = 8;
    public const int MaxWindowsPerMove = 16;
    public const int MaxMovesPerCharacter = 128;
    public const int MaxMotionSegmentsPerMove = 8;
    public const int MaxRingEdges = 8;
    public const int PositionLimitMeters = 1024;
    public const int SinTableSize = 1024;
    public const int AtanTableSize = 257;
    public const byte NoMove = 255;
}
