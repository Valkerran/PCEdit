namespace PCEdit.App.Core.Models;

/// <param name="PlanetHint">Localized, human-readable note about the landmark's world hash.</param>
public sealed record LandmarkOption(int WorldObjectId, string GId, string Position, string PlanetHint)
{
    public string Label => $"{GId} (#{WorldObjectId})";
}
