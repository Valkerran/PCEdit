namespace PCEdit.App.Core.Models;

/// <summary>What a given inventory belongs to — drives the Inventories page type filter.</summary>
public enum InventoryKind
{
    PlayerInventory,
    Equipment,
    Container,
    Other,
}
