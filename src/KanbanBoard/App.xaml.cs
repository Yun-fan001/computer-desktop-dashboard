using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KanbanBoard.Controls;
using KanbanBoard.Models;
using KanbanBoard.Services;

namespace KanbanBoard;

/// <summary>
/// 应用入口：引导、全局崩溃处理、托盘/热键/提醒/边界场景编排。
/// </summary>
public partial class App : Application
{
    public static bool IsSmokeTest { get; private set; }

    private MainWindow? _mainWindow;
    private HotkeyService? _hotkeys;
    private TrayIconService? _tray;
    private NotificationService? _notify;
    private ReminderService? _reminder;
    private FullscreenDetector? _fullscreen;
    private SessionLockDetector? _sessionLock;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        IsSmokeTest = e.Args.Any(a => string.Equals(a, "--smoke-test", StringComparison.OrdinalIgnoreCase));

        AppPaths.EnsureCreated();
        CrashLogService.Init();

        DispatcherUnhandledException += (_, args) =>
        {
            CrashLogService.Write("DispatcherUnhandledException", args.Exception);
            args.Handled = true;
        };

        try
        {
            AppServices.Initialize();
        }
        catch (Exception ex)
        {
            CrashLogService.Write("App.Initialize", ex);
            MessageBox.Show(ex.Message, L10n.T("app.name"), MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        if (AppServices.Db.RecoveredFromBackup)
        {
            MessageBox.Show(L10n.T("msg.dbRecovered"), L10n.T("app.name"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;
        _mainWindow.Show();

        // 延迟启动
        int delay = ParseDelay(e.Args);
        if (delay > 0)
        {
            _mainWindow.Hide();
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(delay) };
            t.Tick += (_, _) => { t.Stop(); _mainWindow.Show(); };
            t.Start();
        }

        StartServices();

        if (IsSmokeTest)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (_, _) => { timer.Stop(); Shutdown(); };
            timer.Start();
        }
    }

    private static int ParseDelay(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--delay" && int.TryParse(args[i + 1], out var d)) return d;
        return 0;
    }

    private void StartServices()
    {
        _notify = new NotificationService();

        _hotkeys = new HotkeyService();
        _hotkeys.QuickAddPressed += () => Dispatcher.Invoke(() => _mainWindow?.ShowQuickAdd());
        _hotkeys.TogglePressed += () => Dispatcher.Invoke(() => _mainWindow?.ToggleVisibility());
        _hotkeys.CommandPalettePressed += () => Dispatcher.Invoke(CommandPaletteWindow.Open);
        _hotkeys.Register(AppServices.Settings.Current.QuickAddHotkey,
            AppServices.Settings.Current.ToggleHotkey,
            AppServices.Settings.Current.CommandPaletteHotkey);

        _tray = new TrayIconService();
        _tray.ToggleRequested += () => Dispatcher.Invoke(() => _mainWindow?.ToggleVisibility());
        _tray.Start(L10n.T("app.name"), BuildTrayMenu());

        _reminder = new ReminderService();
        _reminder.MergedNotify += (title, body) => _notify.Show(title, body);
        _reminder.TaskNotify += OnTaskRemind;
        _reminder.Start();

        _fullscreen = new FullscreenDetector();
        _fullscreen.FullscreenChanged += fs =>
        {
            if (!AppServices.Settings.Current.AutoHideOnFullscreen) return;
            Dispatcher.Invoke(() => { if (fs) _mainWindow?.Hide(); else _mainWindow?.Show(); });
        };
        _fullscreen.Start();

        _sessionLock = new SessionLockDetector();
        _sessionLock.Locked += () => Dispatcher.Invoke(() => _mainWindow?.Hide());
        _sessionLock.Unlocked += () => Dispatcher.Invoke(() => _mainWindow?.Show());
        _sessionLock.Start();

        // 恢复置顶便签的贴桌面浮窗
        StickyNotes.RefreshAll();
    }

    private void OnTaskRemind(TaskItem task)
    {
        var body = task.DueDate != null ? $"{task.DueDate:HH:mm}  {task.Title}" : task.Title;
        _notify?.Show(L10n.T("app.name"), body, showActions: true,
            onSnooze: () => AppServices.Tasks.SetReminder(task.Id, DateTime.Now.AddMinutes(15)),
            onComplete: () => AppServices.Tasks.SetCompleted(task.Id, true));
    }

    private ContextMenu BuildTrayMenu()
    {
        var menu = new ContextMenu();

        var showHide = new MenuItem { Header = L10n.T("menu.showHide") };
        showHide.Click += (_, _) => _mainWindow?.ToggleVisibility();

        var quickAdd = new MenuItem { Header = L10n.T("menu.quickAdd") };
        quickAdd.Click += (_, _) => _mainWindow?.ShowQuickAdd();

        var notes = new MenuItem { Header = L10n.T("notes.title") };
        notes.Click += (_, _) => Controls.NotesWindow.Open();

        var focus = new MenuItem { Header = L10n.T("menu.focusMode"), IsCheckable = true };
        focus.IsChecked = AppServices.Settings.Current.FocusMode;
        focus.Click += (_, _) =>
        {
            AppServices.Settings.Current.FocusMode = focus.IsChecked;
            AppServices.Settings.Save();
        };

        var lockLayout = new MenuItem { Header = L10n.T("menu.lockLayout"), IsCheckable = true };
        lockLayout.IsChecked = AppServices.Settings.Current.LockLayout;
        lockLayout.Click += (_, _) =>
        {
            AppServices.Settings.Current.LockLayout = lockLayout.IsChecked;
            AppServices.Settings.Save();
            _mainWindow?.ApplySettings();
        };

        var clickThrough = new MenuItem { Header = L10n.T("menu.clickThrough"), IsCheckable = true };
        clickThrough.IsChecked = AppServices.Settings.Current.ClickThrough;
        clickThrough.Click += (_, _) =>
        {
            AppServices.Settings.Current.ClickThrough = clickThrough.IsChecked;
            AppServices.Settings.Save();
            _mainWindow?.ApplySettings();
        };

        var autoStart = new MenuItem { Header = L10n.T("menu.autoStart"), IsCheckable = true };
        autoStart.IsChecked = AutoStartService.IsEnabled();
        autoStart.Click += (_, _) =>
        {
            AppServices.Settings.Current.AutoStart = autoStart.IsChecked;
            AutoStartService.SetEnabled(autoStart.IsChecked,
                AppServices.Settings.Current.AutoStartDelayed,
                AppServices.Settings.Current.AutoStartDelaySeconds);
            AppServices.Settings.Save();
        };

        var settings = new MenuItem { Header = L10n.T("menu.settings") };
        settings.Click += (_, _) => _mainWindow?.OpenSettings();

        var update = new MenuItem { Header = L10n.T("menu.checkUpdate") };
        update.Click += async (_, _) =>
        {
            var (has, info, err) = await UpdateService.CheckAsync(AppServices.Settings.Current.UpdateFeedUrl);
            string msg = err == "no-feed" || err == null && !has
                ? L10n.T("upd.latest")
                : err != null
                    ? L10n.T("upd.failed") + ": " + err
                    : $"{L10n.T("upd.available")} v{info!.Version}";
            _notify?.Show(L10n.T("app.name"), msg);
        };

        var exit = new MenuItem { Header = L10n.T("menu.exit") };
        exit.Click += (_, _) => Shutdown();

        menu.Items.Add(showHide);
        menu.Items.Add(quickAdd);
        menu.Items.Add(notes);
        menu.Items.Add(new Separator());
        menu.Items.Add(focus);
        menu.Items.Add(lockLayout);
        menu.Items.Add(clickThrough);
        menu.Items.Add(autoStart);
        menu.Items.Add(new Separator());
        menu.Items.Add(settings);
        menu.Items.Add(update);
        menu.Items.Add(new Separator());
        menu.Items.Add(exit);
        return menu;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _reminder?.Stop();
        _fullscreen?.Stop();
        _sessionLock?.Dispose();
        AppServices.Settings.Save();
        base.OnExit(e);
    }
}


