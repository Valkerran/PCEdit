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

    public T Deserialize<T>(string content, int sectionIndex, int? recordIndex = null)
    {
        var where = recordIndex is null ? $"section {sectionIndex}" : $"section {sectionIndex}, record {recordIndex + 1}";
        try
        {
            return JsonSerializer.Deserialize<T>(content.Trim(), Options)
                ?? throw new InvalidDataException($"Save-file {where} is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Save-file {where} is not valid: {WithoutPosition(exception.Message)}", exception);
        }
    }

    // The reader's line and byte offsets count from the start of this one record, not the file, so
    // in a large save they would read as a file position and mislead; section, record and path say where.
    private static string WithoutPosition(string message)
    {
        var cut = message.IndexOf(" | LineNumber:", StringComparison.Ordinal);
        return cut < 0 ? message : message[..cut] + ".";
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
