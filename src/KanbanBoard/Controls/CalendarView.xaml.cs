using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KanbanBoard.Services;

namespace KanbanBoard.Controls;

/// <summary>
/// 月历视图：显示当前月各天的到期任务数，点击某天触发 DaySelected。
/// </summary>
public partial class CalendarView : UserControl
{
    public DateTime CurrentMonth { get; private set; }

    public event Action<DateTime>? DaySelected;

    public CalendarView()
    {
        InitializeComponent();
        CurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        BuildWeekdayHeader();
        Rebuild();
    }

    public void Rebuild()
    {
        MonthText.Text = CurrentMonth.ToString("yyyy年M月");

        var tasks = AppServices.Tasks.GetDueBetween(CurrentMonth, CurrentMonth.AddMonths(1));
        var byDay = tasks
            .Where(t => t.DueDate != null)
            .GroupBy(t => t.DueDate!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        DaysGrid.Children.Clear();
        DaysGrid.RowDefinitions.Clear();
        DaysGrid.ColumnDefinitions.Clear();
        for (int i = 0; i < 7; i++) DaysGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < 6; i++) DaysGrid.RowDefinitions.Add(new RowDefinition());

        int startOffset = ((int)CurrentMonth.DayOfWeek + 6) % 7; // 周一 = 0
        int daysInMonth = DateTime.DaysInMonth(CurrentMonth.Year, CurrentMonth.Month);
        var today = DateTime.Today;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(CurrentMonth.Year, CurrentMonth.Month, day);
            int pos = startOffset + day - 1;
            var cell = BuildDayCell(date, byDay.TryGetValue(date, out var c) ? c : 0, date == today);
            Grid.SetRow(cell, pos / 7);
            Grid.SetColumn(cell, pos % 7);
            DaysGrid.Children.Add(cell);
        }
    }

    private void Prev_Click(object sender, RoutedEventArgs e) { CurrentMonth = CurrentMonth.AddMonths(-1); Rebuild(); }
    private void Next_Click(object sender, RoutedEventArgs e) { CurrentMonth = CurrentMonth.AddMonths(1); Rebuild(); }

    private void BuildWeekdayHeader()
    {
        var names = new[] { "一", "二", "三", "四", "五", "六", "日" };
        WeekdayHeader.ColumnDefinitions.Clear();
        WeekdayHeader.Children.Clear();
        for (int i = 0; i < 7; i++)
        {
            WeekdayHeader.ColumnDefinitions.Add(new ColumnDefinition());
            var tb = new TextBlock
            {
                Text = names[i],
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 11,
                Foreground = SecondaryBrush(),
            };
            Grid.SetColumn(tb, i);
            WeekdayHeader.Children.Add(tb);
        }
    }

    private Border BuildDayCell(DateTime date, int count, bool isToday)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(2, 4, 2, 4),
            Margin = new Thickness(1),
            Background = isToday ? HoverBrush() : Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = date.ToString("yyyy-MM-dd"),
        };

        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(new TextBlock
        {
            Text = date.Day.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 13,
            FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
            Foreground = PrimaryBrush(),
        });

        if (count > 0)
        {
            var badge = new Border
            {
                Background = AccentBrush(),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(5, 0, 5, 0),
                Margin = new Thickness(0, 2, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            badge.Child = new TextBlock
            {
                Text = count.ToString(),
                FontSize = 10,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            panel.Children.Add(badge);
        }

        border.Child = panel;
        border.MouseLeftButtonDown += (_, _) => DaySelected?.Invoke(date);
        return border;
    }

    private static Brush PrimaryBrush() =>
        (Brush)(Application.Current.TryFindResource("Brush.ForegroundPrimary") ?? Brushes.White);

    private static Brush SecondaryBrush() =>
        (Brush)(Application.Current.TryFindResource("Brush.ForegroundSecondary") ?? Brushes.Gray);

    private static Brush AccentBrush() =>
        (Brush)(Application.Current.TryFindResource("Brush.Accent") ?? Brushes.DodgerBlue);

    private static Brush HoverBrush() =>
        (Brush)(Application.Current.TryFindResource("Brush.HoverBackground")
            ?? new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)));
}
