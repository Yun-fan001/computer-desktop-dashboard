using System;
using System.Collections.Generic;
using KanbanBoard.Models;
using KanbanBoard.Services;
using Xunit;

namespace KanbanBoard.Tests;

public class ImportExportTests
{
    [Fact]
    public void JsonRoundTrip_ResetsIds()
    {
        var tasks = new List<TaskItem>
        {
            new() { Id = 42, Title = "任务", Priority = Priority.High, Tags = new() { "a", "b" } },
        };
        var json = ImportExportService.Export(tasks, ExportFormat.Json);
        var imported = ImportExportService.ImportJson(json);

        Assert.Single(imported);
        Assert.Equal("任务", imported[0].Title);
        Assert.Equal(0, imported[0].Id);
        Assert.Equal(2, imported[0].Tags.Count);
    }

    [Fact]
    public void MarkdownExport_ContainsColumnsAndTitles()
    {
        var tasks = new List<TaskItem> { new() { Title = "写代码", Column = ColumnType.Doing } };
        var md = ImportExportService.Export(tasks, ExportFormat.Markdown);
        Assert.Contains("写代码", md);
    }

    [Fact]
    public void CsvExport_HasHeaderAndRow()
    {
        var tasks = new List<TaskItem> { new() { Title = "买菜", Column = ColumnType.Todo } };
        var csv = ImportExportService.Export(tasks, ExportFormat.Csv);
        Assert.Contains("Title,Description", csv);
        Assert.Contains("买菜", csv);
    }

    [Fact]
    public void IcsExport_HasVevent()
    {
        var tasks = new List<TaskItem> { new() { Title = "开会", DueDate = new DateTime(2026, 1, 1, 9, 0, 0) } };
        var ics = ImportExportService.Export(tasks, ExportFormat.Ics);
        Assert.Contains("BEGIN:VCALENDAR", ics);
        Assert.Contains("BEGIN:VEVENT", ics);
        Assert.Contains("SUMMARY:开会", ics);
    }
}
