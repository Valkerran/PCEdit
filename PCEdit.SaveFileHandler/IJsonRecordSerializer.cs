namespace PCEdit.SaveFileHandler;

public interface IJsonRecordSerializer
{
    T Deserialize<T>(string content, int sectionIndex);

    string Serialize<T>(T value);
}
