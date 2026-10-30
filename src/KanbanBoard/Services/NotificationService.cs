using System;
using System.Collections.Generic;
using System.Windows;
using KanbanBoard.Controls;

namespace KanbanBoard.Services;

/// <summary>
/// 通知服务：在屏幕右下角堆叠显示自定义 Toast（可带操作按钮）。
/// </summary>
public class NotificationService
{
    private readonly List<ToastWindow> _open = new();
    private const double ToastWidth = 320;
    private const double Margin = 12;

    public void Show(string title, string body, bool showActions = false,
        Action? onSnooze = null, Action? onComplete = null, Action? onDismiss = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var toast = new ToastWindow(title, body, showActions);
            toast.Snoozed += () => onSnooze?.Invoke();
            toast.Completed += () => onComplete?.Invoke();
            toast.Dismissed += () => onDismiss?.Invoke();
            toast.SizeChanged += (_, _) => PositionToasts();
            toast.Closed += (_, _) => { _open.Remove(toast); PositionToasts(); };
            _open.Add(toast);
            toast.Show();
            PositionToasts();
        });
    }

    private void PositionToasts()
    {
        var wa = SystemParameters.WorkArea;
        for (int i = 0; i < _open.Count; i++)
        {
            var t = _open[i];
            if (t.ActualHeight <= 0) continue;
            t.Left = wa.Right - ToastWidth - Margin;
            t.Top = wa.Bottom - Margin - (t.ActualHeight + Margin) * (i + 1);
        }
    }
}
