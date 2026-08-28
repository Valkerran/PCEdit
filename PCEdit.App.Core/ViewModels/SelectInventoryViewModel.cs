using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class SelectInventoryViewModel(
    IInventoryEditor inventoryEditor,
    IScreenReaderAnnouncer announcer,
    INavigationService navigation,
    IDialogService dialogs,
    ILocalizer localizer) : ObservableObject
{
    private readonly IInventoryEditor _inventoryEditor = inventoryEditor;
    private readonly IScreenReaderAnnouncer _announcer = announcer;
    private readonly INavigationService _navigation = navigation;
    private readonly IDialogService _dialogs = dialogs;
    private readonly ILocalizer _localizer = localizer;

    [ObservableProperty]
    private int _worldObjectId;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private StatusKind _statusKind;

    [ObservableProperty]
    private string _query = string.Empty;

    public ObservableCollection<InventoryOptionView> Options { get; } = [];

    /// <summary>Options narrowed by <see cref="Query"/> (a real save has hundreds of destinations).</summary>
    public IReadOnlyList<InventoryOptionView> FilteredOptions
    {
        get
        {
            var term = Query.Trim();
            return term.Length == 0
                ? Options.ToList()
                : Options.Where(o => o.Label.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    partial void OnQueryChanged(string value) => OnPropertyChanged(nameof(FilteredOptions));

    /// <summary>Supplied by the navigation layer with the item to be moved.</summary>
    public void Initialize(int worldObjectId)
    {
        WorldObjectId = worldObjectId;
    }

    partial void OnWorldObjectIdChanged(int value)
    {
        Load();
    }

    private void Load()
    {
        Options.Clear();
        foreach (var option in _inventoryEditor.GetDestinationOptions(WorldObjectId))
        {
            Options.Add(option);
        }

        OnPropertyChanged(nameof(FilteredOptions));
    }

    [RelayCommand]
    private async Task SelectAsync(InventoryOptionView destination)
    {
        var result = _inventoryEditor.TryMoveItem(WorldObjectId, destination.InventoryId);
        if (!result.Success)
        {
            var message = result.ErrorMessage ?? _localizer[LocKeys.SelectInv_MoveIncomplete];
            StatusKind = StatusKind.Error;
            StatusMessage = message;
            _announcer.Announce(_localizer.Format(LocKeys.Announce_ErrorPrefix, message));
            await _dialogs.ShowErrorAsync(_localizer[LocKeys.SelectInv_MoveFailedTitle], message);
            return;
        }

        _announcer.Announce(_localizer.Format(LocKeys.SelectInv_Moved, destination.Label));
        await _navigation.CloseModalAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await _navigation.CloseModalAsync();
    }
}
