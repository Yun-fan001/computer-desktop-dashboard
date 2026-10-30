using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using KanbanBoard.Models;
using KanbanBoard.Services;

namespace KanbanBoard.Converters;

public class PriorityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => (Priority)value switch
    {
        Priority.Urgent => new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D)),
        Priority.High => new SolidColorBrush(Color.FromRgb(0xE8, 0x7C, 0x3A)),
        Priority.Medium => new SolidColorBrush(Color.FromRgb(0x2E, 0x9E, 0x5B)),
        Priority.Low => new SolidColorBrush(Color.FromRgb(0x4C, 0x8D, 0xFF)),
        _ => Brushes.Gray,
    };

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

public class PriorityToTextConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => (Priority)value switch
    {
        Priority.Low => L10n.T("priority.low"),
        Priority.Medium => L10n.T("priority.medium"),
        Priority.High => L10n.T("priority.high"),
        Priority.Urgent => L10n.T("priority.urgent"),
        _ => "",
    };

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

public class DueDateToTextConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        if (value is not DateTime due) return "";
        var now = DateTime.Now;
        string datePart;
        if (due.Date == now.Date) datePart = L10n.T("common.today");
        else if (due.Date == now.Date.AddDays(1)) datePart = L10n.T("common.tomorrow");
        else datePart = due.ToString("M月d日");

        return due.TimeOfDay == TimeSpan.Zero ? datePart : $"{datePart} {due:HH:mm}";
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

public class DueDateToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        if (value is not DateTime due) return Brushes.Gray;
        return due < DateTime.Now
            ? new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D))
            : Brushes.Gray;
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is not true;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => value is not true;
}

public class TagsToTextConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        if (value is not System.Collections.Generic.List<string> tags || tags.Count == 0) return "";
        return string.Join("  ", tags.ConvertAll(x => "#" + x));
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

public class NoteColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        KanbanBoard.Controls.NoteColors.Get(value as string ?? "Yellow").Background;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}
