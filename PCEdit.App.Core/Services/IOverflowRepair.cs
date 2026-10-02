namespace PCEdit.App.Core.Services;

/// <summary>How many items of one type a repair removes, by display name.</summary>
public sealed record RemovedItemCount(string DisplayName, int Count);

/// <summary>One item type over capacity, for the player to choose whether to move it first.</summary>
public sealed record ExcessType(string GId, string DisplayName, int Count);

/// <summary>What a repair removes: the totals, and the item types losing the most first.</summary>
public sealed record RepairPlan(int ItemCount, int ContainerCount, IReadOnlyList<RemovedItemCount> ByType)
{
    public int MovedCount { get; init; }

    public int DestinationCount { get; init; }

    public int FreeSlots { get; init; }

    public IReadOnlyList<ExcessType> ExcessByType { get; init; } = [];
}

public interface IOverflowRepair
{
    RepairPlan Plan(int multiple, IReadOnlyList<string>? moveFirst = null);

    RepairPlan Apply(int multiple, IReadOnlyList<string>? moveFirst = null);
}
