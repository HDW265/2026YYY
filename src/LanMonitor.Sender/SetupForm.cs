namespace LanMonitor.Sender;

/// <summary>首次运行配置向导。</summary>
internal sealed class SetupForm : Form
{
    private readonly TextBox _host = new();
    private readonly NumericUpDown _port = new();
    private readonly NumericUpDown _fps = new();
    private readonly NumericUpDown _quality = new();
    private readonly NumericUpDown _maxEdge = new();
    private readonly TextBox _password = new();
    private readonly TextBox _password2 = new();

    public SenderSettings Result { get; private set; } = new();

    public SetupForm()
    {
        Text = "SF_link 首次设置";
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 520);
        MinimumSize = new Size(560, 520);
        Font = new Font("Microsoft YaHei UI", 9.75F);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(20, 16, 20, 16)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 7; i++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        }

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        AddLabeled(root, 0, "接收端 IP", StyleBox(_host, "127.0.0.1"));
        AddLabeled(root, 1, "端口", StyleNum(_port, 1, 65535, 19730));
        AddLabeled(root, 2, "帧率", StyleNum(_fps, 1, 30, 5));
        AddLabeled(root, 3, "质量", StyleNum(_quality, 1, 100, 60));
        AddLabeled(root, 4, "最长边", StyleNum(_maxEdge, 0, 7680, 1280));
        _maxEdge.Increment = 80;
        AddLabeled(root, 5, "管理密码", StyleBox(_password, usePassword: true));
        AddLabeled(root, 6, "确认密码", StyleBox(_password2, usePassword: true));

        var hint = new Label
        {
            Text = "最长边填 0 表示不缩放。\n热键 Ctrl+Shift+Alt+M（无托盘，仅热键调出）。\n重连：持续自动重连（收端下线再上线仍会连上）。",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(180, 180, 190)
        };
        root.Controls.Add(hint, 0, 7);
        root.SetColumnSpan(hint, 2);

        var tip = new Label
        {
            Text = "进程名 SF_link · 配置在 %ProgramData%\\SF_link（本机各用户共用）",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(140, 140, 150)
        };
        root.Controls.Add(tip, 0, 8);
        root.SetColumnSpan(tip, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var cancel = new Button
        {
            Text = "退出",
            Width = 88,
            Height = 34,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 78),
            ForeColor = Color.Gainsboro,
            Margin = new Padding(8, 4, 0, 4)
        };
        cancel.FlatAppearance.BorderSize = 0;
        var save = new Button
        {
            Text = "保存并启动",
            Width = 120,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 78),
            ForeColor = Color.Gainsboro,
            Margin = new Padding(8, 4, 0, 4)
        };
        save.FlatAppearance.BorderSize = 0;
        save.Click += (_, _) => OnSave();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);
        root.Controls.Add(buttons, 0, 9);
        root.SetColumnSpan(buttons, 2);

        Controls.Add(root);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void OnSave()
    {
        if (string.IsNullOrWhiteSpace(_host.Text))
        {
            MessageBox.Show(this, "请填写接收端 IP。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_password.Text))
        {
            MessageBox.Show(this, "必须设置管理密码。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_password.Text != _password2.Text)
        {
            MessageBox.Show(this, "两次密码不一致。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var settings = new SenderSettings
        {
            Host = _host.Text.Trim(),
            Port = (int)_port.Value,
            Fps = (int)_fps.Value,
            Quality = (int)_quality.Value,
            MaxEdge = (int)_maxEdge.Value,
            Streaming = true,
            AutoReconnect = true,
            ContinuousReconnect = true,
            MaxReconnectAttempts = 5,
            HotkeyModifiers = NativeHotkey.ModControl | NativeHotkey.ModShift | NativeHotkey.ModAlt,
            HotkeyVirtualKey = (int)Keys.M,
            Configured = true
        };
        PasswordHasher.SetPassword(settings, _password.Text);
        settings.Save();
        Result = settings;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static void AddLabeled(TableLayoutPanel root, int row, string caption, Control input)
    {
        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false
        };
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(8, 6, 0, 6);
        root.Controls.Add(label, 0, row);
        root.Controls.Add(input, 1, row);
    }

    private static TextBox StyleBox(TextBox box, string? text = null, bool usePassword = false)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = Color.FromArgb(58, 58, 64);
        box.ForeColor = Color.Gainsboro;
        if (text is not null) box.Text = text;
        box.UseSystemPasswordChar = usePassword;
        return box;
    }

    private static NumericUpDown StyleNum(NumericUpDown box, decimal min, decimal max, decimal value)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = Color.FromArgb(58, 58, 64);
        box.ForeColor = Color.Gainsboro;
        box.ThousandsSeparator = false;
        box.Minimum = min;
        box.Maximum = max;
        box.Value = value;
        return box;
    }
}
