namespace PCEdit.App.Core.Models;

/// <summary>One logistics container, as the demand/supply editor needs it. <see cref="Priority"/>
/// is the raw save int.</summary>
public sealed record LogisticsContainerView(
    int InventoryId,
    string Label,
    IReadOnlyList<string> DemandGroupIds,
    IReadOnlyList<string> SupplyGroupIds,
    int Priority);
