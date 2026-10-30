using System.Linq;
using System.Windows;
using System.Windows.Input;
using KanbanBoard.Models;
using KanbanBoard.Services;
using KanbanBoard.ViewModels;

namespace KanbanBoard.Controls;

public partial class QuickAddWindow : Window
{
    public QuickAddWindow()
    {
        InitializeComponent();
        HintText.Text = L10n.T("quickadd.placeholder");
        Input.TextChanged += (_, _) => UpdatePreview();
        Loaded += (_, _) => { Input.Focus(); };
    }

    public static void ShowQuickAdd()
    {
        var w = new QuickAddWindow();
        var wa = SystemParameters.WorkArea;
        w.Left = wa.Left + (wa.Width - w.Width) / 2;
        w.Top = wa.Top + wa.Height / 4;
        w.Show();
        w.Activate();
    }

    private void UpdatePreview()
    {
        var p = NaturalLanguageParser.Parse(Input.Text);
        if (string.IsNullOrWhiteSpace(Input.Text)) { PreviewText.Text = ""; return; }
        var parts = new System.Collections.Generic.List<string>();
        if (p.DueDate != null) parts.Add($"📅 {p.DueDate:yyyy-MM-dd HH:mm}");
        if (p.Priority != Priority.None) parts.Add(L10n.T("editor.priority") + ":" + PriorityText(p.Priority));
        if (p.Tags.Count > 0) parts.Add(string.Join(" ", p.Tags.Select(t => "#" + t)));
        if (!string.IsNullOrEmpty(p.RepeatRule)) parts.Add(L10n.T("editor.repeat") + ":" + p.RepeatRule);
        if (p.ReminderBefore != null) parts.Add($"{L10n.T("editor.dueDate")}-{(int)p.ReminderBefore.Value.TotalMinutes}m");
        PreviewText.Text = string.Join("  ·  ", parts);
    }

    private static string PriorityText(Priority p) => p switch
    {
        Priority.Low => L10n.T("priority.low"),
        Priority.Medium => L10n.T("priority.medium"),
        Priority.High => L10n.T("priority.high"),
        Priority.Urgent => L10n.T("priority.urgent"),
        _ => L10n.T("priority.none"),
    };

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var text = Input.Text.Trim();
            if (text.Length > 0) UiContext.Main?.AddFromText(text);
            Close();
        }
        else if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
