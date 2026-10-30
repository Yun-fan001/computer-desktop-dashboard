using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using KanbanBoard.Interop;

namespace KanbanBoard.Services;

/// <summary>
/// 系统监控：CPU、内存、磁盘、电池。默认轻量轮询，供状态栏/组件展示。
/// </summary>
public class SystemMonitorService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private PerformanceCounter? _cpu;

    public double CpuUsage { get; private set; }
    public int MemoryLoad { get; private set; }
    public string DiskText { get; private set; } = "";
    public int? BatteryPercent { get; private set; }

    public string CpuText => $"CPU {CpuUsage:0}%";
    public string MemoryText => $"MEM {MemoryLoad}%";
    public string BatteryText => BatteryPercent.HasValue ? $"BAT {BatteryPercent}%" : "";

    public event Action? Updated;

    public SystemMonitorService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Sample();
        try { _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total"); }
        catch { _cpu = null; }
    }

    public void Start()
    {
        Sample();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void Sample()
    {
        try
        {
            if (_cpu != null) CpuUsage = Math.Round(_cpu.NextValue(), 1);
        }
        catch { CpuUsage = 0; }

        try
        {
            var mem = new NativeMethods.MEMORYSTATUSEX
            {
                dwLength = (uint)Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>(),
            };
            if (NativeMethods.GlobalMemoryStatusEx(mem)) MemoryLoad = (int)mem.dwMemoryLoad;
        }
        catch { }

        try
        {
            var root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\";
            var drive = new DriveInfo(root);
            var freeGb = drive.AvailableFreeSpace / 1024.0 / 1024 / 1024;
            DiskText = $"DISK {freeGb:0}G";
        }
        catch { DiskText = ""; }

        try
        {
            if (NativeMethods.GetSystemPowerStatus(out var sps))
                BatteryPercent = sps.BatteryLifePercent <= 100 ? sps.BatteryLifePercent : null;
            else BatteryPercent = null;
        }
        catch { BatteryPercent = null; }

        Updated?.Invoke();
    }

    public void Dispose() => _timer.Stop();
}
