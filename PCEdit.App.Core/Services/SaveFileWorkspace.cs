using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.SaveFileHandler;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

public sealed partial class SaveFileWorkspace : ObservableObject, ISaveFileWorkspace
{
    private readonly IPlanetCrafterSaveFileStore _store;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly ILocalizer _localizer;
    private readonly ISaveBackupService _backups;

    // Whether the file currently loaded has been copied aside yet. Reset by Load, so each newly
    // opened file gets exactly one attempt on its first save.
    private bool _backedUpSinceLoad;

    // The save status is held as a resource key so it re-renders when the UI language changes.
    private string? _saveStatusKey;

    public SaveFileWorkspace(
        IPlanetCrafterSaveFileStore store,
        IScreenReaderAnnouncer announcer,
        ILocalizer localizer,
        ISaveBackupService backups)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _announcer = announcer ?? throw new ArgumentNullException(nameof(announcer));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _backups = backups ?? throw new ArgumentNullException(nameof(backups));

        _localizer.CultureChanged += (_, _) => OnPropertyChanged(nameof(SaveStatus));
    }

    [ObservableProperty]
    private PlanetCrafterSaveFile? _current;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isDirty;

    public string? SaveStatus => _saveStatusKey is null ? null : _localizer[_saveStatusKey];

    public bool IsLoaded => Current is not null;

    private void SetSaveStatus(string? key)
    {
        _saveStatusKey = key;
        OnPropertyChanged(nameof(SaveStatus));
    }

    partial void OnCurrentChanged(PlanetCrafterSaveFile? value)
    {
        OnPropertyChanged(nameof(IsLoaded));
    }

    partial void OnIsDirtyChanged(bool value)
    {
        // A fresh edit invalidates any lingering "Saved." confirmation.
        if (value)
        {
            SetSaveStatus(null);
        }
    }

    public void Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Current = _store.Load(path);
        FilePath = path;
        IsDirty = false;
        _backedUpSinceLoad = false;
        SetSaveStatus(null);
    }

    [RelayCommand(CanExecute = nameof(IsDirty))]
    public void Save()
    {
        if (Current is null || FilePath is null)
        {
            throw new InvalidOperationException("No save file is loaded.");
        }

        try
        {
            BackUpOnce(FilePath);
            _store.Save(FilePath, Current);
            IsDirty = false;
            SetSaveStatus(LocKeys.Save_Ok);
            _announcer.Announce(_localizer[LocKeys.Save_OkAnnounce]);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Could not save '{FilePath}': {ex}");
            SetSaveStatus(LocKeys.Save_Failed);
            _announcer.Announce(_localizer[LocKeys.Save_FailedAnnounce]);
        }
    }

    /// <summary>
    /// Copies the save aside as it was before PCEdit first wrote to it.
    /// </summary>
    /// <remarks>
    /// Only the first save after a load is worth keeping: by the second, the file on disk is
    /// already PCEdit's own output, and the pristine copy - the one that cannot be reconstructed -
    /// is gone. The attempt therefore counts whether or not it succeeded; retrying later would
    /// capture something already modified and file it as if it were the original.
    ///
    /// A failed backup never blocks the save. Refusing to write the player's edit because a
    /// safety copy could not be taken inverts the priority - the edit is what they asked for.
    /// </remarks>
    private void BackUpOnce(string path)
    {
        if (_backedUpSinceLoad)
        {
            return;
        }

        _backedUpSinceLoad = true;

        try
        {
            _backups.BackUp(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Could not back up '{path}': {ex}");
        }
    }

    public void MutateUnlocks(Func<SaveFileUnlocks, SaveFileUnlocks> mutate)
    {
        var save = RequireCurrent();

        Current = save with { Unlocks = mutate(save.Unlocks) };
        IsDirty = true;
    }

    public void ReplaceTerraformation(string planetId, Func<PlanetTerraformation, PlanetTerraformation> mutate)
    {
        var save = RequireCurrent();

        var index = save.Terraformations.FindIndex(t => string.Equals(t.PlanetId, planetId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            throw new InvalidOperationException($"Terraformation data for planet '{planetId}' was not found in the loaded save.");
        }

        save.Terraformations[index] = mutate(save.Terraformations[index]);
        IsDirty = true;
        OnPropertyChanged(nameof(Current));
    }

    public void ReplacePlayer(long playerId, Func<PlayerData, PlayerData> mutate)
    {
        var save = RequireCurrent();

        var index = save.Players.FindIndex(p => p.Id == playerId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Player {playerId} was not found in the loaded save.");
        }

        save.Players[index] = mutate(save.Players[index]);
        IsDirty = true;
        OnPropertyChanged(nameof(Current));
    }

    public void ReplaceInventory(int inventoryId, Func<Inventory, Inventory> mutate)
    {
        var save = RequireCurrent();

        var index = save.Inventories.FindIndex(i => i.Id == inventoryId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Inventory {inventoryId} was not found in the loaded save.");
        }

        save.Inventories[index] = mutate(save.Inventories[index]);
        IsDirty = true;
        OnPropertyChanged(nameof(Current));
    }

    public void GrantTerraTokens(long playerId, int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "The amount to grant must be positive.");
        }

        MutateUnlocks(unlocks => unlocks with
        {
            TerraTokens = unlocks.TerraTokens + amount,
            AllTimeTerraTokens = unlocks.AllTimeTerraTokens + amount
        });

        ReplacePlayer(playerId, player => player with
        {
            TotalTerraTokenEarned = player.TotalTerraTokenEarned + amount
        });
    }

    private PlanetCrafterSaveFile RequireCurrent()
    {
        return Current ?? throw new InvalidOperationException("No save file is loaded.");
    }
}
