using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using KanbanBoard.Interop;

namespace KanbanBoard.Services;

/// <summary>
/// 托盘图标（Win32 Shell_NotifyIcon）：左键切换悬浮栏，右键弹出菜单。
/// </summary>
public class TrayIconService : IDisposable
{
    private const int WM_TRAY = NativeMethods.WM_APP + 1;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_CONTEXTMENU = 0x007B;

    private HwndSource? _source;
    private IntPtr _icon = IntPtr.Zero;
    private bool _added;
    private readonly uint _id = 1;
    private ContextMenu? _menu;

    public event Action? ToggleRequested;

    public void Start(string tooltip, ContextMenu? menu)
    {
        EnsureWindow();
        _menu = menu;
        _icon = LoadIconHandle();
        AddOrModify(tooltip);
    }

    public void SetTooltip(string tooltip) => AddOrModify(tooltip);

    private void EnsureWindow()
    {
        if (_source != null) return;
        var parameters = new HwndSourceParameters("KanbanBoardTrayWindow")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            HwndSourceHook = WndProc,
        };
        _source = new HwndSource(parameters);
    }

    private static IntPtr LoadIconHandle()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "App.ico");
        if (File.Exists(path))
        {
            var h = NativeMethods.LoadImage(IntPtr.Zero, path, NativeMethods.IMAGE_ICON, 0, 0,
                NativeMethods.LR_LOADFROMFILE | NativeMethods.LR_DEFAULTSIZE);
            if (h != IntPtr.Zero) return h;
        }
        return NativeMethods.LoadIcon(IntPtr.Zero, new IntPtr(NativeMethods.IDI_APPLICATION));
    }

    private void AddOrModify(string tip)
    {
        if (_source == null || _icon == IntPtr.Zero) return;
        var data = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = _source.Handle,
            uID = _id,
            uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP,
            uCallbackMessage = WM_TRAY,
            hIcon = _icon,
            szTip = string.IsNullOrEmpty(tip) ? "看板" : tip,
        };
        NativeMethods.Shell_NotifyIcon(_added ? NativeMethods.NIM_MODIFY : NativeMethods.NIM_ADD, ref data);
        _added = true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAY)
        {
            uint lmsg = unchecked((uint)lParam.ToInt64());
            if (lmsg == WM_LBUTTONUP)
            {
                ToggleRequested?.Invoke();
            }
            else if (lmsg == WM_RBUTTONUP || lmsg == WM_CONTEXTMENU)
            {
                ShowMenu();
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ShowMenu()
    {
        if (_menu == null) return;
        _menu.Placement = PlacementMode.MousePoint;
        _menu.IsOpen = true;
    }

    public void Dispose()
    {
        if (_source != null && _added)
        {
            var data = new NativeMethods.NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
                hWnd = _source.Handle,
                uID = _id,
            };
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref data);
            _added = false;
        }
        if (_icon != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_icon);
            _icon = IntPtr.Zero;
        }
        _source?.Dispose();
        _source = null;
    }
}
