using System;
using System.Collections.Generic;
using System.Text.Json;

namespace KanbanBoard.Models;

/// <summary>看板列：待办 / 进行中 / 完成。</summary>
public enum ColumnType
{
    Todo = 0,
    Doing = 1,
    Done = 2,
}

/// <summary>优先级。</summary>
public enum Priority
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4,
}

/// <summary>重复规则（数据库存储的字符串常量）。</summary>
public static class RepeatRules
{
    public const string None = "";
    public const string Daily = "daily";
    public const string Weekdays = "weekdays";
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";
    public const string Yearly = "yearly";

    public static readonly string[] All = { Daily, Weekdays, Weekly, Monthly, Yearly };
}

/// <summary>子任务。</summary>
public class Subtask
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public string Title { get; set; } = "";
    public bool IsDone { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>任务卡片。</summary>
public class TaskItem
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public ColumnType Column { get; set; } = ColumnType.Todo;
    public Priority Priority { get; set; } = Priority.None;
    public List<string> Tags { get; set; } = new();
    public DateTime? DueDate { get; set; }
    public DateTime? ReminderAt { get; set; }
    public string RepeatRule { get; set; } = RepeatRules.None;
    public int SortOrder { get; set; }
    public bool IsArchived { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<Subtask> Subtasks { get; set; } = new();

    public bool IsCompleted => Column == ColumnType.Done;

    public static string SerializeTags(List<string>? tags) =>
        JsonSerializer.Serialize(tags ?? new List<string>());

    public static List<string> DeserializeTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch { return new List<string>(); }
    }
}
