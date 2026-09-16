using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Godot3dFighter.Game.E2e;

internal sealed class E2eScenario
{
    [JsonPropertyName("flow")]
    public string Flow { get; init; } = "";

    [JsonPropertyName("rules")]
    public string? Rules { get; init; }

    [JsonPropertyName("stage")]
    public string? Stage { get; init; }

    [JsonPropertyName("characters")]
    public IReadOnlyList<string>? Characters { get; init; }

    [JsonPropertyName("steps")]
    public IReadOnlyList<E2eStep> Steps { get; init; } = [];
}

internal sealed class E2eStep
{
    [JsonPropertyName("expectScreen")]
    public string? ExpectScreen { get; init; }

    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("expectPhase")]
    public string? ExpectPhase { get; init; }

    [JsonPropertyName("frames")]
    public int? Frames { get; init; }

    [JsonPropertyName("p1")]
    public string? P1 { get; init; }

    [JsonPropertyName("p2")]
    public string? P2 { get; init; }

    [JsonPropertyName("expectRoundEnd")]
    public E2eRoundEndExpectation? ExpectRoundEnd { get; init; }

    [JsonPropertyName("expectMatchEnd")]
    public E2eMatchEndExpectation? ExpectMatchEnd { get; init; }

    [JsonPropertyName("withinFrames")]
    public int? WithinFrames { get; init; }
}

internal sealed class E2eRoundEndExpectation
{
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "";

    [JsonPropertyName("winners")]
    public IReadOnlyList<int> Winners { get; init; } = [];
}

internal sealed class E2eMatchEndExpectation
{
    [JsonPropertyName("winners")]
    public IReadOnlyList<int> Winners { get; init; } = [];
}

internal sealed class E2eFlow
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
}
