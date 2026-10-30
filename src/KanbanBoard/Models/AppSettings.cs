namespace KanbanBoard.Models;

/// <summary>
/// 应用设置（存于本地 settings.json，独立于任务数据库，保证异常时设置仍可用）。
/// </summary>
public class AppSettings
{
    // —— 外观 ——
    public string Theme { get; set; } = "Dark";          // Dark / Light / System
    public string Material { get; set; } = "Acrylic";    // None / Transparent / Acrylic / Mica
    public double Opacity { get; set; } = 0.92;          // 10%–100%
    public double FontScale { get; set; } = 1.0;         // 0.8–1.6
    public string AccentColor { get; set; } = "";        // 为空则用主题默认

    // —— 悬浮交互 ——
    public int HoverExpandDelayMs { get; set; } = 250;   // 悬停展开 200–400ms
    public int HoverCollapseDelayMs { get; set; } = 400; // 移开收起 300–600ms
    public bool AlwaysOnTop { get; set; } = true;
    public bool ClickThrough { get; set; }
    public bool LockLayout { get; set; }
    public bool AutoHideOnFullscreen { get; set; } = true;
    public bool AutoHideOnLock { get; set; } = true;
    public string VirtualDesktopMode { get; set; } = "Fixed"; // Fixed / Follow
    public double BarWidth { get; set; } = 300;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }

    // —— 快捷键 ——
    public string QuickAddHotkey { get; set; } = "Ctrl+Shift+Space";
    public string ToggleHotkey { get; set; } = "";
    public string CommandPaletteHotkey { get; set; } = "Ctrl+K";

    // —— 提醒 ——
    public bool FocusMode { get; set; }
    public bool MergeNotifications { get; set; } = true;
    public bool SuppressWhenFocusAssist { get; set; } = true;

    // —— 数据与备份 ——
    public bool AutoBackup { get; set; } = true;
    public int BackupDays { get; set; } = 14;            // 7–30
    public bool AnonymousReporting { get; set; } = false;

    // —— 启动 ——
    public bool AutoStart { get; set; }
    public bool AutoStartDelayed { get; set; }
    public int AutoStartDelaySeconds { get; set; } = 10;
    public bool StartMinimized { get; set; }

    // —— 通用 ——
    public string Language { get; set; } = "zh-CN";      // zh-CN / zh-TW / en
    public bool FollowSystemRegion { get; set; } = true;

    // —— 更新 ——
    public string UpdateChannel { get; set; } = "stable";
    public string UpdateFeedUrl { get; set; } = "";
    public bool AutoUpdate { get; set; } = true;
}
