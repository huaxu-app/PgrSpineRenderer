using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PgrSpineRenderer;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, Converters = [typeof(Vector2Converter)])]
[JsonSerializable(typeof(Index.Index))]
internal sealed partial class SerializerContext : JsonSerializerContext
{
}

internal sealed class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array.");

        reader.Read();
        float x = reader.GetSingle();

        reader.Read();
        float y = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array.");

        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}