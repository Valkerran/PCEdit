using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler;

public interface IPlanetCrafterSaveFileSerializer
{
    PlanetCrafterSaveFile Deserialize(string content);

    string Serialize(PlanetCrafterSaveFile saveFile);
}
