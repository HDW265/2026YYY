using LanMonitor.Core;

namespace LanMonitor.Sender;

internal sealed class MainForm : Form
{
    private static readonly Color BarBack = Color.FromArgb(45, 45, 50);
    private static readonly Color FormBack = Color.FromArgb(32, 32, 36);
    private static readonly Color TextColor = Color.Gainsboro;
    private static readonly Color InputBack = Color.FromArgb(58, 58, 64);
    private static readonly Color ButtonBack = Color.FromArgb(70, 70, 78);

    private readonly StreamSession _session;
    private readonly SenderAppContext _app;
    private readonly Queue<string> _logLines = new();
    private readonly FrameRateCounter _sendFps = new();

    private readonly TextBox _host = new();
    private readonly NumericUpDown _port = new();
    private readonly Button _btnConnect = new();
    private readonly CheckBox _chkAutoReconnect = new();
    private readonly CheckBox _chkContinuous = new();
    private readonly NumericUpDown _reconnectMax = new();
    private readonly Label _reconnectUsed = new();
    private readonly Label _status = new();
    private readonly NumericUpDown _fps = new();
    private readonly NumericUpDown _quality = new();
    private readonly NumericUpDown _maxEdge = new();
    private readonly CheckBox _chkStream = new();
    private readonly Label _lastFrame = new();
    private readonly Label _fpsLabel = new();
    private readonly TextBox _log = new();
    private readonly TextBox _newPassword = new();
    private readonly TextBox _newPassword2 = new();
    private readonly Label _hotkeyLabel = new();
    private readonly CheckBox _chkAutoStart = new();
    private bool _allowClose;
    private bool _syncingAutoStart;

    public MainForm(SenderAppContext app, StreamSession session)
    {
        _app = app;
        _session = session;

        Text = "SF_link 设置";
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(920, 720);
        Size = new Size(1000, 780);
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        BackColor = FormBack;
        ForeColor = TextColor;
        Font = new Font("Microsoft YaHei UI", 9.75F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildConnectBar(), 0, 0);
        root.Controls.Add(BuildCaptureBar(), 0, 1);
        root.Controls.Add(BuildSecurityBar(), 0, 2);
        root.Controls.Add(BuildStatusBar(), 0, 3);
        root.Controls.Add(BuildLog(), 0, 4);

        _session.Log += msg => BeginInvokeSafe(() => AppendLog(msg));
        _session.StateChanged += state => BeginInvokeSafe(() => OnState(state));
        _session.FrameSent += (bytes, _) => BeginInvokeSafe(() => OnFrameSent(bytes));

        LoadFromSettings(_app.Settings);
        SyncAutoStartCheckbox();
        Shown += (_, _) => SyncAutoStartCheckbox();
        FormClosing += OnFormClosing;
        AppendLog("关闭窗口=隐藏（无托盘）。热键 " +
                  NativeHotkey.Describe(_app.Settings.HotkeyModifiers, _app.Settings.HotkeyVirtualKey) +
                  " + 密码可再打开。点「退出程序」才结束。配置：%AppData%\\SF_link");
    }

    public void RequestExit()
    {
        _allowClose = true;
        Close();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        AppendLog("已隐藏。进程仍在后台推流/重连。");
    }

    private Control BuildConnectBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BarBack,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0)
        };
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var row1 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        row1.Controls.Add(MakeCaption("主机"), 0, 0);
        StyleTextBox(_host);
        _host.Dock = DockStyle.Fill;
        _host.Margin = new Padding(8, 8, 8, 8);
        _host.MinimumSize = new Size(160, 28);
        row1.Controls.Add(_host, 1, 0);

        row1.Controls.Add(MakeCaption("端口"), 2, 0);
        StyleNumeric(_port);
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Dock = DockStyle.Fill;
        _port.Margin = new Padding(8, 8, 0, 8);
        _port.MinimumSize = new Size(120, 28);
        row1.Controls.Add(_port, 3, 0);

        StyleButton(_btnConnect, "连接");
        _btnConnect.Click += (_, _) => ToggleConnect();
        row1.Controls.Add(_btnConnect, 5, 0);

        var row2 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _chkAutoReconnect.Text = "自动重连";
        _chkAutoReconnect.Checked = true;
        _chkAutoReconnect.AutoSize = true;
        _chkAutoReconnect.Margin = new Padding(0, 10, 16, 0);
        _chkAutoReconnect.ForeColor = TextColor;
        row2.Controls.Add(_chkAutoReconnect, 0, 0);

        _chkContinuous.Text = "持续重连";
        _chkContinuous.Checked = true;
        _chkContinuous.AutoSize = true;
        _chkContinuous.Margin = new Padding(0, 10, 16, 0);
        _chkContinuous.ForeColor = TextColor;
        _chkContinuous.CheckedChanged += (_, _) =>
        {
            _reconnectMax.Enabled = !_chkContinuous.Checked;
        };
        row2.Controls.Add(_chkContinuous, 1, 0);

        row2.Controls.Add(MakeCaption("有限次数"), 2, 0);
        StyleNumeric(_reconnectMax);
        _reconnectMax.Minimum = 0;
        _reconnectMax.Maximum = 100;
        _reconnectMax.Value = 5;
        _reconnectMax.Enabled = false;
        _reconnectMax.Dock = DockStyle.Fill;
        _reconnectMax.Margin = new Padding(8, 8, 8, 8);
        row2.Controls.Add(_reconnectMax, 3, 0);

        _reconnectUsed.Text = "已尝试 0";
        _reconnectUsed.AutoSize = true;
        _reconnectUsed.Margin = new Padding(8, 10, 0, 0);
        _reconnectUsed.AutoEllipsis = false;
        row2.Controls.Add(_reconnectUsed, 4, 0);

        _status.Text = "状态 未连接";
        _status.AutoSize = true;
        _status.Margin = new Padding(24, 10, 0, 0);
        _status.AutoEllipsis = false;
        row2.Controls.Add(_status, 5, 0);

        bar.Controls.Add(row1, 0, 0);
        bar.Controls.Add(row2, 0, 1);
        return bar;
    }

    private Control BuildCaptureBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BarBack,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0)
        };
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var row1 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        row1.Controls.Add(MakeCaption("帧率"), 0, 0);
        StyleNumeric(_fps);
        _fps.Minimum = 1;
        _fps.Maximum = 30;
        _fps.Dock = DockStyle.Fill;
        _fps.Margin = new Padding(8, 8, 8, 8);
        row1.Controls.Add(_fps, 1, 0);

        row1.Controls.Add(MakeCaption("质量"), 2, 0);
        StyleNumeric(_quality);
        _quality.Minimum = 1;
        _quality.Maximum = 100;
        _quality.Dock = DockStyle.Fill;
        _quality.Margin = new Padding(8, 8, 8, 8);
        row1.Controls.Add(_quality, 3, 0);

        row1.Controls.Add(MakeCaption("最长边"), 4, 0);
        StyleNumeric(_maxEdge);
        _maxEdge.Minimum = 0;
        _maxEdge.Maximum = 7680;
        _maxEdge.Increment = 80;
        _maxEdge.Dock = DockStyle.Fill;
        _maxEdge.Margin = new Padding(8, 8, 8, 8);
        row1.Controls.Add(_maxEdge, 5, 0);

        var tip = MakeCaption("0=不缩放");
        tip.ForeColor = Color.FromArgb(160, 160, 170);
        row1.Controls.Add(tip, 7, 0);

        var row2 = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        _chkStream.Text = "推流开";
        _chkStream.Checked = true;
        _chkStream.AutoSize = true;
        _chkStream.Margin = new Padding(0, 10, 20, 0);
        _chkStream.ForeColor = TextColor;
        row2.Controls.Add(_chkStream);

        bar.Controls.Add(row1, 0, 0);
        bar.Controls.Add(row2, 0, 1);
        return bar;
    }

    private Control BuildSecurityBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BarBack,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0)
        };
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var row1 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        row1.Controls.Add(MakeCaption("新密码"), 0, 0);
        StyleTextBox(_newPassword);
        _newPassword.UseSystemPasswordChar = true;
        _newPassword.Dock = DockStyle.Fill;
        _newPassword.Margin = new Padding(8, 8, 8, 8);
        row1.Controls.Add(_newPassword, 1, 0);

        row1.Controls.Add(MakeCaption("确认"), 2, 0);
        StyleTextBox(_newPassword2);
        _newPassword2.UseSystemPasswordChar = true;
        _newPassword2.Dock = DockStyle.Fill;
        _newPassword2.Margin = new Padding(8, 8, 8, 8);
        row1.Controls.Add(_newPassword2, 3, 0);

        var save = new Button();
        StyleButton(save, "保存配置");
        save.Click += (_, _) => SaveSettingsFromUi(restartSession: true);
        row1.Controls.Add(save, 5, 0);

        var row2 = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0)
        };
        _hotkeyLabel.AutoSize = true;
        _hotkeyLabel.Margin = new Padding(0, 10, 20, 0);
        _hotkeyLabel.ForeColor = TextColor;
        row2.Controls.Add(_hotkeyLabel);

        _chkAutoStart.Text = "开机自启";
        _chkAutoStart.AutoSize = true;
        _chkAutoStart.Margin = new Padding(0, 10, 16, 0);
        _chkAutoStart.ForeColor = TextColor;
        _chkAutoStart.CheckedChanged += (_, _) => OnAutoStartChanged();
        row2.Controls.Add(_chkAutoStart);

        var hide = new Button
        {
            Text = "隐藏窗口",
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ButtonBack,
            ForeColor = TextColor,
            Margin = new Padding(0, 4, 12, 4)
        };
        hide.FlatAppearance.BorderSize = 0;
        hide.Click += (_, _) => Hide();
        row2.Controls.Add(hide);

        var exit = new Button
        {
            Text = "退出程序",
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(110, 55, 55),
            ForeColor = TextColor,
            Margin = new Padding(0, 4, 12, 4)
        };
        exit.FlatAppearance.BorderSize = 0;
        exit.Click += (_, _) =>
        {
            if (MessageBox.Show(this, "确定退出发送端？将停止推流与重连。", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _app.ExitApplication();
            }
        };
        row2.Controls.Add(exit);

        bar.Controls.Add(row1, 0, 0);
        bar.Controls.Add(row2, 0, 1);
        return bar;
    }

    private Control BuildStatusBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 44),
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0)
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _lastFrame.Text = "最近发送 --";
        _lastFrame.AutoSize = true;
        _lastFrame.Margin = new Padding(0, 6, 0, 0);
        bar.Controls.Add(_lastFrame, 0, 0);

        _fpsLabel.Text = "发送 0 fps";
        _fpsLabel.AutoSize = true;
        _fpsLabel.Margin = new Padding(0, 6, 0, 0);
        bar.Controls.Add(_fpsLabel, 1, 0);
        return bar;
    }

    private Control BuildLog()
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(28, 28, 30),
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0)
        };
        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.BorderStyle = BorderStyle.None;
        _log.BackColor = Color.FromArgb(28, 28, 30);
        _log.ForeColor = Color.FromArgb(200, 200, 200);
        host.Controls.Add(_log);
        return host;
    }

    private void SyncAutoStartCheckbox()
    {
        _syncingAutoStart = true;
        try
        {
            _chkAutoStart.Checked = AutoStartService.IsEnabledForThisExe();
        }
        finally
        {
            _syncingAutoStart = false;
        }
    }

    private void OnAutoStartChanged()
    {
        if (_syncingAutoStart)
        {
            return;
        }

        var want = _chkAutoStart.Checked;
        if (!AutoStartService.TryApplyWithElevation(want, out var error))
        {
            var detail = string.IsNullOrEmpty(error)
                ? "修改开机自启失败（原因未知）。"
                : error;
            MessageBox.Show(this, detail, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SyncAutoStartCheckbox();
            AppendLog("开机自启未更改：" + detail.Replace('\n', ' '));
            return;
        }

        AppendLog(want
            ? "已开启开机自启（HKLM\\...\\Run\\SF_link）。"
            : "已关闭开机自启。");
        SyncAutoStartCheckbox();
    }

    private void LoadFromSettings(SenderSettings s)
    {
        _host.Text = s.Host;
        _port.Value = Math.Clamp(s.Port, 1, 65535);
        _fps.Value = Math.Clamp(s.Fps, 1, 30);
        _quality.Value = Math.Clamp(s.Quality, 1, 100);
        _maxEdge.Value = Math.Clamp(s.MaxEdge, 0, 7680);
        _chkStream.Checked = s.Streaming;
        _chkAutoReconnect.Checked = s.AutoReconnect;
        _chkContinuous.Checked = s.ContinuousReconnect;
        _reconnectMax.Value = Math.Clamp(s.MaxReconnectAttempts, 0, 100);
        _reconnectMax.Enabled = !s.ContinuousReconnect;
        _hotkeyLabel.Text = "热键 " + NativeHotkey.Describe(s.HotkeyModifiers, s.HotkeyVirtualKey) +
                            "（无托盘，仅热键调出）";
        SyncConnectButton();
        OnState(_session.State);
    }

    private void SaveSettingsFromUi(bool restartSession)
    {
        if (string.IsNullOrWhiteSpace(_host.Text))
        {
            MessageBox.Show(this, "请填写主机 IP。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!string.IsNullOrEmpty(_newPassword.Text) || !string.IsNullOrEmpty(_newPassword2.Text))
        {
            if (_newPassword.Text != _newPassword2.Text)
            {
                MessageBox.Show(this, "两次新密码不一致。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_newPassword.Text))
            {
                MessageBox.Show(this, "新密码不能为空。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PasswordHasher.SetPassword(_app.Settings, _newPassword.Text);
            _newPassword.Clear();
            _newPassword2.Clear();
        }

        var s = _app.Settings;
        s.Host = _host.Text.Trim();
        s.Port = (int)_port.Value;
        s.Fps = (int)_fps.Value;
        s.Quality = (int)_quality.Value;
        s.MaxEdge = (int)_maxEdge.Value;
        s.Streaming = _chkStream.Checked;
        s.AutoReconnect = _chkAutoReconnect.Checked;
        s.ContinuousReconnect = _chkContinuous.Checked;
        s.MaxReconnectAttempts = (int)_reconnectMax.Value;
        s.Configured = true;
        s.Save();

        _session.ApplySettings(s);
        AppendLog("配置已保存：" + SenderSettings.ConfigPath);

        if (restartSession)
        {
            if (_session.IsRunning)
            {
                _session.Stop();
            }

            _session.Start();
            SyncConnectButton();
        }
    }

    private void ToggleConnect()
    {
        if (_session.IsRunning)
        {
            _session.Stop();
            SyncConnectButton();
            return;
        }

        SaveSettingsFromUi(restartSession: false);
        _session.ApplySettings(_app.Settings);
        _session.Start();
        SyncConnectButton();
    }

    private void SyncConnectButton()
    {
        _btnConnect.Text = _session.IsRunning ? "断开" : "连接";
        _host.Enabled = !_session.IsRunning;
        _port.Enabled = !_session.IsRunning;
    }

    private void OnState(SenderState state)
    {
        _status.Text = state switch
        {
            SenderState.Connecting => "状态 连接中",
            SenderState.Connected => "状态 已连接",
            SenderState.Reconnecting => _chkContinuous.Checked
                ? $"状态 持续重连中（已尝试 {_session.ReconnectAttemptsUsed}）"
                : $"状态 重连中（{_session.ReconnectAttemptsUsed}）",
            SenderState.StoppedAtLimit => "状态 已达重连上限",
            _ => "状态 未连接"
        };
        RefreshReconnectLabel();
        SyncConnectButton();
    }

    private void OnFrameSent(int bytes)
    {
        _sendFps.Tick();
        _lastFrame.Text = $"最近发送 {bytes / 1024.0:0.0} KB";
        _fpsLabel.Text = $"发送 {_sendFps.Fps:0.0} fps";
        RefreshReconnectLabel();
    }

    private void RefreshReconnectLabel()
    {
        _reconnectUsed.Text = _chkContinuous.Checked
            ? $"本轮失败重试 {_session.ReconnectAttemptsUsed}"
            : $"已用 {_session.ReconnectAttemptsUsed} / 剩余 {_session.ReconnectAttemptsRemaining}";
    }

    private void AppendLog(string line)
    {
        _logLines.Enqueue($"[{DateTime.Now:HH:mm:ss}] {line}");
        while (_logLines.Count > 14)
        {
            _logLines.Dequeue();
        }

        _log.Text = string.Join(Environment.NewLine, _logLines);
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private void BeginInvokeSafe(Action action)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try { BeginInvoke(action); } catch { /* closing */ }
            return;
        }

        action();
    }

    private static Label MakeCaption(string text, int padLeft = 0, int padRight = 8) => new()
    {
        Text = text,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = TextColor,
        Margin = new Padding(padLeft, 10, padRight, 0),
        AutoEllipsis = false
    };

    private static void StyleButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = ButtonBack;
        button.ForeColor = TextColor;
        button.Margin = new Padding(0, 6, 0, 6);
        button.Cursor = Cursors.Hand;
        button.MinimumSize = new Size(100, 32);
        button.AutoEllipsis = false;
    }

    private static void StyleNumeric(NumericUpDown box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = InputBack;
        box.ForeColor = TextColor;
        box.TextAlign = HorizontalAlignment.Left;
        box.ThousandsSeparator = false;
    }

    private static void StyleTextBox(TextBox box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = InputBack;
        box.ForeColor = TextColor;
    }
}
