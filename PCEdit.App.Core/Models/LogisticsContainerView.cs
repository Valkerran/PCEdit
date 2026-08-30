namespace PCEdit.App.Core.Models;

/// <summary>One logistics container, as the demand/supply editor needs it.</summary>
public sealed record LogisticsContainerView(
    int InventoryId,
    string Label,
    IReadOnlyList<string> DemandGroupIds,
    IReadOnlyList<string> SupplyGroupIds,
    LogisticsPriority Priority);
