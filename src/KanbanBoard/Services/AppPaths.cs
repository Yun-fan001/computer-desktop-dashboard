using System;
using System.IO;

namespace KanbanBoard.Services;

/// <summary>
/// 应用数据目录、数据库、备份、日志路径的统一管理。
/// </summary>
public static class AppPaths
{
    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KanbanBoard");

    public static string DbPath => Path.Combine(DataDir, "kanban.db");
    public static string SettingsPath => Path.Combine(DataDir, "settings.json");
    public static string BackupDir => Path.Combine(DataDir, "backups");
    public static string LogDir => Path.Combine(DataDir, "logs");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(BackupDir);
        Directory.CreateDirectory(LogDir);
    }
}