using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using KanbanBoard.Services;

namespace KanbanBoard.Data;

/// <summary>
/// 备份服务：使用 VACUUM INTO 生成一致快照，并按保留天数轮转。
/// </summary>
public static class BackupService
{
    /// <summary>立即备份（失败静默，不阻断主流程）。</summary>
    public static void BackupNow(string? srcPath = null)
    {
        try
        {
            AppPaths.EnsureCreated();
            var src = srcPath ?? AppPaths.DbPath;
            if (!File.Exists(src)) return;

            var dest = Path.Combine(AppPaths.BackupDir, $"kanban-{DateTime.Now:yyyyMMdd-HHmmss-fff}.db");
            if (File.Exists(dest))
                dest = Path.Combine(AppPaths.BackupDir, $"kanban-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.db");
            using var conn = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = src,
                Mode = SqliteOpenMode.ReadOnly,
            }.ToString());
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "VACUUM INTO $dest;";
            cmd.Parameters.AddWithValue("$dest", dest);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            CrashLogService.Write("BackupService.BackupNow", ex);
        }
    }

    /// <summary>按保留天数清理旧备份。</summary>
    public static void Prune(int days)
    {
        try
        {
            if (days < 7) days = 7;
            if (days > 30) days = 30;
            var dir = AppPaths.BackupDir;
            if (!Directory.Exists(dir)) return;
            var cutoff = DateTime.Now.AddDays(-days);
            foreach (var f in Directory.GetFiles(dir, "kanban-*.db"))
            {
                try
                {
                    if (File.GetLastWriteTime(f) < cutoff) File.Delete(f);
                }
                catch { /* 忽略单个文件删除失败 */ }
            }
        }
        catch { }
    }

    /// <summary>查找最近一个通过完整性校验的备份。</summary>
    public static string? FindLatestValidBackup()
    {
        try
        {
            var dir = AppPaths.BackupDir;
            if (!Directory.Exists(dir)) return null;
            foreach (var f in Directory.GetFiles(dir, "kanban-*.db").OrderByDescending(x => x))
            {
                if (IsValid(f)) return f;
            }
        }
        catch { }
        return null;
    }

    private static bool IsValid(string path)
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
            if (!string.Equals(cmd.ExecuteScalar() as string, "ok", StringComparison.OrdinalIgnoreCase))
                return false;
            cmd.CommandText = "PRAGMA user_version;";
            return cmd.ExecuteScalar() is long v && v >= 1;
        }
        catch
        {
            return false;
        }
    }
}
