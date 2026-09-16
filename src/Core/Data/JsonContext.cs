using System.Text.Json.Serialization;

namespace Godot3dFighter.Core.Data;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    Converters = [typeof(JsonStringEnumConverter<MoveKind>), typeof(JsonStringEnumConverter<Posture>),
        typeof(JsonStringEnumConverter<HitHeight>), typeof(JsonStringEnumConverter<ThrowSide>),
        typeof(JsonStringEnumConverter<RingShape>), typeof(JsonStringEnumConverter<EdgeKind>)])]
[JsonSerializable(typeof(CharacterDataDto))]
[JsonSerializable(typeof(StageDataDto))]
[JsonSerializable(typeof(RulesDto))]
internal sealed partial class JsonContext : JsonSerializerContext;
