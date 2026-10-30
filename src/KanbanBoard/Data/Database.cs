using System;
using System.IO;
using Microsoft.Data.Sqlite;
using KanbanBoard.Services;

namespace KanbanBoard.Data;

/// <summary>
/// SQLite 数据库：连接管理、WAL、完整性校验、崩溃恢复、版本迁移。
/// </summary>
public class Database
{
    public const int CurrentVersion = 2;

    private readonly string _path;
    private string _connectionString = "";

    public Database(string? path = null) => _path = path ?? AppPaths.DbPath;

    /// <summary>本次启动是否从备份恢复过（UI 需提示用户）。</summary>
    public bool RecoveredFromBackup { get; private set; }

    /// <summary>数据库版本高于程序支持版本（需阻止打开）。</summary>
    public bool BlockedByNewerVersion { get; private set; }

    public string? LastError { get; private set; }

    public void Open()
    {
        AppPaths.EnsureCreated();
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        // 完整性校验：损坏时读取最近有效备份，保留原文件。
        if (File.Exists(_path) && !IntegrityOk(_path))
        {
            var backup = BackupService.FindLatestValidBackup();
            if (backup != null)
            {
                var corrupted = _path + $".corrupted-{DateTime.Now:yyyyMMdd-HHmmss}";
                try
                {
                    File.Move(_path, corrupted);
                    File.Copy(backup, _path, overwrite: true);
                    RecoveredFromBackup = true;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }
            }
        }

        using var conn = CreateConnection();
        SetPragmas(conn);
        Migrate(conn);

        // 启动时执行一次自动备份 + 轮转（仅真实应用环境，测试时跳过）。
        if (AppServices.Settings != null && AppServices.Settings.Current.AutoBackup)
        {
            BackupService.BackupNow(_path);
            BackupService.Prune(AppServices.Settings.Current.BackupDays);
        }
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        SetPragmas(conn);
        return conn;
    }

    private static void SetPragmas(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;"; cmd.ExecuteNonQuery();
        cmd.CommandText = "PRAGMA synchronous=NORMAL;"; cmd.ExecuteNonQuery();
        cmd.CommandText = "PRAGMA foreign_keys=ON;"; cmd.ExecuteNonQuery();
        cmd.CommandText = "PRAGMA busy_timeout=3000;"; cmd.ExecuteNonQuery();
    }

    private void Migrate(SqliteConnection conn)
    {
        int version = GetUserVersion(conn);
        if (version > CurrentVersion)
        {
            BlockedByNewerVersion = true;
            throw new InvalidOperationException(
                $"数据库版本 v{version} 高于当前程序支持的 v{CurrentVersion}，请升级程序后再打开。");
        }
        if (version == CurrentVersion) return;

        // 迁移前备份旧库（仅当已有数据/旧版本时，全新库无需备份）。
        if (version > 0) BackupService.BackupNow(_path);

        using var tx = conn.BeginTransaction();
        try
        {
            if (version < 1)
            {
                Execute(conn, tx, SchemaV1);
                version = 1;
            }
            if (version < 2)
            {
                Execute(conn, tx, SchemaV2);
                version = 2;
            }
            // 未来迁移在此追加：if (version < 3) { ...; version = 3; }

            SetUserVersion(conn, tx, version);
            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            LastError = "数据库迁移失败，已回滚，可从备份恢复。";
            CrashLogService.Write("Database.Migrate", ex);
            throw;
        }
    }

    private static int GetUserVersion(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        return cmd.ExecuteScalar() is long l ? (int)l : 0;
    }

    private static void SetUserVersion(SqliteConnection conn, SqliteTransaction tx, int version)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"PRAGMA user_version = {version};";
        cmd.ExecuteNonQuery();
    }

    private static void Execute(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static void Execute(SqliteConnection conn, SqliteTransaction tx, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static bool IntegrityOk(string path)
    {
        try
        {
            using var conn = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
            }.ToString());
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA quick_check;";
            return string.Equals(cmd.ExecuteScalar() as string, "ok", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private const string SchemaV1 = @"
CREATE TABLE tasks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Description TEXT NOT NULL DEFAULT '',
    ColumnIndex INTEGER NOT NULL DEFAULT 0,
    Priority INTEGER NOT NULL DEFAULT 0,
    Tags TEXT NOT NULL DEFAULT '[]',
    DueDate INTEGER NULL,
    ReminderAt INTEGER NULL,
    RepeatRule TEXT NOT NULL DEFAULT '',
    SortOrder INTEGER NOT NULL DEFAULT 0,
    IsArchived INTEGER NOT NULL DEFAULT 0,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt INTEGER NULL,
    CompletedAt INTEGER NULL,
    CreatedAt INTEGER NOT NULL,
    UpdatedAt INTEGER NOT NULL
);
CREATE INDEX idx_tasks_column ON tasks(ColumnIndex);
CREATE INDEX idx_tasks_due ON tasks(DueDate);

CREATE TABLE subtasks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TaskId INTEGER NOT NULL,
    Title TEXT NOT NULL,
    IsDone INTEGER NOT NULL DEFAULT 0,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY(TaskId) REFERENCES tasks(Id) ON DELETE CASCADE
);
CREATE INDEX idx_subtasks_task ON subtasks(TaskId);
";

    private const string SchemaV2 = @"
CREATE TABLE notes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL DEFAULT '',
    Content TEXT NOT NULL DEFAULT '',
    Color TEXT NOT NULL DEFAULT 'Yellow',
    IsPinned INTEGER NOT NULL DEFAULT 0,
    IsLocked INTEGER NOT NULL DEFAULT 0,
    CreatedAt INTEGER NOT NULL,
    UpdatedAt INTEGER NOT NULL
);
CREATE INDEX idx_notes_pinned ON notes(IsPinned);
";
}

