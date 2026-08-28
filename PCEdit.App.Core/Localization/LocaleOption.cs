namespace PCEdit.App.Core.Localization;

/// <summary>One selectable UI language.</summary>
/// <param name="CultureName">.NET culture name, e.g. <c>"pt-BR"</c>.</param>
/// <param name="NativeName">Language name in its own language, for the picker.</param>
/// <param name="EnglishName">Language name in English, for logs/docs.</param>
public sealed record LocaleOption(string CultureName, string NativeName, string EnglishName);
