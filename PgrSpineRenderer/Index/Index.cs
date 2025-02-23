using System.Numerics;
using System.Text.Json.Serialization;

namespace PgrSpineRenderer.Index;

public record Index
{
    public required string Name { get; init; }
    public Vector2? Size { get; init; }
    public required Entry[] Spines { get; init; }
    public BoneFollower[] BoneFollowers { get; set; } = [];
    public RenderQuirk? RenderQuirk { get; set; }
    public string? DefaultAnimation { get; set; }
};

[JsonConverter(typeof(JsonStringEnumConverter<RenderQuirk>))]
public enum RenderQuirk
{
    Short
}

public record Entry
{
    public required string Name { get; init; }
    public Vector2? Position { get; init; }
    public Vector2? Size { get; init; }
    public float Scale { get; init; } = 1;
    public Vector2 Pivot { get; init; } = new(0.5f, 0.5f);
}

public record BoneFollower
{
    public required string Bone { get; init; }
    public required string Skeleton { get; init; }
    public required string[] Spines { get; init; }
}