using System;
using KanbanBoard.Models;
using KanbanBoard.Services;
using Xunit;

namespace KanbanBoard.Tests;

public class NaturalLanguageParserTests
{
    [Fact]
    public void Parse_ChineseWeekdayAndTime_ExtractsAll()
    {
        var p = NaturalLanguageParser.Parse("周五下午3点交报告 #工作 !高");
        Assert.Equal("交报告", p.Title);
        Assert.Contains("工作", p.Tags);
        Assert.Equal(Priority.High, p.Priority);
        Assert.NotNull(p.DueDate);
        Assert.Equal(15, p.DueDate!.Value.Hour);
        Assert.Equal(DayOfWeek.Friday, p.DueDate.Value.DayOfWeek);
    }

    [Fact]
    public void Parse_TodayAndReminder()
    {
        var p = NaturalLanguageParser.Parse("今天下午4点开会 提前15分钟提醒");
        Assert.Equal("开会", p.Title);
        Assert.Equal(DateTime.Now.Date, p.DueDate!.Value.Date);
        Assert.Equal(16, p.DueDate.Value.Hour);
        Assert.Equal(TimeSpan.FromMinutes(15), p.ReminderBefore);
    }

    [Fact]
    public void Parse_RepeatRule()
    {
        var p = NaturalLanguageParser.Parse("每天锻炼");
        Assert.Equal("锻炼", p.Title);
        Assert.Equal(RepeatRules.Daily, p.RepeatRule);
    }

    [Fact]
    public void Parse_EnglishDateAndPriority()
    {
        var p = NaturalLanguageParser.Parse("Submit report tomorrow 3pm #work !high");
        Assert.Equal("Submit report", p.Title);
        Assert.Contains("work", p.Tags);
        Assert.Equal(Priority.High, p.Priority);
        Assert.NotNull(p.DueDate);
        Assert.Equal(15, p.DueDate!.Value.Hour);
    }

    [Fact]
    public void Parse_PlainTitle_NoCrash()
    {
        var p = NaturalLanguageParser.Parse("随便记点什么");
        Assert.Equal("随便记点什么", p.Title);
        Assert.Null(p.DueDate);
        Assert.Equal(Priority.None, p.Priority);
    }
}
