using SuperLighter.App.UI;

namespace SuperLighter.App.Services;

internal static class SelfTests
{
    public static bool Run()
    {
        try
        {
            var settings = new AppSettings
            {
                BrightnessBoostPercent = 999,
                ContrastPercent = -10,
                GammaPercent = 999,
                SaturationPercent = 999
            };
            settings.Normalize();
            Require(settings.BrightnessBoostPercent == 60, "brightness clamp");
            Require(settings.ContrastPercent == 50, "contrast clamp");
            Require(settings.GammaPercent == 300, "gamma clamp");
            Require(settings.SaturationPercent == 300, "saturation clamp");
            Require(HotkeyDefinition.DefaultToggle().ToDisplayString() == "Ctrl+Alt+B", "toggle shortcut default");
            Require(HotkeyDefinition.DefaultOpenSettings().ToDisplayString() == "Ctrl+Alt+O", "settings shortcut default");

            var identity = GammaRampService.CreateIdentityRamp();
            var neutral = GammaRampService.BuildRamp(identity, 1d, 1d);
            for (var index = 0; index < 256; index++)
            {
                var expected = (ushort)(index * 257);
                Require(neutral.Red[index] == expected, "neutral red ramp");
                Require(neutral.Green[index] == expected, "neutral green ramp");
                Require(neutral.Blue[index] == expected, "neutral blue ramp");
            }

            var boosted = GammaRampService.BuildRamp(identity, 1.4d, 1.8d);
            Require(IsMonotonic(boosted.Red), "boosted ramp monotonicity");
            Require(boosted.Red[128] > neutral.Red[128], "gamma raises midtones");

            var neutralSaturation = SaturationService.BuildSaturationEffect(1f);
            RequireApproximately(neutralSaturation.Transform[0], 1f, "neutral saturation red");
            RequireApproximately(neutralSaturation.Transform[6], 1f, "neutral saturation green");
            RequireApproximately(neutralSaturation.Transform[12], 1f, "neutral saturation blue");
            RequireApproximately(neutralSaturation.Transform[1], 0f, "neutral saturation cross-channel");

            var grayscale = SaturationService.BuildSaturationEffect(0f);
            RequireApproximately(grayscale.Transform[0], grayscale.Transform[1], "grayscale red contribution");
            RequireApproximately(grayscale.Transform[5], grayscale.Transform[6], "grayscale green contribution");
            RequireApproximately(grayscale.Transform[10], grayscale.Transform[11], "grayscale blue contribution");

            using var appIcon = AppIcon.Load();
            Require(appIcon.Width >= 16 && appIcon.Height >= 16, "embedded application icon");

            using var settingsForm = new SettingsForm(new AppSettings(), _ => { });
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
