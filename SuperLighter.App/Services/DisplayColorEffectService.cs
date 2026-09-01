using SuperLighter.App.Native;

namespace SuperLighter.App.Services;

internal sealed class DisplayColorEffectService : IDisposable
{
    internal const float GammaApproximationStrength = 0.35f;

    private readonly bool _initialized;
    private readonly bool _originalEffectCaptured;
    private readonly NativeMethods.MagnificationColorEffect _originalEffect;
    private bool _disposed;

    public DisplayColorEffectService()
    {
        _initialized = NativeMethods.MagInitialize();
        if (!_initialized)
        {
            return;
        }

        _originalEffect = NativeMethods.MagnificationColorEffect.CreateEmpty();
        _originalEffectCaptured = NativeMethods.MagGetFullscreenColorEffect(ref _originalEffect);
    }

    public bool Apply(AppSettings settings, bool useNvidiaCompatibilityMatrix)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var isNeutral = useNvidiaCompatibilityMatrix
            ? settings.GammaPercent == 100 &&
              settings.ContrastPercent == 100 &&
              settings.SaturationPercent == 100 &&
              settings.BrightnessBoostPercent == 0
            : settings.SaturationPercent == 100;
        if (!settings.Enabled || isNeutral)
        {
            Restore();
            return true;
        }

        if (!_initialized || !_originalEffectCaptured)
        {
            return false;
        }

        var composedEffect = Clone(_originalEffect);
        if (useNvidiaCompatibilityMatrix)
        {
            composedEffect = Multiply(
                composedEffect,
                BuildGammaEffect(settings.GammaPercent / 100f));
            composedEffect = Multiply(
                composedEffect,
                BuildContrastEffect(settings.ContrastPercent / 100f));
        }

        composedEffect = Multiply(
            composedEffect,
            BuildSaturationEffect(settings.SaturationPercent / 100f));
        if (useNvidiaCompatibilityMatrix)
        {
            composedEffect = Multiply(
                composedEffect,
                BuildBrightnessEffect(settings.BrightnessBoostPercent / 100f));
        }

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

    internal static NativeMethods.MagnificationColorEffect BuildGammaEffect(float gamma)
    {
        gamma = Math.Clamp(gamma, 0.5f, 6f);
        var exactMidtone = MathF.Pow(0.5f, 1f / gamma);
        var approximatedMidtone = 0.5f +
            (GammaApproximationStrength * (exactMidtone - 0.5f));

        if (approximatedMidtone >= 0.5f)
        {
            var inputScale = 2f * (1f - approximatedMidtone);
            return BuildRgbAffineEffect(inputScale, 1f - inputScale);
        }

        return BuildRgbAffineEffect(2f * approximatedMidtone, 0f);
    }

    internal static NativeMethods.MagnificationColorEffect BuildContrastEffect(float contrast)
    {
        contrast = Math.Clamp(contrast, 0.5f, 2f);
        return BuildRgbAffineEffect(contrast, 0.5f * (1f - contrast));
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

    internal static NativeMethods.MagnificationColorEffect BuildBrightnessEffect(float boost)
    {
        boost = Math.Clamp(boost, 0f, 0.6f);
        return BuildRgbAffineEffect(1f - boost, boost);
    }

    private static NativeMethods.MagnificationColorEffect BuildRgbAffineEffect(
        float inputScale,
        float translation)
    {
        var effect = NativeMethods.MagnificationColorEffect.CreateEmpty();
        effect.Transform[0] = inputScale;
        effect.Transform[6] = inputScale;
        effect.Transform[12] = inputScale;
        effect.Transform[18] = 1f;
        effect.Transform[20] = translation;
        effect.Transform[21] = translation;
        effect.Transform[22] = translation;
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
