namespace PCEdit.App.Core.Models;

/// <summary>
/// A logistics container's demand / supply / priority, parsed from
/// <c>Inventory.DemandGroups</c> / <c>SupplyGroups</c> / <c>Priority</c>. Group ids are raw —
/// the view resolves friendly names via <c>ILogisticsGroupCatalog</c>.
/// </summary>
public sealed record LogisticsConfig(
    IReadOnlyList<string> DemandGroupIds,
    IReadOnlyList<string> SupplyGroupIds,
    LogisticsPriority Priority);
