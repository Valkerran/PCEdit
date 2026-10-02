namespace PCEdit.App.Core.Models;

public sealed record InventoryItemView(
    int WorldObjectId,
    string GId,
    int InventoryId,
    string DisplayName,
    string IconFile)
{
    /// <summary>The item's identity apart from its id (<c>ItemStackKey.Of</c>); items sharing it
    /// share a row on the card. Defaults to the type id alone.</summary>
    public string StackKey { get; init; } = GId;
}
