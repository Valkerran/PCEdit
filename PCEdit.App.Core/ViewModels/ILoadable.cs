namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// A page view model that (re)reads its state from the workspace each time its page is shown.
/// Heads call <see cref="Load"/> when navigating to the page.
/// </summary>
public interface ILoadable
{
    void Load();
}
