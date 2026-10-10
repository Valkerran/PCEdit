namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// A page that holds input the user has typed but not yet applied. When the UI language changes,
/// the shell calls <see cref="Relocalize"/> instead of <see cref="ILoadable.Load"/>: the page
/// refreshes the translated text it has baked into its own state and keeps everything the user
/// entered, where a reload threw it away without warning (issue #94).
/// </summary>
public interface IRelocalizable
{
    void Relocalize();
}
