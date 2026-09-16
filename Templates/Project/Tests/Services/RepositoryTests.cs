using System.Threading.Tasks;
using DevTemWinUi3.Services.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class RepositoryTests
{
    private sealed class Note
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    private static InMemoryRepository<Note> Create() =>
        new(note => note.Id);

    [TestMethod]
    public async Task Upsert_ThenGetById_RoundTrips()
    {
        var repo = Create();
        await repo.UpsertAsync(new Note { Id = "a", Text = "hello" });
        var found = await repo.GetByIdAsync("a");
        Assert.IsNotNull(found);
        Assert.AreEqual("hello", found.Text);
    }

    [TestMethod]
    public async Task GetById_Missing_ReturnsNull()
    {
        var repo = Create();
        Assert.IsNull(await repo.GetByIdAsync("nope"));
    }

    [TestMethod]
    public async Task Delete_RemovesItem()
    {
        var repo = Create();
        await repo.UpsertAsync(new Note { Id = "a" });
        Assert.IsTrue(await repo.DeleteAsync("a"));
        Assert.IsFalse(await repo.DeleteAsync("a"));
        Assert.AreEqual(0, repo.Count);
    }

    [TestMethod]
    public async Task GetAll_ReturnsSnapshot()
    {
        var repo = Create();
        await repo.UpsertAsync(new Note { Id = "a" });
        await repo.UpsertAsync(new Note { Id = "b" });
        var all = await repo.GetAllAsync();
        Assert.HasCount(2, all);
    }
}
