using System.ComponentModel;
using PCEdit.App.Core.ViewModels;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

public interface ISaveFileWorkspace : INotifyPropertyChanged
{
    PlanetCrafterSaveFile? Current { get; }

    string? FilePath { get; }

    bool IsLoaded { get; }

    bool IsDirty { get; }

    string? SaveStatus { get; }

    /// <summary>Whether <see cref="SaveStatus"/> reports a success or a problem, so the header
    /// can show it by more than its wording.</summary>
    StatusKind SaveStatusKind { get; }

    void Load(string path);

    /// <summary>Unloads the save and discards any unsaved edits. Nothing is written.</summary>
    void Close();

    void Save();

    /// <summary>
    /// Whether the file was rewritten or removed by something else after PCEdit last read or
    /// wrote it - typically the game saving. <see cref="Save"/> refuses while this is true.
    /// </summary>
    bool HasChangedOnDisk { get; }

    /// <summary>
    /// The player chose to overwrite what is on disk now: the next <see cref="Save"/> goes
    /// ahead, after backing up the file as it is now.
    /// </summary>
    void AcceptDiskChange();

    /// <summary>
    /// Writes the loaded save to a new file and carries on from there, so later saves go to the
    /// copy and the file it was loaded from is never written. Throws if the copy cannot be written,
    /// leaving the workspace on the original file.
    /// </summary>
    void SaveCopy(string targetPath);

    void MutateUnlocks(Func<SaveFileUnlocks, SaveFileUnlocks> mutate);

    void ReplaceTerraformation(string planetId, Func<PlanetTerraformation, PlanetTerraformation> mutate);

    void ReplacePlayer(long playerId, Func<PlayerData, PlayerData> mutate);

    void ReplaceInventory(int inventoryId, Func<Inventory, Inventory> mutate);

    /// <summary>
    /// Deletes the world-object records with these ids. Callers first take the items out of
    /// every inventory that lists them; a record no inventory lists is an item that no longer exists.
    /// </summary>
    void RemoveWorldObjects(IReadOnlySet<int> worldObjectIds);

    /// <summary>
    /// Adds <paramref name="amount"/> tokens to the unlocks totals and the player's earned total,
    /// saturating rather than overflowing.
    /// </summary>
    /// <returns>
    /// What the spendable balance could actually take - less than <paramref name="amount"/> when
    /// the grant hit the ceiling. Callers report this, not the requested figure.
    /// </returns>
    int GrantTerraTokens(long playerId, int amount);
}
