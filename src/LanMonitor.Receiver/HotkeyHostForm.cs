namespace LanMonitor.Receiver;

/// <summary>无界面消息窗，专用于全局热键；不进任务栏、无托盘。</summary>
internal sealed class HotkeyHostForm : Form
{
    public event Action? HotkeyPressed;

    public HotkeyHostForm()
    {
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Opacity = 0;
        Size = new Size(0, 0);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-10000, -10000);
        Text = "SF_view 热键宿主";
    }

    public bool TryRegister(int modifiers, int virtualKey, out string error)
    {
        error = string.Empty;
        NativeHotkey.UnregisterHotKey(Handle, NativeHotkey.HotkeyId);
        if (!NativeHotkey.RegisterHotKey(Handle, NativeHotkey.HotkeyId, modifiers, virtualKey))
        {
            error = "热键注册失败（可能被占用）：" + NativeHotkey.Describe(modifiers, virtualKey);
            return false;
        }

        return true;
    }

    public void Unregister()
    {
        try { NativeHotkey.UnregisterHotKey(Handle, NativeHotkey.HotkeyId); } catch { /* ignore */ }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeHotkey.WmHotkey && m.WParam == (IntPtr)NativeHotkey.HotkeyId)
        {
            HotkeyPressed?.Invoke();
        }

        base.WndProc(ref m);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Unregister();
        base.OnFormClosed(e);
    }
}
