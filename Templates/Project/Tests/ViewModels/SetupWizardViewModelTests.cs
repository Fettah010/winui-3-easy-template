using System;
using System.IO;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.ViewModels;

[TestClass]
public class SetupWizardViewModelTests
{
    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
        LocalizationService.Current.SetLanguage("en-US");
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void Steps_NavigateInBounds()
    {
        var vm = new SetupWizardViewModel();

        Assert.AreEqual(0, vm.SelectedStepIndex);
        Assert.IsTrue(vm.CanGoNext);
        Assert.IsFalse(vm.CanGoBack);

        vm.GoNext();
        vm.GoNext();
        Assert.AreEqual(2, vm.SelectedStepIndex);
        Assert.IsTrue(vm.CanGoBack);

        vm.GoBack();
        Assert.AreEqual(1, vm.SelectedStepIndex);
        Assert.AreEqual(Visibility.Visible, vm.LocationStepVisibility);
        Assert.AreEqual(Visibility.Collapsed, vm.WelcomeStepVisibility);
    }

    [TestMethod]
    public void LastStep_ShowsCompleteOnly()
    {
        var vm = new SetupWizardViewModel();

        for (int i = 0; i < 5; i++)
            vm.GoNext();

        Assert.AreEqual(4, vm.SelectedStepIndex);
        Assert.IsTrue(vm.IsLastStep);
        Assert.AreEqual(Visibility.Visible, vm.CompleteButtonVisibility);
        Assert.AreEqual(Visibility.Collapsed, vm.NextButtonVisibility);
        Assert.AreEqual(Visibility.Visible, vm.DoneStepVisibility);
    }

    [TestMethod]
    public void Complete_PersistsChoices()
    {
        var vm = new SetupWizardViewModel
        {
            CreateDesktopShortcut = false,
            LaunchAtLogin = true,
        };

        vm.Complete();

        Assert.IsTrue(SetupWizardViewModel.IsCompleted);
        Assert.IsFalse(LocalSettingsStore.Shared.Get("SetupWizardDesktopShortcut", true));
        Assert.IsTrue(LocalSettingsStore.Shared.Get("SetupWizardLaunchAtLogin", false));
    }

    [TestMethod]
    public void Skip_MarksCompletedWithoutApplying()
    {
        var vm = new SetupWizardViewModel();

        vm.Skip();

        Assert.IsTrue(SetupWizardViewModel.IsCompleted);
    }

    [TestMethod]
    public void GoNext_BlockedOnEmptyLocation()
    {
        var vm = new SetupWizardViewModel { DataFolder = string.Empty };
        vm.GoNext();
        Assert.AreEqual(1, vm.SelectedStepIndex);

        vm.GoNext();
        Assert.AreEqual(1, vm.SelectedStepIndex);
        Assert.IsTrue(vm.HasLocationError);
        Assert.AreNotEqual("SetupLocationRequired", vm.LocationError);
    }

    [TestMethod]
    public void GoNext_BlockedOnRelativeLocation()
    {
        var vm = new SetupWizardViewModel { DataFolder = @"relative\path" };
        vm.GoNext();
        Assert.AreEqual(1, vm.SelectedStepIndex);

        vm.GoNext();
        Assert.AreEqual(1, vm.SelectedStepIndex);
        Assert.IsTrue(vm.HasLocationError);
        Assert.AreNotEqual("SetupLocationInvalid", vm.LocationError);
    }

    [TestMethod]
    public void GoNext_AdvancesAndClearsErrorOnAbsoluteLocation()
    {
        var vm = new SetupWizardViewModel { DataFolder = string.Empty };
        vm.GoNext();
        vm.GoNext();
        Assert.IsTrue(vm.HasLocationError);

        vm.DataFolder = Path.Combine(Path.GetTempPath(), "DevTemWizard");
        Assert.IsFalse(vm.HasLocationError);
        vm.GoNext();
        Assert.AreEqual(2, vm.SelectedStepIndex);
    }

    [TestMethod]
    public void Complete_InvalidFolder_ParksOnLocationStep()
    {
        var vm = new SetupWizardViewModel { DataFolder = string.Empty };

        Assert.IsFalse(vm.Complete());
        Assert.AreEqual(1, vm.SelectedStepIndex);
        Assert.IsTrue(vm.HasLocationError);
    }

    [TestMethod]
    public void LocationError_ResolvesInEveryLanguage()
    {
        var loc = LocalizationService.Current;
        foreach (string lang in new[] { "en-US", "es-ES", "fr-FR" })
        {
            loc.SetLanguage(lang);
            var vm = new SetupWizardViewModel { DataFolder = string.Empty };
            Assert.IsFalse(vm.ValidateLocation());
            Assert.AreNotEqual("SetupLocationRequired", vm.LocationError, lang);
            vm.DataFolder = @"relative\path";
            Assert.IsFalse(vm.ValidateLocation());
            Assert.AreNotEqual("SetupLocationInvalid", vm.LocationError, lang);
        }
    }
}
