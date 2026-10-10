using System.Text.Encodings.Web;

namespace PCEdit.SaveFileHandler;

/// <summary>
/// Escapes only what JSON itself requires - a quote, a backslash and the control characters
/// below U+0020 - and writes every other character as-is. The default encoder also escapes all
/// non-ASCII and HTML-sensitive characters, and even the relaxed one escapes emoji, so a player's
/// "Café &amp; Bob's" label came back as escapes on an unedited save (issue #70). The save is not
/// HTML; nothing here is embedded in a page.
/// </summary>
internal sealed class MinimalJsonEncoder : JavaScriptEncoder
{
    public static readonly MinimalJsonEncoder Instance = new();

    private MinimalJsonEncoder()
    {
    }

    // The longest escape written is \uXXXX.
    public override int MaxOutputCharactersPerInputCharacter => 6;

    public override bool WillEncode(int unicodeScalar) => unicodeScalar is < 0x20 or '"' or '\\';

    public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
    {
        for (var i = 0; i < textLength; i++)
        {
            // Surrogates go through the encoder too: left alone, a lone one makes the writer's
            // UTF-8 transcoding stop silently and drop the rest of the string (issue #70).
            if (char.IsSurrogate(text[i]) || WillEncode(text[i]))
            {
                return i;
            }
        }

        return -1;
    }

    public override unsafe bool TryEncodeUnicodeScalar(
        int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
    {
        var output = Escape(unicodeScalar) ?? char.ConvertFromUtf32(unicodeScalar);
        if (output.Length > bufferLength)
        {
            numberOfCharactersWritten = 0;
            return false;
        }

        output.AsSpan().CopyTo(new Span<char>(buffer, bufferLength));
        numberOfCharactersWritten = output.Length;
        return true;
    }

    /// <summary>The short escapes JSON defines, else \uXXXX for any other control character.</summary>
    private static string? Escape(int unicodeScalar) => unicodeScalar switch
    {
        '"' => "\\\"",
        '\\' => "\\\\",
        '\b' => "\\b",
        '\f' => "\\f",
        '\n' => "\\n",
        '\r' => "\\r",
        '\t' => "\\t",
        < 0x20 => $"\\u{unicodeScalar:X4}",
        _ => null
    };
}
