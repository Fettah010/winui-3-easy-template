namespace DevTemWinUi3.Services;

/// <summary>
/// Represents a supported language.
/// </summary>
public record LanguageInfo(string Tag, string NativeName, string EnglishName)
{
    public override string ToString() => NativeName;
}
