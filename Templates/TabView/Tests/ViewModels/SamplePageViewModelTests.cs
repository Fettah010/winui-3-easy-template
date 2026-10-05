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
    public void ViewModel_Constructs_WithSeededTabs()
    {
        var vm = new SamplePageViewModel();
        Assert.IsNotNull(vm);
        Assert.HasCount(3, vm.Tabs);
        Assert.AreEqual(0, vm.SelectedIndex);
        Assert.IsTrue(vm.CanCloseTabs);
    }

    [TestMethod]
    public void AddTabCommand_OpensAndSelectsNewTab()
    {
        var vm = new SamplePageViewModel();
        vm.AddTabCommand.Execute(null);
        Assert.HasCount(4, vm.Tabs);
        Assert.AreEqual(3, vm.SelectedIndex);
        Assert.IsTrue(vm.CanCloseTabs);
    }

    [TestMethod]
    public void CloseTab_RemovesAndClampsSelection()
    {
        var vm = new SamplePageViewModel();
        var tab = vm.Tabs[2];
        vm.SelectedIndex = 2;
        vm.CloseTab(tab);
        Assert.HasCount(2, vm.Tabs);
        Assert.AreEqual(1, vm.SelectedIndex);
    }

    [TestMethod]
    public void CloseTab_RefusesLastTabAndUnknownTab()
    {
        var vm = new SamplePageViewModel();
        foreach (var tab in new System.Collections.Generic.List<SampleTabItem>(vm.Tabs))
        {
            if (vm.Tabs.Count > 1)
                vm.CloseTab(tab);
        }
        Assert.HasCount(1, vm.Tabs);
        vm.CloseTab(vm.Tabs[0]);
        Assert.HasCount(1, vm.Tabs);
        vm.CloseTab(new SampleTabItem("Ghost", "Nowhere"));
        Assert.HasCount(1, vm.Tabs);
    }

    [TestMethod]
    public void CloseTabAt_RemovesClampsAndRefuses()
    {
        var vm = new SamplePageViewModel();
        vm.SelectedIndex = 2;
        vm.CloseTabAt(2);
        Assert.HasCount(2, vm.Tabs);
        Assert.AreEqual(1, vm.SelectedIndex);
        vm.CloseTabAt(-1);
        vm.CloseTabAt(99);
        Assert.HasCount(2, vm.Tabs);
        vm.CloseTabAt(0);
        Assert.HasCount(1, vm.Tabs);
        Assert.IsFalse(vm.CanCloseTabs);
        vm.CloseTabAt(0);
        Assert.HasCount(1, vm.Tabs);
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
        // Fails until the SampleTitle/SampleDescription keys are added to all
        // three dictionaries in Services/Localization (see wire-up step 2
        // in the generated page code-behind).
        var loc = LocalizationService.Current;
        foreach (var lang in new[] { "en-US", "es-ES", "fr-FR" })
        {
            loc.SetLanguage(lang);
            Assert.AreNotEqual("SampleTitle", loc.GetString("SampleTitle"), lang);
            Assert.AreNotEqual("SampleDescription", loc.GetString("SampleDescription"), lang);
        }
    }
}
