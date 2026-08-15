using LanMonitor.Core;

namespace LanMonitor.Receiver;

internal sealed class MainForm : Form
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
    private readonly CheckBox _chkPreview = new();
    private readonly Button _btnDisconnect = new();
    private readonly RadioButton _radioZoom = new();
    private readonly RadioButton _radioStretch = new();
    private readonly Label _allowSummary = new();
    private readonly Button _btnAllowManage = new();
    private readonly AllowIpPolicy _allowPolicy = new();
    private readonly PictureBox _preview = new();
    private readonly Label _waiting = new();
    private readonly Label _previewOffHint = new();
    private readonly CheckBox _chkSave = new();
    private readonly TextBox _directory = new();
    private readonly NumericUpDown _interval = new();
    private readonly NumericUpDown _quality = new();
    private readonly Label _lastSave = new();
    private readonly TextBox _log = new();
    private readonly Button _btnHide = new();
    private readonly Label _hostIp = new();
    private readonly Label _waitingSub = new();
    private readonly ToolTip _tips = new();
    private readonly ReceiverSettings _settings;
    private readonly Icon _appIcon;
    private TableLayoutPanel? _root;
    private NotifyIcon? _tray;
    private ContextMenuStrip? _trayMenu;
    private string _primaryIp = "--";

    private int _saveSequence = 1;
    private bool _fullPreview;
    private bool _allowExit;
    private bool _authPromptOpen;
    private DateTime _lastFailLogUtc = DateTime.MinValue;

    public MainForm(ReceiverSettings settings)
    {
        _settings = settings;
        _appIcon = AppIcon.Resolve();
        Text = "SF_view";
        Icon = _appIcon;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(1280, 800);
        Size = new Size(1360, 860);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        BackColor = FormBack;
        ForeColor = TextColor;
        Font = new Font("Microsoft YaHei UI", 9.75F);

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        Controls.Add(_root);

        _root.Controls.Add(BuildTopBar(), 0, 0);
        _root.Controls.Add(BuildPreview(), 0, 1);
        _root.Controls.Add(BuildSaveBar(), 0, 2);
        _root.Controls.Add(BuildLog(), 0, 3);

        RefreshLocalIp();
        InitTray();

        _server.Log += msg => BeginInvokeSafe(() => AppendLog(msg));
        _server.ClientChanged += ep => BeginInvokeSafe(() =>
        {
            if (!string.IsNullOrEmpty(ep))
            {
                _allowPolicy.RememberEndpoint(ep);
            }

            _client.Text = string.IsNullOrEmpty(ep) ? "连接 无" : "连接 " + ep;
        });
        _server.FrameReceived += OnFrameReceived;

        FormClosing += OnFormClosing;
        KeyDown += OnKeyDown;
        RefreshAllowSummary();
        AppendLog("就绪。点「开始监听」。关窗隐藏到托盘（再开需密码）。");
    }

    private Control BuildTopBar()
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

        // 行1：状态 | 端口 | 开始监听 | 断开 | 隐藏 | 预览开 | (弹性) | 本机 IP
        var row1 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 11,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _listenDot.Text = "● 未监听";
        _listenDot.ForeColor = Color.Gray;
        _listenDot.AutoSize = true;
        _listenDot.TextAlign = ContentAlignment.MiddleLeft;
        _listenDot.Margin = new Padding(0, 10, 16, 0);
        _listenDot.AutoEllipsis = false;
        row1.Controls.Add(_listenDot, 0, 0);

        row1.Controls.Add(MakeCaption("端口"), 1, 0);

        StyleNumeric(_port);
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Value = 19730;
        _port.ThousandsSeparator = false;
        _port.Dock = DockStyle.Fill;
        _port.Margin = new Padding(8, 8, 0, 8);
        _port.MinimumSize = new Size(120, 28);
        _port.ValueChanged += (_, _) => UpdateWaitingSub();
        row1.Controls.Add(_port, 2, 0);

        StyleButton(_btnListen, "开始监听");
        _btnListen.MinimumSize = new Size(120, 32);
        _btnListen.Click += (_, _) => ToggleListen();
        row1.Controls.Add(_btnListen, 4, 0);

        StyleButton(_btnDisconnect, "断开");
        _btnDisconnect.MinimumSize = new Size(88, 32);
        _btnDisconnect.Click += (_, _) => _server.DisconnectCurrent();
        row1.Controls.Add(_btnDisconnect, 6, 0);

        StyleButton(_btnHide, "隐藏");
        _btnHide.MinimumSize = new Size(80, 32);
        _btnHide.Click += (_, _) => HideToTray();
        row1.Controls.Add(_btnHide, 8, 0);

        _chkPreview.Text = "预览开";
        _chkPreview.Checked = true;
        _chkPreview.AutoSize = true;
        _chkPreview.Anchor = AnchorStyles.Left;
        _chkPreview.Margin = new Padding(12, 12, 0, 0);
        _chkPreview.ForeColor = TextColor;
        _chkPreview.CheckedChanged += (_, _) => UpdatePreviewHint();
        row1.Controls.Add(_chkPreview, 9, 0);

        _hostIp.Text = "本机 --";
        _hostIp.AutoSize = true;
        _hostIp.ForeColor = Color.FromArgb(170, 190, 205);
        _hostIp.Cursor = Cursors.Hand;
        _hostIp.Margin = new Padding(12, 10, 0, 0);
        _hostIp.TextAlign = ContentAlignment.MiddleRight;
        _hostIp.Click += (_, _) => CopyPrimaryIp();
        row1.Controls.Add(_hostIp, 10, 0);

        // 行2：客户 | fps | 最近帧 | 等比/拉伸 | 允许摘要 | 管理
        var row2 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 10,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        _client.Text = "连接 无";
        _client.AutoSize = true;
        _client.TextAlign = ContentAlignment.MiddleLeft;
        _client.Margin = new Padding(0, 10, 20, 0);
        _client.AutoEllipsis = false;
        row2.Controls.Add(_client, 0, 0);

        _fpsLabel.Text = "0 fps";
        _fpsLabel.AutoSize = true;
        _fpsLabel.TextAlign = ContentAlignment.MiddleLeft;
        _fpsLabel.Margin = new Padding(0, 10, 20, 0);
        _fpsLabel.AutoEllipsis = false;
        row2.Controls.Add(_fpsLabel, 1, 0);

        _frameSize.Text = "最近帧 --";
        _frameSize.AutoSize = true;
        _frameSize.TextAlign = ContentAlignment.MiddleLeft;
        _frameSize.Margin = new Padding(0, 10, 12, 0);
        _frameSize.AutoEllipsis = false;
        row2.Controls.Add(_frameSize, 2, 0);

        _radioZoom.Text = "等比";
        _radioZoom.Checked = true;
        _radioZoom.AutoSize = true;
        _radioZoom.Margin = new Padding(0, 10, 12, 0);
        _radioZoom.ForeColor = TextColor;
        _radioZoom.CheckedChanged += (_, _) => ApplySizeMode();
        row2.Controls.Add(_radioZoom, 4, 0);

        _radioStretch.Text = "拉伸";
        _radioStretch.AutoSize = true;
        _radioStretch.Margin = new Padding(0, 10, 12, 0);
        _radioStretch.ForeColor = TextColor;
        row2.Controls.Add(_radioStretch, 5, 0);

        row2.Controls.Add(MakeCaption("允许"), 7, 0);

        _allowSummary.Text = "全部";
        _allowSummary.AutoSize = true;
        _allowSummary.Margin = new Padding(4, 10, 12, 0);
        _allowSummary.AutoEllipsis = false;
        row2.Controls.Add(_allowSummary, 8, 0);

        StyleButton(_btnAllowManage, "设置");
        _btnAllowManage.MinimumSize = new Size(88, 32);
        _btnAllowManage.Click += (_, _) => OpenAllowDialog();
        row2.Controls.Add(_btnAllowManage, 9, 0);

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
        _waitingSub.Text = "本机 -- · 端口 19730";
        _waitingSub.ForeColor = Color.FromArgb(110, 110, 120);
        _waitingSub.AutoSize = true;
        _waitingSub.Font = new Font("Microsoft YaHei UI", 10F);
        _previewOffHint.Text = "预览已关闭";
        _previewOffHint.ForeColor = Color.FromArgb(220, 200, 120);
        _previewOffHint.AutoSize = true;
        _previewOffHint.Font = new Font("Microsoft YaHei UI", 14F);
        _previewOffHint.Visible = false;
        host.Controls.Add(_previewOffHint);
        host.Controls.Add(_waitingSub);
        host.Controls.Add(_waiting);
        host.Controls.Add(_preview);
        _preview.SendToBack();
        host.Resize += (_, _) =>
        {
            CenterWaiting(host);
            CenterOverlay(host, _previewOffHint);
        };
        CenterWaiting(host);
        CenterOverlay(host, _previewOffHint);
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
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0)
        };
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        // 行1：保存 | 目录 | 路径 | 浏览
        var row1 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        _chkSave.Text = "保存";
        _chkSave.Checked = true;
        _chkSave.AutoSize = true;
        _chkSave.Margin = new Padding(0, 10, 16, 0);
        _chkSave.ForeColor = TextColor;
        _chkSave.CheckedChanged += (_, _) => UpdatePreviewHint();
        row1.Controls.Add(_chkSave, 0, 0);

        row1.Controls.Add(MakeCaption("目录"), 1, 0);

        StyleTextBox(_directory);
        _directory.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SF_view");
        _directory.Dock = DockStyle.Fill;
        _directory.Margin = new Padding(8, 8, 8, 8);
        _directory.MinimumSize = new Size(200, 28);
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
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        row2.Controls.Add(MakeCaption("间隔"), 0, 0);

        StyleNumeric(_interval);
        _interval.DecimalPlaces = 1;
        _interval.Minimum = 0.2M;
        _interval.Maximum = 60;
        _interval.Increment = 0.1M;
        _interval.Value = 1.0M;
        _interval.Dock = DockStyle.Fill;
        _interval.Margin = new Padding(8, 8, 4, 8);
        _interval.MinimumSize = new Size(90, 28);
        _interval.ValueChanged += (_, _) => _saveScheduler.IntervalSeconds = (double)_interval.Value;
        row2.Controls.Add(_interval, 1, 0);

        row2.Controls.Add(MakeCaption("秒", padLeft: 4, padRight: 20), 2, 0);
        row2.Controls.Add(MakeCaption("质量"), 3, 0);

        StyleNumeric(_quality);
        _quality.Minimum = 1;
        _quality.Maximum = 100;
        _quality.Value = 60;
        _quality.Dock = DockStyle.Fill;
        _quality.Margin = new Padding(8, 8, 8, 8);
        _quality.MinimumSize = new Size(90, 28);
        row2.Controls.Add(_quality, 4, 0);

        _lastSave.Text = "最近写入 --";
        _lastSave.AutoSize = true;
        _lastSave.TextAlign = ContentAlignment.MiddleLeft;
        _lastSave.Margin = new Padding(20, 10, 0, 0);
        _lastSave.AutoEllipsis = true;
        _lastSave.Dock = DockStyle.Fill;
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

    private void CenterWaiting(Control host)
    {
        var totalH = _waiting.Height + 6 + _waitingSub.Height;
        var top = Math.Max(0, (host.ClientSize.Height - totalH) / 2);
        _waiting.Location = new Point(
            Math.Max(0, (host.ClientSize.Width - _waiting.Width) / 2),
            top);
        _waitingSub.Location = new Point(
            Math.Max(0, (host.ClientSize.Width - _waitingSub.Width) / 2),
            top + _waiting.Height + 6);
        _waitingSub.Visible = _waiting.Visible;
    }

    private static void CenterOverlay(Control host, Label label)
    {
        label.Location = new Point(
            Math.Max(0, (host.ClientSize.Width - label.Width) / 2),
            Math.Max(0, (host.ClientSize.Height - label.Height) / 2));
        label.BringToFront();
    }

    private void InitTray()
    {
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("打开", null, (_, _) => TryShowFromTray());
        _trayMenu.Items.Add("退出", null, (_, _) => TryExitWithAuth());

        _tray = new NotifyIcon
        {
            Text = "SF_view",
            Icon = _appIcon,
            Visible = true,
            ContextMenuStrip = _trayMenu
        };
        _tray.DoubleClick += (_, _) => TryShowFromTray();
    }

    private void RefreshLocalIp()
    {
        var info = LocalIpHelper.Get();
        _primaryIp = info.PrimaryDisplay;
        _hostIp.Text = "本机 " + _primaryIp;
        _tips.SetToolTip(_hostIp, info.Tooltip);
        UpdateWaitingSub();
    }

    private void UpdateWaitingSub()
    {
        var port = _server.IsListening ? _server.BoundPort : (int)_port.Value;
        _waitingSub.Text = "本机 " + _primaryIp + " · 端口 " + port;
        if (_waiting.Parent is Control host)
        {
            CenterWaiting(host);
        }
    }

    private void CopyPrimaryIp()
    {
        if (string.IsNullOrEmpty(_primaryIp) || _primaryIp == "--")
        {
            return;
        }

        try
        {
            Clipboard.SetText(_primaryIp);
            _tips.SetToolTip(_hostIp, "已复制");
            AppendLog("已复制本机 IP：" + _primaryIp);
            var timer = new System.Windows.Forms.Timer { Interval = 1500 };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                timer.Dispose();
                var tip = LocalIpHelper.Get().Tooltip;
                _tips.SetToolTip(_hostIp, tip);
            };
            timer.Start();
        }
        catch (Exception ex)
        {
            AppendLog("复制 IP 失败：" + ex.Message);
        }
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
        if (_tray is not null)
        {
            _tray.Visible = true;
        }

        AppendLog("已隐藏到托盘。双击托盘图标并输入密码可再打开。");
    }

    private void TryShowFromTray()
    {
        if (_authPromptOpen)
        {
            return;
        }

        if (!PromptAuth("SF_view 验证", "输入管理密码以打开窗口"))
        {
            return;
        }

        ShowInTaskbar = true;
        Show();
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Activate();
    }

    private void TryExitWithAuth()
    {
        if (_authPromptOpen)
        {
            return;
        }

        if (!PromptAuth("SF_view 退出", "输入管理密码以退出程序"))
        {
            return;
        }

        _allowExit = true;
        Close();
    }

    private bool PromptAuth(string title, string prompt)
    {
        try
        {
            _authPromptOpen = true;
            using var dlg = new AuthDialog(title, prompt, _settings.VerifyPassword);
            return dlg.ShowDialog(Visible ? this : null) == DialogResult.OK;
        }
        finally
        {
            _authPromptOpen = false;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }

        _trayMenu?.Dispose();
        _trayMenu = null;
        _server.Dispose();
    }

    private void RefreshAllowSummary()
    {
        _allowSummary.Text = _allowPolicy.SummaryText();
        _server.AllowList = _allowPolicy.ToAllowListString();
    }

    private void OpenAllowDialog()
    {
        using var dlg = new AllowIpDialog(_allowPolicy);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            RefreshAllowSummary();
            AppendLog("允许策略已更新：" + _allowPolicy.SummaryText());
        }
    }

    private void UpdatePreviewHint()
    {
        if (_chkPreview.Checked)
        {
            _previewOffHint.Visible = false;
            return;
        }

        _previewOffHint.Text = _chkSave.Checked
            ? "预览已关闭 · 保存仍可进行"
            : "预览已关闭";
        _previewOffHint.Visible = true;
        if (_preview.Parent is Control host)
        {
            CenterOverlay(host, _previewOffHint);
        }
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
            UpdateWaitingSub();
            return;
        }

        _server.Port = (int)_port.Value;
        _server.AllowList = _allowPolicy.ToAllowListString();
        try
        {
            _server.Start();
            _btnListen.Text = "停止监听";
            _listenDot.Text = "● 监听 " + _server.BoundPort;
            _listenDot.ForeColor = Color.LightGreen;
            _port.Enabled = false;
            RefreshLocalIp();
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

            _frameSize.Text = "最近帧 " + FormatBytes(frame.Length);

            if (_chkPreview.Checked)
            {
                _waiting.Visible = false;
                _fps.Tick();
                _fpsLabel.Text = _fps.Fps.ToString("0") + " fps";
                try
                {
                    using var ms = new MemoryStream(jpeg, writable: false);
                    using var img = Image.FromStream(ms);
                    var old = _preview.Image;
                    _preview.Image = (Image)img.Clone();
                    old?.Dispose();
                    _waiting.Visible = false;
                    _waitingSub.Visible = false;
                }
                catch (Exception ex)
                {
                    LogFail("预览失败: " + ex.Message);
                }
            }
            else
            {
                UpdatePreviewHint();
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
