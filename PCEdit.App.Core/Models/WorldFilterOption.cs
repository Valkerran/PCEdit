namespace PCEdit.App.Core.Models;

/// <summary>
/// One entry in a "filter by world" picker. <see cref="PlanetId"/> is null for the two
/// pseudo-entries: the "all worlds" entry (<see cref="IsAll"/> true) and the "unknown world"
/// entry (<see cref="IsAll"/> false), which is why they can't be distinguished by id alone.
/// </summary>
public sealed record WorldFilterOption(string? PlanetId, string Label, bool IsAll)
{
    public static WorldFilterOption All(string label) => new(null, label, true);

    public static WorldFilterOption Unknown(string label) => new(null, label, false);

    public static WorldFilterOption ForPlanet(string planetId) => new(planetId, planetId, false);

    /// <summary>Does an inventory/landmark with this (possibly null) world id pass this filter?</summary>
    public bool Accepts(string? worldPlanetId) =>
        IsAll
        || (PlanetId is null
            ? worldPlanetId is null
            : string.Equals(PlanetId, worldPlanetId, StringComparison.OrdinalIgnoreCase));
}
