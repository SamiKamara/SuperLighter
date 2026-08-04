namespace SuperLighter.App;

internal static class AppIcon
{
    private const string ResourceName = "SuperLighter.App.Assets.SuperLighter.ico";

    public static Icon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is not null)
        {
            using var embeddedIcon = new Icon(stream);
            return (Icon)embeddedIcon.Clone();
        }

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            ?? (Icon)SystemIcons.Application.Clone();
    }
}
