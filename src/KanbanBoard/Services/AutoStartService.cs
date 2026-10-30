using System;
using System.IO;
using Microsoft.Win32;

namespace KanbanBoard.Services;

/// <summary>开机自启（HKCU Run 键），支持延迟启动。</summary>
public static class AutoStartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KanbanBoard";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) != null;
    }

    public static void SetEnabled(bool enabled, bool delayed, int delaySeconds)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (!enabled)
        {
            try { key.DeleteValue(ValueName, false); } catch { }
            return;
        }
        var exe = Path.Combine(AppContext.BaseDirectory, "KanbanBoard.exe");
        var args = delayed ? $"--autostart --delay {delaySeconds}" : "--autostart";
        key.SetValue(ValueName, $"\"{exe}\" {args}");
    }
}
