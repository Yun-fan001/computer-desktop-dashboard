using System;
using Microsoft.Win32;

namespace KanbanBoard.Services;

/// <summary>会话锁定检测：锁屏隐藏 UI，托盘保留。</summary>
public class SessionLockDetector : IDisposable
{
    public event Action? Locked;
    public event Action? Unlocked;
    public bool IsLocked { get; private set; }

    public void Start() => SystemEvents.SessionSwitch += OnSessionSwitch;

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock)
        {
            IsLocked = true;
            Locked?.Invoke();
        }
        else if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            IsLocked = false;
            Unlocked?.Invoke();
        }
    }

    public void Dispose() => SystemEvents.SessionSwitch -= OnSessionSwitch;
}
