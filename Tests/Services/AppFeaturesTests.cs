using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class AppFeaturesTests
{
    [TestMethod]
    public void DerivedFlags_AgreeWithModes()
    {
        // Relational on purpose: scaffolded copies render different
        // literals per combo, so this suite must hold in all of them —
        // the matrix runs it in each one. (Token rendering itself is
        // proven by the matrix props assertions.)
        Assert.AreEqual(AppFeatures.UpdateMode != "none", AppFeatures.Updates);
        Assert.AreEqual(
            AppFeatures.UpdateMode is "appinstaller" or "store",
            AppFeatures.IsExternalUpdateMode);
    }

    [TestMethod]
    public void ExternallyManaged_CoversExternalModesAndPackagedRuns()
    {
        // Relational, same matrix rule as above. Unit tests always run
        // unpackaged, so IsPackaged is false here and the seam must equal
        // the scaffold flag; packaged runs take the slim surface (proven
        // by the packaged install checklist, not headless).
        Assert.IsFalse(AppInfo.IsPackaged);
        Assert.AreEqual(AppFeatures.IsExternalUpdateMode, AppFeatures.IsExternallyManaged);
    }
}
