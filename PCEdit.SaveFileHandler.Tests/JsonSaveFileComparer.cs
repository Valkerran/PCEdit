using System.Text.Json;

namespace PCEdit.SaveFileHandler.Tests;

/// <summary>
/// Compares two raw Planet Crafter save-file strings key-by-key. Object key order is ignored;
/// every value is compared as a strict JSON token (<see cref="JsonElement.GetRawText"/>), so a
/// dropped key, an added key, a renamed key, or a reformatted number all fail. This is the check
/// that catches a wrong <c>[JsonPropertyName]</c> — a model round trip cannot, because it reads
/// back whatever key it wrote.
/// </summary>
internal static class JsonSaveFileComparer
{
    private static readonly int[] SingleObjectSections = [0, 5, 8];

    /// <summary>Returns the human-readable differences between two save strings; empty when identical.</summary>
    public static IReadOnlyList<string> Diff(string expected, string actual)
    {
        var differences = new List<string>();

        var expectedSections = SplitSections(expected);
        var actualSections = SplitSections(actual);

        if (expectedSections.Length != actualSections.Length)
        {
            differences.Add($"section count: expected {expectedSections.Length}, got {actualSections.Length}");
            return differences;
        }

        for (var section = 0; section < expectedSections.Length; section++)
        {
            var expectedRecords = SplitRecords(expectedSections[section], section);
            var actualRecords = SplitRecords(actualSections[section], section);

            if (expectedRecords.Length != actualRecords.Length)
            {
                differences.Add($"section {section}: record count expected {expectedRecords.Length}, got {actualRecords.Length}");
                continue;
            }

            for (var record = 0; record < expectedRecords.Length; record++)
            {
                using var expectedDoc = JsonDocument.Parse(expectedRecords[record]);
                using var actualDoc = JsonDocument.Parse(actualRecords[record]);

                CompareElement(
                    expectedDoc.RootElement,
                    actualDoc.RootElement,
                    $"section {section} record {record}",
                    differences);
            }
        }

        return differences;
    }

    private static string[] SplitSections(string content)
    {
        var sections = content.Split('@');
        // Drop a single trailing empty section produced by the writer's terminal "\r\n@\r\n".
        if (sections.Length > 0 && string.IsNullOrWhiteSpace(sections[^1]))
        {
            sections = sections[..^1];
        }

        sections[0] = sections[0].TrimStart('\uFEFF');
        return sections;
    }

    private static string[] SplitRecords(string section, int sectionIndex)
    {
        if (Array.IndexOf(SingleObjectSections, sectionIndex) >= 0)
        {
            return [section.Trim()];
        }

        return section.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static void CompareElement(
        JsonElement expected,
        JsonElement actual,
        string path,
        List<string> differences)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            differences.Add($"{path}: kind expected {expected.ValueKind}, got {actual.ValueKind}");
            return;
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProps = expected.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var actualProps = actual.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                foreach (var (name, value) in expectedProps)
                {
                    if (!actualProps.TryGetValue(name, out var actualValue))
                    {
                        differences.Add($"{path}: key '{name}' dropped (was {value.GetRawText()})");
                        continue;
                    }

                    CompareElement(value, actualValue, $"{path}.{name}", differences);
                }

                foreach (var name in actualProps.Keys.Where(name => !expectedProps.ContainsKey(name)))
                {
                    differences.Add($"{path}: key '{name}' added ({actualProps[name].GetRawText()})");
                }

                break;

            case JsonValueKind.Array:
                var expectedItems = expected.EnumerateArray().ToArray();
                var actualItems = actual.EnumerateArray().ToArray();
                if (expectedItems.Length != actualItems.Length)
                {
                    differences.Add($"{path}: array length expected {expectedItems.Length}, got {actualItems.Length}");
                    break;
                }

                for (var i = 0; i < expectedItems.Length; i++)
                {
                    CompareElement(expectedItems[i], actualItems[i], $"{path}[{i}]", differences);
                }

                break;

            default:
                if (expected.GetRawText() != actual.GetRawText())
                {
                    differences.Add($"{path}: value expected {expected.GetRawText()}, got {actual.GetRawText()}");
                }

                break;
        }
    }
}
