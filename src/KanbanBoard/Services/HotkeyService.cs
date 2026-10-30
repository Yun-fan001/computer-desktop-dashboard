using System;
using System.Windows.Input;
using System.Windows.Interop;
using KanbanBoard.Interop;

namespace KanbanBoard.Services;

/// <summary>
/// 全局热键服务：注册/注销快捷键，冲突时注册失败可被上层检测。
/// </summary>
public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int QuickAddId = 0x9001;
    private const int ToggleId = 0x9002;
    private const int CommandId = 0x9003;

    private HwndSource? _source;

    public event Action? QuickAddPressed;
    public event Action? TogglePressed;
    public event Action? CommandPalettePressed;

    public bool QuickAddRegistered { get; private set; }
    public bool ToggleRegistered { get; private set; }
    public bool CommandRegistered { get; private set; }

    public void Register(string? quickAdd, string? toggle, string? command)
    {
        EnsureWindow();
        if (_source != null)
        {
            NativeMethods.UnregisterHotKey(_source.Handle, QuickAddId);
            NativeMethods.UnregisterHotKey(_source.Handle, ToggleId);
            NativeMethods.UnregisterHotKey(_source.Handle, CommandId);
        }
        QuickAddRegistered = TryRegister(quickAdd, QuickAddId);
        ToggleRegistered = TryRegister(toggle, ToggleId);
        CommandRegistered = TryRegister(command, CommandId);
    }

    private bool TryRegister(string? spec, int id)
    {
        if (string.IsNullOrWhiteSpace(spec)) return false;
        if (!HotkeyParser.TryParse(spec, out uint mods, out uint vk)) return false;
        return NativeMethods.RegisterHotKey(_source!.Handle, id, mods, vk);
    }

    private void EnsureWindow()
    {
        if (_source != null) return;
        var parameters = new HwndSourceParameters("KanbanBoardHotkeyWindow")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            HwndSourceHook = WndProc,
        };
        _source = new HwndSource(parameters);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (id == QuickAddId) QuickAddPressed?.Invoke();
            else if (id == ToggleId) TogglePressed?.Invoke();
            else if (id == CommandId) CommandPalettePressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source == null) return;
        NativeMethods.UnregisterHotKey(_source.Handle, QuickAddId);
        NativeMethods.UnregisterHotKey(_source.Handle, ToggleId);
        NativeMethods.UnregisterHotKey(_source.Handle, CommandId);
        _source.Dispose();
        _source = null;
    }
}

/// <summary>快捷键字符串解析："Ctrl+Shift+Space" → modifiers + vk。</summary>
public static class HotkeyParser
{
    public static bool TryParse(string spec, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;
        if (string.IsNullOrWhiteSpace(spec)) return false;

        var parts = spec.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;

        string keyPart = parts[^1];
        foreach (var p in parts[..^1])
        {
            switch (p.ToLowerInvariant())
            {
                case "ctrl":
                case "control": modifiers |= NativeMethods.MOD_CONTROL; break;
                case "shift": modifiers |= NativeMethods.MOD_SHIFT; break;
                case "alt": modifiers |= NativeMethods.MOD_ALT; break;
                case "win":
                case "windows": modifiers |= NativeMethods.MOD_WIN; break;
                default: return false;
            }
        }
        if (modifiers == 0) return false;

        if (!TryGetKey(keyPart, out var key)) return false;
        vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        return vk != 0;
    }

    private static bool TryGetKey(string name, out Key key)
    {
        key = Key.None;
        var normalized = name.ToLowerInvariant() switch
        {
            "esc" => "Escape",
            "space" => "Space",
            "return" or "enter" => "Enter",
            "ins" => "Insert",
            "del" => "Delete",
            "pgup" => "PageUp",
            "pgdn" => "PageDown",
            "up" => "Up",
            "down" => "Down",
            "left" => "Left",
            "right" => "Right",
            "ctrl" or "control" => "LeftCtrl",
            "shift" => "LeftShift",
            "alt" => "LeftAlt",
            _ => name,
        };
        return Enum.TryParse(normalized, true, out key);
    }
}
