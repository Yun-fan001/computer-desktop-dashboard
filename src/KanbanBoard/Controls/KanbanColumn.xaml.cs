using System.Collections;
using System.Windows;
using System.Windows.Controls;
using KanbanBoard.Models;
using KanbanBoard.ViewModels;

namespace KanbanBoard.Controls;

public partial class KanbanColumn : UserControl
{
    public ColumnType Column { get; set; } = ColumnType.Todo;

    public KanbanColumn()
    {
        InitializeComponent();
        TaskList.DragOver += OnDragOver;
        TaskList.Drop += OnDrop;
    }

    public void SetHeader(string text, int count) => HeaderText.Text = $"{text} · {count}";

    public void SetSource(IEnumerable source) => TaskList.ItemsSource = source;

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("KanbanTask") ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("KanbanTask")) return;
        var id = (long)e.Data.GetData("KanbanTask");
        int index = GetInsertIndex(e.GetPosition(TaskList));
        UiContext.Main?.MoveTask(id, Column, index);
    }

    private int GetInsertIndex(Point pos)
    {
        for (int i = 0; i < TaskList.Items.Count; i++)
        {
            if (TaskList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
                continue;
            var top = container.TransformToAncestor(TaskList).Transform(new Point(0, 0)).Y;
            if (pos.Y < top + container.ActualHeight / 2) return i;
        }
        return TaskList.Items.Count;
    }
}
