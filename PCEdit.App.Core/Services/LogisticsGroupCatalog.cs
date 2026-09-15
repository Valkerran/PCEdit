using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// <see cref="ILogisticsGroupCatalog"/> projected from item-catalog logistics metadata.
/// </summary>
public sealed class LogisticsGroupCatalog(IItemCatalog itemCatalog) : ILogisticsGroupCatalog
{
    private readonly IItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));

    public IReadOnlyList<LogisticsGroupInfo> DemandGroups { get; } = CreateChoices(
        itemCatalog,
        item => item.CanDemand);

    public IReadOnlyList<LogisticsGroupInfo> SupplyGroups { get; } = CreateChoices(
        itemCatalog,
        item => item.CanSupply);

    public IReadOnlyList<LogisticsGroupInfo> All { get; } = CreateChoices(
        itemCatalog,
        item => item.CanDemand || item.CanSupply);

    public LogisticsGroupInfo Resolve(string groupId)
    {
        ArgumentNullException.ThrowIfNull(groupId);

        var item = _itemCatalog.ResolveInfo(groupId);
        return new LogisticsGroupInfo(groupId, item.DisplayName, item.IsKnown);
    }

    private static IReadOnlyList<LogisticsGroupInfo> CreateChoices(
        IItemCatalog catalog,
        Func<ItemCatalogInfo, bool> isEligible) =>
        catalog.All
            .Where(item => !item.IsDeprecated && isEligible(item))
            .Select(item => new LogisticsGroupInfo(item.GId, item.DisplayName, IsKnown: true))
            .OrderBy(group => group.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
}
