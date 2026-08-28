using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Fakes;

/// <summary>Records navigation calls made by the shared ViewModels.</summary>
internal sealed class FakeNavigationService : INavigationService
{
    public int OverviewCount { get; private set; }
    public int OpenFileCount { get; private set; }
    public List<int> SelectInventoryRequests { get; } = [];
    public int CloseModalCount { get; private set; }

    public Task GoToOverviewAsync()
    {
        OverviewCount++;
        return Task.CompletedTask;
    }

    public Task GoToOpenFileAsync()
    {
        OpenFileCount++;
        return Task.CompletedTask;
    }

    public Task OpenSelectInventoryAsync(int worldObjectId)
    {
        SelectInventoryRequests.Add(worldObjectId);
        return Task.CompletedTask;
    }

    public Task CloseModalAsync()
    {
        CloseModalCount++;
        return Task.CompletedTask;
    }
}
