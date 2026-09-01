using SuperLighter.App.Native;
using SuperLighter.App.UI;

namespace SuperLighter.App.Services;

internal static class SelfTests
{
    public static bool Run()
    {
        try
        {
            var defaults = new AppSettings();
            Require(defaults.Enabled, "enhancement enabled by default");
            Require(defaults.BrightnessBoostPercent == 0, "default brightness");
            Require(defaults.ContrastPercent == 120, "default contrast");
            Require(defaults.GammaPercent == 250, "default gamma");
            Require(defaults.SaturationPercent == 140, "default saturation");

            var settings = new AppSettings
            {
                BrightnessBoostPercent = 999,
                ContrastPercent = -10,
                GammaPercent = 999,
                SaturationPercent = 999
            };
            settings.MonitorBrightnessPercent["test-monitor"] = 999;
            settings.Normalize();
            Require(settings.BrightnessBoostPercent == 60, "brightness clamp");
            Require(settings.ContrastPercent == 50, "contrast clamp");
            Require(settings.GammaPercent == 600, "gamma clamp");
            Require(settings.SaturationPercent == 300, "saturation clamp");
            Require(
                settings.MonitorBrightnessPercent["test-monitor"] == 100,
                "monitor brightness clamp");
            Require(HotkeyDefinition.DefaultToggle().ToDisplayString() == "Ctrl+Alt+B", "toggle shortcut default");
            Require(HotkeyDefinition.DefaultOpenSettings().ToDisplayString() == "Ctrl+Alt+O", "settings shortcut default");

            var identity = GammaRampService.CreateIdentityRamp();
            var neutralRamp = GammaRampService.BuildRamp(identity, 1d, 1d);
            for (var index = 0; index < 256; index++)
            {
                var expected = (ushort)(index * 257);
                Require(neutralRamp.Red[index] == expected, "neutral red ramp");
                Require(neutralRamp.Green[index] == expected, "neutral green ramp");
                Require(neutralRamp.Blue[index] == expected, "neutral blue ramp");
            }

            var boostedRamp = GammaRampService.BuildRamp(identity, 1.4d, 1.8d);
            Require(IsMonotonic(boostedRamp.Red), "boosted ramp monotonicity");
            Require(boostedRamp.Red[128] > neutralRamp.Red[128], "legacy gamma raises midtones");

            var formerMaximumGamma = GammaRampService.BuildRamp(identity, 1d, 3d);
            var maximumGamma = GammaRampService.BuildRamp(identity, 1d, 6d);
            Require(IsMonotonic(maximumGamma.Red), "maximum gamma ramp monotonicity");
            Require(
                maximumGamma.Red[128] > formerMaximumGamma.Red[128],
                "gamma supports values above 3.00");

            var neutralGamma = DisplayColorEffectService.BuildGammaEffect(1f);
            RequireApproximately(neutralGamma.Transform[0], 1f, "neutral gamma input scale");
            RequireApproximately(neutralGamma.Transform[20], 0f, "neutral gamma translation");

            var brighterGamma = DisplayColorEffectService.BuildGammaEffect(4.17f);
            Require(ApplyGray(brighterGamma, 0.5f) > 0.5f, "gamma raises midtones");

            var darkerGamma = DisplayColorEffectService.BuildGammaEffect(0.5f);
            Require(ApplyGray(darkerGamma, 0.5f) < 0.5f, "gamma lowers midtones");

            var contrast = DisplayColorEffectService.BuildContrastEffect(1.2f);
            RequireApproximately(ApplyGray(contrast, 0.5f), 0.5f, "contrast midpoint");
            Require(ApplyGray(contrast, 0.25f) < 0.25f, "contrast darkens shadows");
            Require(ApplyGray(contrast, 0.75f) > 0.75f, "contrast raises highlights");

            Require(
                MonitorBrightnessService.RawToPercent(60, 20, 100) == 50,
                "monitor brightness raw to percent");
            Require(
                MonitorBrightnessService.PercentToRaw(50, 20, 100) == 60,
                "monitor brightness percent to raw");

            var neutralSaturation = DisplayColorEffectService.BuildSaturationEffect(1f);
            RequireApproximately(neutralSaturation.Transform[0], 1f, "neutral saturation red");
            RequireApproximately(neutralSaturation.Transform[6], 1f, "neutral saturation green");
            RequireApproximately(neutralSaturation.Transform[12], 1f, "neutral saturation blue");
            RequireApproximately(neutralSaturation.Transform[1], 0f, "neutral saturation cross-channel");

            var grayscale = DisplayColorEffectService.BuildSaturationEffect(0f);
            RequireApproximately(grayscale.Transform[0], grayscale.Transform[1], "grayscale red contribution");
            RequireApproximately(grayscale.Transform[5], grayscale.Transform[6], "grayscale green contribution");
            RequireApproximately(grayscale.Transform[10], grayscale.Transform[11], "grayscale blue contribution");

            var brightness = DisplayColorEffectService.BuildBrightnessEffect(0.3f);
            RequireApproximately(brightness.Transform[0], 0.7f, "brightness input scale");
            RequireApproximately(brightness.Transform[6], 0.7f, "brightness green input scale");
            RequireApproximately(brightness.Transform[12], 0.7f, "brightness blue input scale");
            RequireApproximately(brightness.Transform[20], 0.3f, "brightness red translation");
            RequireApproximately(brightness.Transform[21], 0.3f, "brightness green translation");
            RequireApproximately(brightness.Transform[22], 0.3f, "brightness blue translation");

            Require(
                DisplayAdapterDetector.IsNvidiaAdapter(
                    @"PCI\VEN_10DE&DEV_2206",
                    "Microsoft Basic Display Adapter"),
                "NVIDIA vendor detection");
            Require(
                DisplayAdapterDetector.IsNvidiaAdapter(
                    string.Empty,
                    "NVIDIA GeForce RTX 3080"),
                "NVIDIA description fallback");
            Require(
                !DisplayAdapterDetector.IsNvidiaAdapter(
                    @"PCI\VEN_1002&DEV_73BF",
                    "AMD Radeon RX 6800 XT"),
                "AMD adapter rejection");
            Require(
                !DisplayAdapterDetector.IsNvidiaAdapter(
                    @"PCI\VEN_8086&DEV_9BC5",
                    "Intel UHD Graphics"),
                "Intel adapter rejection");
            Require(
                !DisplayAdapterDetector.IsNvidiaAdapter(
                    @"USB\VID_17E9&PID_6006",
                    "DisplayLink USB Device"),
                "DisplayLink adapter rejection");

            var nvidiaRouting = DisplayEffectRouting.FromNvidiaPresence(true);
            Require(!nvidiaRouting.UseLegacyGammaAndBrightness, "NVIDIA bypasses legacy display effects");
            Require(nvidiaRouting.UseNvidiaCompatibilityMatrix, "NVIDIA uses compatibility matrix");

            var legacyRouting = DisplayEffectRouting.FromNvidiaPresence(false);
            Require(legacyRouting.UseLegacyGammaAndBrightness, "non-NVIDIA keeps legacy display effects");
            Require(!legacyRouting.UseNvidiaCompatibilityMatrix, "non-NVIDIA bypasses compatibility matrix");

            using var appIcon = AppIcon.Load();
            Require(appIcon.Width >= 16 && appIcon.Height >= 16, "embedded application icon");

            using var settingsForm = new SettingsForm(
                new AppSettings(),
                _ => { },
                [new MonitorBrightnessInfo("test-monitor", "Test monitor", 50)],
                (_, _) => true);
            _ = settingsForm.Handle;

            var primaryScreen = Screen.PrimaryScreen ?? Screen.AllScreens.First();
            using var overlayForm = new BrightnessOverlayForm(primaryScreen);
            _ = overlayForm.Handle;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static float ApplyGray(
        NativeMethods.MagnificationColorEffect effect,
        float input) => (input * effect.Transform[0]) + effect.Transform[20];

    private static bool IsMonotonic(ushort[] values)
    {
        for (var index = 1; index < values.Length; index++)
        {
            if (values[index] < values[index - 1])
            {
                return false;
            }
        }

        return true;
    }

    private static void Require(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Self-test failed: {name}");
        }
    }

    private static void RequireApproximately(float actual, float expected, string name)
    {
        Require(Math.Abs(actual - expected) < 0.0001f, name);
    }
}
