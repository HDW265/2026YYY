using System.Diagnostics;
using System.Security;
using Microsoft.Win32;

namespace LanMonitor.Sender;

/// <summary>
/// 机器级开机自启：HKLM\...\Run，值名 SF_link（不写 WOW6432Node）。
/// </summary>
internal static class AutoStartService
{
    public const string ValueName = "SF_link";
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string WowRunKeyPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";

    public static string ExePath =>
        Environment.ProcessPath
        ?? Application.ExecutablePath
        ?? throw new InvalidOperationException("无法解析当前程序路径。");

    public static string QuotedCommand => "\"" + ExePath + "\"";

    public static bool IsEnabledForThisExe()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RunKeyPath, writable: false);
            var raw = key?.GetValue(ValueName) as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var path = raw.Trim().Trim('"');
            return string.Equals(path, ExePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void Apply(bool enable)
    {
        using var key = Registry.LocalMachine.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.LocalMachine.CreateSubKey(RunKeyPath, writable: true)
                        ?? throw new InvalidOperationException("无法打开 HKLM Run。");

        if (enable)
        {
            key.SetValue(ValueName, QuotedCommand, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        CleanupLegacyEntries();
    }

    public static void CleanupLegacyEntries()
    {
        TryDeleteValue(RunKeyPath, "SF_Link");
        TryDeleteValue(WowRunKeyPath, "SF_link");
        TryDeleteValue(WowRunKeyPath, "SF_Link");
    }

    private static void TryDeleteValue(string keyPath, string name)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch
        {
            // ignore
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
            return enable == IsEnabledForThisExe() || !enable && !IsEnabledForThisExe();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            // fall through to elevation
        }
        catch (Exception ex)
        {
            error = ex.Message;
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
                error = "需要管理员权限才能修改开机自启（用户可能取消了 UAC）。";
                return false;
            }

            var ok = enable ? IsEnabledForThisExe() : !ValueExists();
            if (!ok)
            {
                error = "注册表已写但未能确认状态，请用管理员重试。";
            }

            return ok;
        }
        catch (Exception ex)
        {
            error = "需要管理员权限：" + ex.Message;
            return false;
        }
    }

    private static bool ValueExists()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch
        {
            return false;
        }
    }

    public static int RunElevatedCli(string[] args)
    {
        // --autostart on|off
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
            return 0;
        }
        catch
        {
            return 1;
        }
    }
}
