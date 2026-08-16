using Microsoft.Win32;
using SuperLighter.App.Services;
using SuperLighter.App.UI;

namespace SuperLighter.App;

internal sealed class SuperLighterApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore = new();
    private readonly GammaRampService _gammaRampService = new();
    private readonly SaturationService _saturationService = new();
    private readonly OverlayManager _overlayManager = new();
    private readonly MonitorBrightnessService _monitorBrightnessService = new();
    private readonly SystemHotkeyWindow _hotkeyWindow = new();
    private readonly Control _uiDispatcher = new();
    private readonly EventWaitHandle _openSettingsSignal;
    private readonly RegisteredWaitHandle _openSettingsSignalRegistration;
    private readonly Icon _appIcon = AppIcon.Load();
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _trayMenu = new();
    private readonly ToolStripMenuItem _enabledMenuItem;
    private readonly System.Windows.Forms.Timer _startupTimer = new() { Interval = 50 };
    private readonly System.Windows.Forms.Timer _topMostTimer = new() { Interval = 1500 };
    private AppSettings _settings;
    private SettingsForm? _activeSettingsForm;
    private bool _displayWarningShown;
    private bool _hotkeyWarningShown;
    private bool _isExiting;
    private bool _effectsRestored;

    public SuperLighterApplicationContext(EventWaitHandle openSettingsSignal)
    {
        _openSettingsSignal = openSettingsSignal;
        _settings = _settingsStore.Load();
        _settings.Normalize();
        _ = _uiDispatcher.Handle;
        _monitorBrightnessService.Refresh();
        ApplyStoredMonitorBrightness();

        _openSettingsSignalRegistration = ThreadPool.RegisterWaitForSingleObject(
            _openSettingsSignal,
            static (state, _) => ((SuperLighterApplicationContext)state!).RequestOpenSettings(),
            this,
            Timeout.Infinite,
            executeOnlyOnce: false);

        _enabledMenuItem = new ToolStripMenuItem("Enhancement enabled", null, (_, _) => ToggleEnabled());
        var settingsMenuItem = new ToolStripMenuItem("Settings...", null, (_, _) => OpenSettings());
        var neutralMenuItem = new ToolStripMenuItem("Reset display", null, (_, _) => ResetNeutral());
        var exitMenuItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication());
        _trayMenu.BackColor = Color.FromArgb(30, 34, 42);
        _trayMenu.ForeColor = Color.FromArgb(241, 244, 249);
        _trayMenu.Renderer = new ToolStripProfessionalRenderer(new DarkToolStripColorTable());
        _trayMenu.ShowCheckMargin = true;
        _trayMenu.ShowImageMargin = false;
        _trayMenu.Items.AddRange(
            _enabledMenuItem,
            settingsMenuItem,
            neutralMenuItem,
            new ToolStripSeparator(),
            exitMenuItem);
        foreach (ToolStripItem item in _trayMenu.Items)
        {
            item.ForeColor = _trayMenu.ForeColor;
        }

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _trayMenu,
            Icon = _appIcon,
            Text = "SuperLighter",
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => OpenSettings();

        _hotkeyWindow.HotkeyPressed += HandleHotkeyPressed;
        _startupTimer.Tick += HandleStartupTimerTick;
        _topMostTimer.Tick += (_, _) => _overlayManager.EnsureTopMost();
        SystemEvents.DisplaySettingsChanged += HandleDisplaySettingsChanged;
        SystemEvents.PowerModeChanged += HandlePowerModeChanged;
        SystemEvents.SessionSwitch += HandleSessionSwitch;

        RegisterHotkeys();
        ApplySettings(_settings);
        _topMostTimer.Start();
        _startupTimer.Start();
    }

    public void RestoreDisplayEffects()
    {
        if (_effectsRestored)
        {
            return;
        }

        _overlayManager.Hide();
        _gammaRampService.Restore();
        _saturationService.Restore();
        _effectsRestored = true;
    }

    private void ApplySettings(AppSettings settings)
    {
        _effectsRestored = false;
        settings.Normalize();
        _overlayManager.Apply(settings);
        var failedDisplays = _gammaRampService.Apply(settings);
        var saturationApplied = _saturationService.Apply(settings);
        _enabledMenuItem.Checked = settings.Enabled;

        var state = settings.Enabled ? "enabled" : "disabled";
        var trayText = $"SuperLighter - {state}";
        _notifyIcon.Text = trayText.Length <= 63 ? trayText : trayText[..63];

        if ((failedDisplays.Count > 0 || !saturationApplied) && !_displayWarningShown)
        {
            _displayWarningShown = true;
            _notifyIcon.ShowBalloonTip(
                5000,
                "A display effect could not be applied",
                "HDR, Remote Desktop, another color tool, or the display driver may restrict gamma, contrast, or saturation. The brightness overlay still works.",
                ToolTipIcon.Warning);
        }
    }

    private void RegisterHotkeys()
    {
        if (_isExiting)
        {
            return;
        }

        var failures = _hotkeyWindow.ReplaceBindings(_settings);
        if (failures.Count == 0 || _hotkeyWarningShown)
        {
            return;
        }

        _hotkeyWarningShown = true;
        var labels = failures.Select(action => action switch
        {
            HotkeyAction.ToggleBoost => "Toggle enhancement",
            HotkeyAction.OpenSettings => "Open settings",
            _ => action.ToString()
        });
        _notifyIcon.ShowBalloonTip(
            5000,
            "Shortcut unavailable",
            $"Could not register: {string.Join(", ", labels)}. Another app may already be using the combination.",
            ToolTipIcon.Warning);
    }

    private void ToggleEnabled()
    {
        _settings.Enabled = !_settings.Enabled;
        _settingsStore.Save(_settings);
        ApplySettings(_settings);
    }

    private void ResetNeutral()
    {
        _settings.SetNeutral();
        _settingsStore.Save(_settings);
        ApplySettings(_settings);
        _notifyIcon.ShowBalloonTip(
            2500,
            "SuperLighter",
            "Gamma, contrast, saturation, and brightness were reset to neutral.",
            ToolTipIcon.Info);
    }

    private void OpenSettings()
    {
        if (_isExiting)
        {
            return;
        }

        if (_activeSettingsForm is not null && !_activeSettingsForm.IsDisposed)
        {
            if (_activeSettingsForm.WindowState == FormWindowState.Minimized)
            {
                _activeSettingsForm.WindowState = FormWindowState.Normal;
            }

            _activeSettingsForm.Activate();
            return;
        }

        _hotkeyWindow.ClearBindings();
        _activeSettingsForm = new SettingsForm(
            _settings,
            ApplySettings,
            _monitorBrightnessService.Monitors,
            _monitorBrightnessService.TrySetBrightness);
        try
        {
            if (_activeSettingsForm.ShowDialog() == DialogResult.OK)
            {
                _settings = _activeSettingsForm.ResultSettings.Clone();
                _settingsStore.Save(_settings);
                ApplySettings(_settings);
            }
            else
            {
                ApplySettings(_settings);
            }
        }
        finally
        {
            _activeSettingsForm.Dispose();
            _activeSettingsForm = null;
            RegisterHotkeys();
        }
    }

    private void RequestOpenSettings()
    {
        try
        {
            if (!_uiDispatcher.IsDisposed)
            {
                _uiDispatcher.BeginInvoke(new MethodInvoker(OpenSettings));
            }
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void HandleHotkeyPressed(object? sender, HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.ToggleBoost:
                ToggleEnabled();
                break;
            case HotkeyAction.OpenSettings:
                OpenSettings();
                break;
        }
    }

    private void HandleStartupTimerTick(object? sender, EventArgs eventArgs)
    {
        _startupTimer.Stop();
        OpenSettings();
    }

    private void HandleDisplaySettingsChanged(object? sender, EventArgs eventArgs)
    {
        RequestDisplayRefresh();
    }

    private void HandlePowerModeChanged(object? sender, PowerModeChangedEventArgs eventArgs)
    {
        if (eventArgs.Mode == PowerModes.Resume)
        {
            RequestDisplayRefresh();
        }
    }

    private void HandleSessionSwitch(object? sender, SessionSwitchEventArgs eventArgs)
    {
        if (eventArgs.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.ConsoleConnect)
        {
            RequestDisplayRefresh();
        }
    }

    private void RequestDisplayRefresh()
    {
        try
        {
            if (_isExiting || _uiDispatcher.IsDisposed)
            {
                return;
            }

            _uiDispatcher.BeginInvoke(new MethodInvoker(() =>
            {
                _monitorBrightnessService.Refresh();
                if (_activeSettingsForm is not null && !_activeSettingsForm.IsDisposed)
                {
                    _activeSettingsForm.RefreshMonitorBrightnessControls(
                        _monitorBrightnessService.Monitors);
                }
                else
                {
                    ApplyStoredMonitorBrightness();
                }

                _overlayManager.RefreshDisplays();
                ApplySettings(_settings);
            }));
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void ApplyStoredMonitorBrightness()
    {
        foreach (var monitor in _monitorBrightnessService.Monitors)
        {
            if (_settings.MonitorBrightnessPercent.TryGetValue(
                    monitor.Id,
                    out var brightnessPercent))
            {
                _monitorBrightnessService.TrySetBrightness(
                    monitor.Id,
                    brightnessPercent);
            }
        }
    }

    internal void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _startupTimer.Stop();
        _topMostTimer.Stop();
        _hotkeyWindow.ClearBindings();
        _notifyIcon.Visible = false;
        if (_activeSettingsForm is not null && !_activeSettingsForm.IsDisposed)
        {
            _activeSettingsForm.Close();
        }

        RestoreDisplayEffects();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RestoreDisplayEffects();
            _startupTimer.Stop();
            _topMostTimer.Stop();
            _openSettingsSignalRegistration.Unregister(null);
            SystemEvents.DisplaySettingsChanged -= HandleDisplaySettingsChanged;
            SystemEvents.PowerModeChanged -= HandlePowerModeChanged;
            SystemEvents.SessionSwitch -= HandleSessionSwitch;
            _activeSettingsForm?.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _appIcon.Dispose();
            _trayMenu.Dispose();
            _hotkeyWindow.Dispose();
            _monitorBrightnessService.Dispose();
            _overlayManager.Dispose();
            _saturationService.Dispose();
            _gammaRampService.Dispose();
            _startupTimer.Dispose();
            _topMostTimer.Dispose();
            _uiDispatcher.Dispose();
        }

        base.Dispose(disposing);
    }
}
