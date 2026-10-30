using System;
using System.IO;
using System.Text.Json;
using KanbanBoard.Models;

namespace KanbanBoard.Services;

/// <summary>
/// 设置服务：负责从 settings.json 加载、保存，并在变化时通知。
/// </summary>
public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private readonly object _lock = new();

    public AppSettings Current { get; private set; } = new();

    public event EventHandler? Changed;

    public void Load()
    {
        try
        {
            if (File.Exists(AppPaths.SettingsPath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppPaths.SettingsPath));
                if (loaded != null) Current = loaded;
            }
        }
        catch (Exception ex)
        {
            CrashLogService.Write("SettingsService.Load", ex);
            Current = new AppSettings();
        }
        Normalize();
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                AppPaths.EnsureCreated();
                Normalize();
                File.WriteAllText(AppPaths.SettingsPath, JsonSerializer.Serialize(Current, JsonOpts));
            }
            catch (Exception ex)
            {
                // 磁盘权限不足等：记录日志；UI 层负责友好提示。
                CrashLogService.Write("SettingsService.Save", ex);
            }
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Normalize()
    {
        if (Current.Opacity < 0.1) Current.Opacity = 0.1;
        if (Current.Opacity > 1.0) Current.Opacity = 1.0;
        if (Current.BackupDays < 7) Current.BackupDays = 7;
        if (Current.BackupDays > 30) Current.BackupDays = 30;
        if (Current.HoverExpandDelayMs < 0) Current.HoverExpandDelayMs = 0;
        if (Current.HoverCollapseDelayMs < 0) Current.HoverCollapseDelayMs = 0;
        if (Current.FontScale < 0.8) Current.FontScale = 0.8;
        if (Current.FontScale > 1.6) Current.FontScale = 1.6;
    }
}
