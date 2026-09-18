using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Controls;

/// <summary>
/// XAML string lookup against <see cref="LocalizationService"/>.
/// <c>Text="{loc:Loc Key=HomeTitle}"</c> creates a one-way binding to the
/// indexer that refreshes on every language switch — no code-behind string
/// mapping needed. Classic <c>Binding</c> (not <c>x:Bind</c>) because
/// compiled bindings cannot index by string literal.
/// <c>Property=…</c> binds a service property by name instead of the
/// indexer (for version-formatted lines the dictionaries must not hardcode).
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    /// <summary>Dictionary key (e.g. <c>HomeTitle</c>).</summary>
    public string? Key { get; set; }

    /// <summary>
    /// Service property name instead of a key.
    /// </summary>
    public string? Property { get; set; }

    protected override object ProvideValue()
    {
        string path = !string.IsNullOrEmpty(Property)
            ? Property!
            : $"[{Key}]";
        return new Binding
        {
            Source = LocalizationService.Current,
            Path = new PropertyPath(path),
            Mode = BindingMode.OneWay,
        };
    }
}
