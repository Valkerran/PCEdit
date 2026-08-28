using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// Looks up presentation data (friendly name + icon) for an item
/// <see cref="SaveFileHandler.Models.WorldObject.GId"/>. Backed by an app-bundled
/// dataset; it never reads or writes the save file.
/// </summary>
public interface IItemCatalog
{
    /// <summary>
    /// Resolves the display name and icon for <paramref name="gId"/>. Always
    /// returns a value — an unknown id maps to itself plus the fallback icon.
    /// </summary>
    ItemDisplayInfo Resolve(string gId);
}
