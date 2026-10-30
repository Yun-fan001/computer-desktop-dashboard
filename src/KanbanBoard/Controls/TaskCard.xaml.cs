using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KanbanBoard.Models;
using KanbanBoard.Services;
using KanbanBoard.ViewModels;

namespace KanbanBoard.Controls;

public partial class TaskCard : UserControl
{
    private Point _start;
    private bool _mouseDown;

    public TaskCard() => InitializeComponent();

    private TaskItem? Task => DataContext as TaskItem;

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsWithinCheckBox(e.OriginalSource as DependencyObject)) return;
        _start = e.GetPosition(this);
        _mouseDown = true;
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => _mouseDown = false;

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_mouseDown || e.LeftButton != MouseButtonState.Pressed) return;
        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _start.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pos.Y - _start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        if (Task == null) return;
        _mouseDown = false;
        DragDrop.DoDragDrop(this, new DataObject("KanbanTask", Task.Id), DragDropEffects.Move);
    }

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (Task != null) UiContext.Main?.ToggleComplete(Task.Id);
    }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && Task != null) TaskEditorWindow.Open(Task);
    }

    private void OnCardRightClick(object sender, MouseButtonEventArgs e)
    {
        var task = Task;
        if (task == null) return;

        var menu = new ContextMenu();
        var edit = new MenuItem { Header = L10n.T("common.edit") };
        edit.Click += (_, _) => TaskEditorWindow.Open(task);
        var archive = new MenuItem { Header = task.IsArchived ? L10n.T("common.unarchive") : L10n.T("common.archive") };
        archive.Click += (_, _) => UiContext.Main?.ArchiveTask(task.Id, !task.IsArchived);
        var del = new MenuItem { Header = L10n.T("common.delete") };
        del.Click += (_, _) => UiContext.Main?.DeleteTask(task.Id);

        menu.Items.Add(edit);
        menu.Items.Add(new Separator());
        menu.Items.Add(archive);
        menu.Items.Add(del);
        menu.PlacementTarget = this;
        menu.IsOpen = true;
    }

    private static bool IsWithinCheckBox(DependencyObject? d)
    {
        while (d != null)
        {
            if (d is CheckBox) return true;
            d = VisualTreeHelper.GetParent(d);
        }
        return false;
    }
}
