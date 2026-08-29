using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.ViewModels;
using PCEdit.Desktop.ViewModels;
using PCEdit.Desktop.Views;

namespace PCEdit.Desktop.Platform;

public sealed class AvaloniaNavigationService(
    IServiceProvider services,
    MainWindowViewModel mainViewModel,
    MainWindowAccessor mainWindow) : INavigationService
{
    private readonly IServiceProvider _services = services;
    private readonly MainWindowViewModel _mainViewModel = mainViewModel;
    private readonly MainWindowAccessor _mainWindow = mainWindow;

    private Window? _modal;

    /// <summary>Called once by <c>App</c> after the main window exists.</summary>
    public void Attach(Window window) => _mainWindow.Window = window;

    public Task GoToOverviewAsync()
    {
        _mainViewModel.NavigateTo(NavDestination.Overview);
        return Task.CompletedTask;
    }

    public Task GoToOpenFileAsync()
    {
        _mainViewModel.NavigateTo(NavDestination.OpenFile);
        return Task.CompletedTask;
    }

    public Task OpenSelectInventoryAsync(int worldObjectId) =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var vm = _services.GetRequiredService<SelectInventoryViewModel>();
            vm.Initialize(worldObjectId);

            _modal = new SelectInventoryWindow { DataContext = vm };
            await _modal.ShowDialog(_mainWindow.Require());
            _modal = null;

            // A move mutates the workspace; refresh whatever page is showing.
            _mainViewModel.ReloadCurrent();
        });

    public Task OpenLogisticsEditorAsync(int inventoryId) =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var vm = _services.GetRequiredService<LogisticsEditorViewModel>();
            vm.Initialize(inventoryId);

            _modal = new LogisticsEditorWindow { DataContext = vm };
            await _modal.ShowDialog(_mainWindow.Require());
            _modal = null;

            _mainViewModel.ReloadCurrent();
        });

    public Task CloseModalAsync()
    {
        _modal?.Close();
        _modal = null;
        return Task.CompletedTask;
    }
}
