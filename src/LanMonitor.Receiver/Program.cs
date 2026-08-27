namespace LanMonitor.Receiver;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settings = ReceiverSettings.LoadOrDefault();

        // 首次：只设置验证码，设完直接进主界面（不再立刻再要一次）
        if (!settings.HasPassword)
        {
            using var setup = new SetupPinDialog(settings);
            if (setup.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Application.Run(new MainForm(settings));
            return;
        }

        // 之后启动：验证已有验证码
        using (var verify = new PinDialog(settings, "SF_view 验证", "输入验证码以打开 SF_view"))
        {
            if (verify.ShowDialog() != DialogResult.OK)
            {
                return;
            }
        }

        Application.Run(new MainForm(settings));
    }
}
