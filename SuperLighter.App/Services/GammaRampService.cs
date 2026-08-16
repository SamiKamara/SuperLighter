using SuperLighter.App.Native;

namespace SuperLighter.App.Services;

internal sealed class GammaRampService : IDisposable
{
    private readonly Dictionary<string, NativeMethods.GammaRamp> _originalRamps =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public IReadOnlyList<string> Apply(AppSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!settings.Enabled)
        {
            Restore();
            return Array.Empty<string>();
        }

        settings.Normalize();
        var failedDisplays = new List<string>();

        foreach (var screen in Screen.AllScreens)
        {
            if (!TryGetOriginalRamp(screen.DeviceName, out var originalRamp))
            {
                failedDisplays.Add(screen.DeviceName);
                continue;
            }

            var adjustedRamp = BuildRamp(
                originalRamp,
                settings.ContrastPercent / 100d,
                settings.GammaPercent / 100d);

            if (!TrySetRamp(screen.DeviceName, adjustedRamp))
            {
                failedDisplays.Add(screen.DeviceName);
            }
        }

        return failedDisplays;
    }

    public void Restore()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var (deviceName, ramp) in _originalRamps)
        {
            var originalRamp = Clone(ramp);
            TrySetRamp(deviceName, originalRamp);
        }
    }

    internal static NativeMethods.GammaRamp BuildRamp(
        NativeMethods.GammaRamp baseline,
        double contrast,
        double gamma)
    {
        contrast = Math.Clamp(contrast, 0.5d, 2d);
        gamma = Math.Clamp(gamma, 0.5d, 6d);

        var result = NativeMethods.GammaRamp.CreateEmpty();
        for (var index = 0; index < 256; index++)
        {
            var input = index / 255d;
            var gammaAdjusted = Math.Pow(input, 1d / gamma);
            var transformed = Math.Clamp(((gammaAdjusted - 0.5d) * contrast) + 0.5d, 0d, 1d);

            result.Red[index] = Sample(baseline.Red, transformed);
            result.Green[index] = Sample(baseline.Green, transformed);
            result.Blue[index] = Sample(baseline.Blue, transformed);
        }

        return result;
    }

    internal static NativeMethods.GammaRamp CreateIdentityRamp()
    {
        var ramp = NativeMethods.GammaRamp.CreateEmpty();
        for (var index = 0; index < 256; index++)
        {
            var value = (ushort)(index * 257);
            ramp.Red[index] = value;
            ramp.Green[index] = value;
            ramp.Blue[index] = value;
        }

        return ramp;
    }

    private bool TryGetOriginalRamp(string deviceName, out NativeMethods.GammaRamp ramp)
    {
        if (_originalRamps.TryGetValue(deviceName, out ramp))
        {
            ramp = Clone(ramp);
            return true;
        }

        if (!TryReadRamp(deviceName, out ramp))
        {
            return false;
        }

        _originalRamps[deviceName] = Clone(ramp);
        return true;
    }

    private static bool TryReadRamp(string deviceName, out NativeMethods.GammaRamp ramp)
    {
        ramp = NativeMethods.GammaRamp.CreateEmpty();
        var deviceContext = NativeMethods.CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
        if (deviceContext == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return NativeMethods.GetDeviceGammaRamp(deviceContext, ref ramp);
        }
        finally
        {
            NativeMethods.DeleteDC(deviceContext);
        }
    }

    private static bool TrySetRamp(string deviceName, NativeMethods.GammaRamp ramp)
    {
        var deviceContext = NativeMethods.CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
        if (deviceContext == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return NativeMethods.SetDeviceGammaRamp(deviceContext, ref ramp);
        }
        finally
        {
            NativeMethods.DeleteDC(deviceContext);
        }
    }

    private static ushort Sample(ushort[] channel, double normalizedPosition)
    {
        var exactIndex = normalizedPosition * 255d;
        var lowerIndex = (int)Math.Floor(exactIndex);
        var upperIndex = Math.Min(255, lowerIndex + 1);
        var fraction = exactIndex - lowerIndex;
        var value = channel[lowerIndex] + ((channel[upperIndex] - channel[lowerIndex]) * fraction);
        return (ushort)Math.Clamp((int)Math.Round(value), ushort.MinValue, ushort.MaxValue);
    }

    private static NativeMethods.GammaRamp Clone(NativeMethods.GammaRamp source) => new()
    {
        Red = (ushort[])source.Red.Clone(),
        Green = (ushort[])source.Green.Clone(),
        Blue = (ushort[])source.Blue.Clone()
    };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Restore();
        _disposed = true;
    }
}
