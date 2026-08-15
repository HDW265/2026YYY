using System.Diagnostics;
using System.Security;
using Microsoft.Win32;

namespace LanMonitor.Sender;

/// <summary>
/// 机器级开机自启：HKLM\...\Run，值名 SF_link（不写 WOW6432Node）。
/// 注意：注册表值名不区分大小写，清理旧项时绝不能 DeleteValue("SF_Link")，否则会删掉刚写的 SF_link。
/// </summary>
internal static class AutoStartService
{
    public const string ValueName = "SF_link";
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string WowRunKeyPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";

    public static string ExePath
    {
        get
        {
            var raw = Environment.ProcessPath ?? Application.ExecutablePath
                      ?? throw new InvalidOperationException("无法解析当前程序路径。");
            return NormalizePath(raw);
        }
    }

    public static string QuotedCommand => "\"" + ExePath + "\"";

    public static bool IsEnabledForThisExe()
    {
        try
        {
            using var key = OpenRunKey(writable: false);
            var raw = key?.GetValue(ValueName) as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var path = NormalizePath(raw.Trim().Trim('"'));
            return string.Equals(path, ExePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static string? ReadStoredCommand()
    {
        try
        {
            using var key = OpenRunKey(writable: false);
            return key?.GetValue(ValueName) as string;
        }
        catch
        {
            return null;
        }
    }

    public static void Apply(bool enable)
    {
        using var key = OpenRunKey(writable: true)
                        ?? throw new InvalidOperationException("无法打开 HKLM Run（请用管理员权限）。");

        if (enable)
        {
            // SetValue 会覆盖同名项（含仅大小写不同的旧 SF_Link），无需先 Delete
            key.SetValue(ValueName, QuotedCommand, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        // 只清 WOW6432Node 里的重复项；不要在 64 位 Run 里 DeleteValue("SF_Link")
        CleanupWowLegacyEntries();
    }

    private static void CleanupWowLegacyEntries()
    {
        TryDeleteValue(WowRunKeyPath, "SF_link");
        TryDeleteValue(WowRunKeyPath, "SF_Link");
    }

    private static void TryDeleteValue(string keyPath, string name)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(keyPath, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch
        {
            // ignore
        }
    }

    private static RegistryKey? OpenRunKey(bool writable)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        if (writable)
        {
            return baseKey.CreateSubKey(RunKeyPath, writable: true);
        }

        return baseKey.OpenSubKey(RunKeyPath, writable: false);
    }

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path.Trim().Trim('"'));
        }
        catch
        {
            return path.Trim().Trim('"');
        }
    }

    /// <summary>
    /// 无权限时以 runas 再启自身执行 --autostart on|off。
    /// </summary>
    public static bool TryApplyWithElevation(bool enable, out string error)
    {
        error = string.Empty;
        try
        {
            Apply(enable);
            if (Verify(enable, out error))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            // fall through to elevation
        }
        catch (Exception ex)
        {
            error = "写入失败：" + ex.Message;
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ExePath,
                Arguments = enable ? "--autostart on" : "--autostart off",
                UseShellExecute = true,
                Verb = "runas"
            };
            using var process = Process.Start(psi);
            if (process is null)
            {
                error = "无法启动提权进程。";
                return false;
            }

            if (!process.WaitForExit(90_000))
            {
                error = "提权操作超时。";
                return false;
            }

            if (process.ExitCode != 0)
            {
                error = "需要管理员权限才能修改开机自启（可能取消了 UAC，或安全软件拦截）。";
                return false;
            }

            return Verify(enable, out error);
        }
        catch (Exception ex)
        {
            error = "提权失败：" + ex.Message;
            return false;
        }
    }

    private static bool Verify(bool enable, out string error)
    {
        error = string.Empty;
        if (enable)
        {
            if (IsEnabledForThisExe())
            {
                return true;
            }

            var stored = ReadStoredCommand();
            if (string.IsNullOrWhiteSpace(stored))
            {
                error = "未能在 HKLM\\...\\Run 找到 SF_link。可能被安全软件删掉，请将 SF_link.exe 加入信任后再试。";
            }
            else
            {
                error = "开机项已写入但路径不一致。\n注册表：" + stored + "\n当前程序：" + ExePath +
                        "\n请用同一份 SF_link.exe 操作，或检查是否被安全软件改写。";
            }

            return false;
        }

        if (!ValueExists())
        {
            return true;
        }

        error = "关闭开机自启后注册表项仍在，请用管理员重试或手动删除 HKLM\\...\\Run\\SF_link。";
        return false;
    }

    private static bool ValueExists()
    {
        try
        {
            using var key = OpenRunKey(writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch
        {
            return false;
        }
    }

    public static int RunElevatedCli(string[] args)
    {
        if (args.Length < 2 || !string.Equals(args[0], "--autostart", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        var on = string.Equals(args[1], "on", StringComparison.OrdinalIgnoreCase);
        var off = string.Equals(args[1], "off", StringComparison.OrdinalIgnoreCase);
        if (!on && !off)
        {
            return 2;
        }

        try
        {
            Apply(on);
            // 提权进程内自检，失败则非 0，便于父进程识别
            if (on && !IsEnabledForThisExe())
            {
                return 3;
            }

            if (off && ValueExists())
            {
                return 3;
            }

            return 0;
        }
        catch
        {
            return 1;
        }
    }
}
