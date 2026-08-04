using SuperLighter.App.Native;

namespace SuperLighter.App.Services;

internal enum HotkeyAction
{
    ToggleBoost,
    OpenSettings
}

internal sealed class SystemHotkeyWindow : NativeWindow, IDisposable
{
    private const int ToggleBoostHotkeyId = 1;
    private const int OpenSettingsHotkeyId = 2;
    private readonly Dictionary<int, HotkeyAction> _registeredActions = [];
    private bool _disposed;

    public event EventHandler<HotkeyAction>? HotkeyPressed;

    public SystemHotkeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "SuperLighter.Hotkeys"
        });
    }

    public IReadOnlyList<HotkeyAction> ReplaceBindings(AppSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ClearBindings();

        var failures = new List<HotkeyAction>();
        RegisterBinding(ToggleBoostHotkeyId, HotkeyAction.ToggleBoost, settings.ToggleHotkey, failures);
        RegisterBinding(OpenSettingsHotkeyId, HotkeyAction.OpenSettings, settings.OpenSettingsHotkey, failures);
        return failures;
    }

    public void ClearBindings()
    {
        foreach (var hotkeyId in _registeredActions.Keys)
        {
            NativeMethods.UnregisterHotKey(Handle, hotkeyId);
        }

        _registeredActions.Clear();
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WM_HOTKEY &&
            _registeredActions.TryGetValue(message.WParam.ToInt32(), out var action))
        {
            HotkeyPressed?.Invoke(this, action);
        }

        base.WndProc(ref message);
    }

    private void RegisterBinding(
        int hotkeyId,
        HotkeyAction action,
        HotkeyDefinition hotkey,
        ICollection<HotkeyAction> failures)
    {
        if (!hotkey.IsValid)
        {
            failures.Add(action);
            return;
        }

        var modifiers = NativeMethods.MOD_NOREPEAT;
        if (hotkey.Control)
        {
            modifiers |= NativeMethods.MOD_CONTROL;
        }

        if (hotkey.Alt)
        {
            modifiers |= NativeMethods.MOD_ALT;
        }

        if (hotkey.Shift)
        {
            modifiers |= NativeMethods.MOD_SHIFT;
        }

        if (hotkey.Windows)
        {
            modifiers |= NativeMethods.MOD_WIN;
        }

        if (NativeMethods.RegisterHotKey(Handle, hotkeyId, modifiers, (uint)hotkey.KeyCode))
        {
            _registeredActions[hotkeyId] = action;
        }
        else
        {
            failures.Add(action);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ClearBindings();
        DestroyHandle();
        _disposed = true;
    }
}
