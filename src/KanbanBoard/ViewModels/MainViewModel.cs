using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using KanbanBoard.Models;
using KanbanBoard.Services;

namespace KanbanBoard.ViewModels;

/// <summary>看板筛选模式。</summary>
public enum BoardFilter { Board, Archive, Trash }

/// <summary>视图模式。</summary>
public enum ViewMode { Kanban, List, Calendar, Schedule }

/// <summary>主视图模型：看板列、搜索、时钟、今日待办数、任务操作。</summary>
public class MainViewModel : ObservableObject
{
    public ObservableCollection<TaskItem> Todo { get; } = new();
    public ObservableCollection<TaskItem> Doing { get; } = new();
    public ObservableCollection<TaskItem> Done { get; } = new();
    public ObservableCollection<TaskItem> ListViewTasks { get; } = new();

    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };

    /// <summary>数据刷新后触发，用于 UI 更新列标题等。</summary>
    public event Action? Refreshed;

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { if (Set(ref _searchText, value)) Refresh(); }
    }

    private ViewMode _viewMode = ViewMode.Kanban;
    public ViewMode ViewMode
    {
        get => _viewMode;
        set { if (Set(ref _viewMode, value)) OnPropertyChanged(); }
    }

    public ObservableCollection<TaskItem> SelectedDayTasks { get; } = new();
    private DateTime? _selectedDate;
    public string SelectedDateText => _selectedDate?.ToString("M月d日 dddd") ?? "";

    private BoardFilter _filter = BoardFilter.Board;
    public BoardFilter Filter
    {
        get => _filter;
        set { if (Set(ref _filter, value)) Refresh(); }
    }

    private string _timeText = "";
    public string TimeText { get => _timeText; private set => Set(ref _timeText, value); }

    private string _dateText = "";
    public string DateText { get => _dateText; private set => Set(ref _dateText, value); }

    private int _todayCount;
    public int TodayCount { get => _todayCount; private set => Set(ref _todayCount, value); }

    public MainViewModel()
    {
        _clock.Tick += (_, _) => Tick();
    }

    public void Start()
    {
        Refresh();
        Tick();
        _clock.Start();
    }

    private void Tick()
    {
        TimeText = DateTime.Now.ToString("HH:mm");
        DateText = DateTime.Now.ToString("M月d日 dddd");
        if (DateTime.Now.Second == 0) UpdateTodayCount();
    }

    public void Refresh()
    {
        Todo.Clear(); Doing.Clear(); Done.Clear(); ListViewTasks.Clear();

        if (_filter == BoardFilter.Board)
        {
            var query = _searchText?.Trim();
            var all = string.IsNullOrEmpty(query)
                ? AppServices.Tasks.GetActiveTasks()
                : AppServices.Tasks.Search(query);

            foreach (var t in all)
            {
                switch (t.Column)
                {
                    case ColumnType.Doing: Doing.Add(t); break;
                    case ColumnType.Done: Done.Add(t); break;
                    default: Todo.Add(t); break;
                }
                ListViewTasks.Add(t);
            }
        }
        else if (_filter == BoardFilter.Archive)
        {
            foreach (var t in AppServices.Tasks.GetArchived()) ListViewTasks.Add(t);
        }
        else
        {
            foreach (var t in AppServices.Tasks.GetDeleted()) ListViewTasks.Add(t);
        }

        OnPropertyChanged(nameof(ListViewTasks));
        UpdateTodayCount();
        ReloadSelectedDay();
        Refreshed?.Invoke();
    }

    private void UpdateTodayCount() => TodayCount = AppServices.Tasks.GetTodayCount(DateTime.Now);

    // ==================== 任务操作 ====================

    public long AddFromText(string text)
    {
        var p = NaturalLanguageParser.Parse(text);
        if (string.IsNullOrWhiteSpace(p.Title)) return -1;

        var t = new TaskItem
        {
            Title = p.Title,
            Priority = p.Priority,
            Tags = p.Tags,
            RepeatRule = p.RepeatRule,
            DueDate = p.DueDate,
            SortOrder = AppServices.Tasks.GetActiveByColumn(ColumnType.Todo).Count,
        };

        if (p.DueDate != null)
        {
            var baseTime = p.HasTime ? p.DueDate.Value : p.DueDate.Value.Date.AddHours(9);
            t.ReminderAt = p.ReminderBefore.HasValue ? baseTime.Subtract(p.ReminderBefore.Value) : baseTime;
        }

        var id = AppServices.Tasks.Add(t);
        Refresh();
        return id;
    }

    public void AddTask(TaskItem t)
    {
        AppServices.Tasks.Add(t);
        Refresh();
    }

    public void UpdateTask(TaskItem t)
    {
        AppServices.Tasks.Update(t);
        Refresh();
    }

    public void DeleteTask(long id)
    {
        AppServices.Tasks.SoftDelete(id);
        Refresh();
    }

    public void RestoreTask(long id)
    {
        AppServices.Tasks.Restore(id);
        Refresh();
    }

    public void HardDeleteTask(long id)
    {
        AppServices.Tasks.HardDelete(id);
        Refresh();
    }

    public void SelectDay(DateTime day)
    {
        _selectedDate = day.Date;
        ReloadSelectedDay();
        OnPropertyChanged(nameof(SelectedDateText));
    }

    private void ReloadSelectedDay()
    {
        SelectedDayTasks.Clear();
        if (_selectedDate == null) return;
        foreach (var t in AppServices.Tasks.GetDueBetween(_selectedDate.Value, _selectedDate.Value.AddDays(1)))
            SelectedDayTasks.Add(t);
    }

    public void ArchiveTask(long id, bool archive)
    {
        AppServices.Tasks.SetArchived(id, archive);
        Refresh();
    }

    public void ToggleComplete(long id)
    {
        var t = Find(id);
        if (t == null) return;
        AppServices.Tasks.SetCompleted(id, t.Column != ColumnType.Done);
        Refresh();
    }

    public void MoveTask(long id, ColumnType target, int targetIndex)
    {
        var task = Find(id);
        if (task == null) return;
        var source = task.Column;

        RemoveFromAll(task);
        task.Column = target;
        var targetCol = GetCollection(target);
        targetIndex = Math.Clamp(targetIndex, 0, targetCol.Count);
        targetCol.Insert(targetIndex, task);

        PersistOrder(source);
        PersistOrder(target);
        UpdateTodayCount();
        Refreshed?.Invoke();
    }

    // ==================== 内部 ====================

    private TaskItem? Find(long id) =>
        Todo.FirstOrDefault(t => t.Id == id)
        ?? Doing.FirstOrDefault(t => t.Id == id)
        ?? Done.FirstOrDefault(t => t.Id == id);

    private void RemoveFromAll(TaskItem t)
    {
        Todo.Remove(t);
        Doing.Remove(t);
        Done.Remove(t);
        ListViewTasks.Remove(t);
    }

    private ObservableCollection<TaskItem> GetCollection(ColumnType c) => c switch
    {
        ColumnType.Doing => Doing,
        ColumnType.Done => Done,
        _ => Todo,
    };

    private void PersistOrder(ColumnType c)
    {
        var ids = GetCollection(c).Select(t => t.Id).ToList();
        AppServices.Tasks.Reorder(c, ids);
    }
}
