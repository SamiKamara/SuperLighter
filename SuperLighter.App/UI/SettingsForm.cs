using System.Globalization;
using SuperLighter.App.Native;
using SuperLighter.App.Services;

namespace SuperLighter.App.UI;

internal sealed class SettingsForm : Form
{
    private sealed class MonitorBrightnessBinding
    {
        public required MonitorBrightnessInfo Monitor { get; init; }
        public required DarkSlider Slider { get; init; }
        public required Label ValueLabel { get; init; }
    }

    private const int FooterHeight = 66;

    private static readonly Color WindowBackground = Color.FromArgb(20, 23, 29);
    private static readonly Color CardBackground = Color.FromArgb(30, 34, 42);
    private static readonly Color InputBackground = Color.FromArgb(24, 28, 35);
    private static readonly Color BorderColor = Color.FromArgb(55, 62, 76);
    private static readonly Color PrimaryText = Color.FromArgb(241, 244, 249);
    private static readonly Color SecondaryText = Color.FromArgb(166, 176, 192);
    private static readonly Color Accent = Color.FromArgb(92, 160, 255);
    private static readonly Color ErrorText = Color.FromArgb(255, 173, 92);

    private readonly AppSettings _initialSettings;
    private readonly Action<AppSettings> _preview;
    private readonly Func<string, int, bool> _setMonitorBrightness;
    private readonly CheckBox _enabledCheckBox = new();
    private readonly DarkSlider _gammaSlider = new();
    private readonly DarkSlider _contrastSlider = new();
    private readonly DarkSlider _saturationSlider = new();
    private readonly DarkSlider _brightnessSlider = new();
    private readonly Label _gammaValueLabel = new();
    private readonly Label _contrastValueLabel = new();
    private readonly Label _saturationValueLabel = new();
    private readonly Label _brightnessValueLabel = new();
    private readonly HotkeyTextBox _toggleHotkeyTextBox = new();
    private readonly HotkeyTextBox _openSettingsHotkeyTextBox = new();
    private readonly Panel _scrollPanel = new();
    private readonly TableLayoutPanel _contentPanel;
    private readonly TableLayoutPanel _monitorBrightnessPanel = new();
    private readonly List<MonitorBrightnessBinding> _monitorBrightnessBindings = [];
    private readonly Dictionary<string, int> _initialMonitorBrightness =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _pendingMonitorBrightness =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Label> _wrappingLabels = [];
    private readonly System.Windows.Forms.Timer _previewTimer = new() { Interval = 90 };
    private readonly System.Windows.Forms.Timer _monitorBrightnessTimer = new() { Interval = 120 };
    private readonly Icon _windowIcon = AppIcon.Load();
    private bool _saved;

    public AppSettings ResultSettings { get; private set; }

    public SettingsForm(
        AppSettings settings,
        Action<AppSettings> preview,
        IReadOnlyList<MonitorBrightnessInfo>? adjustableMonitors = null,
        Func<string, int, bool>? setMonitorBrightness = null)
    {
        settings.Normalize();
        _initialSettings = settings.Clone();
        ResultSettings = settings.Clone();
        _preview = preview;
        _setMonitorBrightness = setMonitorBrightness ?? ((_, _) => false);

        Text = "SuperLighter";
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = WindowBackground;
        ForeColor = PrimaryText;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        ClientSize = new Size(740, 720);
        MinimumSize = new Size(600, 520);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        Icon = _windowIcon;
        ShowIcon = true;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        ConfigureSlider(_gammaSlider, 50, 600, 5, 25);
        ConfigureSlider(_contrastSlider, 50, 200, 5, 10);
        ConfigureSlider(_saturationSlider, 0, 300, 5, 25);
        ConfigureSlider(_brightnessSlider, 0, 60, 1, 5);
        ConfigureHotkeyTextBox(_toggleHotkeyTextBox);
        ConfigureHotkeyTextBox(_openSettingsHotkeyTextBox);

        var root = new TableLayoutPanel
        {
            BackColor = WindowBackground,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, FooterHeight));

        _scrollPanel.AutoScroll = true;
        _scrollPanel.BackColor = WindowBackground;
        _scrollPanel.Dock = DockStyle.Fill;
        _scrollPanel.Padding = new Padding(20, 18, 20, 12);

        _contentPanel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddContent(_contentPanel, CreateHeader());
        AddContent(_contentPanel, CreateEnabledCard());
        AddContent(_contentPanel, CreateSliderCard(
            "Gamma",
            "Raises or lowers midtones without changing the physical backlight. 1.00 is neutral.",
            _gammaSlider,
            _gammaValueLabel));
        AddContent(_contentPanel, CreateSliderCard(
            "Contrast",
            "Expands or compresses the tonal range. 100% is neutral; high values may clip shadows and highlights.",
            _contrastSlider,
            _contrastValueLabel));
        AddContent(_contentPanel, CreateSliderCard(
            "Saturation",
            "Controls color intensity. 0% is grayscale, 100% is neutral, and higher values produce stronger colors.",
            _saturationSlider,
            _saturationValueLabel));
        AddContent(_contentPanel, CreateSliderCard(
            "Software brightness",
            "Lifts rendered colors toward white. High values also make blacks look lighter.",
            _brightnessSlider,
            _brightnessValueLabel));
        ConfigureMonitorBrightnessPanel(adjustableMonitors ?? []);
        AddContent(_contentPanel, _monitorBrightnessPanel);
        AddContent(_contentPanel, CreateHotkeysCard());
        AddContent(_contentPanel, CreateLimitationsNote());

        _scrollPanel.Controls.Add(_contentPanel);
        root.Controls.Add(_scrollPanel, 0, 0);
        root.Controls.Add(CreateFooter(), 0, 1);
        Controls.Add(root);

        Bind(settings);
        _enabledCheckBox.CheckedChanged += HandleDisplayValueChanged;
        _gammaSlider.ValueChanged += HandleDisplayValueChanged;
        _contrastSlider.ValueChanged += HandleDisplayValueChanged;
        _saturationSlider.ValueChanged += HandleDisplayValueChanged;
        _brightnessSlider.ValueChanged += HandleDisplayValueChanged;
        _previewTimer.Tick += HandlePreviewTimerTick;
        _monitorBrightnessTimer.Tick += HandleMonitorBrightnessTimerTick;
        _scrollPanel.ClientSizeChanged += (_, _) => UpdateWrappingWidths();
        Shown += HandleShown;
        FormClosing += HandleFormClosing;
    }

    private static void AddContent(TableLayoutPanel layout, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(control, 0, row);
    }

    private Control CreateHeader()
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            BackColor = WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 16),
            Padding = Padding.Empty
        };

        var title = new Label
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            ForeColor = PrimaryText,
            Margin = Padding.Empty,
            Text = "Display enhancement"
        };
        var subtitle = CreateWrappingLabel(
            "Tune the image beyond the standard Windows controls. Changes are previewed live and restored when the app exits.",
            SecondaryText);
        subtitle.Margin = new Padding(0, 5, 0, 0);
        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(subtitle, 0, 1);
        return panel;
    }

    private Control CreateEnabledCard()
    {
        var card = CreateCard();
        _enabledCheckBox.AutoSize = true;
        _enabledCheckBox.BackColor = CardBackground;
        _enabledCheckBox.FlatStyle = FlatStyle.Flat;
        _enabledCheckBox.Font = new Font(Font, FontStyle.Bold);
        _enabledCheckBox.ForeColor = PrimaryText;
        _enabledCheckBox.Margin = Padding.Empty;
        _enabledCheckBox.Padding = new Padding(1);
        _enabledCheckBox.Text = "Enhancement enabled";
        card.Controls.Add(_enabledCheckBox, 0, 0);
        return card;
    }

    private Control CreateSliderCard(
        string title,
        string description,
        DarkSlider slider,
        Label valueLabel)
    {
        var card = CreateCard();
        card.ColumnCount = 1;
        card.RowCount = 3;
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel
        {
            AutoSize = true,
            BackColor = CardBackground,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));

        var titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = PrimaryText,
            Margin = Padding.Empty,
            Text = title
        };
        valueLabel.AutoSize = true;
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Font = new Font(Font, FontStyle.Bold);
        valueLabel.ForeColor = Accent;
        valueLabel.Margin = Padding.Empty;
        valueLabel.TextAlign = ContentAlignment.MiddleRight;

        slider.BackColor = CardBackground;
        slider.Dock = DockStyle.Fill;
        slider.Margin = new Padding(0, 7, 0, 2);

        var descriptionLabel = CreateWrappingLabel(description, SecondaryText);
        descriptionLabel.Margin = new Padding(0, 3, 0, 0);

        header.Controls.Add(titleLabel, 0, 0);
        header.Controls.Add(valueLabel, 1, 0);
        card.Controls.Add(header, 0, 0);
        card.Controls.Add(slider, 0, 1);
        card.Controls.Add(descriptionLabel, 0, 2);
        return card;
    }

    private Control CreateHotkeysCard()
    {
        var card = CreateCard();
        card.ColumnCount = 2;
        card.RowCount = 4;
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var title = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = PrimaryText,
            Margin = new Padding(0, 0, 0, 9),
            Text = "Keyboard shortcuts"
        };
        card.Controls.Add(title, 0, 0);
        card.SetColumnSpan(title, 2);

        card.Controls.Add(CreateFieldLabel("Toggle enhancement"), 0, 1);
        card.Controls.Add(_toggleHotkeyTextBox, 1, 1);
        card.Controls.Add(CreateFieldLabel("Open settings"), 0, 2);
        card.Controls.Add(_openSettingsHotkeyTextBox, 1, 2);

        var note = CreateWrappingLabel(
            "Focus a shortcut field and press the new combination. Press Delete to clear it. Letter and number keys require a modifier.",
            SecondaryText);
        note.Margin = new Padding(0, 9, 0, 0);
        card.Controls.Add(note, 0, 3);
        card.SetColumnSpan(note, 2);
        return card;
    }

    private Control CreateLimitationsNote()
    {
        var note = CreateWrappingLabel(
            "The app changes the rendered image; it cannot exceed the display panel's physical brightness limit. " +
            "HDR, Remote Desktop, exclusive fullscreen, or a display driver may restrict gamma, contrast, saturation, or software brightness effects.",
            SecondaryText);
        note.Margin = new Padding(4, 4, 4, 10);
        return note;
    }

    private Control CreateFooter()
    {
        var footer = new Panel
        {
            BackColor = CardBackground,
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 13, 18, 12)
        };
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            BackColor = CardBackground,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Padding.Empty,
            WrapContents = false
        };

        var saveButton = CreateButton("Save", primary: true, 100);
        var cancelButton = CreateButton("Cancel", primary: false, 100);
        var neutralButton = CreateButton("Reset display", primary: false, 125);
        saveButton.Click += (_, _) => SaveAndClose();
        cancelButton.Click += (_, _) => CancelAndClose();
        neutralButton.Click += (_, _) => SetNeutralValues();
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(neutralButton);
        footer.Controls.Add(buttons);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        return footer;
    }

    internal void RefreshMonitorBrightnessControls(
        IReadOnlyList<MonitorBrightnessInfo> adjustableMonitors)
    {
        ConfigureMonitorBrightnessPanel(adjustableMonitors);
        if (IsHandleCreated && !IsDisposed)
        {
            BeginInvoke(new MethodInvoker(FitWindowToContent));
        }
    }

    private void ConfigureMonitorBrightnessPanel(
        IReadOnlyList<MonitorBrightnessInfo> adjustableMonitors)
    {
        var existingValues = _monitorBrightnessBindings.ToDictionary(
            binding => binding.Monitor.Id,
            binding => binding.Slider.Value,
            StringComparer.OrdinalIgnoreCase);

        _monitorBrightnessPanel.SuspendLayout();
        while (_monitorBrightnessPanel.Controls.Count > 0)
        {
            var control = _monitorBrightnessPanel.Controls[0];
            RemoveWrappingLabels(control);
            _monitorBrightnessPanel.Controls.Remove(control);
            control.Dispose();
        }

        _monitorBrightnessPanel.RowStyles.Clear();
        _monitorBrightnessPanel.RowCount = 0;
        _monitorBrightnessBindings.Clear();
        _monitorBrightnessPanel.AutoSize = true;
        _monitorBrightnessPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _monitorBrightnessPanel.BackColor = WindowBackground;
        _monitorBrightnessPanel.ColumnCount = 1;
        _monitorBrightnessPanel.Dock = DockStyle.Top;
        _monitorBrightnessPanel.Margin = Padding.Empty;
        _monitorBrightnessPanel.Padding = Padding.Empty;
        if (_monitorBrightnessPanel.ColumnStyles.Count == 0)
        {
            _monitorBrightnessPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        }

        foreach (var monitor in adjustableMonitors)
        {
            var slider = new DarkSlider();
            var valueLabel = new Label();
            slider.AccessibleName = $"{monitor.DisplayName} physical backlight";
            ConfigureSlider(slider, 0, 100, 1, 10);
            var card = CreateSliderCard(
                $"Physical backlight — {monitor.DisplayName}",
                "Controls this monitor's hardware backlight through DDC/CI. This setting remains on the monitor after SuperLighter exits.",
                slider,
                valueLabel);
            slider.Value = existingValues.GetValueOrDefault(
                monitor.Id,
                monitor.BrightnessPercent);
            var binding = new MonitorBrightnessBinding
            {
                Monitor = monitor,
                Slider = slider,
                ValueLabel = valueLabel
            };
            _monitorBrightnessBindings.Add(binding);
            _initialMonitorBrightness.TryAdd(monitor.Id, monitor.BrightnessPercent);
            slider.ValueChanged += HandleMonitorBrightnessValueChanged;
            AddContent(_monitorBrightnessPanel, card);
        }

        var currentMonitorIds = _monitorBrightnessBindings
            .Select(binding => binding.Monitor.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pendingMonitorId in _pendingMonitorBrightness.Keys.ToArray())
        {
            if (!currentMonitorIds.Contains(pendingMonitorId))
            {
                _pendingMonitorBrightness.Remove(pendingMonitorId);
            }
        }

        _monitorBrightnessPanel.Visible = _monitorBrightnessBindings.Count > 0;
        _monitorBrightnessPanel.ResumeLayout(performLayout: true);
        UpdateValueLabels();
    }

    private void RemoveWrappingLabels(Control control)
    {
        if (control is Label label)
        {
            _wrappingLabels.Remove(label);
        }

        foreach (Control child in control.Controls)
        {
            RemoveWrappingLabels(child);
        }
    }

    private TableLayoutPanel CreateCard() => new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = CardBackground,
        Dock = DockStyle.Top,
        Margin = new Padding(0, 0, 0, 10),
        Padding = new Padding(14)
    };

    private Label CreateFieldLabel(string text) => new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        ForeColor = PrimaryText,
        Margin = new Padding(0, 6, 8, 6),
        Text = text
    };

    private Label CreateWrappingLabel(string text, Color color)
    {
        var label = new Label
        {
            AutoSize = true,
            ForeColor = color,
            MaximumSize = new Size(620, 0),
            Text = text
        };
        _wrappingLabels.Add(label);
        return label;
    }

    private static Button CreateButton(string text, bool primary, int width)
    {
        var button = new Button
        {
            AutoSize = false,
            BackColor = primary ? Accent : InputBackground,
            FlatStyle = FlatStyle.Flat,
            ForeColor = PrimaryText,
            Height = 38,
            Margin = new Padding(8, 0, 0, 0),
            Text = text,
            UseVisualStyleBackColor = false,
            Width = width
        };
        button.FlatAppearance.BorderColor = primary ? Accent : BorderColor;
        button.FlatAppearance.MouseDownBackColor = primary
            ? Color.FromArgb(65, 126, 211)
            : Color.FromArgb(39, 45, 56);
        button.FlatAppearance.MouseOverBackColor = primary
            ? Color.FromArgb(104, 170, 255)
            : Color.FromArgb(44, 50, 62);
        return button;
    }

    private static void ConfigureSlider(
        DarkSlider slider,
        int minimum,
        int maximum,
        int smallChange,
        int largeChange)
    {
        slider.Minimum = minimum;
        slider.Maximum = maximum;
        slider.SmallChange = smallChange;
        slider.LargeChange = largeChange;
    }

    private static void ConfigureHotkeyTextBox(HotkeyTextBox textBox)
    {
        textBox.AutoSize = false;
        textBox.BackColor = InputBackground;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Dock = DockStyle.Fill;
        textBox.ForeColor = PrimaryText;
        textBox.Height = 31;
        textBox.Margin = new Padding(0, 3, 0, 3);
    }

    private void Bind(AppSettings settings)
    {
        _enabledCheckBox.Checked = settings.Enabled;
        _gammaSlider.Value = settings.GammaPercent;
        _contrastSlider.Value = settings.ContrastPercent;
        _saturationSlider.Value = settings.SaturationPercent;
        _brightnessSlider.Value = settings.BrightnessBoostPercent;
        _toggleHotkeyTextBox.Hotkey = settings.ToggleHotkey;
        _openSettingsHotkeyTextBox.Hotkey = settings.OpenSettingsHotkey;
        UpdateValueLabels();
    }

    private void HandleDisplayValueChanged(object? sender, EventArgs eventArgs)
    {
        UpdateValueLabels();
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private void HandlePreviewTimerTick(object? sender, EventArgs eventArgs)
    {
        _previewTimer.Stop();
        _preview(BuildCurrentSettings());
    }

    private void HandleMonitorBrightnessValueChanged(object? sender, EventArgs eventArgs)
    {
        var binding = _monitorBrightnessBindings.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Slider, sender));
        if (binding is null)
        {
            return;
        }

        UpdateValueLabels();
        _pendingMonitorBrightness[binding.Monitor.Id] = binding.Slider.Value;
        _monitorBrightnessTimer.Stop();
        _monitorBrightnessTimer.Start();
    }

    private void HandleMonitorBrightnessTimerTick(object? sender, EventArgs eventArgs)
    {
        _monitorBrightnessTimer.Stop();
        ApplyPendingMonitorBrightness();
    }

    private void ApplyPendingMonitorBrightness()
    {
        var pendingChanges = _pendingMonitorBrightness.ToArray();
        _pendingMonitorBrightness.Clear();
        foreach (var (monitorId, brightnessPercent) in pendingChanges)
        {
            var succeeded = _setMonitorBrightness(monitorId, brightnessPercent);
            var binding = _monitorBrightnessBindings.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Monitor.Id,
                    monitorId,
                    StringComparison.OrdinalIgnoreCase));
            if (binding is not null)
            {
                binding.ValueLabel.ForeColor = succeeded ? Accent : ErrorText;
            }
        }
    }

    private void HandleShown(object? sender, EventArgs eventArgs)
    {
        FitWindowToContent();
    }

    private void FitWindowToContent()
    {
        var workingArea = Screen.FromControl(this).WorkingArea;
        var targetWidth = Math.Min(820, Math.Max(MinimumSize.Width, workingArea.Width - 32));
        Width = targetWidth;
        UpdateWrappingWidths();
        PerformLayout();

        var nonClientHeight = Height - ClientSize.Height;
        var desiredClientHeight =
            _contentPanel.PreferredSize.Height +
            _scrollPanel.Padding.Vertical +
            FooterHeight;
        Height = Math.Min(
            workingArea.Height - 32,
            Math.Max(MinimumSize.Height, desiredClientHeight + nonClientHeight));
        CenterToScreen();
    }

    private void UpdateWrappingWidths()
    {
        var maximumWidth = Math.Max(280, _scrollPanel.ClientSize.Width - 76);
        foreach (var label in _wrappingLabels)
        {
            label.MaximumSize = new Size(maximumWidth, 0);
        }
    }

    private void UpdateValueLabels()
    {
        _gammaValueLabel.Text = (_gammaSlider.Value / 100d).ToString("0.00", CultureInfo.InvariantCulture);
        _contrastValueLabel.Text = $"{_contrastSlider.Value}%";
        _saturationValueLabel.Text = $"{_saturationSlider.Value}%";
        _brightnessValueLabel.Text = $"{_brightnessSlider.Value}%";
        foreach (var binding in _monitorBrightnessBindings)
        {
            binding.ValueLabel.Text = $"{binding.Slider.Value}%";
        }
    }

    private AppSettings BuildCurrentSettings()
    {
        var settings = _initialSettings.Clone();
        settings.Enabled = _enabledCheckBox.Checked;
        settings.GammaPercent = _gammaSlider.Value;
        settings.ContrastPercent = _contrastSlider.Value;
        settings.SaturationPercent = _saturationSlider.Value;
        settings.BrightnessBoostPercent = _brightnessSlider.Value;
        foreach (var binding in _monitorBrightnessBindings)
        {
            settings.MonitorBrightnessPercent[binding.Monitor.Id] = binding.Slider.Value;
        }

        settings.ToggleHotkey = _toggleHotkeyTextBox.Hotkey;
        settings.OpenSettingsHotkey = _openSettingsHotkeyTextBox.Hotkey;
        settings.Normalize();
        return settings;
    }

    private void SetNeutralValues()
    {
        _enabledCheckBox.Checked = true;
        _gammaSlider.Value = 100;
        _contrastSlider.Value = 100;
        _saturationSlider.Value = 100;
        _brightnessSlider.Value = 0;
        _previewTimer.Stop();
        _preview(BuildCurrentSettings());
    }

    private void SaveAndClose()
    {
        _previewTimer.Stop();
        _monitorBrightnessTimer.Stop();
        ApplyPendingMonitorBrightness();
        var settings = BuildCurrentSettings();
        if (!settings.ToggleHotkey.IsValid || !settings.OpenSettingsHotkey.IsValid)
        {
            MessageBox.Show(
                this,
                "Both shortcuts are required. Letter and number keys must include Ctrl, Alt, Shift, or Win.",
                "Invalid shortcut",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (settings.ToggleHotkey.Equals(settings.OpenSettingsHotkey))
        {
            MessageBox.Show(
                this,
                "Toggle enhancement and Open settings cannot use the same shortcut.",
                "Duplicate shortcut",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        ResultSettings = settings;
        _preview(ResultSettings);
        _saved = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void CancelAndClose()
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        _previewTimer.Stop();
        _monitorBrightnessTimer.Stop();
        _pendingMonitorBrightness.Clear();
        if (!_saved)
        {
            foreach (var (monitorId, brightnessPercent) in _initialMonitorBrightness)
            {
                _setMonitorBrightness(monitorId, brightnessPercent);
            }

            _preview(_initialSettings.Clone());
        }
    }

    protected override void OnHandleCreated(EventArgs eventArgs)
    {
        base.OnHandleCreated(eventArgs);
        var darkModeEnabled = 1;
        const int useImmersiveDarkMode = 20;
        const int useImmersiveDarkModeBefore20H1 = 19;
        if (NativeMethods.DwmSetWindowAttribute(
                Handle,
                useImmersiveDarkMode,
                ref darkModeEnabled,
                sizeof(int)) != 0)
        {
            NativeMethods.DwmSetWindowAttribute(
                Handle,
                useImmersiveDarkModeBefore20H1,
                ref darkModeEnabled,
                sizeof(int));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _previewTimer.Dispose();
            _monitorBrightnessTimer.Dispose();
            _windowIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
