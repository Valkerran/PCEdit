namespace PCEdit.SaveFileHandler;

public interface IJsonRecordSerializer
{
    /// <param name="recordIndex">The record's zero-based position in a list section, named in
    /// the error when it is malformed; null for a single-object section.</param>
    T Deserialize<T>(string content, int sectionIndex, int? recordIndex = null);

    string Serialize<T>(T value);
}
