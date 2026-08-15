namespace LanMonitor.Sender;

static class Program
{
        private const string MutexName = "Local\\LanMonitor.Sender.SingleInstance.ff9a";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var created);
        if (!created)
        {
            // 已有实例：尽量唤起热键提示（无托盘，只能提示用户按热键）
            MessageBox.Show(
                "发送端已在运行。\n请按 Ctrl+Shift+Alt+M 并输入密码打开设置。",
                "局域网监控发送端",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var settings = SenderSettings.LoadOrDefault();
        if (!settings.Configured || !settings.HasPassword)
        {
            using var setup = new SetupForm();
            if (setup.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            settings = setup.Result;
        }

        Application.Run(new SenderAppContext(settings));
    }
}
