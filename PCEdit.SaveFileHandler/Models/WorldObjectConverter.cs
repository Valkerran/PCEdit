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
        var element = document.RootElement;

        int id = 0;
        string? gId = null;
        int? linkedInventoryId = null, planet = null, growth = null, linkedWorldObjectId = null;
        string? linkedInventoryGroups = null, spawnedInstanceIds = null, position = null,
            rotation = null, mineableCount = null, color = null, panelSettings = null, text = null;
        var keyOrder = new List<string>();
        Dictionary<string, JsonElement>? extension = null;

        foreach (var property in element.EnumerateObject())
        {
            keyOrder.Add(property.Name);
            switch (property.Name)
            {
                case "id": id = property.Value.GetInt32(); break;
                case "gId": gId = property.Value.GetString(); break;
                case "liId": linkedInventoryId = property.Value.GetInt32(); break;
                case "liGrps": linkedInventoryGroups = property.Value.GetString(); break;
                case "siIds": spawnedInstanceIds = property.Value.GetString(); break;
                case "pos": position = property.Value.GetString(); break;
                case "rot": rotation = property.Value.GetString(); break;
                case "planet": planet = property.Value.GetInt32(); break;
                case "grwth": growth = property.Value.GetInt32(); break;
                case "count": mineableCount = property.Value.GetString(); break;
                case "color": color = property.Value.GetString(); break;
                case "pnls": panelSettings = property.Value.GetString(); break;
                case "linkedWo": linkedWorldObjectId = property.Value.GetInt32(); break;
                case "text": text = property.Value.GetString(); break;
                default: (extension ??= [])[property.Name] = property.Value.Clone(); break;
            }
        }

        return new WorldObject
        {
            Id = id,
            GId = gId ?? throw new JsonException("A world-object record is missing its required \"gId\" key."),
            LinkedInventoryId = linkedInventoryId,
            LinkedInventoryGroups = linkedInventoryGroups,
            SpawnedInstanceIds = spawnedInstanceIds,
            Position = position,
            Rotation = rotation,
            Planet = planet,
            Growth = growth,
            MineableCount = mineableCount,
            Color = color,
            PanelSettings = panelSettings,
            LinkedWorldObjectId = linkedWorldObjectId,
            Text = text,
            ExtensionData = extension,
            KeyOrder = keyOrder
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
