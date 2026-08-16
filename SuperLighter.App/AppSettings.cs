namespace SuperLighter.App;

public sealed class AppSettings
{
    public bool Enabled { get; set; } = true;

    public int BrightnessBoostPercent { get; set; }

    public int ContrastPercent { get; set; } = 120;

    public int GammaPercent { get; set; } = 250;

    public int SaturationPercent { get; set; } = 140;

    public Dictionary<string, int> MonitorBrightnessPercent { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HotkeyDefinition ToggleHotkey { get; set; } = HotkeyDefinition.DefaultToggle();

    public HotkeyDefinition OpenSettingsHotkey { get; set; } = HotkeyDefinition.DefaultOpenSettings();

    public void Normalize()
    {
        BrightnessBoostPercent = Math.Clamp(BrightnessBoostPercent, 0, 60);
        ContrastPercent = Math.Clamp(ContrastPercent, 50, 200);
        GammaPercent = Math.Clamp(GammaPercent, 50, 600);
        SaturationPercent = Math.Clamp(SaturationPercent, 0, 300);
        var normalizedMonitorBrightness = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var (monitorId, brightnessPercent) in MonitorBrightnessPercent ?? [])
        {
            if (!string.IsNullOrWhiteSpace(monitorId))
            {
                normalizedMonitorBrightness[monitorId] = Math.Clamp(brightnessPercent, 0, 100);
            }
        }

        MonitorBrightnessPercent = normalizedMonitorBrightness;
        ToggleHotkey ??= HotkeyDefinition.DefaultToggle();
        OpenSettingsHotkey ??= HotkeyDefinition.DefaultOpenSettings();
    }

    public AppSettings Clone() => new()
    {
        Enabled = Enabled,
        BrightnessBoostPercent = BrightnessBoostPercent,
        ContrastPercent = ContrastPercent,
        GammaPercent = GammaPercent,
        SaturationPercent = SaturationPercent,
        MonitorBrightnessPercent = new Dictionary<string, int>(
            MonitorBrightnessPercent,
            StringComparer.OrdinalIgnoreCase),
        ToggleHotkey = ToggleHotkey.Clone(),
        OpenSettingsHotkey = OpenSettingsHotkey.Clone()
    };

    public void SetNeutral()
    {
        Enabled = true;
        BrightnessBoostPercent = 0;
        ContrastPercent = 100;
        GammaPercent = 100;
        SaturationPercent = 100;
    }
}
