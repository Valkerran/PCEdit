using PCEdit.SaveFileHandler;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

public sealed class PlanetIndex(ISaveFileWorkspace workspace) : IPlanetIndex
{
    private readonly ISaveFileWorkspace _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

    public IReadOnlyList<string> KnownPlanetIds()
    {
        var save = _workspace.Current;
        if (save is null)
        {
            return [];
        }

        return new[] { save.Metadata.PlanetId }
            .Concat(save.Terraformations.Select(t => t.PlanetId))
            .Concat(save.Players.Select(p => p.PlanetId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? ResolvePlanetId(int? planetHash)
    {
        if (planetHash is not { } hash)
        {
            return null;
        }

        foreach (var planetId in KnownPlanetIds())
        {
            if (PlanetHash.Of(planetId) == hash)
            {
                return planetId;
            }
        }

        return null;
    }
}
