using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler;

public interface IPlanetCrafterSaveFileStore
{
    PlanetCrafterSaveFile Load(string path);

    void Save(string path, PlanetCrafterSaveFile saveFile);
}
