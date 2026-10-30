using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KanbanBoard.Models;
using KanbanBoard.Services;

namespace KanbanBoard.Controls;

public partial class NoteEditorWindow : Window
{
    private readonly Note _note;
    private readonly bool _isNew;
    private string _selectedColor = "Yellow";
    private readonly List<Border> _swatches = new();

    private NoteEditorWindow(Note note, bool isNew)
    {
        InitializeComponent();
        _note = note;
        _isNew = isNew;
        Topmost = true;
        HeaderText.Text = L10n.T("notes.title");
        Title = L10n.T("notes.title");
        DeleteBtn.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;
        BuildColorPanel();
        Load();
    }

    public static void Open(Note note) => new NoteEditorWindow(Clone(note), false).ShowDialog();

    public static void Create() => new NoteEditorWindow(new Note(), true).ShowDialog();

    private void Load()
    {
        TitleBox.Text = _note.Title;
        ContentBox.Text = _note.Content;
        PinCheck.IsChecked = _note.IsPinned;
        SelectColor(_note.Color);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _note.Title = TitleBox.Text.Trim();
        _note.Content = ContentBox.Text;
        _note.Color = _selectedColor;
        _note.IsPinned = PinCheck.IsChecked == true;

        if (_isNew) AppServices.Notes.Add(_note);
        else AppServices.Notes.Update(_note);

        Close();
        StickyNotes.RefreshAll();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (!_isNew) AppServices.Notes.Delete(_note.Id);
        Close();
        StickyNotes.RefreshAll();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void BuildColorPanel()
    {
        foreach (var key in NoteColors.Keys)
        {
            var (bg, _) = NoteColors.Get(key);
            var b = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(6),
                Background = bg,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0),
                Tag = key,
            };
            b.MouseLeftButtonDown += (_, _) => SelectColor(key);
            ColorPanel.Children.Add(b);
            _swatches.Add(b);
        }
    }

    private void SelectColor(string key)
    {
        _selectedColor = key;
        foreach (var s in _swatches)
            s.BorderThickness = s.Tag?.ToString() == key ? new Thickness(2) : new Thickness(0);
    }

    private static Note Clone(Note n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Content = n.Content,
        Color = n.Color,
        IsPinned = n.IsPinned,
        IsLocked = n.IsLocked,
        CreatedAt = n.CreatedAt,
        UpdatedAt = n.UpdatedAt,
    };
}
