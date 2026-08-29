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
    /// Opens the "choose a destination inventory" screen for the given world object,
    /// as a modal/secondary view.
    /// </summary>
    Task OpenSelectInventoryAsync(int worldObjectId);

    /// <summary>Opens the demand/supply editor for a logistics container, as a modal view.</summary>
    Task OpenLogisticsEditorAsync(int inventoryId);

    /// <summary>Closes the current modal/secondary view and returns to the previous screen.</summary>
    Task CloseModalAsync();
}
