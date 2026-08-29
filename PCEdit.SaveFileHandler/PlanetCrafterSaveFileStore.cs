using System.Text;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler;

public sealed class PlanetCrafterSaveFileStore(IPlanetCrafterSaveFileSerializer serializer)
    : IPlanetCrafterSaveFileStore
{
    // The game writes the save as UTF-8 with a leading BOM; match it so a load→save leaves the
    // file byte-identical. (File.ReadAllText auto-detects and strips the BOM on the way in.)
    private static readonly Encoding SaveEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

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
        File.WriteAllText(path, _serializer.Serialize(saveFile), SaveEncoding);
    }
}
