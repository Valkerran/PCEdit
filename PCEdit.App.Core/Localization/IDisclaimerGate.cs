namespace PCEdit.App.Core.Localization;

/// <summary>
/// Tracks whether the user has acknowledged the "use at your own risk" disclaimer for the
/// current disclaimer <see cref="DisclaimerMeta.Version"/>. Each UI head supplies storage.
/// </summary>
public interface IDisclaimerGate
{
    /// <summary><c>true</c> once the user has acknowledged the current disclaimer version.</summary>
    bool HasAcknowledged { get; }

    /// <summary>Records acknowledgement of the current disclaimer version.</summary>
    void Acknowledge();
}

/// <summary>Disclaimer metadata shared by every head. Bump <see cref="Version"/> to re-prompt.</summary>
public static class DisclaimerMeta
{
    /// <remarks>
    /// 2 — the disclaimer now describes the copy PCEdit keeps of a save before it first writes to
    /// it, so anyone who acknowledged the older wording is asked again rather than being left
    /// believing their own backup is the only thing standing between them and a lost save.
    /// </remarks>
    public const int Version = 2;
}
