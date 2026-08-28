namespace PCEdit.App.Core.Models;

public sealed record InventoryOptionView(int InventoryId, string Label, int Count, int Size)
{
    public bool IsFull => Count >= Size;

    public string CapacityLabel => $"{Count}/{Size}";

    /// <summary>Accessible name for the destination button, including the full/disabled reason.</summary>
    public string AccessibleLabel => IsFull
        ? $"{Label}, full, {CapacityLabel}"
        : $"{Label}, {CapacityLabel}";
}
