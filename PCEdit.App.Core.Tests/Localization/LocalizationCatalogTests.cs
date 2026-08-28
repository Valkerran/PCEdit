using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using PCEdit.App.Core.Localization;

namespace PCEdit.App.Core.Tests.Localization;

public sealed class LocalizationCatalogTests
{
    private static readonly string[] SatelliteCultures =
    [
        "en-GB", "fr", "de", "es-ES", "zh-Hans", "ru", "pl", "pt-PT",
        "ko", "ja", "pt-BR", "it", "zh-Hant", "tr",
    ];

    private static Dictionary<string, string> ReadResx(string? culture)
    {
        var suffix = culture is null ? string.Empty : "." + culture;
        var resourceName = $"PCEdit.App.Core.Resources.Strings{suffix}.resx.embedded";

        // The .resx sources are embedded for the test via <EmbeddedResource>; fall back to reading
        // from the project tree when running from a plain build.
        var path = Path.Combine(FindProjectRoot(), "PCEdit.App.Core", "Resources", $"Strings{suffix}.resx");
        Assert.True(File.Exists(path), $"Missing resx file: {path}");

        return XDocument.Load(path).Root!
            .Elements("data")
            .ToDictionary(
                d => (string)d.Attribute("name")!,
                d => (string)d.Element("value")!);
    }

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PCEdit.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate the solution root.");
    }

    [Fact]
    public void NeutralCatalog_HasNoEmptyValues()
    {
        foreach (var (key, value) in ReadResx(null))
        {
            Assert.False(string.IsNullOrWhiteSpace(value), $"Neutral key '{key}' has an empty value.");
        }
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es-ES")]
    [InlineData("zh-Hans")]
    [InlineData("ru")]
    [InlineData("pl")]
    [InlineData("pt-PT")]
    [InlineData("ko")]
    [InlineData("ja")]
    [InlineData("pt-BR")]
    [InlineData("it")]
    [InlineData("zh-Hant")]
    [InlineData("tr")]
    public void SatelliteCatalog_HasExactlyTheNeutralKeySet_AndNoEmptyValues(string culture)
    {
        var neutral = ReadResx(null);
        var satellite = ReadResx(culture);

        var missing = neutral.Keys.Except(satellite.Keys).OrderBy(k => k).ToList();
        var extra = satellite.Keys.Except(neutral.Keys).OrderBy(k => k).ToList();

        Assert.True(missing.Count == 0, $"[{culture}] missing keys: {string.Join(", ", missing)}");
        Assert.True(extra.Count == 0, $"[{culture}] unexpected keys: {string.Join(", ", extra)}");

        foreach (var (key, value) in satellite)
        {
            Assert.False(string.IsNullOrWhiteSpace(value), $"[{culture}] key '{key}' has an empty value.");
        }
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es-ES")]
    [InlineData("zh-Hans")]
    [InlineData("ru")]
    [InlineData("pl")]
    [InlineData("pt-PT")]
    [InlineData("ko")]
    [InlineData("ja")]
    [InlineData("pt-BR")]
    [InlineData("it")]
    [InlineData("zh-Hant")]
    [InlineData("tr")]
    public void SatelliteCatalog_KeepsFormatPlaceholdersIdentical(string culture)
    {
        var neutral = ReadResx(null);
        var satellite = ReadResx(culture);

        foreach (var (key, neutralValue) in neutral)
        {
            var expected = Placeholders(neutralValue);
            var actual = Placeholders(satellite[key]);
            Assert.True(
                expected.SetEquals(actual),
                $"[{culture}] key '{key}' placeholder mismatch: expected {{{string.Join(",", expected)}}} got {{{string.Join(",", actual)}}}");
        }

        static HashSet<int> Placeholders(string s)
        {
            var set = new HashSet<int>();
            for (var i = 0; i < s.Length - 2; i++)
            {
                if (s[i] == '{' && char.IsDigit(s[i + 1]) && s[i + 2] == '}')
                {
                    set.Add(s[i + 1] - '0');
                }
            }

            return set;
        }
    }

    [Fact]
    public void LocKeys_Constants_AllExistInNeutralCatalog()
    {
        var neutral = ReadResx(null);
        var constants = typeof(LocKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false })
            .Select(f => (string)f.GetRawConstantValue()!);

        foreach (var key in constants)
        {
            Assert.True(neutral.ContainsKey(key), $"LocKeys.{key} is not defined in Strings.resx.");
        }
    }

    [Fact]
    public void Localizer_ResolvesEmbeddedResource_AndFallsBackToEnglish()
    {
        var localizer = new Localizer();

        Assert.Equal("Save", localizer["Common_Save"]);

        localizer.SetCulture("fr");
        Assert.Equal("fr", localizer.Current.Name);
        Assert.NotEqual("#Common_Save", localizer["Common_Save"]);

        // Unknown key -> visible sentinel, never throws.
        Assert.Equal("#definitely_not_a_key", localizer["definitely_not_a_key"]);
    }

    [Fact]
    public void SupportedLocales_CountAndFallback()
    {
        Assert.Equal(15, Localizer.SupportedLocales.Count);
        Assert.Equal("en-US", Localizer.SupportedLocales[0].CultureName);
        foreach (var culture in SatelliteCultures)
        {
            Assert.Contains(Localizer.SupportedLocales, l => l.CultureName == culture);
        }
    }
}
