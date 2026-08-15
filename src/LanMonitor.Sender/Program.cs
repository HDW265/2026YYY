namespace LanMonitor.Sender;

static class Program
{
    private const string MutexName = "Local\\SF_link.SingleInstance.ff9a";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var created);
        if (!created)
        {
            MessageBox.Show(
                "SF_link 已在运行。\n请按 Ctrl+Shift+Alt+M 并输入密码打开设置。",
                "SF_link",
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
