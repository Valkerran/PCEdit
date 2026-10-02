using System.Text;
using System.Text.Json;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// An item's identity apart from its id: two items share a stack on the Inventories page only
/// when everything else the save holds about them is equal. Most items carry just <c>id</c> and
/// <c>gId</c>, but some do not - every <c>GeneticTrait</c> has its own <c>color</c> and
/// <c>trtInd</c> - and merging those would hide which trait the player is moving (issue #64).
/// Where the item once lay in the world (<c>pos</c>, <c>rot</c>, <c>planet</c>) is left out: crops
/// keep the random rotation they were spawned with, and counting it split a grower's 100 Eggplants
/// into 100 rows.
/// </summary>
public static class ItemStackKey
{
    // Cannot appear in a game id or in JSON text, so fields cannot run into each other.
    private const char Separator = '\u001F';

    public static string Of(WorldObject item)
    {
        var key = new StringBuilder(item.GId);
        Append(key, "liId", item.LinkedInventoryId);
        Append(key, "liGrps", item.LinkedInventoryGroups);
        Append(key, "siIds", item.SpawnedInstanceIds);
        Append(key, "grwth", item.Growth);
        Append(key, "count", item.MineableCount);
        Append(key, "color", item.Color);
        Append(key, "pnls", item.PanelSettings);
        Append(key, "linkedWo", item.LinkedWorldObjectId);
        Append(key, "text", item.Text);
        AppendUnknownKeys(key, item.ExtensionData);
        return key.ToString();
    }

    /// <summary>Sorted, because the game's key order differs between records (see
    /// <c>WorldObjectConverter</c>) and must not split a stack.</summary>
    private static void AppendUnknownKeys(StringBuilder key, IReadOnlyDictionary<string, JsonElement>? unknownKeys)
    {
        if (unknownKeys is null)
        {
            return;
        }

        foreach (var (name, value) in unknownKeys.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            Append(key, name, value.GetRawText());
        }
    }

    private static void Append(StringBuilder key, string name, object? value)
    {
        if (value is not null)
        {
            key.Append(Separator).Append(name).Append('=').Append(value);
        }
    }
}
