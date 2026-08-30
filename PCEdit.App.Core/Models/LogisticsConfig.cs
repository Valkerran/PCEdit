namespace PCEdit.App.Core.Models;

/// <summary>
/// A logistics container's demand / supply / priority, parsed from
/// <c>Inventory.DemandGroups</c> / <c>SupplyGroups</c> / <c>Priority</c>. Group ids are raw —
/// the view resolves friendly names via <c>ILogisticsGroupCatalog</c>. <see cref="Priority"/> is
/// the raw save int (-3..3 for the named levels; any other value is carried through untouched).
/// </summary>
public sealed record LogisticsConfig(
    IReadOnlyList<string> DemandGroupIds,
    IReadOnlyList<string> SupplyGroupIds,
    int Priority);
