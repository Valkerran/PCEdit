namespace PCEdit.App.Core.Models;

/// <param name="PlanetHint">Localized, human-readable note about the landmark's world (the
/// resolved planet id when known, otherwise a note about the raw world hash).</param>
/// <param name="PlanetId">The resolved planet id this landmark sits on, or null when its
/// <c>WorldObject.Planet</c> hash is absent or matches no known planet.</param>
public sealed record LandmarkOption(int WorldObjectId, string GId, string Position, string PlanetHint, string? PlanetId = null)
{
    public string Label => $"{GId} (#{WorldObjectId})";
}
