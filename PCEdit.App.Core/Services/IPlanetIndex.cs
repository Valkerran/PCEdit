namespace PCEdit.App.Core.Services;

/// <summary>
/// Resolves the planets (worlds) present in the loaded save and bridges the two ways the save
/// file identifies a planet: the string <c>PlanetId</c> (on players / terraformations / metadata)
/// and the integer <c>WorldObject.Planet</c> hash.
/// </summary>
public interface IPlanetIndex
{
    /// <summary>
    /// Distinct planet ids in the current save — the union of <c>Metadata.PlanetId</c>,
    /// every <c>Terraformations[].PlanetId</c> and every <c>Players[].PlanetId</c> — non-blank,
    /// case-insensitively distinct, ordered. Empty when no save is loaded.
    /// </summary>
    IReadOnlyList<string> KnownPlanetIds();

    /// <summary>
    /// Maps a <c>WorldObject.Planet</c> hash back to one of <see cref="KnownPlanetIds"/>, or null
    /// when the hash is absent or matches no known planet.
    /// </summary>
    string? ResolvePlanetId(int? planetHash);
}
