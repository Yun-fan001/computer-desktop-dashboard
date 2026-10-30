using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KanbanBoard.Models;
using KanbanBoard.Services;

namespace KanbanBoard.Controls;

/// <summary>日程视图：今天 + 未来 7 天的到期任务，按天分组。</summary>
public partial class ScheduleView : UserControl
{
    public ObservableCollection<DayGroup> Days { get; } = new();

    public ScheduleView()
    {
        InitializeComponent();
        DaysList.ItemsSource = Days;
        EmptyText.Text = L10n.T("schedule.empty");
        Rebuild();
    }

    public void Rebuild()
    {
        Days.Clear();
        var today = DateTime.Today;
        var tasks = AppServices.Tasks.GetDueBetween(today, today.AddDays(7));
        foreach (var g in tasks.GroupBy(t => t.DueDate!.Value.Date).OrderBy(x => x.Key))
            Days.Add(new DayGroup { Label = DayLabel(g.Key), Tasks = g.ToList() });

        EmptyText.Visibility = Days.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string DayLabel(DateTime d)
    {
        if (d == DateTime.Today) return L10n.T("common.today") + " · " + d.ToString("M月d日 dddd");
        if (d == DateTime.Today.AddDays(1)) return L10n.T("common.tomorrow") + " · " + d.ToString("M月d日 dddd");
        return d.ToString("M月d日 dddd");
    }
}

/// <summary>某天的日程分组。</summary>
public class DayGroup
{
    public string Label { get; set; } = "";
    public List<TaskItem> Tasks { get; set; } = new();
}
