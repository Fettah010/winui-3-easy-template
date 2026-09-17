using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class FirstRunDialogServiceTests
{
    [TestMethod]
    public void ShouldShowSetupWizard_NeverAutoOpens()
    {
        // Install-time setup belongs to the installer (the MSIX package or
        // Velopack setup): the in-app wizard must never auto-open after
        // installing — first run lands on Home with a welcome dialog.
        Assert.IsFalse(FirstRunDialogService.ShouldShowSetupWizard());
    }
}
