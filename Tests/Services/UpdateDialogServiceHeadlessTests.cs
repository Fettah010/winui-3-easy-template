using System;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class UpdateDialogServiceHeadlessTests
{
    [TestMethod]
    public async Task AllEntries_Headless_CompleteWithoutHanging()
    {
        // Never-throw + headless-safe: with no window wired (unit-test
        // process), every entry point must complete instead of hanging on
        // UI that does not exist. Regression guard for the "click check,
        // nothing ever resolves" class of failures. The WhenAny bounds the
        // wait explicitly (no [Timeout]: the pinned MSTest does not feed
        // cooperative cancellation tokens to parameterless tests).
        var run = Task.Run(DriveAllEntriesAsync);
        var finished = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.AreSame(run, finished, "Update dialog entries hung with no window wired.");
        await finished;
    }

    private static async Task DriveAllEntriesAsync()
    {
        await UpdateDialogService.ShowCheckAsync();
        await UpdateDialogService.DismissAsync();
        await UpdateDialogService.ShowReadyAsync("9.9.9", null);
        await UpdateDialogService.ShowAvailableAsync(new UpdateCheckResult(false, null));
    }
}
