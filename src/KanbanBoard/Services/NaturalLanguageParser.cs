using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KanbanBoard.Models;

namespace KanbanBoard.Services;

/// <summary>自然语言解析结果。</summary>
public class ParsedTask
{
    public string Title = "";
    public DateTime? DueDate;
    public bool HasTime;
    public string RepeatRule = RepeatRules.None;
    public Priority Priority = Priority.None;
    public List<string> Tags = new();
    public TimeSpan? ReminderBefore;
}

/// <summary>
/// 自然语言解析：从输入中识别日期、时间、重复、标签、优先级、提醒。
/// 示例："周五下午3点交报告 #工作 !高 提前15分钟提醒"
/// </summary>
public static class NaturalLanguageParser
{
    public static ParsedTask Parse(string input)
    {
        var r = new ParsedTask();
        if (string.IsNullOrWhiteSpace(input)) return r;
        var text = input.Trim();

        text = ExtractTags(text, r);
        text = ExtractPriority(text, r);
        text = ExtractRepeat(text, r);
        text = ExtractReminder(text, r);
        text = ExtractDateTime(text, r);

        r.Title = Clean(text);
        if (string.IsNullOrWhiteSpace(r.Title)) r.Title = input.Trim();
        return r;
    }

    private static string ExtractTags(string text, ParsedTask r)
    {
        return Regex.Replace(text, @"#([\p{L}\p{N}_-]+)", m =>
        {
            var tag = m.Groups[1].Value;
            if (!r.Tags.Contains(tag)) r.Tags.Add(tag);
            return " ";
        });
    }

    private static string ExtractPriority(string text, ParsedTask r)
    {
        var m = Regex.Match(text, @"!!|![紧急高中低]|!(?:urgent|high|medium|low)", RegexOptions.IgnoreCase);
        if (!m.Success) return text;
        r.Priority = m.Value.ToLowerInvariant() switch
        {
            "!!" or "!紧急" or "!urgent" => Priority.Urgent,
            "!高" or "!high" => Priority.High,
            "!中" or "!medium" => Priority.Medium,
            "!低" or "!low" => Priority.Low,
            _ => Priority.None,
        };
        return text.Remove(m.Index, m.Length);
    }

    private static string ExtractRepeat(string text, ParsedTask r)
    {
        var m = Regex.Match(text,
            @"每工作日|工作日|每天|每日|每周|每月|每年|every\s+weekday|every\s+day|every\s+week|every\s+month|every\s+year|weekly|daily|monthly|yearly",
            RegexOptions.IgnoreCase);
        if (!m.Success) return text;
        r.RepeatRule = m.Value.ToLowerInvariant() switch
        {
            "每天" or "每日" or "every day" or "daily" => RepeatRules.Daily,
            "每工作日" or "工作日" or "every weekday" => RepeatRules.Weekdays,
            "每周" or "every week" or "weekly" => RepeatRules.Weekly,
            "每月" or "every month" or "monthly" => RepeatRules.Monthly,
            "每年" or "every year" or "yearly" => RepeatRules.Yearly,
            _ => RepeatRules.None,
        };
        return text.Remove(m.Index, m.Length);
    }

    private static string ExtractReminder(string text, ParsedTask r)
    {
        var patterns = new (string Pattern, RegexOptions Opts)[]
        {
            (@"提前\s*(\d+)\s*(分钟|小时|天|分|时)\s*提醒", RegexOptions.None),
            (@"(\d+)\s*(分钟|小时|天|分|时)\s*前\s*提醒", RegexOptions.None),
            (@"提醒\s*(\d+)\s*(分钟|小时|天|分|时)\s*前", RegexOptions.None),
            (@"remind\s+(\d+)\s*(min|hour|day)s?\s+before", RegexOptions.IgnoreCase),
        };
        foreach (var (p, o) in patterns)
        {
            var m = Regex.Match(text, p, o);
            if (!m.Success) continue;
            int n = int.Parse(m.Groups[1].Value);
            var unit = m.Groups[2].Value;
            r.ReminderBefore = unit switch
            {
                "分钟" or "分" or "min" => TimeSpan.FromMinutes(n),
                "小时" or "时" or "hour" => TimeSpan.FromHours(n),
                "天" or "day" => TimeSpan.FromDays(n),
                _ => TimeSpan.FromMinutes(n),
            };
            return text.Remove(m.Index, m.Length);
        }
        return text;
    }

    private static Match? LastTimeMatch;

    private static string ExtractDateTime(string text, ParsedTask r)
    {
        var now = DateTime.Now;

        // 1. 相对日期
        var rel = Regex.Match(text, @"今天|明天|后天|大后天|today|tomorrow");
        if (rel.Success)
        {
            int days = rel.Value switch
            {
                "明天" or "tomorrow" => 1,
                "后天" => 2,
                "大后天" => 3,
                _ => 0,
            };
            var d = now.Date.AddDays(days);
            ApplyTime(text, d, r);
            text = RemoveMatches(text, rel, LastTimeMatch);
            return text;
        }

        // 2. 星期
        var wd = Regex.Match(text, @"(?:周|星期|礼拜)([一二三四五六日天])");
        if (wd.Success)
        {
            var d = NextWeekday(now.Date, ToDayOfWeek(wd.Groups[1].Value));
            ApplyTime(text, d, r);
            text = RemoveMatches(text, wd, LastTimeMatch);
            return text;
        }

        // 3. 月日
        var md = Regex.Match(text, @"(\d{1,2})月(\d{1,2})[日号]?");
        if (md.Success && TryMakeDate(now.Year, md.Groups[1].Value, md.Groups[2].Value, out var date1))
        {
            if (date1 < now.Date && TryMakeDate(now.Year + 1, md.Groups[1].Value, md.Groups[2].Value, out var date2))
                date1 = date2;
            ApplyTime(text, date1, r);
            text = RemoveMatches(text, md, LastTimeMatch);
            return text;
        }

        // 4. 仅时间
        if (TryMatchTime(text, out var time, out var timeMatch))
        {
            var d = now.Date;
            if (time < now.TimeOfDay) d = d.AddDays(1);
            r.DueDate = d.Add(time);
            r.HasTime = true;
            text = RemoveMatches(text, timeMatch);
        }

        return text;
    }

    private static void ApplyTime(string text, DateTime date, ParsedTask r)
    {
        LastTimeMatch = null;
        if (TryMatchTime(text, out var time, out var m))
        {
            r.DueDate = date.Add(time);
            r.HasTime = true;
            LastTimeMatch = m;
        }
        else
        {
            r.DueDate = date;
            r.HasTime = false;
        }
    }

    private static bool TryMatchTime(string text, out TimeSpan time, out Match match)
    {
        time = default;
        match = null!;

        var m1 = Regex.Match(text, @"(?<p>凌晨|早上|上午|中午|下午|傍晚|晚上)\s*(?<h>\d{1,2})(?:[点时:：](?<min>\d{1,2})?分?)?(?<half>半)?");
        if (m1.Success)
        {
            int h = int.Parse(m1.Groups["h"].Value);
            int min = m1.Groups["min"].Success ? int.Parse(m1.Groups["min"].Value) : (m1.Groups["half"].Success ? 30 : 0);
            time = new TimeSpan(Math.Min(AdjustHour(h, m1.Groups["p"].Value), 23), Math.Min(min, 59), 0);
            match = m1;
            return true;
        }

        var m2 = Regex.Match(text, @"(?<h>\d{1,2})[点时:：](?<min>\d{1,2})?分?(?<half>半)?");
        if (m2.Success)
        {
            int h = int.Parse(m2.Groups["h"].Value);
            int min = m2.Groups["min"].Success ? int.Parse(m2.Groups["min"].Value) : (m2.Groups["half"].Success ? 30 : 0);
            time = new TimeSpan(Math.Min(h, 23), Math.Min(min, 59), 0);
            match = m2;
            return true;
        }

        var m3 = Regex.Match(text, @"(?<h>\d{1,2})(?::(?<min>\d{2}))?\s*(?<ap>am|pm)", RegexOptions.IgnoreCase);
        if (m3.Success)
        {
            int h = int.Parse(m3.Groups["h"].Value);
            int min = m3.Groups["min"].Success ? int.Parse(m3.Groups["min"].Value) : 0;
            bool pm = m3.Groups["ap"].Value.Equals("pm", StringComparison.OrdinalIgnoreCase);
            if (pm && h < 12) h += 12;
            if (!pm && h == 12) h = 0;
            time = new TimeSpan(h, min, 0);
            match = m3;
            return true;
        }

        return false;
    }

    private static int AdjustHour(int h, string? period) => period switch
    {
        "下午" or "晚上" or "傍晚" => h < 12 ? h + 12 : h,
        "凌晨" or "早上" or "上午" => h == 12 ? 0 : h,
        "中午" => h < 12 ? h + 12 : h,
        _ => h,
    };

    private static DayOfWeek ToDayOfWeek(string c) => c switch
    {
        "一" => DayOfWeek.Monday,
        "二" => DayOfWeek.Tuesday,
        "三" => DayOfWeek.Wednesday,
        "四" => DayOfWeek.Thursday,
        "五" => DayOfWeek.Friday,
        "六" => DayOfWeek.Saturday,
        _ => DayOfWeek.Sunday,
    };

    private static DateTime NextWeekday(DateTime from, DayOfWeek target)
    {
        int diff = ((int)target - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(diff);
    }

    private static bool TryMakeDate(int year, string month, string day, out DateTime date)
    {
        date = default;
        if (!int.TryParse(month, out int mo) || !int.TryParse(day, out int d)) return false;
        if (mo < 1 || mo > 12 || d < 1 || d > DateTime.DaysInMonth(year, mo)) return false;
        date = new DateTime(year, mo, d);
        return true;
    }

    private static string RemoveMatches(string text, params Match?[] matches)
    {
        foreach (var m in matches.Where(x => x != null && x.Success).OrderByDescending(x => x!.Index))
            text = text.Remove(m!.Index, m.Length);
        return text;
    }

    private static string Clean(string s) =>
        Regex.Replace(s, @"\s+", " ").Trim(' ', ',', '，', '。', '！', '!', '；', ';', '、');
}

