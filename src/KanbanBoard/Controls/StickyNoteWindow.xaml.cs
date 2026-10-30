using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KanbanBoard.Models;
using KanbanBoard.Services;

namespace KanbanBoard.Controls;

/// <summary>贴桌面的便签小窗（置顶便签）。</summary>
public partial class StickyNoteWindow : Window
{
    private readonly Note _note;
    private Point _start;
    private bool _down;

    public StickyNoteWindow(Note note)
    {
        InitializeComponent();
        _note = note;
        TitleText.Text = note.Title;
        ContentText.Text = note.Content;
        var (bg, fg) = NoteColors.Get(note.Color);
        Root.Background = bg;
        TitleText.Foreground = fg;
        ContentText.Foreground = fg;
        PositionRandom();
        Show();
    }

    private void PositionRandom()
    {
        var wa = SystemParameters.WorkArea;
        var rnd = new Random();
        Left = wa.Right - 230 - rnd.Next(0, 3) * 30;
        Top = wa.Top + 40 + rnd.Next(0, 6) * 40;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        AppServices.Notes.SetPinned(_note.Id, false);
        Close();
    }

    private void OnPreviewDown(object sender, MouseButtonEventArgs e)
    {
        if (IsWithinButton(e.OriginalSource as DependencyObject)) return;
        _start = e.GetPosition(this);
        _down = true;
    }

    private void OnPreviewUp(object sender, MouseButtonEventArgs e) => _down = false;

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_down || e.LeftButton != MouseButtonState.Pressed) return;
        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _start.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pos.Y - _start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _down = false;
        try { DragMove(); } catch { }
    }

    private void OnClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && !IsWithinButton(e.OriginalSource as DependencyObject))
            NoteEditorWindow.Open(_note);
    }

    private static bool IsWithinButton(DependencyObject? d)
    {
        while (d != null)
        {
            if (d is Button) return true;
            d = VisualTreeHelper.GetParent(d);
        }
        return false;
    }
}
