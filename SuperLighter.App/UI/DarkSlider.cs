using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace SuperLighter.App.UI;

internal sealed class DarkSlider : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private bool _dragging;

    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            Value = _value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum);
            Value = _value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minimum, _maximum);
            if (_value == clamped)
            {
                return;
            }

            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SmallChange { get; set; } = 1;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int LargeChange { get; set; } = 10;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TrackColor { get; set; } = Color.FromArgb(55, 62, 76);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor { get; set; } = Color.FromArgb(92, 160, 255);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ThumbColor { get; set; } = Color.FromArgb(235, 241, 250);

    public DarkSlider()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable |
            ControlStyles.UserPaint,
            true);
        Height = 34;
        MinimumSize = new Size(120, 34);
        TabStop = true;
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var scale = DeviceDpi / 96f;
        var horizontalPadding = 11f * scale;
        var railHeight = Math.Max(4f, 4f * scale);
        var railY = (Height - railHeight) / 2f;
        var railWidth = Math.Max(1f, Width - (horizontalPadding * 2f));
        var ratio = _maximum == _minimum ? 0f : (_value - _minimum) / (float)(_maximum - _minimum);
        var thumbX = horizontalPadding + (railWidth * ratio);

        using var trackBrush = new SolidBrush(TrackColor);
        using var accentBrush = new SolidBrush(AccentColor);
        using var thumbBrush = new SolidBrush(ThumbColor);
        using var focusPen = new Pen(Color.FromArgb(125, AccentColor), Math.Max(1f, scale));

        var fullRail = new RectangleF(horizontalPadding, railY, railWidth, railHeight);
        eventArgs.Graphics.FillRoundedRectangle(trackBrush, fullRail, railHeight / 2f);

        if (thumbX > horizontalPadding)
        {
            var activeRail = new RectangleF(horizontalPadding, railY, thumbX - horizontalPadding, railHeight);
            eventArgs.Graphics.FillRoundedRectangle(accentBrush, activeRail, railHeight / 2f);
        }

        var thumbRadius = 7f * scale;
        var thumbBounds = new RectangleF(
            thumbX - thumbRadius,
            (Height / 2f) - thumbRadius,
            thumbRadius * 2f,
            thumbRadius * 2f);
        eventArgs.Graphics.FillEllipse(thumbBrush, thumbBounds);

        if (Focused)
        {
            var focusBounds = RectangleF.Inflate(thumbBounds, 3f * scale, 3f * scale);
            eventArgs.Graphics.DrawEllipse(focusPen, focusBounds);
        }
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        if (eventArgs.Button != MouseButtons.Left)
        {
            return;
        }

        Focus();
        _dragging = true;
        Capture = true;
        SetValueFromMouse(eventArgs.X);
    }

    protected override void OnMouseMove(MouseEventArgs eventArgs)
    {
        base.OnMouseMove(eventArgs);
        if (_dragging)
        {
            SetValueFromMouse(eventArgs.X);
        }
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        _dragging = false;
        Capture = false;
    }

    protected override void OnMouseWheel(MouseEventArgs eventArgs)
    {
        base.OnMouseWheel(eventArgs);
        Value += eventArgs.Delta > 0 ? SmallChange : -SmallChange;
    }

    protected override bool IsInputKey(Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        return keyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or
            Keys.PageUp or Keys.PageDown || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        switch (eventArgs.KeyCode)
        {
            case Keys.Left:
            case Keys.Down:
                Value -= SmallChange;
                break;
            case Keys.Right:
            case Keys.Up:
                Value += SmallChange;
                break;
            case Keys.PageDown:
                Value -= LargeChange;
                break;
            case Keys.PageUp:
                Value += LargeChange;
                break;
            case Keys.Home:
                Value = Minimum;
                break;
            case Keys.End:
                Value = Maximum;
                break;
            default:
                base.OnKeyDown(eventArgs);
                return;
        }

        eventArgs.Handled = true;
    }

    protected override void OnGotFocus(EventArgs eventArgs)
    {
        base.OnGotFocus(eventArgs);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs eventArgs)
    {
        base.OnLostFocus(eventArgs);
        Invalidate();
    }

    private void SetValueFromMouse(int mouseX)
    {
        var scale = DeviceDpi / 96f;
        var horizontalPadding = 11f * scale;
        var usableWidth = Math.Max(1f, Width - (horizontalPadding * 2f));
        var ratio = Math.Clamp((mouseX - horizontalPadding) / usableWidth, 0f, 1f);
        Value = (int)Math.Round(_minimum + ((_maximum - _minimum) * ratio));
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(
        this Graphics graphics,
        Brush brush,
        RectangleF bounds,
        float radius)
    {
        if (radius <= 0f)
        {
            graphics.FillRectangle(brush, bounds);
            return;
        }

        var diameter = radius * 2f;
        using var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
