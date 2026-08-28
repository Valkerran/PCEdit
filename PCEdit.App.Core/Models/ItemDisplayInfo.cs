namespace PCEdit.App.Core.Models;

/// <summary>
/// Resolved presentation data for an item <c>GId</c>, from the app-bundled item
/// catalog. <see cref="IconFile"/> is a category-icon file name (e.g.
/// <c>"cat_ore.png"</c>), never null. For an unknown <c>GId</c>,
/// <see cref="DisplayName"/> is the raw id and <see cref="IconFile"/> is the
/// fallback category icon.
/// </summary>
public sealed record ItemDisplayInfo(string GId, string DisplayName, string IconFile);
