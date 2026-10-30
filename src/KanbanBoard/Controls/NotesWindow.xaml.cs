using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KanbanBoard.Models;
using KanbanBoard.Services;

namespace KanbanBoard.Controls;

public partial class NotesWindow : Window
{
    public NotesWindow()
    {
        InitializeComponent();
        HeaderText.Text = L10n.T("notes.title");
        Title = L10n.T("notes.title");
        NewBtn.Content = "＋ " + L10n.T("notes.new");
        Activated += (_, _) => Refresh();
        Refresh();
    }

    public static void Open()
    {
        var existing = Application.Current.Windows.OfType<NotesWindow>().FirstOrDefault();
        if (existing != null) { existing.Activate(); return; }
        var w = new NotesWindow();
        w.Show();
        w.Activate();
    }

    public void Refresh() => NoteList.ItemsSource = AppServices.Notes.GetAll();

    private void New_Click(object sender, RoutedEventArgs e)
    {
        NoteEditorWindow.Create();
        Refresh();
        StickyNotes.RefreshAll();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is Note n)
        {
            AppServices.Notes.Delete(n.Id);
            Refresh();
            StickyNotes.RefreshAll();
        }
    }

    private void NoteList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (NoteList.SelectedItem is Note n)
        {
            NoteEditorWindow.Open(n);
            Refresh();
            StickyNotes.RefreshAll();
        }
    }
}
