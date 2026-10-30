using System.Windows.Media;

namespace KanbanBoard.Controls;

/// <summary>便签颜色调色板。</summary>
public static class NoteColors
{
    public static readonly string[] Keys = { "Yellow", "Blue", "Green", "Pink", "Purple", "Gray" };

    private static readonly SolidColorBrush Dark = new(Color.FromRgb(0x33, 0x33, 0x33));

    public static (SolidColorBrush Background, SolidColorBrush Foreground) Get(string key) => key switch
    {
        "Blue" => (C(0xBB, 0xDE, 0xFB), Dark),
        "Green" => (C(0xC8, 0xE6, 0xC9), Dark),
        "Pink" => (C(0xF8, 0xBB, 0xD0), Dark),
        "Purple" => (C(0xE1, 0xBE, 0xE7), Dark),
        "Gray" => (C(0xE0, 0xE0, 0xE0), Dark),
        _ => (C(0xFF, 0xF9, 0xC4), Dark),
    };

    private static SolidColorBrush C(byte r, byte g, byte b) => new(Color.FromRgb(r, g, b));
}
