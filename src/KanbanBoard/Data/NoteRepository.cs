using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using KanbanBoard.Models;

namespace KanbanBoard.Data;

/// <summary>便签仓储。</summary>
public class NoteRepository
{
    private readonly Database _db;

    public NoteRepository(Database db) => _db = db;

    public List<Note> GetAll()
    {
        var list = new List<Note>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id,Title,Content,Color,IsPinned,IsLocked,CreatedAt,UpdatedAt FROM notes " +
            "ORDER BY IsPinned DESC, UpdatedAt DESC;";
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<Note> GetPinned()
    {
        var list = new List<Note>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id,Title,Content,Color,IsPinned,IsLocked,CreatedAt,UpdatedAt FROM notes " +
            "WHERE IsPinned=1 ORDER BY UpdatedAt DESC;";
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public Note? Get(long id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id,Title,Content,Color,IsPinned,IsLocked,CreatedAt,UpdatedAt FROM notes WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public long Add(Note n)
    {
        n.CreatedAt = DateTime.Now;
        n.UpdatedAt = n.CreatedAt;
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO notes (Title,Content,Color,IsPinned,IsLocked,CreatedAt,UpdatedAt) " +
            "VALUES ($t,$c,$col,$p,$l,$ca,$ua); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$t", n.Title);
        cmd.Parameters.AddWithValue("$c", n.Content);
        cmd.Parameters.AddWithValue("$col", n.Color);
        cmd.Parameters.AddWithValue("$p", n.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("$l", n.IsLocked ? 1 : 0);
        cmd.Parameters.AddWithValue("$ca", n.CreatedAt.Ticks);
        cmd.Parameters.AddWithValue("$ua", n.UpdatedAt.Ticks);
        n.Id = (long)cmd.ExecuteScalar()!;
        return n.Id;
    }

    public void Update(Note n)
    {
        n.UpdatedAt = DateTime.Now;
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "UPDATE notes SET Title=$t, Content=$c, Color=$col, IsPinned=$p, IsLocked=$l, UpdatedAt=$ua WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$t", n.Title);
        cmd.Parameters.AddWithValue("$c", n.Content);
        cmd.Parameters.AddWithValue("$col", n.Color);
        cmd.Parameters.AddWithValue("$p", n.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("$l", n.IsLocked ? 1 : 0);
        cmd.Parameters.AddWithValue("$ua", n.UpdatedAt.Ticks);
        cmd.Parameters.AddWithValue("$id", n.Id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM notes WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void SetPinned(long id, bool pinned)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notes SET IsPinned=$p WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$p", pinned ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static Note Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        Title = r.GetString(1),
        Content = r.GetString(2),
        Color = r.GetString(3),
        IsPinned = r.GetInt64(4) != 0,
        IsLocked = r.GetInt64(5) != 0,
        CreatedAt = new DateTime(r.GetInt64(6)),
        UpdatedAt = new DateTime(r.GetInt64(7)),
    };
}