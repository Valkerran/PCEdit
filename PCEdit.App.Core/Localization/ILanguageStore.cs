namespace PCEdit.App.Core.Localization;

/// <summary>Persists the user's chosen UI language across runs. Each UI head supplies storage.</summary>
public interface ILanguageStore
{
    /// <summary>The saved culture name, or <c>null</c> if the user has never chosen one.</summary>
    string? GetSavedCulture();

    /// <summary>Persists <paramref name="cultureName"/> as the chosen UI language.</summary>
    void SaveCulture(string cultureName);
}
