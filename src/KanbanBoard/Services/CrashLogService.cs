using System;
using System.IO;

namespace KanbanBoard.Services;

/// <summary>
/// 崩溃日志：记录未处理异常到本地日志文件，日志本身绝不抛异常。
/// </summary>
public static class CrashLogService
{
    private static readonly object Lock = new();

    public static void Init()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Write("AppDomain.UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Write("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    public static void Write(string context, Exception? ex)
    {
        if (ex is null)
        {
            Write(context, "<null exception>");
            return;
        }
        Write(context, ex.ToString());
    }

    public static void Write(string context, string message)
    {
        try
        {
            AppPaths.EnsureCreated();
            var file = Path.Combine(AppPaths.LogDir, $"crash-{DateTime.Now:yyyyMMdd}.log");
            lock (Lock)
            {
                File.AppendAllText(file,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {context}{Environment.NewLine}" +
                    $"{message}{Environment.NewLine}{new string('-', 60)}{Environment.NewLine}");
            }
        }
        catch
        {
            // 日志失败时保持静默，绝不影响主流程。
        }
    }
}