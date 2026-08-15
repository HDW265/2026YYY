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
    private readonly Label _hotkeyHint = new();

    public SenderSettings Result { get; private set; } = new();

    public SetupForm()
    {
        Text = "局域网监控 · 发送端首次设置";
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 420);
        Font = new Font("Microsoft YaHei UI", 9.75F);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;

        var y = 16;
        void AddRow(string caption, Control input, int height = 28)
        {
            var label = new Label
            {
                Text = caption,
                AutoSize = true,
                Location = new Point(24, y + 4)
            };
            input.Location = new Point(160, y);
            input.Width = 320;
            input.Height = height;
            Controls.Add(label);
            Controls.Add(input);
            y += height + 14;
        }

        StyleBox(_host);
        _host.Text = "127.0.0.1";
        AddRow("接收端 IP", _host);

        StyleNum(_port);
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Value = 19730;
        AddRow("端口", _port);

        StyleNum(_fps);
        _fps.Minimum = 1;
        _fps.Maximum = 30;
        _fps.Value = 5;
        AddRow("帧率", _fps);

        StyleNum(_quality);
        _quality.Minimum = 1;
        _quality.Maximum = 100;
        _quality.Value = 60;
        AddRow("质量", _quality);

        StyleNum(_maxEdge);
        _maxEdge.Minimum = 0;
        _maxEdge.Maximum = 7680;
        _maxEdge.Increment = 80;
        _maxEdge.Value = 1280;
        AddRow("最长边(0不缩放)", _maxEdge);

        StyleBox(_password);
        _password.UseSystemPasswordChar = true;
        AddRow("管理密码", _password);

        StyleBox(_password2);
        _password2.UseSystemPasswordChar = true;
        AddRow("确认密码", _password2);

        _hotkeyHint.Text = "热键：Ctrl+Shift+Alt+M（保存后无托盘，仅热键调出）\n重连：持续自动重连（收端下线再上线仍会连上）";
        _hotkeyHint.AutoSize = true;
        _hotkeyHint.Location = new Point(24, y);
        _hotkeyHint.MaximumSize = new Size(460, 0);
        Controls.Add(_hotkeyHint);
        y += 56;

        var save = new Button
        {
            Text = "保存并启动",
            Width = 120,
            Height = 34,
            Location = new Point(260, y),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 78),
            ForeColor = Color.Gainsboro
        };
        save.Click += (_, _) => OnSave();

        var cancel = new Button
        {
            Text = "退出",
            Width = 80,
            Height = 34,
            Location = new Point(400, y),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 78),
            ForeColor = Color.Gainsboro
        };

        Controls.Add(save);
        Controls.Add(cancel);
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

    private static void StyleBox(TextBox box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = Color.FromArgb(58, 58, 64);
        box.ForeColor = Color.Gainsboro;
    }

    private static void StyleNum(NumericUpDown box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = Color.FromArgb(58, 58, 64);
        box.ForeColor = Color.Gainsboro;
        box.ThousandsSeparator = false;
    }
}
