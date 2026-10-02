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

    public PlanetCrafterSaveFile Load(string path)
    {
        LoadCallCount++;
        return _filesByPath[path];
    }

    public void Save(string path, PlanetCrafterSaveFile saveFile)
    {
        SaveCallCount++;
        _filesByPath[path] = saveFile;
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
        _filesByPath[targetPath] = saveFile;
    }

    public bool Contains(string path) => _filesByPath.ContainsKey(path);
}
