using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler;

public interface IPlanetCrafterSaveFileStore
{
    PlanetCrafterSaveFile Load(string path);

    void Save(string path, PlanetCrafterSaveFile saveFile);

    /// <summary>
    /// Writes the save to a new file, leaving <paramref name="sourcePath"/> untouched. The copy
    /// takes the source's framing - with or without a BOM - rather than a new file's default, so
    /// a copy of a Game Pass save is still one the game will load. Refuses the source path itself.
    /// </summary>
    void SaveCopy(string sourcePath, string targetPath, PlanetCrafterSaveFile saveFile);
}
