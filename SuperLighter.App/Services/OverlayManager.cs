using SuperLighter.App.UI;

namespace SuperLighter.App.Services;

internal sealed class OverlayManager : IDisposable
{
    private readonly List<BrightnessOverlayForm> _overlays = [];
    private bool _disposed;

    public OverlayManager()
    {
        RefreshDisplays();
    }

    public void RefreshDisplays()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var overlay in _overlays)
        {
            overlay.Close();
            overlay.Dispose();
        }

        _overlays.Clear();
        _overlays.AddRange(Screen.AllScreens.Select(screen => new BrightnessOverlayForm(screen)));
    }

    public void Apply(AppSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var shouldShow = settings.Enabled && settings.BrightnessBoostPercent > 0;

        foreach (var overlay in _overlays)
        {
            if (!shouldShow)
            {
                overlay.Hide();
                continue;
            }

            overlay.SetBrightnessBoost(settings.BrightnessBoostPercent);
            if (!overlay.Visible)
            {
                overlay.Show();
            }

            overlay.EnsureTopMost();
        }
    }

    public void EnsureTopMost()
    {
        foreach (var overlay in _overlays)
        {
            overlay.EnsureTopMost();
        }
    }

    public void Hide()
    {
        foreach (var overlay in _overlays)
        {
            overlay.Hide();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var overlay in _overlays)
        {
            overlay.Close();
            overlay.Dispose();
        }

        _overlays.Clear();
        _disposed = true;
    }
}
