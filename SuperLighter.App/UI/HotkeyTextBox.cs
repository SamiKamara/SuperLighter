using System.ComponentModel;
using SuperLighter.App.Native;

namespace SuperLighter.App.UI;

internal sealed class HotkeyTextBox : TextBox
{
    private HotkeyDefinition _hotkey = new();

    public event EventHandler? HotkeyChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HotkeyDefinition Hotkey
    {
        get => _hotkey.Clone();
        set
        {
            _hotkey = value?.Clone() ?? new HotkeyDefinition();
            Text = _hotkey.ToDisplayString();
            SelectAll();
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public HotkeyTextBox()
    {
        ReadOnly = true;
        ShortcutsEnabled = false;
        TabStop = true;
        TextAlign = HorizontalAlignment.Center;
        Cursor = Cursors.Hand;
    }

    protected override void OnEnter(EventArgs eventArgs)
    {
        base.OnEnter(eventArgs);
        SelectAll();
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        Focus();
        SelectAll();
    }

    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        eventArgs.SuppressKeyPress = true;

        if (eventArgs.KeyCode is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or
            Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey or Keys.Menu or Keys.LMenu or
            Keys.RMenu or Keys.LWin or Keys.RWin)
        {
            return;
        }

        if (eventArgs.Modifiers == Keys.None && eventArgs.KeyCode is Keys.Delete or Keys.Back)
        {
            Hotkey = new HotkeyDefinition();
            return;
        }

        var hotkey = HotkeyDefinition.FromKeyEvent(eventArgs);
        hotkey.Windows =
            (NativeMethods.GetAsyncKeyState((int)Keys.LWin) & 0x8000) != 0 ||
            (NativeMethods.GetAsyncKeyState((int)Keys.RWin) & 0x8000) != 0;
        Hotkey = hotkey;
    }
}
