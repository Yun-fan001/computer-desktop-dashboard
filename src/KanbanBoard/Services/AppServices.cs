using KanbanBoard.Data;

namespace KanbanBoard.Services;

/// <summary>
/// 服务定位器：在启动时按依赖顺序初始化全局单例服务。
/// </summary>
public static class AppServices
{
    public static SettingsService Settings { get; private set; } = null!;
    public static Database Db { get; private set; } = null!;
    public static TaskRepository Tasks { get; private set; } = null!;
    public static NoteRepository Notes { get; private set; } = null!;
    public static SystemMonitorService Monitor { get; private set; } = null!;

    public static void Initialize()
    {
        Settings = new SettingsService();
        Settings.Load();

        Db = new Database();
        Db.Open();

        Tasks = new TaskRepository(Db);
        Notes = new NoteRepository(Db);
        Monitor = new SystemMonitorService();
    }
}
