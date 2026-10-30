using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using KanbanBoard.Interop;

namespace KanbanBoard.Services;

/// <summary>窗口背景材质：亚克力（DWM 模糊）/ Mica（Win11）/ 透明 / 无。</summary>
public static class WindowBackdrop
{
    public static void Apply(Window window, string material)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        switch (material)
        {
            case "Acrylic":
                TryAcrylic(hwnd);
                break;
            case "Mica":
                if (!TryMica(hwnd)) TryAcrylic(hwnd);
                break;
            case "Transparent":
                window.Background = Brushes.Transparent;
                break;
            default:
                break;
        }
    }

    private static void TryAcrylic(IntPtr hwnd)
    {
        try
        {
            var bb = new NativeMethods.DWM_BLURBEHIND
            {
                dwFlags = 0x1, // DWM_BB_ENABLE
                fEnable = true,
                hRgnBlur = IntPtr.Zero,
                fTransitionOnMaximized = false,
            };
            NativeMethods.DwmEnableBlurBehindWindow(hwnd, ref bb);
        }
        catch { /* 失败回退到普通背景 */ }
    }

    private static bool TryMica(IntPtr hwnd)
    {
        try
        {
            int backdrop = 2; // DWMSBT_MAINWINDOW (Mica)
            return NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int)) == 0;
        }
        catch
        {
            return false;
        }
    }
}
