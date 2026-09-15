namespace PCEdit.App.Core.Models;

/// <summary>
/// Catalog metadata for an item <c>GId</c>. Unknown ids resolve to their raw id,
/// the fallback icon, disabled logistics capabilities, and <see cref="IsKnown"/> false.
/// </summary>
public sealed record ItemCatalogInfo(
    string GId,
    string DisplayName,
    string IconFile,
    bool CanDemand,
    bool CanSupply,
    bool IsDeprecated,
    string? AddedIn,
    string? DeprecatedIn,
    bool IsKnown);
