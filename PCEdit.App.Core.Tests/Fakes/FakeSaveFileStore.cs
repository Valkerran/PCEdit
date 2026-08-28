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
}
