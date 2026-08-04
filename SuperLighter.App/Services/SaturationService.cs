using SuperLighter.App.Native;

namespace SuperLighter.App.Services;

internal sealed class SaturationService : IDisposable
{
    private readonly bool _initialized;
    private readonly bool _originalEffectCaptured;
    private readonly NativeMethods.MagnificationColorEffect _originalEffect;
    private bool _disposed;

    public SaturationService()
    {
        _initialized = NativeMethods.MagInitialize();
        if (!_initialized)
        {
            return;
        }

        _originalEffect = NativeMethods.MagnificationColorEffect.CreateEmpty();
        _originalEffectCaptured = NativeMethods.MagGetFullscreenColorEffect(ref _originalEffect);
    }

    public bool Apply(AppSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!settings.Enabled || settings.SaturationPercent == 100)
        {
            Restore();
            return true;
        }

        if (!_initialized || !_originalEffectCaptured)
        {
            return false;
        }

        var saturationEffect = BuildSaturationEffect(settings.SaturationPercent / 100f);
        var composedEffect = Multiply(_originalEffect, saturationEffect);
        return NativeMethods.MagSetFullscreenColorEffect(ref composedEffect);
    }

    public void Restore()
    {
        if (_disposed || !_initialized || !_originalEffectCaptured)
        {
            return;
        }

        var originalEffect = Clone(_originalEffect);
        NativeMethods.MagSetFullscreenColorEffect(ref originalEffect);
    }

    internal static NativeMethods.MagnificationColorEffect BuildSaturationEffect(float saturation)
    {
        saturation = Math.Clamp(saturation, 0f, 3f);
        const float redLuminance = 0.2126f;
        const float greenLuminance = 0.7152f;
        const float blueLuminance = 0.0722f;
        var inverse = 1f - saturation;

        var effect = NativeMethods.MagnificationColorEffect.CreateEmpty();
        effect.Transform[0] = (redLuminance * inverse) + saturation;
        effect.Transform[1] = redLuminance * inverse;
        effect.Transform[2] = redLuminance * inverse;

        effect.Transform[5] = greenLuminance * inverse;
        effect.Transform[6] = (greenLuminance * inverse) + saturation;
        effect.Transform[7] = greenLuminance * inverse;

        effect.Transform[10] = blueLuminance * inverse;
        effect.Transform[11] = blueLuminance * inverse;
        effect.Transform[12] = (blueLuminance * inverse) + saturation;

        effect.Transform[18] = 1f;
        effect.Transform[24] = 1f;
        return effect;
    }

    private static NativeMethods.MagnificationColorEffect Multiply(
        NativeMethods.MagnificationColorEffect left,
        NativeMethods.MagnificationColorEffect right)
    {
        var result = NativeMethods.MagnificationColorEffect.CreateEmpty();
        for (var row = 0; row < 5; row++)
        {
            for (var column = 0; column < 5; column++)
            {
                var value = 0f;
                for (var index = 0; index < 5; index++)
                {
                    value += left.Transform[(row * 5) + index] * right.Transform[(index * 5) + column];
                }

                result.Transform[(row * 5) + column] = value;
            }
        }

        return result;
    }

    private static NativeMethods.MagnificationColorEffect Clone(
        NativeMethods.MagnificationColorEffect source)
    {
        return new NativeMethods.MagnificationColorEffect
        {
            Transform = (float[])source.Transform.Clone()
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Restore();
        _disposed = true;
        if (_initialized)
        {
            NativeMethods.MagUninitialize();
        }
    }
}
