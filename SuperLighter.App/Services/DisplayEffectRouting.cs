namespace SuperLighter.App.Services;

internal readonly record struct DisplayEffectRouting(
    bool UseLegacyGammaAndBrightness,
    bool UseNvidiaCompatibilityMatrix)
{
    public static DisplayEffectRouting Detect() =>
        FromNvidiaPresence(DisplayAdapterDetector.HasNvidiaDisplayAdapter());

    internal static DisplayEffectRouting FromNvidiaPresence(bool hasNvidiaAdapter) =>
        hasNvidiaAdapter
            ? new DisplayEffectRouting(
                UseLegacyGammaAndBrightness: false,
                UseNvidiaCompatibilityMatrix: true)
            : new DisplayEffectRouting(
                UseLegacyGammaAndBrightness: true,
                UseNvidiaCompatibilityMatrix: false);
}
