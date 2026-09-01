using SuperLighter.App.Native;

namespace SuperLighter.App.Services;

internal static class DisplayAdapterDetector
{
    private const string NvidiaVendorId = "VEN_10DE";

    public static bool HasNvidiaDisplayAdapter()
    {
        for (uint deviceNumber = 0; ; deviceNumber++)
        {
            var displayDevice = NativeMethods.DisplayDevice.Create();
            if (!NativeMethods.EnumDisplayDevices(
                    null,
                    deviceNumber,
                    ref displayDevice,
                    0))
            {
                return false;
            }

            if (IsNvidiaAdapter(displayDevice.DeviceId, displayDevice.DeviceString))
            {
                return true;
            }
        }
    }

    internal static bool IsNvidiaAdapter(string? deviceId, string? deviceDescription) =>
        (!string.IsNullOrWhiteSpace(deviceId) &&
         deviceId.Contains(NvidiaVendorId, StringComparison.OrdinalIgnoreCase)) ||
        (!string.IsNullOrWhiteSpace(deviceDescription) &&
         deviceDescription.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase));
}
