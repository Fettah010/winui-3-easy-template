using System;
using System.IO;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.ViewModels;

[TestClass]
public class SamplePageViewModelTests
{
    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Never leak a test language into other tests.
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void ViewModel_Constructs_WithSeededItemsAndNoSelection()
    {
        var vm = new SamplePageViewModel();
        Assert.IsNotNull(vm);
        Assert.HasCount(3, vm.Items);
        Assert.IsFalse(vm.IsEmpty);
        Assert.IsTrue(vm.HasItems);
        Assert.IsNull(vm.SelectedItem);
        Assert.IsFalse(vm.HasSelection);
        Assert.IsTrue(vm.HasNoSelection);
    }

    [TestMethod]
    public void SelectingItem_FlipsSelectionFlags()
    {
        var vm = new SamplePageViewModel();
        vm.SelectedItem = vm.Items[0];
        Assert.IsTrue(vm.HasSelection);
        Assert.IsFalse(vm.HasNoSelection);
        vm.SelectedItem = null;
        Assert.IsFalse(vm.HasSelection);
        Assert.IsTrue(vm.HasNoSelection);
    }

    [TestMethod]
    public void ClearingItems_ShowsEmptyState()
    {
        var vm = new SamplePageViewModel();
        vm.Items.Clear();
        Assert.IsTrue(vm.IsEmpty);
        Assert.IsFalse(vm.HasItems);
    }

    [TestMethod]
    public void NavSymbol_MatchesChosenIcon()
    {
        var vm = new SamplePageViewModel();
        Assert.AreEqual(Symbol.TemplateIcon, vm.NavSymbol);
    }

    [TestMethod]
    public void Strings_AreTranslated()
    {
        // Fails until the SampleTitle/SampleDescription (+ list keys) are
        // added to all three dictionaries in Services/Localization (see
        // wire-up step 2 in the generated page code-behind).
        var loc = LocalizationService.Current;
        foreach (var lang in new[] { "en-US", "es-ES", "fr-FR" })
        {
            loc.SetLanguage(lang);
            Assert.AreNotEqual("SampleTitle", loc.GetString("SampleTitle"), lang);
            Assert.AreNotEqual("SampleDescription", loc.GetString("SampleDescription"), lang);
            Assert.AreNotEqual("SampleEmptyTitle", loc.GetString("SampleEmptyTitle"), lang);
            Assert.AreNotEqual("SampleEmptyMessage", loc.GetString("SampleEmptyMessage"), lang);
            Assert.AreNotEqual("SampleSelectPrompt", loc.GetString("SampleSelectPrompt"), lang);
        }
    }
}
