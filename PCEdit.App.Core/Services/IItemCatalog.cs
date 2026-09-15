using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// Looks up presentation data (friendly name + icon) for an item
/// <see cref="SaveFileHandler.Models.WorldObject.GId"/>. Backed by an app-bundled
/// dataset; it never reads or writes the save file.
/// </summary>
public interface IItemCatalog
{
    /// <summary>Every known item, in catalog order.</summary>
    IReadOnlyList<ItemCatalogInfo> All { get; }

    /// <summary>
    /// Resolves the display name and icon for <paramref name="gId"/>. Always
    /// returns a value — an unknown id maps to itself plus the fallback icon.
    /// </summary>
    ItemDisplayInfo Resolve(string gId);

    /// <summary>
    /// Resolves all catalog metadata for <paramref name="gId"/>. Unknown ids
    /// are returned with disabled capabilities and <see cref="ItemCatalogInfo.IsKnown"/> false.
    /// </summary>
    ItemCatalogInfo ResolveInfo(string gId);
}
