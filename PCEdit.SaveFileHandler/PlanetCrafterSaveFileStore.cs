using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler;

public sealed class PlanetCrafterSaveFileStore(IPlanetCrafterSaveFileSerializer serializer)
    : IPlanetCrafterSaveFileStore
{
    private readonly IPlanetCrafterSaveFileSerializer _serializer 
        = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public PlanetCrafterSaveFile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _serializer.Deserialize(File.ReadAllText(path));
    }

    public void Save(string path, PlanetCrafterSaveFile saveFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(saveFile);
        File.WriteAllText(path, _serializer.Serialize(saveFile));
    }
}
