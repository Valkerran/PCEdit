using PCEdit.SaveFileHandler;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Tests.Fakes;

/// <summary>In-memory <see cref="IPlanetCrafterSaveFileStore"/> so workspace tests never touch disk.</summary>
internal sealed class FakeSaveFileStore : IPlanetCrafterSaveFileStore
{
    private readonly Dictionary<string, PlanetCrafterSaveFile> _filesByPath = new();

    public int LoadCallCount { get; private set; }

    public int SaveCallCount { get; private set; }

    public void Seed(string path, PlanetCrafterSaveFile saveFile)
    {
        _filesByPath[path] = saveFile;
    }

    /// <summary>Set to make <see cref="Load"/> throw, standing in for a refused file.</summary>
    public Exception? LoadFailure { get; set; }

    public PlanetCrafterSaveFile Load(string path)
    {
        if (LoadFailure is not null)
        {
            throw LoadFailure;
        }

        LoadCallCount++;
        return _filesByPath[path];
    }

    /// <summary>What <see cref="Fingerprint"/> reports. Set it to stand in for the game writing
    /// the file behind PCEdit's back; PCEdit's own writes change it too, as on a real disk.</summary>
    public string? FingerprintValue { get; set; } = "as loaded";

    public string? Fingerprint(string path) => FingerprintValue;

    /// <summary>Set to make <see cref="Save"/> throw.</summary>
    public Exception? SaveFailure { get; set; }

    public void Save(string path, PlanetCrafterSaveFile saveFile)
    {
        if (SaveFailure is not null)
        {
            throw SaveFailure;
        }

        SaveCallCount++;
        _filesByPath[path] = saveFile;
        FingerprintValue = $"after save {SaveCallCount}";
    }

    /// <summary>Every <see cref="SaveCopy"/> that succeeded, as (source, target).</summary>
    public List<(string Source, string Target)> Copies { get; } = [];

    /// <summary>Set to make <see cref="SaveCopy"/> throw, standing in for an unwritable target.</summary>
    public bool FailSaveCopy { get; set; }

    public void SaveCopy(string sourcePath, string targetPath, PlanetCrafterSaveFile saveFile)
    {
        if (FailSaveCopy)
        {
            throw new IOException("Simulated write failure.");
        }

        Copies.Add((sourcePath, targetPath));
        FingerprintValue = $"after copy {Copies.Count}";
        _filesByPath[targetPath] = saveFile;
    }

    public bool Contains(string path) => _filesByPath.ContainsKey(path);
}
