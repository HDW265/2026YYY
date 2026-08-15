using LanMonitor.Core;

namespace LanMonitor.Receiver;

public sealed class MainForm : Form
{
    private static readonly Color BarBack = Color.FromArgb(45, 45, 50);
    private static readonly Color FormBack = Color.FromArgb(32, 32, 36);
    private static readonly Color TextColor = Color.Gainsboro;
    private static readonly Color InputBack = Color.FromArgb(58, 58, 64);
    private static readonly Color ButtonBack = Color.FromArgb(70, 70, 78);

    private readonly TcpReceiveServer _server = new();
    private readonly SaveScheduler _saveScheduler = new();
    private readonly FrameRateCounter _fps = new();
    private readonly Queue<string> _logLines = new();

    private readonly Label _listenDot = new();
    private readonly NumericUpDown _port = new();
    private readonly Button _btnListen = new();
    private readonly Label _client = new();
    private readonly Label _fpsLabel = new();
    private readonly Label _frameSize = new();
    private readonly CheckBox _chkReceive = new();
    private readonly Button _btnDisconnect = new();
    private readonly RadioButton _radioZoom = new();
    private readonly RadioButton _radioStretch = new();
    private readonly TextBox _allowIp = new();
    private readonly PictureBox _preview = new();
    private readonly Label _waiting = new();
    private readonly CheckBox _chkSave = new();
    private readonly TextBox _directory = new();
    private readonly NumericUpDown _interval = new();
    private readonly NumericUpDown _quality = new();
    private readonly Label _lastSave = new();
    private readonly TextBox _log = new();
    private TableLayoutPanel? _root;

    private int _saveSequence = 1;
    private bool _fullPreview;
    private DateTime _lastFailLogUtc = DateTime.MinValue;

    public MainForm()
    {
        Text = "局域网监控 · 接收端";
        MinimumSize = new Size(1200, 760);
        Size = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        BackColor = FormBack;
        ForeColor = TextColor;
        Font = new Font("Microsoft YaHei UI", 9F);

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        Controls.Add(_root);

        _root.Controls.Add(BuildTopBar(), 0, 0);
        _root.Controls.Add(BuildPreview(), 0, 1);
        _root.Controls.Add(BuildSaveBar(), 0, 2);
        _root.Controls.Add(BuildLog(), 0, 3);

        _server.Log += msg => BeginInvokeSafe(() => AppendLog(msg));
        _server.ClientChanged += ep => BeginInvokeSafe(() =>
        {
            _client.Text = string.IsNullOrEmpty(ep) ? "客户 无连接" : "客户 " + ep;
        });
        _server.FrameReceived += OnFrameReceived;

        FormClosing += (_, _) => _server.Dispose();
        KeyDown += OnKeyDown;
        AppendLog("就绪。点「开始监听」。默认端口 19730（13689 在本机常被系统保留）。");
    }

    private Control BuildTopBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BarBack,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0)
        };
        bar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        bar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        // 行1：状态 | 端口 | 开始 | 断开 | 接收开
        var row1 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // status
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));  // 端口 label
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // port
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));  // gap
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108)); // listen
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108)); // disconnect
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // receive

        _listenDot.Text = "● 未监听";
        _listenDot.ForeColor = Color.Gray;
        _listenDot.Dock = DockStyle.Fill;
        _listenDot.TextAlign = ContentAlignment.MiddleLeft;
        row1.Controls.Add(_listenDot, 0, 0);

        row1.Controls.Add(MakeLabel("端口", ContentAlignment.MiddleRight), 1, 0);

        StyleNumeric(_port);
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Value = 19730;
        _port.Width = 100;
        _port.Dock = DockStyle.Fill;
        row1.Controls.Add(_port, 2, 0);

        StyleButton(_btnListen, "开始监听");
        _btnListen.Click += (_, _) => ToggleListen();
        row1.Controls.Add(_btnListen, 4, 0);

        StyleButton(_btnDisconnect, "断开客户");
        _btnDisconnect.Click += (_, _) => _server.DisconnectCurrent();
        row1.Controls.Add(_btnDisconnect, 6, 0);

        _chkReceive.Text = "接收开";
        _chkReceive.Checked = true;
        _chkReceive.AutoSize = true;
        _chkReceive.Dock = DockStyle.Left;
        _chkReceive.Margin = new Padding(16, 8, 0, 0);
        _chkReceive.ForeColor = TextColor;
        _chkReceive.CheckedChanged += (_, _) => _server.ReceiveEnabled = _chkReceive.Checked;
        row1.Controls.Add(_chkReceive, 7, 0);

        // 行2：客户 | fps | 最近帧 | 等比/拉伸 | 允许IP
        var row2 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 9,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260)); // client
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // fps
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140)); // frame size
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));  // zoom
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));  // stretch
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));  // allow label
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // allow ip

        _client.Text = "客户 无连接";
        _client.Dock = DockStyle.Fill;
        _client.TextAlign = ContentAlignment.MiddleLeft;
        row2.Controls.Add(_client, 0, 0);

        _fpsLabel.Text = "0 fps";
        _fpsLabel.Dock = DockStyle.Fill;
        _fpsLabel.TextAlign = ContentAlignment.MiddleLeft;
        row2.Controls.Add(_fpsLabel, 1, 0);

        _frameSize.Text = "最近帧 --";
        _frameSize.Dock = DockStyle.Fill;
        _frameSize.TextAlign = ContentAlignment.MiddleLeft;
        row2.Controls.Add(_frameSize, 2, 0);

        _radioZoom.Text = "等比";
        _radioZoom.Checked = true;
        _radioZoom.AutoSize = true;
        _radioZoom.Dock = DockStyle.Left;
        _radioZoom.Margin = new Padding(0, 8, 0, 0);
        _radioZoom.ForeColor = TextColor;
        _radioZoom.CheckedChanged += (_, _) => ApplySizeMode();
        row2.Controls.Add(_radioZoom, 4, 0);

        _radioStretch.Text = "拉伸";
        _radioStretch.AutoSize = true;
        _radioStretch.Dock = DockStyle.Left;
        _radioStretch.Margin = new Padding(0, 8, 0, 0);
        _radioStretch.ForeColor = TextColor;
        row2.Controls.Add(_radioStretch, 5, 0);

        row2.Controls.Add(MakeLabel("允许IP", ContentAlignment.MiddleRight), 7, 0);

        StyleTextBox(_allowIp);
        _allowIp.PlaceholderText = "空=全部";
        _allowIp.Dock = DockStyle.Fill;
        _allowIp.Margin = new Padding(8, 6, 0, 6);
        _allowIp.TextChanged += (_, _) => _server.AllowList = _allowIp.Text;
        row2.Controls.Add(_allowIp, 8, 0);

        bar.Controls.Add(row1, 0, 0);
        bar.Controls.Add(row2, 0, 1);
        return bar;
    }

    private Control BuildPreview()
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Margin = new Padding(0) };
        _preview.Dock = DockStyle.Fill;
        _preview.BackColor = Color.Black;
        _preview.SizeMode = PictureBoxSizeMode.Zoom;
        _preview.TabStop = false;
        _waiting.Text = "等待画面";
        _waiting.ForeColor = Color.FromArgb(160, 160, 160);
        _waiting.AutoSize = true;
        _waiting.Font = new Font("Microsoft YaHei UI", 16F);
        host.Controls.Add(_waiting);
        host.Controls.Add(_preview);
        _preview.SendToBack();
        host.Resize += (_, _) => CenterWaiting(host);
        CenterWaiting(host);
        return host;
    }

    private Control BuildSaveBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BarBack,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0)
        };
        bar.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        bar.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        // 行1：保存 | 目录 | 路径 | 浏览
        var row1 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));

        _chkSave.Text = "保存";
        _chkSave.Checked = true;
        _chkSave.AutoSize = true;
        _chkSave.Dock = DockStyle.Left;
        _chkSave.Margin = new Padding(0, 8, 0, 0);
        _chkSave.ForeColor = TextColor;
        row1.Controls.Add(_chkSave, 0, 0);

        row1.Controls.Add(MakeLabel("目录", ContentAlignment.MiddleRight), 1, 0);

        StyleTextBox(_directory);
        _directory.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "局域网监控");
        _directory.Dock = DockStyle.Fill;
        _directory.Margin = new Padding(8, 6, 8, 6);
        row1.Controls.Add(_directory, 2, 0);

        var browse = new Button();
        StyleButton(browse, "浏览");
        browse.Click += (_, _) => BrowseDirectory();
        row1.Controls.Add(browse, 3, 0);

        // 行2：间隔 | 数值 | 秒 | 质量 | 数值 | 最近写入
        var row2 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        row2.Controls.Add(MakeLabel("间隔", ContentAlignment.MiddleRight), 0, 0);

        StyleNumeric(_interval);
        _interval.DecimalPlaces = 1;
        _interval.Minimum = 0.2M;
        _interval.Maximum = 60;
        _interval.Increment = 0.1M;
        _interval.Value = 1.0M;
        _interval.Dock = DockStyle.Fill;
        _interval.Margin = new Padding(8, 6, 4, 6);
        _interval.ValueChanged += (_, _) => _saveScheduler.IntervalSeconds = (double)_interval.Value;
        row2.Controls.Add(_interval, 1, 0);

        row2.Controls.Add(MakeLabel("秒", ContentAlignment.MiddleLeft), 2, 0);

        row2.Controls.Add(MakeLabel("质量", ContentAlignment.MiddleRight), 3, 0);

        StyleNumeric(_quality);
        _quality.Minimum = 1;
        _quality.Maximum = 100;
        _quality.Value = 60;
        _quality.Dock = DockStyle.Fill;
        _quality.Margin = new Padding(8, 6, 8, 6);
        row2.Controls.Add(_quality, 4, 0);

        _lastSave.Text = "最近写入 --";
        _lastSave.Dock = DockStyle.Fill;
        _lastSave.TextAlign = ContentAlignment.MiddleLeft;
        _lastSave.Margin = new Padding(16, 0, 0, 0);
        row2.Controls.Add(_lastSave, 5, 0);

        bar.Controls.Add(row1, 0, 0);
        bar.Controls.Add(row2, 0, 1);
        _saveScheduler.IntervalSeconds = (double)_interval.Value;
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

    private static Label MakeLabel(string text, ContentAlignment align) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = align,
        ForeColor = TextColor,
        Margin = new Padding(0),
        AutoEllipsis = true
    };

    private static void StyleButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = ButtonBack;
        button.ForeColor = TextColor;
        button.Margin = new Padding(0, 4, 0, 4);
        button.Cursor = Cursors.Hand;
    }

    private static void StyleNumeric(NumericUpDown box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = InputBack;
        box.ForeColor = TextColor;
        box.TextAlign = HorizontalAlignment.Left;
    }

    private static void StyleTextBox(TextBox box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = InputBack;
        box.ForeColor = TextColor;
    }

    private void CenterWaiting(Control host)
    {
        _waiting.Location = new Point(
            Math.Max(0, (host.ClientSize.Width - _waiting.Width) / 2),
            Math.Max(0, (host.ClientSize.Height - _waiting.Height) / 2));
    }

    private void ToggleListen()
    {
        if (_server.IsListening)
        {
            _server.Stop();
            _btnListen.Text = "开始监听";
            _listenDot.Text = "● 未监听";
            _listenDot.ForeColor = Color.Gray;
            _port.Enabled = true;
            return;
        }

        _server.Port = (int)_port.Value;
        _server.AllowList = _allowIp.Text;
        _server.ReceiveEnabled = _chkReceive.Checked;
        try
        {
            _server.Start();
            _btnListen.Text = "停止监听";
            _listenDot.Text = "● 监听 " + _server.BoundPort;
            _listenDot.ForeColor = Color.LightGreen;
            _port.Enabled = false;
        }
        catch (Exception ex)
        {
            AppendLog("监听失败: " + ex.Message);
            _lastSave.ForeColor = Color.Salmon;
        }
    }

    private void BrowseDirectory()
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = Directory.Exists(_directory.Text)
                ? _directory.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _directory.Text = dialog.SelectedPath;
        }
    }

    private void ApplySizeMode()
    {
        _preview.SizeMode = _radioZoom.Checked ? PictureBoxSizeMode.Zoom : PictureBoxSizeMode.StretchImage;
    }

    private void OnFrameReceived(byte[] frame)
    {
        BeginInvokeSafe(() =>
        {
            if (!ImagePayload.TryEncodeJpeg(frame, (int)_quality.Value, out var jpeg, out var error))
            {
                LogFail("预览/保存失败: " + error + "  原始=" + FormatBytes(frame.Length));
                return;
            }

            _waiting.Visible = false;
            _fps.Tick();
            _fpsLabel.Text = _fps.Fps.ToString("0") + " fps";
            _frameSize.Text = "最近帧 " + FormatBytes(frame.Length);

            try
            {
                using var ms = new MemoryStream(jpeg, writable: false);
                using var img = Image.FromStream(ms);
                var old = _preview.Image;
                _preview.Image = (Image)img.Clone();
                old?.Dispose();
            }
            catch (Exception ex)
            {
                LogFail("预览失败: " + ex.Message);
                return;
            }

            if (!_chkSave.Checked)
            {
                return;
            }

            _saveScheduler.IntervalSeconds = (double)_interval.Value;
            if (!_saveScheduler.ShouldSave(DateTime.UtcNow))
            {
                return;
            }

            try
            {
                var path = JpegFileSaver.BuildPath(_directory.Text, _saveSequence);
                File.WriteAllBytes(path, jpeg);
                _saveSequence++;
                _lastSave.ForeColor = TextColor;
                _lastSave.Text = "最近写入 " + Path.GetFileName(path) + "  " + FormatBytes(jpeg.Length) + "  " + DateTime.Now.ToString("HH:mm:ss");
                AppendLog("JPEG保存成功：" + path + " 大小=" + jpeg.Length);
            }
            catch (Exception ex)
            {
                _lastSave.ForeColor = Color.Salmon;
                _lastSave.Text = "无法写入目录：" + ex.Message;
                AppendLog("保存异常：" + ex.Message);
            }
        });
    }

    private void LogFail(string message)
    {
        if (DateTime.UtcNow - _lastFailLogUtc < TimeSpan.FromSeconds(2))
        {
            return;
        }

        _lastFailLogUtc = DateTime.UtcNow;
        _lastSave.ForeColor = Color.Salmon;
        _lastSave.Text = message;
        AppendLog(message);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11)
        {
            ToggleFullPreview();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.S && !e.Control)
        {
            if (ActiveControl is NumericUpDown or TextBox)
            {
                return;
            }

            _chkSave.Checked = !_chkSave.Checked;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape && _fullPreview)
        {
            ToggleFullPreview();
            e.Handled = true;
        }
    }

    private void ToggleFullPreview()
    {
        if (_root is null)
        {
            return;
        }

        _fullPreview = !_fullPreview;
        var preview = _root.GetControlFromPosition(0, 1);
        foreach (Control child in _root.Controls)
        {
            if (!ReferenceEquals(child, preview))
            {
                child.Visible = !_fullPreview;
            }
        }

        WindowState = _fullPreview ? FormWindowState.Maximized : FormWindowState.Normal;
    }

    private void AppendLog(string line)
    {
        var text = DateTime.Now.ToString("HH:mm:ss") + "  " + line;
        _logLines.Enqueue(text);
        while (_logLines.Count > 8)
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
            try { BeginInvoke(action); } catch (ObjectDisposedException) { }
            return;
        }

        action();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return bytes + " B";
        }

        if (bytes < 1024 * 1024)
        {
            return (bytes / 1024.0).ToString("0") + " KB";
        }

        return (bytes / 1024.0 / 1024.0).ToString("0.00") + " MB";
    }
}

internal sealed class FrameRateCounter
{
    private readonly Queue<DateTime> _ticks = new();

    public double Fps { get; private set; }

    public void Tick()
    {
        var now = DateTime.UtcNow;
        _ticks.Enqueue(now);
        while (_ticks.Count > 0 && now - _ticks.Peek() > TimeSpan.FromSeconds(1))
        {
            _ticks.Dequeue();
        }

        Fps = _ticks.Count;
    }
}
