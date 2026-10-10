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
            // JsonProperty.Name builds a new string on every access, and this runs for every key
            // of every world object in the save, so it is read once.
            var name = property.Name;
            RejectNull(name, property.Value);
            RejectDuplicate(name, fields);
            fields.KeyOrder.Add(name);
            try
            {
                ReadKey(name, property.Value, fields);
            }
            catch (Exception exception) when (exception is InvalidOperationException or FormatException)
            {
                // GetInt32/GetString throw these on a value of the wrong type; say which key.
                throw new JsonException($"The world-object key \"{name}\" has the wrong type.", exception);
            }
        }

        return fields.ToWorldObject();
    }

    // The serializer's own duplicate check does not reach a converter that parses the record
    // itself, so it is repeated here (issue #126). KeyOrder already holds the keys seen so far.
    private static void RejectDuplicate(string name, Fields fields)
    {
        if (fields.KeyOrder.Contains(name))
        {
            throw new JsonException($"The world-object key \"{name}\" appears more than once.");
        }
    }

    // Only the keys the model names: an unknown key holding null goes to extension data and is
    // written back as it came (issue #125).
    private static void RejectNull(string name, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null && DefaultOrder.Contains(name))
        {
            throw new JsonException($"The world-object key \"{name}\" is null.");
        }
    }

    private static void ReadKey(string name, JsonElement value, Fields fields)
    {
        switch (name)
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
            default: (fields.Extension ??= [])[name] = value.Clone(); break;
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
        WriteInRecordedOrder(writer, value, written);
        WriteKeysAddedInCode(writer, value, written);
        WriteExtensionData(writer, value, written);

        writer.WriteEndObject();
    }

    private static void WriteInRecordedOrder(Utf8JsonWriter writer, WorldObject value, HashSet<string> written)
    {
        foreach (var key in value.KeyOrder ?? DefaultOrder)
        {
            if (written.Add(key))
            {
                WriteKey(writer, key, value);
            }
        }
    }

    // Keys set in code after the record was read (an edit that added a field) aren't in the
    // stored order — append them after the replayed keys, in `DefaultOrder`.
    private static void WriteKeysAddedInCode(Utf8JsonWriter writer, WorldObject value, HashSet<string> written)
    {
        foreach (var key in DefaultOrder)
        {
            if (!written.Contains(key) && HasValue(key, value) && written.Add(key))
            {
                WriteKey(writer, key, value);
            }
        }
    }

    private static void WriteExtensionData(Utf8JsonWriter writer, WorldObject value, HashSet<string> written)
    {
        if (value.ExtensionData is null)
        {
            return;
        }

        foreach (var (key, element) in value.ExtensionData)
        {
            if (written.Add(key))
            {
                writer.WritePropertyName(key);
                element.WriteTo(writer);
            }
        }
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
