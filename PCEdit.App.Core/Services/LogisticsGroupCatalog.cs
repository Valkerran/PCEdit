using System.Text.Json;
using System.Text.Json.Serialization;
using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// <see cref="ILogisticsGroupCatalog"/> backed by the embedded <c>Data/LogisticsGroups.json</c>
/// dataset. Read once at construction (a few KB) and held in memory. Regenerate the JSON with
/// <c>tools/item-catalog/gen_logistics_groups.py</c>.
/// </summary>
public sealed class LogisticsGroupCatalog : ILogisticsGroupCatalog
{
    private const string ResourceSuffix = "Data.LogisticsGroups.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlyDictionary<string, string> _names;

    public LogisticsGroupCatalog()
        : this(OpenEmbeddedResource())
    {
    }

    internal LogisticsGroupCatalog(Stream json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var document = JsonSerializer.Deserialize<CatalogDocument>(json, SerializerOptions)
            ?? throw new InvalidDataException("The logistics-group catalog stream is empty or invalid.");

        _names = document.Groups;
        All = document.Groups
            .Select(pair => new LogisticsGroupInfo(pair.Key, pair.Value, IsKnown: true))
            .OrderBy(group => group.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<LogisticsGroupInfo> All { get; }

    public LogisticsGroupInfo Resolve(string groupId)
    {
        ArgumentNullException.ThrowIfNull(groupId);

        return _names.TryGetValue(groupId, out var name) && !string.IsNullOrEmpty(name)
            ? new LogisticsGroupInfo(groupId, name, IsKnown: true)
            : new LogisticsGroupInfo(groupId, groupId, IsKnown: false);
    }

    private static Stream OpenEmbeddedResource()
    {
        var assembly = typeof(LogisticsGroupCatalog).Assembly;
        var resourceName = Array.Find(
            assembly.GetManifestResourceNames(),
            name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Embedded logistics-group catalog resource ('*{ResourceSuffix}') was not found.");

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded logistics-group catalog resource '{resourceName}' could not be opened.");
    }

    private sealed class CatalogDocument
    {
        [JsonPropertyName("groups")]
        public Dictionary<string, string> Groups { get; init; } = new();
    }
}
