using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using KanbanBoard.Controls;
using KanbanBoard.Interop;
using KanbanBoard.Models;
using KanbanBoard.Services;
using KanbanBoard.ViewModels;

namespace KanbanBoard;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private DispatcherTimer? _expandTimer;
    private DispatcherTimer? _collapseTimer;
    private bool _isExpanded;

    public bool IsExpanded => _isExpanded;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        UiContext.Main = _vm;

        TodoCol.Column = ColumnType.Todo;
        DoingCol.Column = ColumnType.Doing;
        DoneCol.Column = ColumnType.Done;

        _vm.Refreshed += RefreshColumns;

        Loaded += OnLoaded;
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
        LocationChanged += (_, _) => SavePosition();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm.Start();
        UiContext.OpenSettings = OpenSettings;
        RefreshColumns();
        ApplySettings();
        PositionInitial();
        SetToolWindowStyle();
        PopulateFilterCombo();
        ApplyViewMode();
        Calendar.DaySelected += OnDaySelected;
        AppServices.Monitor.Updated += UpdateMonitorText;
        AppServices.Monitor.Start();
        UpdateMonitorText();
    }

    public void RefreshBoard() => _vm.Refresh();

    public void ShowQuickAdd() => QuickAddWindow.ShowQuickAdd();

    public void OpenSettings()
    {
        SettingsWindow.Open();
        ApplySettings();
        _vm.Refresh();
        RefreshColumns();
    }

    public void ToggleVisibility()
    {
        if (Visibility == Visibility.Visible) Hide();
        else Show();
    }

    public void ApplySettings()
    {
        var s = AppServices.Settings.Current;
        L10n.SetLanguage(s.Language);
        ThemeService.Apply(s.Theme);
        ApplyOpacity();
        SetTopmost(s.AlwaysOnTop);
        SetClickThrough(s.ClickThrough);
        ApplyLocalization();
        ApplyViewMode();
        if (!_isExpanded) { Width = s.BarWidth; Height = 44; }
        ClampToWorkArea();
    }

    public void RefreshColumns()
    {
        TodoCol.SetHeader(L10n.T("board.todo"), _vm.Todo.Count);
        DoingCol.SetHeader(L10n.T("board.doing"), _vm.Doing.Count);
        DoneCol.SetHeader(L10n.T("board.done"), _vm.Done.Count);
        TodoCol.SetSource(_vm.Todo);
        DoingCol.SetSource(_vm.Doing);
        DoneCol.SetSource(_vm.Done);
        BoardList.ItemsSource = _vm.ListViewTasks;
        FilterList.ItemsSource = _vm.ListViewTasks;
        SelectedDayList.ItemsSource = _vm.SelectedDayTasks;
        if (CalendarPanel.Visibility == Visibility.Visible) Calendar?.Rebuild();
        if (SchedulePanel.Visibility == Visibility.Visible) Schedule?.Rebuild();
    }

    private void ApplyOpacity()
    {
        var s = AppServices.Settings.Current;
        Opacity = _isExpanded ? Math.Max(s.Opacity, 0.97) : s.Opacity;
    }

    private void SetTopmost(bool topmost)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        NativeMethods.SetWindowPos(hwnd, topmost ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST,
            0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        Topmost = topmost;
    }

    private void SetClickThrough(bool through)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        long style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        if (through) style |= NativeMethods.WS_EX_TRANSPARENT;
        else style &= ~(long)NativeMethods.WS_EX_TRANSPARENT;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(style));
    }

    private void SetToolWindowStyle()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        long style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        style |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_LAYERED;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(style));
    }

    private void ApplyLocalization()
    {
        TodayLabel.Text = L10n.T("common.today");
        Title = L10n.T("app.name");
        QuickAddBtn.ToolTip = L10n.T("menu.quickAdd");
        ExpandBtn.ToolTip = L10n.T("board.kanban");
        SearchBox.ToolTip = L10n.T("board.search");
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (AppServices.Settings.Current.ClickThrough) return;
        _collapseTimer?.Stop();
        _collapseTimer = null;
        if (_isExpanded) return;
        _expandTimer?.Stop();
        _expandTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AppServices.Settings.Current.HoverExpandDelayMs) };
        _expandTimer.Tick += (_, _) => { _expandTimer.Stop(); _expandTimer = null; Expand(); };
        _expandTimer.Start();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _expandTimer?.Stop();
        _expandTimer = null;
        if (!_isExpanded) return;
        _collapseTimer?.Stop();
        _collapseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AppServices.Settings.Current.HoverCollapseDelayMs) };
        _collapseTimer.Tick += (_, _) => { _collapseTimer.Stop(); _collapseTimer = null; Collapse(); };
        _collapseTimer.Start();
    }

    public void Expand()
    {
        if (_isExpanded) return;
        _isExpanded = true;
        CollapsedPanel.Visibility = Visibility.Collapsed;
        ExpandedPanel.Visibility = Visibility.Visible;
        Width = Math.Min(940, SystemParameters.WorkArea.Width - 20);
        Height = Math.Min(560, SystemParameters.WorkArea.Height - 20);
        ClampToWorkArea();
        ApplyOpacity();
    }

    public void Collapse()
    {
        if (!_isExpanded) return;
        _isExpanded = false;
        ExpandedPanel.Visibility = Visibility.Collapsed;
        CollapsedPanel.Visibility = Visibility.Visible;
        Width = AppServices.Settings.Current.BarWidth;
        Height = 44;
        ClampToWorkArea();
        ApplyOpacity();
    }

    private void QuickAdd_Click(object sender, RoutedEventArgs e) => QuickAddWindow.ShowQuickAdd();
    private void Expand_Click(object sender, RoutedEventArgs e) => Expand();
    private void Collapse_Click(object sender, RoutedEventArgs e) => Collapse();
    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private void Notes_Click(object sender, RoutedEventArgs e) => NotesWindow.Open();

    private void ViewToggle_Click(object sender, RoutedEventArgs e)
    {
        _vm.ViewMode = _vm.ViewMode switch
        {
            ViewMode.Kanban => ViewMode.List,
            ViewMode.List => ViewMode.Calendar,
            ViewMode.Calendar => ViewMode.Schedule,
            _ => ViewMode.Kanban,
        };
        ApplyViewMode();
    }

    private void OnDaySelected(DateTime day) => _vm.SelectDay(day);

    private void ApplyViewMode()
    {
        bool board = _vm.Filter == BoardFilter.Board;
        var mode = _vm.ViewMode;
        KanbanGrid.Visibility = board && mode == ViewMode.Kanban ? Visibility.Visible : Visibility.Collapsed;
        BoardList.Visibility = board && mode == ViewMode.List ? Visibility.Visible : Visibility.Collapsed;
        CalendarPanel.Visibility = board && mode == ViewMode.Calendar ? Visibility.Visible : Visibility.Collapsed;
        SchedulePanel.Visibility = board && mode == ViewMode.Schedule ? Visibility.Visible : Visibility.Collapsed;
        FilterList.Visibility = board ? Visibility.Collapsed : Visibility.Visible;
        ViewToggleBtn.Visibility = board ? Visibility.Visible : Visibility.Collapsed;
        ViewToggleBtn.Content = mode switch
        {
            ViewMode.Kanban => L10n.T("board.list"),
            ViewMode.List => L10n.T("board.calendar"),
            ViewMode.Calendar => L10n.T("board.schedule"),
            _ => L10n.T("board.kanban"),
        };
        if (mode == ViewMode.Calendar) Calendar?.Rebuild();
        if (mode == ViewMode.Schedule) Schedule?.Rebuild();
    }

    private void PopulateFilterCombo()
    {
        FilterCombo.Items.Clear();
        FilterCombo.Items.Add(new ComboBoxItem { Content = L10n.T("filter.board"), Tag = BoardFilter.Board });
        FilterCombo.Items.Add(new ComboBoxItem { Content = L10n.T("filter.archive"), Tag = BoardFilter.Archive });
        FilterCombo.Items.Add(new ComboBoxItem { Content = L10n.T("filter.trash"), Tag = BoardFilter.Trash });
        FilterCombo.SelectedIndex = 0;
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterCombo.SelectedItem is not ComboBoxItem item) return;
        _vm.Filter = item.Tag is BoardFilter f ? f : BoardFilter.Board;
        ApplyViewMode();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is TaskItem t)
        {
            if (t.IsArchived) _vm.ArchiveTask(t.Id, false);
            else _vm.RestoreTask(t.Id);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is TaskItem t)
        {
            if (t.IsDeleted)
            {
                if (MessageBox.Show(L10n.T("common.deleteForever") + "?", L10n.T("app.name"),
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    _vm.HardDeleteTask(t.Id);
            }
            else
            {
                _vm.DeleteTask(t.Id);
            }
        }
    }

    private void UpdateMonitorText()
    {
        var m = AppServices.Monitor;
        MonitorText.Text = string.Join("   ", new[] { m.CpuText, m.MemoryText, m.DiskText, m.BatteryText }
            .Where(s => !string.IsNullOrEmpty(s)));
    }

    private void Bar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (AppServices.Settings.Current.LockLayout) return;
        if (IsOnInteractive(e.OriginalSource as DependencyObject)) return;
        try { DragMove(); SavePosition(); } catch { }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (AppServices.Settings.Current.LockLayout) return;
        if (IsOnInteractive(e.OriginalSource as DependencyObject)) return;
        try { DragMove(); SavePosition(); } catch { }
    }

    private static bool IsOnInteractive(DependencyObject? d)
    {
        while (d != null)
        {
            if (d is Button or TextBox or ComboBox or CheckBox or Slider or ListBoxItem) return true;
            d = VisualTreeHelper.GetParent(d);
        }
        return false;
    }

    private void PositionInitial()
    {
        var s = AppServices.Settings.Current;
        var wa = SystemParameters.WorkArea;
        if (s.WindowLeft.HasValue && s.WindowTop.HasValue)
        {
            Left = s.WindowLeft.Value;
            Top = s.WindowTop.Value;
        }
        else
        {
            Left = wa.Right - Width - 20;
            Top = wa.Bottom - Height - 20;
        }
        ClampToWorkArea();
    }

    private void ClampToWorkArea()
    {
        var wa = SystemParameters.WorkArea;
        if (Left + Width > wa.Right) Left = wa.Right - Width;
        if (Top + Height > wa.Bottom) Top = wa.Bottom - Height;
        if (Left < wa.Left) Left = wa.Left;
        if (Top < wa.Top) Top = wa.Top;
    }

    private void SavePosition()
    {
        var s = AppServices.Settings.Current;
        s.WindowLeft = Left;
        s.WindowTop = Top;
    }
}
