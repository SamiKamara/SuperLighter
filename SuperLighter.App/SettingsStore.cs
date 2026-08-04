using System.Text.Json;

namespace SuperLighter.App;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SuperLighter",
        "settings.json");

    private readonly string _legacySettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenBoostOverlay",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            var settingsPath = File.Exists(_settingsPath)
                ? _settingsPath
                : _legacySettingsPath;

            if (!File.Exists(settingsPath))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath))
                ?? new AppSettings();
            settings.Normalize();

            if (!string.Equals(settingsPath, _settingsPath, StringComparison.OrdinalIgnoreCase))
            {
                Save(settings);
            }

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            settings.Normalize();
            var directory = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(directory);

            var temporaryPath = _settingsPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        catch
        {
            // A read-only profile must not prevent the overlay from running.
        }
    }
}
