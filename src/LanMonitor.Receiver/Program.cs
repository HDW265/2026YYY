namespace LanMonitor.Receiver;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settings = ReceiverSettings.LoadOrDefault();
        if (!settings.HasPassword)
        {
            using var setup = new SetupPinDialog(settings);
            if (setup.ShowDialog() != DialogResult.OK)
            {
                return;
            }
        }

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
