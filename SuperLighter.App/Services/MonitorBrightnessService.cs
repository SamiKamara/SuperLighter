using SuperLighter.App.Native;

namespace SuperLighter.App.Services;

internal sealed record MonitorBrightnessInfo(
    string Id,
    string DisplayName,
    int BrightnessPercent);

internal sealed class MonitorBrightnessService : IDisposable
{
    private sealed class AdjustableMonitor
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public required IntPtr Handle { get; init; }
        public required uint MinimumBrightness { get; init; }
        public required uint MaximumBrightness { get; init; }
        public required int BrightnessPercent { get; set; }
    }

    private readonly List<AdjustableMonitor> _monitors = [];
    private bool _disposed;

    public IReadOnlyList<MonitorBrightnessInfo> Monitors => _monitors
        .Select(CreateInfo)
        .ToArray();

    public IReadOnlyList<MonitorBrightnessInfo> Refresh()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ReleaseMonitorHandles();

        try
        {
            var logicalMonitorIndex = 0;
            NativeMethods.MonitorEnumProcedure callback =
                (IntPtr monitorHandle,
                    IntPtr _,
                    ref NativeMethods.Rect _,
                    IntPtr _) =>
                {
                    try
                    {
                        TryAddPhysicalMonitors(monitorHandle, logicalMonitorIndex++);
                    }
                    catch
                    {
                        // A broken DDC/CI implementation must not prevent other displays from being detected.
                    }

                    return true;
                };
            NativeMethods.EnumDisplayMonitors(
                IntPtr.Zero,
                IntPtr.Zero,
                callback,
                IntPtr.Zero);
        }
        catch
        {
            ReleaseMonitorHandles();
        }

        return Monitors;
    }

    public bool TrySetBrightness(string monitorId, int brightnessPercent)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var monitor = _monitors.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, monitorId, StringComparison.OrdinalIgnoreCase));
        if (monitor is null)
        {
            return false;
        }

        var clampedPercent = Math.Clamp(brightnessPercent, 0, 100);
        var rawBrightness = PercentToRaw(
            clampedPercent,
            monitor.MinimumBrightness,
            monitor.MaximumBrightness);
        try
        {
            if (!NativeMethods.SetMonitorBrightness(monitor.Handle, rawBrightness))
            {
                return false;
            }

            monitor.BrightnessPercent = clampedPercent;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static int RawToPercent(uint value, uint minimum, uint maximum)
    {
        if (maximum <= minimum)
        {
            return 0;
        }

        var clamped = Math.Clamp(value, minimum, maximum);
        return (int)Math.Round((clamped - minimum) * 100d / (maximum - minimum));
    }

    internal static uint PercentToRaw(int percent, uint minimum, uint maximum)
    {
        if (maximum <= minimum)
        {
            return minimum;
        }

        var clamped = Math.Clamp(percent, 0, 100);
        return minimum + (uint)Math.Round((maximum - minimum) * clamped / 100d);
    }

    private void TryAddPhysicalMonitors(IntPtr monitorHandle, int logicalMonitorIndex)
    {
        if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(
                monitorHandle,
                out var physicalMonitorCount) ||
            physicalMonitorCount == 0)
        {
            return;
        }

        var monitorInfo = NativeMethods.MonitorInfoEx.Create();
        NativeMethods.GetMonitorInfo(monitorHandle, ref monitorInfo);
        var deviceName = string.IsNullOrWhiteSpace(monitorInfo.DeviceName)
            ? $"DISPLAY{logicalMonitorIndex + 1}"
            : monitorInfo.DeviceName.Replace(@"\\.\", string.Empty, StringComparison.Ordinal);

        var physicalMonitors = new NativeMethods.PhysicalMonitor[physicalMonitorCount];
        if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(
                monitorHandle,
                physicalMonitorCount,
                physicalMonitors))
        {
            return;
        }

        try
        {
            for (var physicalIndex = 0; physicalIndex < physicalMonitors.Length; physicalIndex++)
            {
                var physicalMonitor = physicalMonitors[physicalIndex];
                var keepHandle = false;
                try
                {
                    if (!NativeMethods.GetMonitorBrightness(
                            physicalMonitor.Handle,
                            out var minimumBrightness,
                            out var currentBrightness,
                            out var maximumBrightness) ||
                        maximumBrightness <= minimumBrightness)
                    {
                        continue;
                    }

                    var description = string.IsNullOrWhiteSpace(physicalMonitor.Description)
                        ? "Monitor"
                        : physicalMonitor.Description.Trim();
                    var id = $"{deviceName}|{description.Replace('|', '/')}|{physicalIndex}";
                    var displayName = physicalMonitors.Length == 1
                        ? $"{description} ({deviceName})"
                        : $"{description} ({deviceName}, {physicalIndex + 1})";
                    _monitors.Add(new AdjustableMonitor
                    {
                        Id = id,
                        DisplayName = displayName,
                        Handle = physicalMonitor.Handle,
                        MinimumBrightness = minimumBrightness,
                        MaximumBrightness = maximumBrightness,
                        BrightnessPercent = RawToPercent(
                            currentBrightness,
                            minimumBrightness,
                            maximumBrightness)
                    });
                    keepHandle = true;
                }
                finally
                {
                    if (!keepHandle && physicalMonitor.Handle != IntPtr.Zero)
                    {
                        NativeMethods.DestroyPhysicalMonitor(physicalMonitor.Handle);
                    }

                    physicalMonitors[physicalIndex].Handle = IntPtr.Zero;
                }
            }
        }
        finally
        {
            foreach (var physicalMonitor in physicalMonitors)
            {
                if (physicalMonitor.Handle != IntPtr.Zero)
                {
                    NativeMethods.DestroyPhysicalMonitor(physicalMonitor.Handle);
                }
            }
        }
    }

    private static MonitorBrightnessInfo CreateInfo(AdjustableMonitor monitor) => new(
        monitor.Id,
        monitor.DisplayName,
        monitor.BrightnessPercent);

    private void ReleaseMonitorHandles()
    {
        foreach (var monitor in _monitors)
        {
            if (monitor.Handle != IntPtr.Zero)
            {
                NativeMethods.DestroyPhysicalMonitor(monitor.Handle);
            }
        }

        _monitors.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ReleaseMonitorHandles();
    }
}
