namespace PCEdit.SaveFileHandler.Tests;

public sealed class JsonRecordSerializerTests
{
    private readonly JsonRecordSerializer _serializer = new();

    private sealed class SampleRecord
    {
        public required string RequiredValue { get; init; }

        public string? OptionalValue { get; init; }
    }

    [Fact]
    public void Deserialize_ValidJson_PopulatesObject()
    {
        var result = _serializer.Deserialize<SampleRecord>(
            """{"requiredValue":"hello","optionalValue":"world"}""", sectionIndex: 0);

        Assert.Equal("hello", result.RequiredValue);
        Assert.Equal("world", result.OptionalValue);
    }

    [Fact]
    public void Deserialize_UsesCamelCaseNamingPolicy()
    {
        // PascalCase keys don't match the camelCase policy, so RequiredValue stays unset -> JSON exception
        // because the property is `required`.
        var exception = Assert.Throws<InvalidDataException>(() =>
            _serializer.Deserialize<SampleRecord>("""{"RequiredValue":"hello"}""", sectionIndex: 4));

        Assert.Contains("section 4", exception.Message);
    }

    [Fact]
    public void Deserialize_TrimsSurroundingWhitespace()
    {
        var result = _serializer.Deserialize<SampleRecord>(
            "  \r\n{\"requiredValue\":\"hello\"}\r\n  ", sectionIndex: 0);

        Assert.Equal("hello", result.RequiredValue);
    }

    [Fact]
    public void Deserialize_NullLiteral_ThrowsInvalidDataException_MentioningSectionIndex()
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            _serializer.Deserialize<SampleRecord>("null", sectionIndex: 7));

        Assert.Contains("section 7", exception.Message);
        Assert.Contains("empty", exception.Message);
    }

    [Fact]
    public void Deserialize_MalformedJson_ThrowsInvalidDataException_WrappingJsonException()
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            _serializer.Deserialize<SampleRecord>("{not valid json", sectionIndex: 3));

        Assert.Contains("section 3", exception.Message);
        Assert.Contains("is not valid", exception.Message);
        Assert.IsType<System.Text.Json.JsonException>(exception.InnerException);
    }

    [Fact]
    public void Serialize_UsesCamelCaseNamingPolicy()
    {
        var json = _serializer.Serialize(new SampleRecord { RequiredValue = "hello", OptionalValue = "world" });

        Assert.Contains("\"requiredValue\":\"hello\"", json);
        Assert.Contains("\"optionalValue\":\"world\"", json);
        Assert.DoesNotContain("RequiredValue", json);
    }

    [Fact]
    public void Serialize_OmitsNullProperties()
    {
        var json = _serializer.Serialize(new SampleRecord { RequiredValue = "hello", OptionalValue = null });

        Assert.Contains("\"requiredValue\":\"hello\"", json);
        Assert.DoesNotContain("optionalValue", json);
    }

    // A null in a key the game always writes used to load, then vanish on save (issue #76).
    [Theory]
    [InlineData("""{"id":1,"woIds":null,"size":3}""")]
    public void Deserialize_NullInARequiredStringKey_IsRejected(string json)
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            _serializer.Deserialize<Models.Inventory>(json, sectionIndex: 4));

        Assert.IsType<System.Text.Json.JsonException>(exception.InnerException);
    }

    // A missing id used to become 0 and be written back as "id":0 (issue #86).
    [Theory]
    [InlineData("""{"woIds":"","size":3}""")]
    public void Deserialize_InventoryWithoutAnId_IsRejected(string json)
    {
        Assert.Throws<InvalidDataException>(() => _serializer.Deserialize<Models.Inventory>(json, sectionIndex: 4));
    }

    [Fact]
    public void Deserialize_PlayerWithoutAnId_IsRejected()
    {
        const string json = """{"name":"a","playerPosition":"0,0,0","playerRotation":"0,0,0,1","planetId":"Prime"}""";

        Assert.Throws<InvalidDataException>(() => _serializer.Deserialize<Models.PlayerData>(json, sectionIndex: 2));
    }
}
