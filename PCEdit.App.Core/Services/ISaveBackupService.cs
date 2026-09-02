namespace PCEdit.App.Core.Services;

/// <summary>
/// Keeps a copy of a save file as it was before PCEdit first wrote over it.
/// </summary>
/// <remarks>
/// The pristine file - the one PCEdit has never touched - is the only copy that cannot be
/// reconstructed; every later snapshot is just PCEdit's own output. So a backup is taken once per
/// load, not once per save. Where the copies live is a platform concern, hence the interface: the
/// Avalonia head implements it, and <c>PCEdit.App.Core</c> stays UI- and path-agnostic.
/// </remarks>
public interface ISaveBackupService
{
    /// <summary>
    /// Human-readable location of the backups, for showing the user where to find them.
    /// </summary>
    string DirectoryPath { get; }

    /// <summary>
    /// Copies <paramref name="savePath"/> into the backup directory and prunes older copies of
    /// the same save. Does nothing when the path holds no file yet ("Save As").
    /// </summary>
    /// <remarks>
    /// May throw. The caller decides what a failed backup means; it must not block the save.
    /// </remarks>
    void BackUp(string savePath);
}
