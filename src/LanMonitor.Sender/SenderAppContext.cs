namespace LanMonitor.Sender;

internal sealed class SenderAppContext : ApplicationContext
{
    private readonly HotkeyHostForm _hotkeyHost;
    private readonly StreamSession _session = new();
    private MainForm? _settingsForm;
    private bool _promptOpen;

    public SenderSettings Settings { get; private set; }

    public SenderAppContext(SenderSettings settings)
    {
        Settings = settings;
        _hotkeyHost = new HotkeyHostForm();
        MainForm = _hotkeyHost;
        ScreenCapture.UiMarshal = _hotkeyHost;

        // 强制创建句柄以便注册热键
        _ = _hotkeyHost.Handle;
        if (!_hotkeyHost.TryRegister(settings.HotkeyModifiers, settings.HotkeyVirtualKey, out var hotkeyError))
        {
            MessageBox.Show(hotkeyError + "\n仍可后台运行；请在设置中改热键或关闭占用程序后重启。",
                "SF_link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        _hotkeyHost.HotkeyPressed += OnHotkeyPressed;

        _session.ApplySettings(settings);
        _session.Start();
    }

    private void OnHotkeyPressed()
    {
        if (_promptOpen)
        {
            return;
        }

        try
        {
            _promptOpen = true;
            if (_settingsForm is { Visible: true, IsDisposed: false })
            {
                if (_settingsForm.WindowState == FormWindowState.Minimized)
                {
                    _settingsForm.WindowState = FormWindowState.Normal;
                }

                _settingsForm.Activate();
                return;
            }

            using var dlg = new PasswordDialog(Settings);
            if (dlg.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            ShowSettings();
        }
        finally
        {
            _promptOpen = false;
        }
    }

    public void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Show();
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new MainForm(this, _session);
        _settingsForm.FormClosed += (_, _) =>
        {
            _settingsForm = null;
        };
        _settingsForm.Show();
    }

    public void ExitApplication()
    {
        _session.Dispose();
        _hotkeyHost.Unregister();
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.RequestExit();
        }

        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        _session.Dispose();
        _hotkeyHost.Unregister();
        base.ExitThreadCore();
    }
}
