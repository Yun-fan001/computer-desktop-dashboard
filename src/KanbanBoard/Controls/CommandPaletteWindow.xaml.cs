using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KanbanBoard.Services;
using KanbanBoard.ViewModels;

namespace KanbanBoard.Controls;

public partial class CommandPaletteWindow : Window
{
    private sealed class PaletteItem
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public Action? Action { get; set; }
    }

    private readonly List<PaletteItem> _items = new();

    public CommandPaletteWindow()
    {
        InitializeComponent();
        Input.TextChanged += (_, _) => Rebuild();
        Loaded += (_, _) => { Rebuild(); Input.Focus(); };
    }

    public static void Open()
    {
        var w = new CommandPaletteWindow();
        var wa = SystemParameters.WorkArea;
        w.Left = wa.Left + (wa.Width - w.Width) / 2;
        w.Top = wa.Top + wa.Height / 6;
        w.Show();
        w.Activate();
    }

    private void Rebuild()
    {
        _items.Clear();
        var q = Input.Text.Trim();

        foreach (var cmd in BuildCommands())
            if (q.Length == 0 || cmd.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
                _items.Add(cmd);

        if (q.Length > 0)
        {
            foreach (var t in AppServices.Tasks.Search(q).Take(8))
                _items.Add(new PaletteItem { Title = t.Title, Subtitle = L10n.T("cmd.task"), Action = () => TaskEditorWindow.Open(t) });
        }

        ResultList.ItemsSource = null;
        ResultList.ItemsSource = _items;
        if (ResultList.Items.Count > 0) ResultList.SelectedIndex = 0;
    }

    private static List<PaletteItem> BuildCommands() => new()
    {
        new PaletteItem { Title = L10n.T("menu.quickAdd"), Subtitle = AppServices.Settings.Current.QuickAddHotkey, Action = QuickAddWindow.ShowQuickAdd },
        new PaletteItem { Title = L10n.T("menu.settings"), Action = () => UiContext.OpenSettings?.Invoke() },
        new PaletteItem { Title = L10n.T("cmd.viewToggle"), Action = () => { if (UiContext.Main != null) UiContext.Main.ViewMode = (ViewMode)(((int)UiContext.Main.ViewMode + 1) % 4); } },
        new PaletteItem { Title = L10n.T("menu.focusMode"), Action = ToggleFocus },
        new PaletteItem { Title = L10n.T("menu.exit"), Action = () => Application.Current.Shutdown() },
    };

    private static void ToggleFocus()
    {
        AppServices.Settings.Current.FocusMode = !AppServices.Settings.Current.FocusMode;
        AppServices.Settings.Save();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { ExecuteSelected(); }
        else if (e.Key == Key.Escape) { Close(); }
        else if (e.Key == Key.Down) { Move(1); e.Handled = true; }
        else if (e.Key == Key.Up) { Move(-1); e.Handled = true; }
    }

    private void Move(int delta)
    {
        if (ResultList.Items.Count == 0) return;
        var idx = ResultList.SelectedIndex + delta;
        if (idx < 0) idx = ResultList.Items.Count - 1;
        if (idx >= ResultList.Items.Count) idx = 0;
        ResultList.SelectedIndex = idx;
        ResultList.ScrollIntoView(ResultList.SelectedItem);
    }

    private void ExecuteSelected()
    {
        if (ResultList.SelectedItem is PaletteItem item)
        {
            var action = item.Action;
            Close();
            action?.Invoke();
        }
    }

    private void ResultList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => ExecuteSelected();
}
