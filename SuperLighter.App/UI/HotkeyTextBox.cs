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
            CenterTextVertically();
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
        Multiline = true;
        WordWrap = false;
        Cursor = Cursors.Hand;
    }

    protected override void OnHandleCreated(EventArgs eventArgs)
    {
        base.OnHandleCreated(eventArgs);
        CenterTextVertically();
    }

    protected override void OnFontChanged(EventArgs eventArgs)
    {
        base.OnFontChanged(eventArgs);
        CenterTextVertically();
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        CenterTextVertically();
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

    private void CenterTextVertically()
    {
        if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var verticalInset = Math.Max(0, (ClientSize.Height - Font.Height) / 2);
        var formattingRectangle = new NativeMethods.Rect
        {
            Left = 1,
            Top = verticalInset,
            Right = Math.Max(1, ClientSize.Width - 1),
            Bottom = Math.Max(verticalInset + 1, ClientSize.Height - verticalInset)
        };
        NativeMethods.SendMessage(
            Handle,
            NativeMethods.EM_SETRECT,
            IntPtr.Zero,
            ref formattingRectangle);
    }
}
