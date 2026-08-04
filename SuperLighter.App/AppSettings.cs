namespace SuperLighter.App;

public sealed class AppSettings
{
    public bool Enabled { get; set; } = true;

    public int BrightnessBoostPercent { get; set; }

    public int ContrastPercent { get; set; } = 100;

    public int GammaPercent { get; set; } = 100;

    public int SaturationPercent { get; set; } = 100;

    public HotkeyDefinition ToggleHotkey { get; set; } = HotkeyDefinition.DefaultToggle();

    public HotkeyDefinition OpenSettingsHotkey { get; set; } = HotkeyDefinition.DefaultOpenSettings();

    public void Normalize()
    {
        BrightnessBoostPercent = Math.Clamp(BrightnessBoostPercent, 0, 60);
        ContrastPercent = Math.Clamp(ContrastPercent, 50, 200);
        GammaPercent = Math.Clamp(GammaPercent, 50, 300);
        SaturationPercent = Math.Clamp(SaturationPercent, 0, 300);
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
