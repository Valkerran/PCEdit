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

    // The stack to move from, in the save's order; a single item is a stack of one.
    private IReadOnlyList<int> _stack = [];

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private StatusKind _statusKind;

    [ObservableProperty]
    private string _query = string.Empty;

    public ObservableCollection<InventoryOptionView> Options { get; } = [];

    /// <summary>Options narrowed by <see cref="Query"/> (a real save has hundreds of destinations):
    /// by name, or by id with the same rule as the Inventories page. Each carries the room the
    /// chosen <see cref="Quantity"/> needs, which decides whether it can be picked.</summary>
    public IReadOnlyList<InventoryOptionView> FilteredOptions => Offered
        .Where(o => o.Matches(Query))
        .Select(o => o with { RequiredRoom = Quantity })
        .ToList();

    /// <summary>True when no other inventory has a free slot - not merely when a search hides them.</summary>
    public bool HasNoDestinations => !Offered.Any();

    // Inventories at or over capacity are never offered: they can take nothing, and a stacking-mod
    // save is full of them (issue #64). One with some room, but less than the quantity, stays -
    // lowering the quantity makes it usable.
    private IEnumerable<InventoryOptionView> Offered => Options.Where(o => !o.IsFull);

    partial void OnQueryChanged(string value) => OnPropertyChanged(nameof(FilteredOptions));

    /// <summary>Whether there is a quantity to choose: a lone item just moves.</summary>
    public bool IsStack => MaxQuantity > 1;

    public int MaxQuantity => _stack.Count;

    /// <summary>How many of the stack to move: kept between 1 and the whole stack.</summary>
    [ObservableProperty]
    private int _quantity = 1;

    partial void OnQuantityChanged(int value)
    {
        var kept = Math.Clamp(value, 1, Math.Max(1, MaxQuantity));
        if (kept != value)
        {
            Quantity = kept;
            return;
        }

        OnPropertyChanged(nameof(FilteredOptions));
    }

    [RelayCommand]
    private void All() => Quantity = MaxQuantity;

    /// <summary>Supplied by the navigation layer with the stack to move from.</summary>
    public void Initialize(IReadOnlyList<int> worldObjectIds)
    {
        _stack = worldObjectIds.ToList();
        OnPropertyChanged(nameof(IsStack));
        OnPropertyChanged(nameof(MaxQuantity));
        Quantity = 1;
        Load();
    }

    private void Load()
    {
        Options.Clear();
        foreach (var option in _inventoryEditor.GetDestinationOptions(_stack[0]))
        {
            Options.Add(option);
        }

        OnPropertyChanged(nameof(FilteredOptions));
        OnPropertyChanged(nameof(HasNoDestinations));
    }

    [RelayCommand]
    private async Task SelectAsync(InventoryOptionView destination)
    {
        // The last of the stack, as the game takes overflow from the end.
        var result = _inventoryEditor.TryMoveItems(_stack.TakeLast(Quantity).ToList(), destination.InventoryId);
        if (!result.Success)
        {
            var message = result.ErrorMessage ?? _localizer[LocKeys.SelectInv_MoveIncomplete];
            StatusKind = StatusKind.Error;
            StatusMessage = message;
            _announcer.Announce(_localizer.Format(LocKeys.Announce_ErrorPrefix, message));
            await _dialogs.ShowErrorAsync(_localizer[LocKeys.SelectInv_MoveFailedTitle], message);
            return;
        }

        _announcer.Announce(Quantity == 1
            ? _localizer.Format(LocKeys.SelectInv_Moved, destination.Label)
            : _localizer.Format(LocKeys.SelectInv_MovedMany, Quantity, destination.Label));
        await _navigation.CloseModalAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await _navigation.CloseModalAsync();
    }
}
