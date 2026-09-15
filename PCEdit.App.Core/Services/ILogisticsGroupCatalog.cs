using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// The app-bundled pick-lists of item ids valid for a container's <c>demandGrps</c> and
/// <c>supplyGrps</c>. Backed by the item catalog; never reads or writes a save. The lists are not
/// exhaustive — <see cref="Resolve"/> always returns a value so a user can still enter an unknown id.
/// </summary>
public interface ILogisticsGroupCatalog
{
    /// <summary>Every non-deprecated item valid for either logistics direction.</summary>
    IReadOnlyList<LogisticsGroupInfo> All { get; }

    /// <summary>Every non-deprecated item valid in a demand list.</summary>
    IReadOnlyList<LogisticsGroupInfo> DemandGroups { get; }

    /// <summary>Every non-deprecated item valid in a supply list.</summary>
    IReadOnlyList<LogisticsGroupInfo> SupplyGroups { get; }

    /// <summary>
    /// The entry for <paramref name="groupId"/>. An id the dataset doesn't know maps to itself
    /// (<see cref="LogisticsGroupInfo.IsUnknown"/> is then true).
    /// </summary>
    LogisticsGroupInfo Resolve(string groupId);
}
