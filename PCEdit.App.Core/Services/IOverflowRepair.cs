namespace PCEdit.App.Core.Services;

/// <summary>How many items of one type a repair removes, by display name.</summary>
public sealed record RemovedItemCount(string DisplayName, int Count);

/// <summary>What a repair removes: the totals, and the item types losing the most first.</summary>
public sealed record RepairPlan(int ItemCount, int ContainerCount, IReadOnlyList<RemovedItemCount> ByType);

public interface IOverflowRepair
{
    RepairPlan Plan(int multiple);

    RepairPlan Apply(int multiple);
}
