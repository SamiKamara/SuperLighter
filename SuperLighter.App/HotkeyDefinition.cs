namespace SuperLighter.App;

public sealed class HotkeyDefinition : IEquatable<HotkeyDefinition>
{
    public Keys KeyCode { get; set; }

    public bool Control { get; set; }

    public bool Alt { get; set; }

    public bool Shift { get; set; }

    public bool Windows { get; set; }

    public bool IsValid
    {
        get
        {
            if (KeyCode is Keys.None or Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or
                Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey or Keys.Menu or Keys.LMenu or
                Keys.RMenu or Keys.LWin or Keys.RWin)
            {
                return false;
            }

            if (Control || Alt || Shift || Windows)
            {
                return true;
            }

            return KeyCode is >= Keys.F1 and <= Keys.F24 or Keys.Pause or Keys.PrintScreen or Keys.Scroll;
        }
    }

    public static HotkeyDefinition DefaultToggle() => new()
    {
        KeyCode = Keys.B,
        Control = true,
        Alt = true
    };

    public static HotkeyDefinition DefaultOpenSettings() => new()
    {
        KeyCode = Keys.O,
        Control = true,
        Alt = true
    };

    public static HotkeyDefinition FromKeyEvent(KeyEventArgs eventArgs) => new()
    {
        KeyCode = eventArgs.KeyCode,
        Control = eventArgs.Control,
        Alt = eventArgs.Alt,
        Shift = eventArgs.Shift,
        Windows = eventArgs.KeyCode is Keys.LWin or Keys.RWin ||
            (eventArgs.Modifiers & Keys.LWin) == Keys.LWin ||
            (eventArgs.Modifiers & Keys.RWin) == Keys.RWin
    };

    public HotkeyDefinition Clone() => new()
    {
        KeyCode = KeyCode,
        Control = Control,
        Alt = Alt,
        Shift = Shift,
        Windows = Windows
    };

    public string ToDisplayString()
    {
        if (!IsValid)
        {
            return "Not set";
        }

        var parts = new List<string>(5);
        if (Control)
        {
            parts.Add("Ctrl");
        }

        if (Alt)
        {
            parts.Add("Alt");
        }

        if (Shift)
        {
            parts.Add("Shift");
        }

        if (Windows)
        {
            parts.Add("Win");
        }

        parts.Add(FormatKey(KeyCode));
        return string.Join("+", parts);
    }

    public bool Equals(HotkeyDefinition? other) => other is not null &&
        KeyCode == other.KeyCode &&
        Control == other.Control &&
        Alt == other.Alt &&
        Shift == other.Shift &&
        Windows == other.Windows;

    public override bool Equals(object? obj) => Equals(obj as HotkeyDefinition);

    public override int GetHashCode() => HashCode.Combine(KeyCode, Control, Alt, Shift, Windows);

    private static string FormatKey(Keys keyCode)
    {
        if (keyCode is >= Keys.D0 and <= Keys.D9)
        {
            return ((int)keyCode - (int)Keys.D0).ToString();
        }

        return keyCode switch
        {
            Keys.Oemplus => "+",
            Keys.OemMinus => "-",
            Keys.Oemcomma => ",",
            Keys.OemPeriod => ".",
            Keys.OemQuestion => "/",
            Keys.Oemtilde => "`",
            Keys.OemOpenBrackets => "[",
            Keys.OemCloseBrackets => "]",
            Keys.OemPipe => "\\",
            Keys.OemSemicolon => ";",
            Keys.OemQuotes => "'",
            _ => keyCode.ToString()
        };
    }
}
