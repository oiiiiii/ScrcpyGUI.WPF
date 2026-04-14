using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ScrcpyGUI.WPF.Helpers;

namespace ScrcpyGUI.WPF.Helpers;

public static class WindowHelper
{
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetFocus(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_CHAR = 0x0102;

    public static void SendCtrlVToWindow(IntPtr hWnd)
    {
        LogHelper.Info($"🎯 开始发送 Ctrl+V 到窗口句柄: {hWnd}");
        
        // 先尝试设置窗口为前台窗口
        LogHelper.Info("📌 尝试设置 scrcpy 窗口为前台窗口...");
        bool foregroundResult = SetForegroundWindow(hWnd);
        LogHelper.Info($"   SetForegroundWindow 结果: {foregroundResult}");
        
        // 稍等一下让窗口获得焦点
        System.Threading.Thread.Sleep(100);
        
        // 方案1：使用 SendInput 发送按键
        LogHelper.Info("⌨️ 使用 SendInput 发送 Ctrl+V...");
        SendCtrlV();
        
        LogHelper.Info("✅ SendInput 执行完成");
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    public static bool SetForegroundWindowSafe(IntPtr hWnd)
    {
        return SetForegroundWindow(hWnd);
    }

    public static void SendCtrlV()
    {
        LogHelper.Info("   准备 SendInput 结构体...");
        var inputs = new INPUT[4];

        // 按下 Ctrl
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = VK_CONTROL;

        // 按下 V
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].u.ki.wVk = VK_V;

        // 释放 V
        inputs[2].type = INPUT_KEYBOARD;
        inputs[2].u.ki.wVk = VK_V;
        inputs[2].u.ki.dwFlags = KEYEVENTF_KEYUP;

        // 释放 Ctrl
        inputs[3].type = INPUT_KEYBOARD;
        inputs[3].u.ki.wVk = VK_CONTROL;
        inputs[3].u.ki.dwFlags = KEYEVENTF_KEYUP;

        LogHelper.Info("   调用 SendInput...");
        uint result = SendInput(4, inputs, Marshal.SizeOf<INPUT>());
        LogHelper.Info($"   SendInput 返回值: {result} (应该是 4)");
    }

    public static (IntPtr hWnd, RECT rect)? FindScrcpyWindow(int? processId = null)
    {
        IntPtr foundWindow = IntPtr.Zero;
        RECT foundRect = default;
        var allWindows = new List<string>();

        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                var title = new StringBuilder(256);
                GetWindowText(hWnd, title, title.Capacity);

                var windowTitle = title.ToString();
                if (!string.IsNullOrWhiteSpace(windowTitle))
                {
                    allWindows.Add(windowTitle);
                    
                    // 如果提供了进程 ID，优先通过进程 ID 匹配
                    if (processId.HasValue)
                    {
                        GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                        if (windowProcessId == processId.Value)
                        {
                            if (GetWindowRect(hWnd, out var rect))
                            {
                                // 跳过太小的窗口（可能是悬浮窗或其他工具窗口）
                                if (rect.Width > 300 && rect.Height > 300)
                                {
                                    foundWindow = hWnd;
                                    foundRect = rect;
                                    LogHelper.Info($"✅ 通过进程 ID 找到 scrcpy 窗口: '{windowTitle}', 进程 ID: {processId}, 位置: ({rect.Left}, {rect.Top}), 大小: {rect.Width}x{rect.Height}");
                                    return false;
                                }
                                else
                                {
                                    LogHelper.Info($"⚠️ 通过进程 ID 找到窗口 '{windowTitle}' 但太小: {rect.Width}x{rect.Height}");
                                }
                            }
                        }
                    }
                    // 否则通过标题匹配（备用方案）
                    else if (windowTitle.Contains("scrcpy", StringComparison.OrdinalIgnoreCase) &&
                             !windowTitle.Contains("悬浮", StringComparison.OrdinalIgnoreCase) &&
                             !windowTitle.Contains("快捷", StringComparison.OrdinalIgnoreCase) &&
                             !windowTitle.Contains("GUI", StringComparison.OrdinalIgnoreCase))
                    {
                        if (GetWindowRect(hWnd, out var rect))
                        {
                            // 跳过太小的窗口（可能是悬浮窗或其他工具窗口）
                            if (rect.Width > 300 && rect.Height > 300)
                            {
                                foundWindow = hWnd;
                                foundRect = rect;
                                LogHelper.Info($"✅ 通过标题找到 scrcpy 窗口: '{windowTitle}', 位置: ({rect.Left}, {rect.Top}), 大小: {rect.Width}x{rect.Height}");
                                return false;
                            }
                            else
                            {
                                LogHelper.Info($"⚠️ 通过标题找到窗口 '{windowTitle}' 但太小: {rect.Width}x{rect.Height}");
                            }
                        }
                    }
                    else
                    {
                        LogHelper.Info($"⏭️ 跳过窗口: '{windowTitle}'");
                    }
                }
            }
            return true;
        }, IntPtr.Zero);

        // 输出所有找到的窗口，方便调试
        LogHelper.Info($"📋 枚举到 {allWindows.Count} 个可见窗口:");
        foreach (var win in allWindows.Take(10)) // 只显示前10个避免日志太长
        {
            LogHelper.Info($"  - {win}");
        }
        if (allWindows.Count > 10)
        {
            LogHelper.Info($"  ... 还有 {allWindows.Count - 10} 个窗口");
        }

        if (foundWindow != IntPtr.Zero)
        {
            return (foundWindow, foundRect);
        }

        LogHelper.Warning("❌ 未找到 scrcpy 窗口！");
        return null;
    }
}
