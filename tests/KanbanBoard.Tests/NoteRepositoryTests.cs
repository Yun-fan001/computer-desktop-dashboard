using System;
using System.IO;
using KanbanBoard.Data;
using KanbanBoard.Models;
using Xunit;

namespace KanbanBoard.Tests;

public class NoteRepositoryTests : IDisposable
{
    private readonly string _dir;
    private readonly Database _db;
    private readonly NoteRepository _repo;

    public NoteRepositoryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "kanban_note_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _db = new Database(Path.Combine(_dir, "test.db"));
        _db.Open();
        _repo = new NoteRepository(_db);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void AddGetUpdateDelete_Works()
    {
        var n = new Note { Title = "便签1", Content = "内容", Color = "Blue", IsPinned = true };
        var id = _repo.Add(n);
        Assert.True(id > 0);

        var loaded = _repo.Get(id);
        Assert.NotNull(loaded);
        Assert.Equal("便签1", loaded!.Title);
        Assert.Equal("Blue", loaded.Color);
        Assert.True(loaded.IsPinned);

        loaded.Title = "改";
        _repo.Update(loaded);
        Assert.Equal("改", _repo.Get(id)!.Title);

        _repo.Delete(id);
        Assert.Null(_repo.Get(id));
    }

    [Fact]
    public void GetPinned_ReturnsOnlyPinned()
    {
        _repo.Add(new Note { Title = "钉", IsPinned = true });
        _repo.Add(new Note { Title = "不钉", IsPinned = false });

        var pinned = _repo.GetPinned();
        Assert.Single(pinned);
        Assert.Equal("钉", pinned[0].Title);
    }
}
