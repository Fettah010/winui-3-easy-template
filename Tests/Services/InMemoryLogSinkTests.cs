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
        Assert.AreEqual("second", snapshot[0].Message);
        Assert.AreEqual("first", snapshot[1].Message);
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
        Assert.IsFalse(snapshot.Any(e => e.Message == "one"));
    }

    [TestMethod]
    public void Emit_NullEvent_IsIgnored()
    {
        var sink = new InMemoryLogSink(4);
        sink.Emit(null!);
        Assert.AreEqual(0, sink.Count);
    }

    [TestMethod]
    public void Clear_DropsEverything()
    {
        var sink = new InMemoryLogSink(4);
        sink.Emit(TestEvents.Make("one"));
        sink.Emit(TestEvents.Make("two"));
        sink.Clear();
        Assert.AreEqual(0, sink.Count);
        Assert.IsEmpty(sink.SnapshotNewestFirst());
        sink.Emit(TestEvents.Make("three"));
        Assert.AreEqual(1, sink.Count);
    }

    [TestMethod]
    public void Constructor_NonPositiveCapacity_FallsBackToDefault()
    {
        var sink = new InMemoryLogSink(0);
        sink.Emit(TestEvents.Make("kept"));
        Assert.AreEqual(1, sink.Count);
    }

    [TestMethod]
    public void Count_StaysBounded_UnderConcurrentEmit()
    {
        // P1-1: the O(1) counter stays exact under contention and the
        // buffer never grows past capacity.
        var sink = new InMemoryLogSink(64);
        System.Threading.Tasks.Parallel.For(0, 4096, i =>
            sink.Emit(TestEvents.Make("burst-" + i)));
        Assert.AreEqual(64, sink.Count);
        Assert.HasCount(64, sink.SnapshotNewestFirst());
    }
}
