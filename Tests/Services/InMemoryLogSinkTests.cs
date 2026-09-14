using System.Globalization;
using System.Linq;
using DevTemWinUi3.Services.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class InMemoryLogSinkTests
{
    [TestMethod]
    public void Snapshot_Empty_WhenNothingEmitted()
    {
        var sink = new InMemoryLogSink(4);
        Assert.AreEqual(0, sink.Count);
        Assert.IsEmpty(sink.SnapshotNewestFirst());
    }

    [TestMethod]
    public void Snapshot_ReturnsNewestFirst()
    {
        var sink = new InMemoryLogSink(10);
        sink.Emit(TestEvents.Make("first"));
        sink.Emit(TestEvents.Make("second"));

        var snapshot = sink.SnapshotNewestFirst();
        Assert.HasCount(2, snapshot);
        Assert.AreEqual("second", snapshot[0].RenderMessage(CultureInfo.InvariantCulture));
        Assert.AreEqual("first", snapshot[1].RenderMessage(CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Emit_DropsOldest_WhenOverCapacity()
    {
        var sink = new InMemoryLogSink(2);
        sink.Emit(TestEvents.Make("one"));
        sink.Emit(TestEvents.Make("two"));
        sink.Emit(TestEvents.Make("three"));

        var snapshot = sink.SnapshotNewestFirst();
        Assert.HasCount(2, snapshot);
        Assert.IsFalse(snapshot.Any(e =>
            e.RenderMessage(CultureInfo.InvariantCulture) == "one"));
    }

    [TestMethod]
    public void Emit_NullEvent_IsIgnored()
    {
        var sink = new InMemoryLogSink(4);
        sink.Emit(null!);
        Assert.AreEqual(0, sink.Count);
    }

    [TestMethod]
    public void Constructor_NonPositiveCapacity_FallsBackToDefault()
    {
        var sink = new InMemoryLogSink(0);
        sink.Emit(TestEvents.Make("kept"));
        Assert.AreEqual(1, sink.Count);
    }
}
