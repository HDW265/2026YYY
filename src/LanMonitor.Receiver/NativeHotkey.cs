using System.Runtime.InteropServices;

namespace LanMonitor.Receiver;

internal static class NativeHotkey
{
    public const int WmHotkey = 0x0312;
    public const int ModAlt = 0x0001;
    public const int ModControl = 0x0002;
    public const int ModShift = 0x0004;
    public const int ModWin = 0x0008;
    public const int HotkeyId = 0x53465631; // SFV1

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public static string Describe(int modifiers, int virtualKey)
    {
        var parts = new List<string>();
        if ((modifiers & ModControl) != 0) parts.Add("Ctrl");
        if ((modifiers & ModShift) != 0) parts.Add("Shift");
        if ((modifiers & ModAlt) != 0) parts.Add("Alt");
        if ((modifiers & ModWin) != 0) parts.Add("Win");
        parts.Add(((Keys)virtualKey).ToString());
        return string.Join("+", parts);
    }
}
