using System.Collections.Generic;
using System.Windows;
using KanbanBoard.Services;

namespace KanbanBoard.Controls;

/// <summary>贴桌面便签管理器：根据置顶便签刷新浮窗。</summary>
public static class StickyNotes
{
    private static readonly List<StickyNoteWindow> Windows = new();

    public static void RefreshAll()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var w in Windows) { try { w.Close(); } catch { } }
            Windows.Clear();
            foreach (var note in AppServices.Notes.GetPinned())
                Windows.Add(new StickyNoteWindow(note));
        });
    }
}
