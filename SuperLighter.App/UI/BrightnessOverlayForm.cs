using SuperLighter.App.Native;

namespace SuperLighter.App.UI;

internal sealed class BrightnessOverlayForm : Form
{
    public string DeviceName { get; }

    public BrightnessOverlayForm(Screen screen)
    {
        DeviceName = screen.DeviceName;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.White;
        Bounds = screen.Bounds;
        ControlBox = false;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Opacity = 0.01d;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WS_EX_TRANSPARENT |
                NativeMethods.WS_EX_TOOLWINDOW |
                NativeMethods.WS_EX_LAYERED |
                NativeMethods.WS_EX_NOACTIVATE;
            return parameters;
        }
    }

    public void SetBrightnessBoost(int percentage)
    {
        Opacity = Math.Clamp(percentage, 1, 60) / 100d;
    }

    public void EnsureTopMost()
    {
        if (!Visible || !IsHandleCreated)
        {
            return;
        }

        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE |
            NativeMethods.SWP_NOSIZE |
            NativeMethods.SWP_NOACTIVATE |
            NativeMethods.SWP_SHOWWINDOW);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            message.Result = new IntPtr(NativeMethods.MA_NOACTIVATE);
            return;
        }

        if (message.Msg == NativeMethods.WM_NCHITTEST)
        {
            message.Result = new IntPtr(NativeMethods.HTTRANSPARENT);
            return;
        }

        base.WndProc(ref message);
    }
}
