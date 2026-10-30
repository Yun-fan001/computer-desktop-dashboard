using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using KanbanBoard.Interop;

namespace KanbanBoard.Services;

/// <summary>
/// 全屏应用检测：前景窗口覆盖整个显示器时判定为全屏（游戏/PPT/远程桌面）。
/// </summary>
public class FullscreenDetector : IDisposable
{
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    private readonly DispatcherTimer _timer;
    public event Action<bool>? FullscreenChanged;
    public bool IsFullscreen { get; private set; }

    public FullscreenDetector()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Check();
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void Check()
    {
        bool fs = IsForegroundFullscreen();
        if (fs != IsFullscreen)
        {
            IsFullscreen = fs;
            FullscreenChanged?.Invoke(fs);
        }
    }

    private static bool IsForegroundFullscreen()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return false;
        if (!NativeMethods.GetWindowRect(hwnd, out var rect)) return false;

        var monitor = NativeMethods.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref mi)) return false;

        return rect.Left <= mi.rcMonitor.Left && rect.Top <= mi.rcMonitor.Top
            && rect.Right >= mi.rcMonitor.Right && rect.Bottom >= mi.rcMonitor.Bottom;
    }

    public void Dispose() => _timer.Stop();
}
