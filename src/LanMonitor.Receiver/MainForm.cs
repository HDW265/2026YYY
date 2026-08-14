using LanMonitor.Core;

namespace LanMonitor.Receiver;

public sealed class MainForm : Form
{
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

    private int _saveSequence = 1;
    private bool _fullPreview;

    public MainForm()
    {
        Text = "局域网监控 · 接收端";
        MinimumSize = new Size(1100, 700);
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;
        Font = new Font("Microsoft YaHei UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        Controls.Add(root);

        root.Controls.Add(BuildTopBar(), 0, 0);
        root.Controls.Add(BuildPreview(), 0, 1);
        root.Controls.Add(BuildSaveBar(), 0, 2);
        root.Controls.Add(BuildLog(), 0, 3);

        _server.Log += msg => BeginInvokeSafe(() => AppendLog(msg));
        _server.ClientChanged += ep => BeginInvokeSafe(() =>
        {
            _client.Text = string.IsNullOrEmpty(ep) ? "客户 无连接" : "客户 " + ep;
        });
        _server.FrameReceived += OnFrameReceived;

        FormClosing += (_, _) => _server.Dispose();
        KeyDown += OnKeyDown;
        AppendLog("就绪。点击「开始监听」等待被控端连接。间隔和质量在下方直接改，立即生效。");
    }

    private Control BuildTopBar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 50),
            Padding = new Padding(10, 10, 10, 8),
            WrapContents = false
        };

        _listenDot.AutoSize = true;
        _listenDot.Text = "● 未监听";
        _listenDot.ForeColor = Color.Gray;
        _listenDot.Margin = new Padding(0, 6, 12, 0);
        bar.Controls.Add(_listenDot);

        bar.Controls.Add(LabelOf("端口"));
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Value = 13689;
        _port.Width = 80;
        _port.Margin = new Padding(4, 4, 8, 0);
        bar.Controls.Add(_port);

        _btnListen.Text = "开始监听";
        _btnListen.AutoSize = true;
        _btnListen.Click += (_, _) => ToggleListen();
        bar.Controls.Add(_btnListen);

        _btnDisconnect.Text = "断开客户";
        _btnDisconnect.AutoSize = true;
        _btnDisconnect.Click += (_, _) => _server.DisconnectCurrent();
        bar.Controls.Add(_btnDisconnect);

        _chkReceive.Text = "接收开";
        _chkReceive.Checked = true;
        _chkReceive.AutoSize = true;
        _chkReceive.Margin = new Padding(12, 6, 8, 0);
        _chkReceive.CheckedChanged += (_, _) => _server.ReceiveEnabled = _chkReceive.Checked;
        bar.Controls.Add(_chkReceive);

        _client.AutoSize = true;
        _client.Text = "客户 无连接";
        _client.Margin = new Padding(16, 6, 12, 0);
        bar.Controls.Add(_client);

        _fpsLabel.AutoSize = true;
        _fpsLabel.Text = "0 fps";
        _fpsLabel.Margin = new Padding(8, 6, 12, 0);
        bar.Controls.Add(_fpsLabel);

        _frameSize.AutoSize = true;
        _frameSize.Text = "最近帧 --";
        _frameSize.Margin = new Padding(8, 6, 12, 0);
        bar.Controls.Add(_frameSize);

        _radioZoom.Text = "等比";
        _radioZoom.Checked = true;
        _radioZoom.AutoSize = true;
        _radioZoom.Margin = new Padding(16, 6, 4, 0);
        _radioZoom.CheckedChanged += (_, _) => ApplySizeMode();
        _radioStretch.Text = "拉伸";
        _radioStretch.AutoSize = true;
        _radioStretch.Margin = new Padding(4, 6, 8, 0);
        bar.Controls.Add(_radioZoom);
        bar.Controls.Add(_radioStretch);

        bar.Controls.Add(LabelOf("允许IP"));
        _allowIp.Width = 160;
        _allowIp.PlaceholderText = "空=全部";
        _allowIp.Margin = new Padding(4, 4, 0, 0);
        _allowIp.TextChanged += (_, _) => _server.AllowList = _allowIp.Text;
        bar.Controls.Add(_allowIp);

        return bar;
    }

    private Control BuildPreview()
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
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
            BackColor = Color.FromArgb(45, 45, 50),
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 6, 10, 6)
        };
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

        var row = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _chkSave.Text = "保存";
        _chkSave.Checked = true;
        _chkSave.AutoSize = true;
        _chkSave.Margin = new Padding(0, 6, 12, 0);
        row.Controls.Add(_chkSave);

        row.Controls.Add(LabelOf("目录"));
        _directory.Width = 420;
        _directory.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "局域网监控");
        _directory.Margin = new Padding(4, 4, 6, 0);
        row.Controls.Add(_directory);

        var browse = new Button { Text = "浏览", AutoSize = true };
        browse.Click += (_, _) => BrowseDirectory();
        row.Controls.Add(browse);

        row.Controls.Add(LabelOf("间隔"));
        _interval.DecimalPlaces = 1;
        _interval.Minimum = 0.2M;
        _interval.Maximum = 60;
        _interval.Increment = 0.1M;
        _interval.Value = 1.0M;
        _interval.Width = 70;
        _interval.Margin = new Padding(4, 4, 4, 0);
        _interval.ValueChanged += (_, _) => _saveScheduler.IntervalSeconds = (double)_interval.Value;
        row.Controls.Add(_interval);
        row.Controls.Add(LabelOf("秒"));

        row.Controls.Add(LabelOf("质量"));
        _quality.Minimum = 1;
        _quality.Maximum = 100;
        _quality.Value = 60;
        _quality.Width = 70;
        _quality.Margin = new Padding(12, 4, 4, 0);
        row.Controls.Add(_quality);

        bar.Controls.Add(row, 0, 0);
        _lastSave.Text = "最近写入 --";
        _lastSave.Dock = DockStyle.Fill;
        _lastSave.TextAlign = ContentAlignment.MiddleLeft;
        bar.Controls.Add(_lastSave, 0, 1);
        _saveScheduler.IntervalSeconds = (double)_interval.Value;
        return bar;
    }

    private Control BuildLog()
    {
        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.BorderStyle = BorderStyle.None;
        _log.BackColor = Color.FromArgb(28, 28, 30);
        _log.ForeColor = Color.FromArgb(200, 200, 200);
        return _log;
    }

    private Label LabelOf(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(8, 8, 0, 0)
    };

    private void CenterWaiting(Control host)
    {
        _waiting.Location = new Point(
            Math.Max(0, (host.Width - _waiting.Width) / 2),
            Math.Max(0, (host.Height - _waiting.Height) / 2));
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
            SelectedPath = Directory.Exists(_directory.Text) ? _directory.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
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
            _waiting.Visible = false;
            _fps.Tick();
            _fpsLabel.Text = _fps.Fps.ToString("0") + " fps";
            _frameSize.Text = "最近帧 " + FormatBytes(frame.Length);

            try
            {
                using var ms = new MemoryStream(frame, writable: false);
                using var img = Image.FromStream(ms);
                var old = _preview.Image;
                _preview.Image = (Image)img.Clone();
                old?.Dispose();
            }
            catch (Exception ex)
            {
                AppendLog("预览失败: " + ex.Message);
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
                var result = JpegFileSaver.Save(frame, path, (int)_quality.Value);
                if (result.Success)
                {
                    _saveSequence++;
                    _lastSave.ForeColor = Color.Gainsboro;
                    _lastSave.Text = "最近写入 " + Path.GetFileName(result.Path) + "  " + FormatBytes(result.Bytes) + "  " + DateTime.Now.ToString("HH:mm:ss");
                    AppendLog("JPEG保存成功：" + result.Path + " 大小=" + result.Bytes);
                }
                else
                {
                    _lastSave.ForeColor = Color.Salmon;
                    _lastSave.Text = "保存失败：" + result.Error;
                    AppendLog("保存失败：" + result.Error);
                }
            }
            catch (Exception ex)
            {
                _lastSave.ForeColor = Color.Salmon;
                _lastSave.Text = "无法写入目录：" + ex.Message;
                AppendLog("保存异常：" + ex.Message);
            }
        });
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
        _fullPreview = !_fullPreview;
        foreach (Control child in Controls[0].Controls)
        {
            if (child != ((TableLayoutPanel)Controls[0]).GetControlFromPosition(0, 1))
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
