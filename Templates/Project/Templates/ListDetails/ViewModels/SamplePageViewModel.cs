using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace DevTemWinUi3.ViewModels;

/// <summary>
/// One row in the list pane. Top-level (not nested in the ViewModel) so
/// compiled XAML bindings can name it via <c>x:DataType</c> — mistyped row
/// paths then fail the build instead of rendering blank at runtime.
/// </summary>
public sealed record SampleListItem(string Title, string Detail);

/// <summary>
/// ViewModel for SamplePage (list/details). Transient: register with
/// services.AddTransient&lt;SamplePageViewModel&gt;() in
/// ServiceLocator.Initialize() and resolve it from the container in the
/// page constructor (never new). Keep the constructor side-effect free so
/// unit tests can construct the VM directly. The seeded rows are
/// placeholders — replace <see cref="LoadSampleItems"/> with your data
/// source and delete the seed.
/// </summary>
public partial class SamplePageViewModel : ObservableObject
{
    public const string Route = "sample";

    /// <summary>
    /// The page's nav icon (chosen at scaffold time via --icon). Used for
    /// the NavigationViewItem's SymbolIcon; data-bound nav hosts can bind it.
    /// </summary>
    public Symbol NavSymbol => Symbol.TemplateIcon;

    /// <summary>Rows in the list pane. Empty shows the empty state.</summary>
    public ObservableCollection<SampleListItem> Items { get; } = new();

    [ObservableProperty]
    private SampleListItem? _selectedItem;

    /// <summary>Whether a row is selected (details pane vs prompt).</summary>
    public bool HasSelection => SelectedItem is not null;

    /// <summary>Inverse of <see cref="HasSelection"/> (x:Load binding).</summary>
    public bool HasNoSelection => SelectedItem is null;

    /// <summary>Whether the list is empty (empty-state vs list).</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>Inverse of <see cref="IsEmpty"/> (x:Load binding).</summary>
    public bool HasItems => Items.Count > 0;

    public SamplePageViewModel()
    {
        LoadSampleItems();
        Items.CollectionChanged += OnItemsChanged;
    }

    /// <summary>Placeholder rows. Replace with your data source.</summary>
    private void LoadSampleItems()
    {
        Items.Add(new SampleListItem("First item", "Details for the first item."));
        Items.Add(new SampleListItem("Second item", "Details for the second item."));
        Items.Add(new SampleListItem("Third item", "Details for the third item."));
    }

    partial void OnSelectedItemChanged(SampleListItem? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasNoSelection));
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasItems));
    }
}
