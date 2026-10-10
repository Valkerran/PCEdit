using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler;

public sealed class JsonRecordSerializer : IJsonRecordSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = MinimalJsonEncoder.Instance,
        // A null in a non-nullable property is malformed input, not a value to drop on the next
        // save (issue #76).
        RespectNullableAnnotations = true,
        Converters = { new GameNumberConverter() }
    };

    public T Deserialize<T>(string content, int sectionIndex)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(content.Trim(), Options)
                ?? throw new InvalidDataException($"Save-file section {sectionIndex} is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Save-file section {sectionIndex} is not valid JSON.", exception);
        }
    }

    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    /// <summary>Reads a <see cref="GameNumber"/> keeping its exact text, and writes that text back
    /// untouched; a number set in code is formatted by <see cref="GameNumber.ToString"/>.</summary>
    private sealed class GameNumberConverter : JsonConverter<GameNumber>
    {
        public override GameNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number || !reader.TryGetDouble(out var value) || !double.IsFinite(value))
            {
                throw new JsonException("Expected a finite number.");
            }

            var raw = reader.HasValueSequence ? Encoding.UTF8.GetString(reader.ValueSequence) : Encoding.UTF8.GetString(reader.ValueSpan);
            return new GameNumber(value) { RawText = raw };
        }

        public override void Write(Utf8JsonWriter writer, GameNumber value, JsonSerializerOptions options)
            => writer.WriteRawValue(value.ToString());
    }
}
