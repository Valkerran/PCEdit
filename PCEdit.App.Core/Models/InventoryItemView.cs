namespace PCEdit.App.Core.Models;

public sealed record InventoryItemView(
    int WorldObjectId,
    string GId,
    int InventoryId,
    string DisplayName,
    string IconFile);
