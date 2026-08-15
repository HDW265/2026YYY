using System.Reflection;

namespace LanMonitor.Sender;

/// <summary>
/// 图标加载：exe 旁 SF_link.ico → %ProgramData%\SF_link\app.ico → 内嵌默认。
/// </summary>
internal static class AppIcon
{
    private const string ExeSideName = "SF_link.ico";
    private const string EmbeddedName = "SF_link.ico";

    public static Icon Resolve()
    {
        var beside = Path.Combine(AppContext.BaseDirectory, ExeSideName);
        if (TryLoadFile(beside, out var icon))
        {
            return icon;
        }

        var config = Path.Combine(SenderSettings.ConfigDirectory, "app.ico");
        if (TryLoadFile(config, out icon))
        {
            return icon;
        }

        return LoadEmbedded() ?? SystemIcons.Application;
    }

    private static bool TryLoadFile(string path, out Icon icon)
    {
        icon = null!;
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var bytes = File.ReadAllBytes(path);
            icon = new Icon(new MemoryStream(bytes));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Icon? LoadEmbedded()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(EmbeddedName);
            if (stream is not null)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return new Icon(new MemoryStream(ms.ToArray()));
            }

            var exe = Environment.ProcessPath;
            return string.IsNullOrEmpty(exe) ? null : Icon.ExtractAssociatedIcon(exe);
        }
        catch
        {
            return null;
        }
    }
}
