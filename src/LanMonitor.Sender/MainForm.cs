using LanMonitor.Core;

namespace LanMonitor.Sender;

public sealed class MainForm : Form
{
    private static readonly Color BarBack = Color.FromArgb(45, 45, 50);
    private static readonly Color FormBack = Color.FromArgb(32, 32, 36);
    private static readonly Color TextColor = Color.Gainsboro;
    private static readonly Color InputBack = Color.FromArgb(58, 58, 64);
    private static readonly Color ButtonBack = Color.FromArgb(70, 70, 78);

    private readonly StreamSession _session = new();
    private readonly Queue<string> _logLines = new();
    private readonly FrameRateCounter _sendFps = new();

    private readonly TextBox _host = new();
    private readonly NumericUpDown _port = new();
    private readonly Button _btnConnect = new();
    private readonly CheckBox _chkAutoReconnect = new();
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

    public MainForm()
    {
        Text = "局域网监控 · 发送端";
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(900, 560);
        Size = new Size(980, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = FormBack;
        ForeColor = TextColor;
        Font = new Font("Microsoft YaHei UI", 9.75F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildConnectBar(), 0, 0);
        root.Controls.Add(BuildCaptureBar(), 0, 1);
        root.Controls.Add(BuildStatusBar(), 0, 2);
        root.Controls.Add(BuildLog(), 0, 3);

        _session.Log += msg => BeginInvokeSafe(() => AppendLog(msg));
        _session.StateChanged += state => BeginInvokeSafe(() => OnState(state));
        _session.FrameSent += (bytes, _) => BeginInvokeSafe(() => OnFrameSent(bytes));

        FormClosing += (_, _) => _session.Dispose();
        AppendLog("就绪。接收端先监听，再填主机 IP / 端口点「连接」。协议：长度前缀 JPEG。");
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
        _host.Text = "127.0.0.1";
        _host.Dock = DockStyle.Fill;
        _host.Margin = new Padding(8, 8, 8, 8);
        _host.MinimumSize = new Size(160, 28);
        row1.Controls.Add(_host, 1, 0);

        row1.Controls.Add(MakeCaption("端口"), 2, 0);
        StyleNumeric(_port);
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Value = 19730;
        _port.ThousandsSeparator = false;
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
            ColumnCount = 5,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _chkAutoReconnect.Text = "自动重连";
        _chkAutoReconnect.Checked = true;
        _chkAutoReconnect.AutoSize = true;
        _chkAutoReconnect.Margin = new Padding(0, 10, 16, 0);
        _chkAutoReconnect.ForeColor = TextColor;
        _chkAutoReconnect.CheckedChanged += (_, _) => _session.AutoReconnect = _chkAutoReconnect.Checked;
        row2.Controls.Add(_chkAutoReconnect, 0, 0);

        row2.Controls.Add(MakeCaption("重连次数"), 1, 0);
        StyleNumeric(_reconnectMax);
        _reconnectMax.Minimum = 0;
        _reconnectMax.Maximum = 100;
        _reconnectMax.Value = 5;
        _reconnectMax.ThousandsSeparator = false;
        _reconnectMax.Dock = DockStyle.Fill;
        _reconnectMax.Margin = new Padding(8, 8, 8, 8);
        _reconnectMax.MinimumSize = new Size(90, 28);
        _reconnectMax.ValueChanged += (_, _) => _session.MaxReconnectAttempts = (int)_reconnectMax.Value;
        row2.Controls.Add(_reconnectMax, 2, 0);

        _reconnectUsed.Text = "已用 0 / 剩余 5";
        _reconnectUsed.AutoSize = true;
        _reconnectUsed.Margin = new Padding(8, 10, 0, 0);
        _reconnectUsed.AutoEllipsis = false;
        row2.Controls.Add(_reconnectUsed, 3, 0);

        _status.Text = "状态 未连接";
        _status.AutoSize = true;
        _status.Margin = new Padding(24, 10, 0, 0);
        _status.AutoEllipsis = false;
        row2.Controls.Add(_status, 4, 0);

        bar.Controls.Add(row1, 0, 0);
        bar.Controls.Add(row2, 0, 1);
        _session.AutoReconnect = true;
        _session.MaxReconnectAttempts = 5;
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
        _fps.Value = 5;
        _fps.Dock = DockStyle.Fill;
        _fps.Margin = new Padding(8, 8, 8, 8);
        _fps.ValueChanged += (_, _) => _session.Fps = (int)_fps.Value;
        row1.Controls.Add(_fps, 1, 0);

        row1.Controls.Add(MakeCaption("质量"), 2, 0);
        StyleNumeric(_quality);
        _quality.Minimum = 1;
        _quality.Maximum = 100;
        _quality.Value = 60;
        _quality.Dock = DockStyle.Fill;
        _quality.Margin = new Padding(8, 8, 8, 8);
        _quality.ValueChanged += (_, _) => _session.Quality = (int)_quality.Value;
        row1.Controls.Add(_quality, 3, 0);

        row1.Controls.Add(MakeCaption("最长边"), 4, 0);
        StyleNumeric(_maxEdge);
        _maxEdge.Minimum = 0;
        _maxEdge.Maximum = 7680;
        _maxEdge.Increment = 80;
        _maxEdge.Value = 1280;
        _maxEdge.ThousandsSeparator = false;
        _maxEdge.Dock = DockStyle.Fill;
        _maxEdge.Margin = new Padding(8, 8, 8, 8);
        _maxEdge.ValueChanged += (_, _) => _session.MaxEdge = (int)_maxEdge.Value;
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
        _chkStream.CheckedChanged += (_, _) => _session.Streaming = _chkStream.Checked;
        row2.Controls.Add(_chkStream);

        _session.Fps = 5;
        _session.Quality = 60;
        _session.MaxEdge = 1280;
        _session.Streaming = true;

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
        _lastFrame.AutoEllipsis = false;
        bar.Controls.Add(_lastFrame, 0, 0);

        _fpsLabel.Text = "发送 0 fps";
        _fpsLabel.AutoSize = true;
        _fpsLabel.Margin = new Padding(0, 6, 0, 0);
        _fpsLabel.AutoEllipsis = false;
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

    private void ToggleConnect()
    {
        if (_session.IsRunning)
        {
            _session.Stop();
            _btnConnect.Text = "连接";
            _host.Enabled = true;
            _port.Enabled = true;
            RefreshReconnectLabel();
            return;
        }

        var host = _host.Text.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            AppendLog("请填写主机 IP。");
            return;
        }

        _session.Host = host;
        _session.Port = (int)_port.Value;
        _session.AutoReconnect = _chkAutoReconnect.Checked;
        _session.MaxReconnectAttempts = (int)_reconnectMax.Value;
        _session.Fps = (int)_fps.Value;
        _session.Quality = (int)_quality.Value;
        _session.MaxEdge = (int)_maxEdge.Value;
        _session.Streaming = _chkStream.Checked;
        _btnConnect.Text = "断开";
        _host.Enabled = false;
        _port.Enabled = false;
        _session.Start();
        RefreshReconnectLabel();
    }

    private void OnState(SenderState state)
    {
        _status.Text = state switch
        {
            SenderState.Connecting => "状态 连接中",
            SenderState.Connected => "状态 已连接",
            SenderState.Reconnecting => $"状态 重连中 ({_session.ReconnectAttemptsUsed}/{_session.MaxReconnectAttempts})",
            SenderState.StoppedAtLimit => "状态 已达重连上限",
            _ => "状态 未连接"
        };

        if (state is SenderState.Idle or SenderState.StoppedAtLimit)
        {
            _btnConnect.Text = "连接";
            _host.Enabled = true;
            _port.Enabled = true;
        }

        RefreshReconnectLabel();
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
        _reconnectUsed.Text =
            $"已用 {_session.ReconnectAttemptsUsed} / 剩余 {_session.ReconnectAttemptsRemaining}";
    }

    private void AppendLog(string line)
    {
        _logLines.Enqueue($"[{DateTime.Now:HH:mm:ss}] {line}");
        while (_logLines.Count > 12)
        {
            _logLines.Dequeue();
        }

        _log.Text = string.Join(Environment.NewLine, _logLines);
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
        RefreshReconnectLabel();
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
