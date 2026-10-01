namespace PCEdit.App.Core.Services;

/// <summary>
/// UI-framework-agnostic navigation used by the shared ViewModels. The UI head
/// (Avalonia) supplies the implementation.
/// </summary>
public interface INavigationService
{
    /// <summary>Switches the main content to the Overview after a save file is loaded.</summary>
    Task GoToOverviewAsync();

    /// <summary>Switches the main content to the Open File page (from an empty-state prompt).</summary>
    Task GoToOpenFileAsync();

    /// <summary>
    /// Switches the main content to the Inventories page, filtered to the inventories that need
    /// attention (from the Overview's over-full banner, issue #64).
    /// </summary>
    Task GoToInventoriesNeedingAttentionAsync();

    /// <summary>
    /// Opens the "choose a destination inventory" screen, as a modal/secondary view, for a stack
    /// of items from one inventory in the save's order; the player picks how many of them to move.
    /// A single item is a stack of one.
    /// </summary>
    Task OpenSelectInventoryAsync(IReadOnlyList<int> worldObjectIds);

    /// <summary>Opens the demand/supply editor for a logistics container, as a modal view.</summary>
    Task OpenLogisticsEditorAsync(int inventoryId);

    /// <summary>Closes the current modal/secondary view and returns to the previous screen.</summary>
    Task CloseModalAsync();
}
