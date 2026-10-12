using CommunityToolkit.Mvvm.ComponentModel;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.ViewModels;
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

    // The file as PCEdit last read or wrote it; see HasChangedOnDisk.
    private string? _fingerprint;

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
    private bool _isDirty;

    public string? SaveStatus => _saveStatusKey is null ? null : _localizer[_saveStatusKey];

    public bool IsLoaded => Current is not null;

    public StatusKind SaveStatusKind { get; private set; }

    private void SetSaveStatus(string? key, StatusKind kind = StatusKind.Info)
    {
        _saveStatusKey = key;
        SaveStatusKind = kind;
        OnPropertyChanged(nameof(SaveStatus));
        OnPropertyChanged(nameof(SaveStatusKind));
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

        // Taken before the read: a write that lands during the load then shows as a change,
        // which errs towards asking.
        var fingerprint = _store.Fingerprint(path);
        Current = _store.Load(path);
        FilePath = path;
        IsDirty = false;
        _fingerprint = fingerprint;
        _backedUpSinceLoad = false;
        SetSaveStatus(null);
    }

    public bool HasChangedOnDisk => FilePath is not null && _store.Fingerprint(FilePath) != _fingerprint;

    public void AcceptDiskChange()
    {
        if (FilePath is null)
        {
            return;
        }

        _fingerprint = _store.Fingerprint(FilePath);

        // What is on disk now is not the file backed up at the first save, and nothing else
        // holds a copy of it.
        _backedUpSinceLoad = false;
    }

    public void Save()
    {
        if (Current is null || FilePath is null)
        {
            throw new InvalidOperationException("No save file is loaded.");
        }

        if (HasChangedOnDisk)
        {
            ReportSaveProblem(LocKeys.Save_ChangedOnDisk);
            return;
        }

        try
        {
            WriteToDisk(FilePath, Current);
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Trace.WriteLine($"Not allowed to save '{FilePath}': {ex}");
            ReportSaveProblem(LocKeys.Save_FailedNoPermission);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Could not save '{FilePath}': {ex}");
            ReportSaveProblem(LocKeys.Save_Failed);
        }
    }

    private void WriteToDisk(string path, PlanetCrafterSaveFile save)
    {
        var backedUp = BackUpOnce(path);
        _store.Save(path, save);
        _fingerprint = _store.Fingerprint(path);
        IsDirty = false;

        if (backedUp)
        {
            SetSaveStatus(LocKeys.Save_Ok, StatusKind.Success);
            _announcer.Announce(_localizer[LocKeys.Save_OkAnnounce]);
            return;
        }

        // "Saved." alone would leave the player believing the safety copy exists.
        ReportSaveProblem(LocKeys.Save_OkNoBackup);
    }

    private void ReportSaveProblem(string key)
    {
        SetSaveStatus(key, StatusKind.Error);
        _announcer.Announce(_localizer, StatusKind.Error, _localizer[key]);
    }

    public void Close()
    {
        Current = null;
        FilePath = null;
        IsDirty = false;
        _fingerprint = null;
        _backedUpSinceLoad = false;
        SetSaveStatus(null);
    }

    public void SaveCopy(string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        if (Current is null || FilePath is null)
        {
            throw new InvalidOperationException("No save file is loaded.");
        }

        _store.SaveCopy(FilePath, targetPath, Current);
        FilePath = targetPath;
        _fingerprint = _store.Fingerprint(targetPath);
        IsDirty = false;

        // The copy is PCEdit's own output: nothing in it needs preserving. The pristine save is
        // the file it came from, which this never writes.
        _backedUpSinceLoad = true;
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
    /// <returns>False only when a backup was attempted just now and failed.</returns>
    private bool BackUpOnce(string path)
    {
        if (_backedUpSinceLoad)
        {
            return true;
        }

        _backedUpSinceLoad = true;

        try
        {
            _backups.BackUp(path);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Could not back up '{path}': {ex}");
            return false;
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

    public void RemoveWorldObjects(IReadOnlySet<int> worldObjectIds)
    {
        var save = RequireCurrent();
        if (save.WorldObjects.RemoveAll(w => worldObjectIds.Contains(w.Id)) == 0)
        {
            return;
        }

        IsDirty = true;
        OnPropertyChanged(nameof(Current));
    }

    public int GrantTerraTokens(long playerId, int amount)
    {
        ThrowIfCannotGrant(playerId, amount);

        // All three of these are int in the save format. The addition was unchecked, so a large
        // grant wrapped straight to a negative balance and was written into the player's file -
        // 8,680 + int.MaxValue came out as -2,147,474,969 (issue #42). Saturating beats refusing
        // for what is fundamentally a cheat tool, and clamping here rather than in the view model
        // means it holds however the grant is reached.
        //
        // Each field clamps on its own, because a save can already hold different values in them.
        var granted = Headroom(RequireCurrent().Unlocks.TerraTokens, amount);

        MutateUnlocks(unlocks => unlocks with
        {
            TerraTokens = AddClamped(unlocks.TerraTokens, amount),
            AllTimeTerraTokens = AddClamped(unlocks.AllTimeTerraTokens, amount)
        });

        ReplacePlayer(playerId, player => player with
        {
            TotalTerraTokenEarned = AddClamped(player.TotalTerraTokenEarned, amount)
        });

        return granted;
    }

    // Everything is checked before anything changes: the unlocks used to be updated, and the
    // file marked dirty, before ReplacePlayer noticed a stale player id and threw (issue #89).
    private void ThrowIfCannotGrant(long playerId, int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "The amount to grant must be positive.");
        }

        if (!RequireCurrent().Players.Exists(player => player.Id == playerId))
        {
            throw new InvalidOperationException($"Player {playerId} was not found in the loaded save.");
        }
    }

    /// <summary>Adds without overflowing - an int field cannot hold more than int.MaxValue.</summary>
    private static int AddClamped(int current, int amount)
    {
        var sum = (long)current + amount;

        return sum > int.MaxValue ? int.MaxValue : (int)sum;
    }

    /// <summary>
    /// How much of a grant the balance could actually take. Never exceeds the requested amount,
    /// so it always fits back into an int even when the current balance is negative.
    /// </summary>
    private static int Headroom(int current, int amount)
    {
        return (int)((long)AddClamped(current, amount) - current);
    }

    private PlanetCrafterSaveFile RequireCurrent()
    {
        return Current ?? throw new InvalidOperationException("No save file is loaded.");
    }
}
