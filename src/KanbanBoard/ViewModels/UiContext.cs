namespace KanbanBoard.ViewModels;

/// <summary>UI 全局上下文：控件通过它访问主视图模型与窗口操作。</summary>
public static class UiContext
{
    public static MainViewModel? Main { get; set; }

    /// <summary>打开设置（由 MainWindow 注入，设置窗口关闭后自动重新应用）。</summary>
    public static Action? OpenSettings { get; set; }
}
