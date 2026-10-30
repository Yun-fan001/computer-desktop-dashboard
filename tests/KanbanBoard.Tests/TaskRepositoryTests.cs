using System;
using System.IO;
using KanbanBoard.Data;
using KanbanBoard.Models;
using Xunit;

namespace KanbanBoard.Tests;

public class TaskRepositoryTests : IDisposable
{
    private readonly string _dir;
    private readonly Database _db;
    private readonly TaskRepository _repo;

    public TaskRepositoryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "kanban_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _db = new Database(Path.Combine(_dir, "test.db"));
        _db.Open();
        _repo = new TaskRepository(_db);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void AddAndGet_Works()
    {
        var t = new TaskItem { Title = "测试任务", Priority = Priority.High, Tags = new() { "工作" } };
        var id = _repo.Add(t);
        Assert.True(id > 0);

        var loaded = _repo.Get(id);
        Assert.NotNull(loaded);
        Assert.Equal("测试任务", loaded!.Title);
        Assert.Equal(Priority.High, loaded.Priority);
        Assert.Contains("工作", loaded.Tags);
    }

    [Fact]
    public void SoftDelete_AndRestore_Works()
    {
        var t = new TaskItem { Title = "删除我" };
        var id = _repo.Add(t);

        _repo.SoftDelete(id);
        Assert.Empty(_repo.GetActiveTasks());
        Assert.Single(_repo.GetDeleted());

        _repo.Restore(id);
        Assert.Single(_repo.GetActiveTasks());
    }

    [Fact]
    public void MoveToColumn_Works()
    {
        var a = new TaskItem { Title = "A" };
        _repo.Add(a);

        _repo.Move(a.Id, ColumnType.Done, 0);
        var done = _repo.GetActiveByColumn(ColumnType.Done);
        Assert.Single(done);
        Assert.Equal(a.Id, done[0].Id);
    }

    [Fact]
    public void Subtasks_AreSavedAndLoaded()
    {
        var t = new TaskItem
        {
            Title = "带子任务",
            Subtasks = new()
            {
                new Subtask { Title = "子1", IsDone = true },
                new Subtask { Title = "子2" },
            },
        };
        var id = _repo.Add(t);

        var loaded = _repo.Get(id);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Subtasks.Count);
        Assert.True(loaded.Subtasks[0].IsDone);
    }

    [Fact]
    public void Archive_AndHardDelete_Works()
    {
        var t = new TaskItem { Title = "归档我" };
        var id = _repo.Add(t);

        _repo.SetArchived(id, true);
        Assert.Empty(_repo.GetActiveTasks());
        Assert.Single(_repo.GetArchived());

        _repo.SetArchived(id, false);
        Assert.Single(_repo.GetActiveTasks());

        _repo.SoftDelete(id);
        _repo.HardDelete(id);
        Assert.Empty(_repo.GetDeleted());
        Assert.Null(_repo.Get(id));
    }

    [Fact]
    public void GetDueBetween_ReturnsTasksInRange()
    {
        _repo.Add(new TaskItem { Title = "今天到期", DueDate = DateTime.Today });
        _repo.Add(new TaskItem { Title = "下月到期", DueDate = DateTime.Today.AddDays(40) });

        var inRange = _repo.GetDueBetween(DateTime.Today, DateTime.Today.AddDays(7));
        Assert.Single(inRange);
        Assert.Equal("今天到期", inRange[0].Title);
    }
}
