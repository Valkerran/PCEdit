using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// The app-bundled pick-list of logistics-group ids (a container's <c>demandGrps</c> /
/// <c>supplyGrps</c>). Backed by <c>Data/LogisticsGroups.json</c>; never reads or writes a save.
/// The list is not exhaustive — <see cref="Resolve"/> always returns a value so a user can still
/// enter an id it doesn't know.
/// </summary>
public interface ILogisticsGroupCatalog
{
    /// <summary>Every known group, ordered by display name.</summary>
    IReadOnlyList<LogisticsGroupInfo> All { get; }

    /// <summary>
    /// The entry for <paramref name="groupId"/>. An id the dataset doesn't know maps to itself
    /// (<see cref="LogisticsGroupInfo.IsUnknown"/> is then true).
    /// </summary>
    LogisticsGroupInfo Resolve(string groupId);
}
