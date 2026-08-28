using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler;

public sealed class JsonRecordSerializer : IJsonRecordSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
}
