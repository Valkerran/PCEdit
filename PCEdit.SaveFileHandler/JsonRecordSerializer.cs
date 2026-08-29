using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler;

public sealed class JsonRecordSerializer : IJsonRecordSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new GameDecimalConverter() }
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

    /// <summary>
    /// The game writes every decimal-valued field with a fractional part (<c>0.0</c>, <c>-1.0</c>,
    /// <c>370.0</c>). System.Text.Json would drop the trailing <c>.0</c> from a whole number, which
    /// shows up as noise in a load→save diff after an edit — so force at least one decimal place.
    /// </summary>
    private sealed class GameDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDecimal();

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            var text = value.ToString(CultureInfo.InvariantCulture);
            if (!text.Contains('.'))
            {
                text += ".0";
            }

            writer.WriteRawValue(text);
        }
    }
}
