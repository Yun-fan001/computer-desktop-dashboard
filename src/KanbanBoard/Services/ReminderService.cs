using System;
using System.Windows.Threading;
using KanbanBoard.Models;

namespace KanbanBoard.Services;

/// <summary>
/// 提醒服务：定时检查到期提醒，触发通知，重复任务自动推进。
/// </summary>
public class ReminderService : IDisposable
{
    private readonly DispatcherTimer _timer;

    public event Action<string, string>? MergedNotify;
    public event Action<TaskItem>? TaskNotify;

    public ReminderService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => Check();
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void Check()
    {
        try
        {
            var s = AppServices.Settings.Current;
            if (s.FocusMode) return;
            if (s.SuppressWhenFocusAssist && FocusAssistHelper.IsEnabled()) return;

            var due = AppServices.Tasks.GetDueReminders(DateTime.Now);
            if (due.Count == 0) return;

            if (s.MergeNotifications && due.Count > 3)
            {
                MergedNotify?.Invoke(L10n.T("app.name"),
                    string.Format(L10n.T("notify.tasksDue"), due.Count));
            }
            else
            {
                foreach (var t in due) TaskNotify?.Invoke(t);
            }

            foreach (var t in due)
            {
                TimeSpan? gap = (t.DueDate != null && t.ReminderAt != null)
                    ? t.DueDate - t.ReminderAt
                    : null;
                AppServices.Tasks.ClearReminder(t.Id);
                if (!string.IsNullOrEmpty(t.RepeatRule)) AdvanceRepeat(t, gap);
            }
        }
        catch (Exception ex)
        {
            CrashLogService.Write("ReminderService.Check", ex);
        }
    }

    private static void AdvanceRepeat(TaskItem t, TimeSpan? gap)
    {
        if (t.DueDate == null) return;
        var next = t.RepeatRule switch
        {
            RepeatRules.Daily => t.DueDate.Value.AddDays(1),
            RepeatRules.Weekdays => NextWeekday(t.DueDate.Value.AddDays(1)),
            RepeatRules.Weekly => t.DueDate.Value.AddDays(7),
            RepeatRules.Monthly => t.DueDate.Value.AddMonths(1),
            RepeatRules.Yearly => t.DueDate.Value.AddYears(1),
            _ => (DateTime?)null,
        };
        if (next == null) return;
        t.DueDate = next;
        t.ReminderAt = gap.HasValue ? next.Value.Subtract(gap.Value) : null;
        AppServices.Tasks.Update(t);
    }

    private static DateTime NextWeekday(DateTime d)
    {
        while (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) d = d.AddDays(1);
        return d;
    }

    public void Dispose() => _timer.Stop();
}

/// <summary>尽力而为地检测系统专注助手/勿扰状态。</summary>
public static class FocusAssistHelper
{
    public static bool IsEnabled()
    {
        try
        {
            var v = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings",
                "NOC_GLOBAL_SETTING_TOASTS_ENABLED", 1);
            return v is int i && i == 0;
        }
        catch { return false; }
    }
}
