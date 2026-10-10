using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

/// <summary>
/// Reads a <see cref="WorldObject"/> while remembering the exact key order it was written with,
/// and replays that order on write. The game's world-object serialisation is not order-stable, so
/// this is the only way a load→save leaves an untouched record byte-identical. Keys the model does
/// not name are carried through <see cref="WorldObject.ExtensionData"/> and re-emitted in place.
/// Objects built in code (no <see cref="WorldObject.KeyOrder"/>) serialise in declared order.
/// </summary>
public sealed class WorldObjectConverter : JsonConverter<WorldObject>
{
    private static readonly string[] DefaultOrder =
        ["id", "gId", "liId", "liGrps", "siIds", "pos", "rot", "planet", "grwth", "count", "color", "pnls", "linkedWo", "text"];

    public override WorldObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var fields = new Fields();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            fields.KeyOrder.Add(property.Name);
            try
            {
                ReadKey(property, fields);
            }
            catch (Exception exception) when (exception is InvalidOperationException or FormatException)
            {
                // GetInt32/GetString throw these on a value of the wrong type; say which key.
                throw new JsonException($"The world-object key \"{property.Name}\" has the wrong type.", exception);
            }
        }

        return fields.ToWorldObject();
    }

    private static void ReadKey(JsonProperty property, Fields fields)
    {
        var value = property.Value;
        switch (property.Name)
        {
            case "id": fields.Id = value.GetInt32(); break;
            case "gId": fields.GId = value.GetString(); break;
            case "liId": fields.LinkedInventoryId = value.GetInt32(); break;
            case "liGrps": fields.LinkedInventoryGroups = value.GetString(); break;
            case "siIds": fields.SpawnedInstanceIds = value.GetString(); break;
            case "pos": fields.Position = value.GetString(); break;
            case "rot": fields.Rotation = value.GetString(); break;
            case "planet": fields.Planet = value.GetInt32(); break;
            case "grwth": fields.Growth = value.GetInt32(); break;
            case "count": fields.MineableCount = value.GetString(); break;
            case "color": fields.Color = value.GetString(); break;
            case "pnls": fields.PanelSettings = value.GetString(); break;
            case "linkedWo": fields.LinkedWorldObjectId = value.GetInt32(); break;
            case "text": fields.Text = value.GetString(); break;
            default: (fields.Extension ??= [])[property.Name] = value.Clone(); break;
        }
    }

    /// <summary>The values read from one record, collected before the model is built.</summary>
    private sealed class Fields
    {
        public int? Id, LinkedInventoryId, Planet, Growth, LinkedWorldObjectId;
        public string? GId, LinkedInventoryGroups, SpawnedInstanceIds, Position, Rotation,
            MineableCount, Color, PanelSettings, Text;
        public Dictionary<string, JsonElement>? Extension;
        public List<string> KeyOrder { get; } = [];

        public WorldObject ToWorldObject() => new()
        {
            Id = Id ?? throw new JsonException("A world-object record is missing its required \"id\" key."),
            GId = GId ?? throw new JsonException("A world-object record is missing its required \"gId\" key."),
            LinkedInventoryId = LinkedInventoryId,
            LinkedInventoryGroups = LinkedInventoryGroups,
            SpawnedInstanceIds = SpawnedInstanceIds,
            Position = Position,
            Rotation = Rotation,
            Planet = Planet,
            Growth = Growth,
            MineableCount = MineableCount,
            Color = Color,
            PanelSettings = PanelSettings,
            LinkedWorldObjectId = LinkedWorldObjectId,
            Text = Text,
            ExtensionData = Extension,
            KeyOrder = KeyOrder
        };
    }

    public override void Write(Utf8JsonWriter writer, WorldObject value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var written = new HashSet<string>();

        foreach (var key in value.KeyOrder ?? DefaultOrder)
        {
            if (written.Add(key))
            {
                WriteKey(writer, key, value);
            }
        }

        // Keys set in code after the record was read (an edit that added a field) aren't in the
        // stored order — append them in the canonical position.
        foreach (var key in DefaultOrder)
        {
            if (!written.Contains(key) && HasValue(key, value) && written.Add(key))
            {
                WriteKey(writer, key, value);
            }
        }

        if (value.ExtensionData is not null)
        {
            foreach (var (key, element) in value.ExtensionData)
            {
                if (written.Add(key))
                {
                    writer.WritePropertyName(key);
                    element.WriteTo(writer);
                }
            }
        }

        writer.WriteEndObject();
    }

    private static bool HasValue(string key, WorldObject o) => key switch
    {
        "id" or "gId" => true,
        "liId" => o.LinkedInventoryId is not null,
        "liGrps" => o.LinkedInventoryGroups is not null,
        "siIds" => o.SpawnedInstanceIds is not null,
        "pos" => o.Position is not null,
        "rot" => o.Rotation is not null,
        "planet" => o.Planet is not null,
        "grwth" => o.Growth is not null,
        "count" => o.MineableCount is not null,
        "color" => o.Color is not null,
        "pnls" => o.PanelSettings is not null,
        "linkedWo" => o.LinkedWorldObjectId is not null,
        "text" => o.Text is not null,
        _ => o.ExtensionData?.ContainsKey(key) == true
    };

    private static void WriteKey(Utf8JsonWriter writer, string key, WorldObject o)
    {
        switch (key)
        {
            case "id": writer.WriteNumber("id", o.Id); break;
            case "gId": writer.WriteString("gId", o.GId); break;
            case "liId": WriteOptionalNumber(writer, "liId", o.LinkedInventoryId); break;
            case "liGrps": WriteOptionalString(writer, "liGrps", o.LinkedInventoryGroups); break;
            case "siIds": WriteOptionalString(writer, "siIds", o.SpawnedInstanceIds); break;
            case "pos": WriteOptionalString(writer, "pos", o.Position); break;
            case "rot": WriteOptionalString(writer, "rot", o.Rotation); break;
            case "planet": WriteOptionalNumber(writer, "planet", o.Planet); break;
            case "grwth": WriteOptionalNumber(writer, "grwth", o.Growth); break;
            case "count": WriteOptionalString(writer, "count", o.MineableCount); break;
            case "color": WriteOptionalString(writer, "color", o.Color); break;
            case "pnls": WriteOptionalString(writer, "pnls", o.PanelSettings); break;
            case "linkedWo": WriteOptionalNumber(writer, "linkedWo", o.LinkedWorldObjectId); break;
            case "text": WriteOptionalString(writer, "text", o.Text); break;
            default:
                if (o.ExtensionData is not null && o.ExtensionData.TryGetValue(key, out var element))
                {
                    writer.WritePropertyName(key);
                    element.WriteTo(writer);
                }

                break;
        }
    }

    private static void WriteOptionalString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(name, value);
        }
    }

    private static void WriteOptionalNumber(Utf8JsonWriter writer, string name, int? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(name, value.Value);
        }
    }
}
