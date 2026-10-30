using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using KanbanBoard.Models;

namespace KanbanBoard.Services;

public enum ExportFormat { Json, Csv, Markdown, Ics }

/// <summary>导入导出：JSON / CSV / Markdown / ICS。</summary>
public static class ImportExportService
{
    public static string Export(List<TaskItem> tasks, ExportFormat format) => format switch
    {
        ExportFormat.Csv => ExportCsv(tasks),
        ExportFormat.Markdown => ExportMarkdown(tasks),
        ExportFormat.Ics => ExportIcs(tasks),
        _ => ExportJson(tasks),
    };

    public static List<TaskItem> ImportJson(string json)
    {
        var list = JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        foreach (var t in list)
        {
            t.Id = 0;
            t.Subtasks.ForEach(s => { s.Id = 0; s.TaskId = 0; });
        }
        return list;
    }

    private static string ExportJson(List<TaskItem> tasks) =>
        JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });

    private static string ExportCsv(List<TaskItem> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Title,Description,Column,Priority,DueDate,Tags,RepeatRule,Completed");
        foreach (var t in tasks)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                CsvEscape(t.Title),
                CsvEscape(t.Description),
                t.Column.ToString(),
                t.Priority.ToString(),
                t.DueDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                CsvEscape(string.Join(" ", t.Tags)),
                t.RepeatRule,
                t.IsCompleted ? "yes" : "no",
            }));
        }
        return sb.ToString();
    }

    private static string ExportMarkdown(List<TaskItem> tasks)
    {
        var sb = new StringBuilder();
        foreach (var col in new[] { ColumnType.Todo, ColumnType.Doing, ColumnType.Done })
        {
            sb.AppendLine($"## {ColumnName(col)}");
            foreach (var t in tasks.Where(x => x.Column == col))
            {
                var check = t.IsCompleted ? "x" : " ";
                var due = t.DueDate != null ? $"  🗓 {t.DueDate:yyyy-MM-dd}" : "";
                sb.AppendLine($"- [{check}] {t.Title}{due}");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string ExportIcs(List<TaskItem> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//KanbanBoard//KanbanBoard//CN");
        foreach (var t in tasks.Where(x => x.DueDate != null))
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{Guid.NewGuid()}@kanbanboard");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTART:{t.DueDate!.Value:yyyyMMddTHHmmss}");
            sb.AppendLine($"SUMMARY:{IcsEscape(t.Title)}");
            if (!string.IsNullOrEmpty(t.Description))
                sb.AppendLine($"DESCRIPTION:{IcsEscape(t.Description)}");
            sb.AppendLine("END:VEVENT");
        }
        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private static string ColumnName(ColumnType c) => c switch
    {
        ColumnType.Doing => L10n.T("board.doing"),
        ColumnType.Done => L10n.T("board.done"),
        _ => L10n.T("board.todo"),
    };

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private static string IcsEscape(string s) =>
        s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
}
