using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using KanbanBoard.Models;

namespace KanbanBoard.Data;

/// <summary>
/// 任务仓储：看板、搜索、归档、回收站、提醒等全部数据访问。
/// 日期统一以 DateTime.Ticks (long) 存储，避免区域/时区歧义。
/// </summary>
public class TaskRepository
{
    private const string Cols =
        "Id,Title,Description,ColumnIndex,Priority,Tags,DueDate,ReminderAt,RepeatRule," +
        "SortOrder,IsArchived,IsDeleted,DeletedAt,CompletedAt,CreatedAt,UpdatedAt";

    private readonly Database _db;

    public TaskRepository(Database db) => _db = db;

    // ==================== 查询 ====================

    public List<TaskItem> GetActiveTasks()
    {
        return Query($"SELECT {Cols} FROM tasks WHERE IsArchived=0 AND IsDeleted=0 ORDER BY SortOrder, Id;");
    }

    public List<TaskItem> GetActiveByColumn(ColumnType col)
    {
        return Query(
            $"SELECT {Cols} FROM tasks WHERE IsArchived=0 AND IsDeleted=0 AND ColumnIndex=$col ORDER BY SortOrder, Id;",
            ("$col", (int)col));
    }

    public TaskItem? Get(long id)
    {
        var task = QuerySingle($"SELECT {Cols} FROM tasks WHERE Id=$id;", ("$id", id));
        if (task != null) task.Subtasks = GetSubtasks(id);
        return task;
    }

    public List<TaskItem> GetDeleted()
    {
        return Query($"SELECT {Cols} FROM tasks WHERE IsDeleted=1 ORDER BY DeletedAt DESC, Id DESC;");
    }

    public List<TaskItem> GetArchived()
    {
        return Query($"SELECT {Cols} FROM tasks WHERE IsArchived=1 AND IsDeleted=0 ORDER BY UpdatedAt DESC;");
    }

    public List<TaskItem> Search(string query)
    {
        var q = $"%{query}%";
        return Query(
            $"SELECT {Cols} FROM tasks WHERE IsArchived=0 AND IsDeleted=0 " +
            "AND (Title LIKE $q OR Description LIKE $q OR Tags LIKE $q) ORDER BY SortOrder, Id;",
            ("$q", q));
    }

    public List<TaskItem> GetOverdue(DateTime now)
    {
        return Query(
            $"SELECT {Cols} FROM tasks WHERE IsArchived=0 AND IsDeleted=0 AND ColumnIndex<>{(int)ColumnType.Done} " +
            "AND DueDate IS NOT NULL AND DueDate < $now ORDER BY DueDate;",
            ("$now", now.Ticks));
    }

    public List<TaskItem> GetDueToday(DateTime today)
    {
        var start = today.Date.Ticks;
        var end = today.Date.AddDays(1).Ticks;
        return Query(
            $"SELECT {Cols} FROM tasks WHERE IsArchived=0 AND IsDeleted=0 AND ColumnIndex<>{(int)ColumnType.Done} " +
            "AND DueDate IS NOT NULL AND DueDate >= $start AND DueDate < $end ORDER BY DueDate;",
            ("$start", start), ("$end", end));
    }

    /// <summary>取某时间段内到期的任务（日历用，含已完成）。</summary>
    public List<TaskItem> GetDueBetween(DateTime start, DateTime end)
    {
        return Query(
            $"SELECT {Cols} FROM tasks WHERE IsArchived=0 AND IsDeleted=0 " +
            "AND DueDate IS NOT NULL AND DueDate >= $s AND DueDate < $e ORDER BY DueDate;",
            ("$s", start.Ticks), ("$e", end.Ticks));
    }

    /// <summary>今日待办数：今日到期 + 已逾期。</summary>
    public int GetTodayCount(DateTime today)
    {
        var end = today.Date.AddDays(1).Ticks;
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM tasks WHERE IsArchived=0 AND IsDeleted=0 AND ColumnIndex<>2 " +
            "AND DueDate IS NOT NULL AND DueDate < $end;";
        cmd.Parameters.AddWithValue("$end", end);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<TaskItem> GetDueReminders(DateTime now)
    {
        return Query(
            $"SELECT {Cols} FROM tasks WHERE IsArchived=0 AND IsDeleted=0 AND ColumnIndex<>{(int)ColumnType.Done} " +
            "AND ReminderAt IS NOT NULL AND ReminderAt <= $now ORDER BY ReminderAt;",
            ("$now", now.Ticks));
    }

    public List<Subtask> GetSubtasks(long taskId)
    {
        var list = new List<Subtask>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id,TaskId,Title,IsDone,SortOrder FROM subtasks WHERE TaskId=$t ORDER BY SortOrder, Id;";
        cmd.Parameters.AddWithValue("$t", taskId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Subtask
            {
                Id = r.GetInt64(0),
                TaskId = r.GetInt64(1),
                Title = r.GetString(2),
                IsDone = r.GetInt64(3) != 0,
                SortOrder = (int)r.GetInt64(4),
            });
        }
        return list;
    }

    // ==================== 写入 ====================

    public long Add(TaskItem t)
    {
        t.CreatedAt = DateTime.Now;
        t.UpdatedAt = t.CreatedAt;
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText =
                    "INSERT INTO tasks (Title,Description,ColumnIndex,Priority,Tags,DueDate,ReminderAt,RepeatRule," +
                    "SortOrder,IsArchived,IsDeleted,DeletedAt,CompletedAt,CreatedAt,UpdatedAt) " +
                    "VALUES ($t,$d,$c,$p,$tags,$due,$rem,$rep,$so,0,0,NULL,NULL,$ca,$ua);";
                cmd.Parameters.AddWithValue("$t", t.Title);
                cmd.Parameters.AddWithValue("$d", t.Description);
                cmd.Parameters.AddWithValue("$c", (int)t.Column);
                cmd.Parameters.AddWithValue("$p", (int)t.Priority);
                cmd.Parameters.AddWithValue("$tags", TaskItem.SerializeTags(t.Tags));
                cmd.Parameters.AddWithValue("$due", (object?)t.DueDate?.Ticks ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$rem", (object?)t.ReminderAt?.Ticks ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$rep", t.RepeatRule);
                cmd.Parameters.AddWithValue("$so", t.SortOrder);
                cmd.Parameters.AddWithValue("$ca", t.CreatedAt.Ticks);
                cmd.Parameters.AddWithValue("$ua", t.UpdatedAt.Ticks);
                cmd.ExecuteNonQuery();
            }
            using (var idCmd = conn.CreateCommand())
            {
                idCmd.Transaction = tx;
                idCmd.CommandText = "SELECT last_insert_rowid();";
                t.Id = (long)idCmd.ExecuteScalar()!;
            }
            SaveSubtasks(conn, tx, t);
            tx.Commit();
            return t.Id;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void Update(TaskItem t)
    {
        t.UpdatedAt = DateTime.Now;
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                "UPDATE tasks SET Title=$t, Description=$d, ColumnIndex=$c, Priority=$p, Tags=$tags, DueDate=$due, " +
                "ReminderAt=$rem, RepeatRule=$rep, SortOrder=$so, IsArchived=$ar, IsDeleted=$del, DeletedAt=$da, " +
                "CompletedAt=$co, UpdatedAt=$ua WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$t", t.Title);
            cmd.Parameters.AddWithValue("$d", t.Description);
            cmd.Parameters.AddWithValue("$c", (int)t.Column);
            cmd.Parameters.AddWithValue("$p", (int)t.Priority);
            cmd.Parameters.AddWithValue("$tags", TaskItem.SerializeTags(t.Tags));
            cmd.Parameters.AddWithValue("$due", (object?)t.DueDate?.Ticks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rem", (object?)t.ReminderAt?.Ticks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rep", t.RepeatRule);
            cmd.Parameters.AddWithValue("$so", t.SortOrder);
            cmd.Parameters.AddWithValue("$ar", t.IsArchived ? 1 : 0);
            cmd.Parameters.AddWithValue("$del", t.IsDeleted ? 1 : 0);
            cmd.Parameters.AddWithValue("$da", (object?)t.DeletedAt?.Ticks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$co", (object?)t.CompletedAt?.Ticks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ua", t.UpdatedAt.Ticks);
            cmd.Parameters.AddWithValue("$id", t.Id);
            cmd.ExecuteNonQuery();

            using (var del = conn.CreateCommand())
            {
                del.Transaction = tx;
                del.CommandText = "DELETE FROM subtasks WHERE TaskId=$id;";
                del.Parameters.AddWithValue("$id", t.Id);
                del.ExecuteNonQuery();
            }
            SaveSubtasks(conn, tx, t);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void SoftDelete(long id)
    {
        Execute("UPDATE tasks SET IsDeleted=1, DeletedAt=$n WHERE Id=$id;",
            ("$n", DateTime.Now.Ticks), ("$id", id));
    }

    public void Restore(long id)
    {
        Execute("UPDATE tasks SET IsDeleted=0, DeletedAt=NULL WHERE Id=$id;", ("$id", id));
    }

    public void HardDelete(long id)
    {
        Execute("DELETE FROM tasks WHERE Id=$id;", ("$id", id));
    }

    public void SetArchived(long id, bool archived)
    {
        Execute("UPDATE tasks SET IsArchived=$a WHERE Id=$id;", ("$a", archived ? 1 : 0), ("$id", id));
    }

    public void Move(long id, ColumnType col, int sortOrder)
    {
        Execute("UPDATE tasks SET ColumnIndex=$c, SortOrder=$s WHERE Id=$id;",
            ("$c", (int)col), ("$s", sortOrder), ("$id", id));
    }

    public void SetCompleted(long id, bool done)
    {
        Execute($"UPDATE tasks SET ColumnIndex={(int)(done ? ColumnType.Done : ColumnType.Todo)}, " +
                "CompletedAt=$co WHERE Id=$id;",
            ("$co", (object?)(done ? DateTime.Now.Ticks : (long?)null) ?? DBNull.Value), ("$id", id));
    }

    public void ClearReminder(long id)
    {
        Execute("UPDATE tasks SET ReminderAt=NULL WHERE Id=$id;", ("$id", id));
    }

    public void SetReminder(long id, DateTime at)
    {
        Execute("UPDATE tasks SET ReminderAt=$r WHERE Id=$id;", ("$r", at.Ticks), ("$id", id));
    }

    public void Reorder(ColumnType col, IReadOnlyList<long> orderedIds)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            for (int i = 0; i < orderedIds.Count; i++)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE tasks SET SortOrder=$s, ColumnIndex=$c WHERE Id=$id;";
                cmd.Parameters.AddWithValue("$s", i);
                cmd.Parameters.AddWithValue("$c", (int)col);
                cmd.Parameters.AddWithValue("$id", orderedIds[i]);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static void SaveSubtasks(SqliteConnection conn, SqliteTransaction tx, TaskItem t)
    {
        int i = 0;
        foreach (var s in t.Subtasks)
        {
            s.TaskId = t.Id;
            s.SortOrder = i++;
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO subtasks (TaskId,Title,IsDone,SortOrder) VALUES ($t,$ti,$d,$s);";
            cmd.Parameters.AddWithValue("$t", t.Id);
            cmd.Parameters.AddWithValue("$ti", s.Title);
            cmd.Parameters.AddWithValue("$d", s.IsDone ? 1 : 0);
            cmd.Parameters.AddWithValue("$s", s.SortOrder);
            cmd.ExecuteNonQuery();
        }
    }

    private List<TaskItem> Query(string sql, params (string Name, object Value)[] ps)
    {
        var list = new List<TaskItem>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in ps) cmd.Parameters.AddWithValue(p.Name, p.Value);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    private TaskItem? QuerySingle(string sql, params (string Name, object Value)[] ps)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in ps) cmd.Parameters.AddWithValue(p.Name, p.Value);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    private void Execute(string sql, params (string Name, object Value)[] ps)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in ps) cmd.Parameters.AddWithValue(p.Name, p.Value);
        cmd.ExecuteNonQuery();
    }

    private static TaskItem Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        Title = r.GetString(1),
        Description = r.GetString(2),
        Column = (ColumnType)r.GetInt64(3),
        Priority = (Priority)r.GetInt64(4),
        Tags = TaskItem.DeserializeTags(r.GetString(5)),
        DueDate = ReadDate(r, 6),
        ReminderAt = ReadDate(r, 7),
        RepeatRule = r.GetString(8),
        SortOrder = (int)r.GetInt64(9),
        IsArchived = r.GetInt64(10) != 0,
        IsDeleted = r.GetInt64(11) != 0,
        DeletedAt = ReadDate(r, 12),
        CompletedAt = ReadDate(r, 13),
        CreatedAt = ReadDate(r, 14) ?? DateTime.Now,
        UpdatedAt = ReadDate(r, 15) ?? DateTime.Now,
    };

    private static DateTime? ReadDate(SqliteDataReader r, int i) =>
        r.IsDBNull(i) ? null : new DateTime(r.GetInt64(i));
}



