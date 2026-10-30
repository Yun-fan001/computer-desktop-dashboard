using System;
using System.Windows;
using System.Windows.Threading;
using KanbanBoard.Services;

namespace KanbanBoard.Controls;

public partial class ToastWindow : Window
{
    private DispatcherTimer? _closeTimer;

    public event Action? Snoozed;
    public event Action? Completed;
    public event Action? Dismissed;

    public ToastWindow(string title, string body, bool showActions)
    {
        InitializeComponent();
        TitleText.Text = title;
        BodyText.Text = body;
        SnoozeBtn.Content = L10n.T("notify.snooze");
        CompleteBtn.Content = L10n.T("notify.complete");
        DismissBtn.Content = L10n.T("notify.dismiss");
        ActionsPanel.Visibility = showActions ? Visibility.Visible : Visibility.Collapsed;
        Loaded += (_, _) => StartAutoClose(TimeSpan.FromSeconds(6));
    }

    public void StartAutoClose(TimeSpan after)
    {
        _closeTimer = new DispatcherTimer { Interval = after };
        _closeTimer.Tick += (_, _) => Close();
        _closeTimer.Start();
    }

    private void Snooze_Click(object sender, RoutedEventArgs e) { Snoozed?.Invoke(); Close(); }
    private void Complete_Click(object sender, RoutedEventArgs e) { Completed?.Invoke(); Close(); }
    private void Dismiss_Click(object sender, RoutedEventArgs e) { Dismissed?.Invoke(); Close(); }
}
