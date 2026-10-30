using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KanbanBoard.Models;
using KanbanBoard.Services;
using KanbanBoard.ViewModels;

namespace KanbanBoard.Controls;

public partial class TaskEditorWindow : Window
{
    private readonly TaskItem _task;
    private readonly bool _isNew;

    private TaskEditorWindow(TaskItem task, bool isNew)
    {
        InitializeComponent();
        _task = task;
        _isNew = isNew;
        Topmost = true;
        HeaderText.Text = L10n.T("editor.title");
        Title = L10n.T("editor.title");
        DeleteBtn.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;
        PopulateCombos();
        LoadTask();
    }

    public static void Open(TaskItem task)
    {
        new TaskEditorWindow(Clone(task), false).ShowDialog();
    }

    public static void Create()
    {
        new TaskEditorWindow(new TaskItem(), true).ShowDialog();
    }

    private void PopulateCombos()
    {
        PriorityCombo.Items.Clear();
        foreach (var p in new[] { Priority.None, Priority.Low, Priority.Medium, Priority.High, Priority.Urgent })
            PriorityCombo.Items.Add(new ComboBoxItem { Content = PriorityText(p), Tag = p });

        RepeatCombo.Items.Clear();
        var repeats = new (string Key, string Val)[]
        {
            (RepeatRules.None, L10n.T("repeat.none")),
            (RepeatRules.Daily, L10n.T("repeat.daily")),
            (RepeatRules.Weekdays, L10n.T("repeat.weekdays")),
            (RepeatRules.Weekly, L10n.T("repeat.weekly")),
            (RepeatRules.Monthly, L10n.T("repeat.monthly")),
            (RepeatRules.Yearly, L10n.T("repeat.yearly")),
        };
        foreach (var (k, v) in repeats) RepeatCombo.Items.Add(new ComboBoxItem { Content = v, Tag = k });
    }

    private void LoadTask()
    {
        TitleBox.Text = _task.Title;
        DescBox.Text = _task.Description;
        PriorityCombo.SelectedIndex = (int)_task.Priority;
        DueDatePicker.SelectedDate = _task.DueDate?.Date;
        DueTimeBox.Text = _task.DueDate is { } d && d.TimeOfDay != TimeSpan.Zero ? d.ToString("HH:mm") : "";
        RepeatCombo.SelectedIndex = RepeatIndex(_task.RepeatRule);
        TagsBox.Text = string.Join(" ", _task.Tags);
        SubtaskList.ItemsSource = _task.Subtasks;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();
        if (string.IsNullOrEmpty(title))
        {
            MessageBox.Show(L10n.T("editor.taskTitle") + " " + L10n.T("common.ok"), L10n.T("app.name"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _task.Title = title;
        _task.Description = DescBox.Text.Trim();
        _task.Priority = PriorityCombo.SelectedItem is ComboBoxItem pi && pi.Tag is Priority p ? p : Priority.None;
        _task.RepeatRule = RepeatCombo.SelectedItem is ComboBoxItem ri && ri.Tag is string r ? r : RepeatRules.None;

        _task.DueDate = null;
        if (DueDatePicker.SelectedDate is DateTime date)
        {
            var d = date.Date;
            if (TimeSpan.TryParse(DueTimeBox.Text.Trim(), out var time)) d = d.Add(time);
            _task.DueDate = d;
        }

        _task.Tags = TagsBox.Text.Split(new[] { ' ', '#', '，', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList();

        // 子任务已通过 TwoWay 绑定更新到 _task.Subtasks
        _task.Subtasks = _task.Subtasks.Where(s => !string.IsNullOrWhiteSpace(s.Title)).ToList();

        if (_isNew) UiContext.Main?.AddTask(_task);
        else UiContext.Main?.UpdateTask(_task);
        Close();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (!_isNew) UiContext.Main?.DeleteTask(_task.Id);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void AddSubtask_Click(object sender, RoutedEventArgs e) => AddSubtask();

    private void SubtaskInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddSubtask();
    }

    private void AddSubtask()
    {
        var text = SubtaskInput.Text.Trim();
        if (text.Length == 0) return;
        _task.Subtasks.Add(new Subtask { Title = text, IsDone = false, SortOrder = _task.Subtasks.Count });
        SubtaskInput.Clear();
        SubtaskList.ItemsSource = null;
        SubtaskList.ItemsSource = _task.Subtasks;
    }

    private static int RepeatIndex(string rule) => rule switch
    {
        RepeatRules.Daily => 1,
        RepeatRules.Weekdays => 2,
        RepeatRules.Weekly => 3,
        RepeatRules.Monthly => 4,
        RepeatRules.Yearly => 5,
        _ => 0,
    };

    private static string PriorityText(Priority p) => p switch
    {
        Priority.Low => L10n.T("priority.low"),
        Priority.Medium => L10n.T("priority.medium"),
        Priority.High => L10n.T("priority.high"),
        Priority.Urgent => L10n.T("priority.urgent"),
        _ => L10n.T("priority.none"),
    };

    private static TaskItem Clone(TaskItem t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Column = t.Column,
        Priority = t.Priority,
        Tags = new List<string>(t.Tags),
        DueDate = t.DueDate,
        ReminderAt = t.ReminderAt,
        RepeatRule = t.RepeatRule,
        SortOrder = t.SortOrder,
        IsArchived = t.IsArchived,
        IsDeleted = t.IsDeleted,
        DeletedAt = t.DeletedAt,
        CompletedAt = t.CompletedAt,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        Subtasks = t.Subtasks.Select(s => new Subtask
        {
            Id = s.Id, TaskId = s.TaskId, Title = s.Title, IsDone = s.IsDone, SortOrder = s.SortOrder,
        }).ToList(),
    };
}
