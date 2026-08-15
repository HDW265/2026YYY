namespace LanMonitor.Receiver;

static class Program
{
    private const string MutexName = "Local\\SF_view.SingleInstance.ff9a";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var created);
        if (!created)
        {
            MessageBox.Show(
                "SF_view 已在运行。\n请从托盘图标打开（需密码）。",
                "SF_view",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var settings = ReceiverSettings.LoadOrDefault();
        if (!settings.HasPassword)
        {
            using var setup = new SetupPasswordForm();
            if (setup.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            settings = setup.Result;
        }
        else
        {
            using var login = new AuthDialog(
                "SF_view 登录",
                "输入管理密码以启动",
                settings.VerifyPassword);
            if (login.ShowDialog() != DialogResult.OK)
            {
                return;
            }
        }

        Application.Run(new MainForm(settings));
    }
}
