using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Fakes;

/// <summary>In-memory <see cref="ISaveBackupService"/> so workspace tests never touch disk.</summary>
internal sealed class FakeSaveBackupService : ISaveBackupService
{
    /// <summary>Set to have <see cref="BackUp"/> throw, standing in for an unwritable directory.</summary>
    public Exception? ThrowOnBackUp { get; set; }

    public List<string> BackedUpPaths { get; } = [];

    /// <summary>Runs inside BackUp, so a test can observe the world at that exact moment.</summary>
    public Action? OnBackUp { get; set; }

    public string DirectoryPath => @"C:\fake\backups";

    public void BackUp(string savePath)
    {
        BackedUpPaths.Add(savePath);
        OnBackUp?.Invoke();

        if (ThrowOnBackUp is not null)
        {
            throw ThrowOnBackUp;
        }
    }
}
