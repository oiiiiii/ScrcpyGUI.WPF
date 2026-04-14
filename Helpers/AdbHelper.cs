using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.Helpers;

public static class AdbHelper
{
    private static string? _adbPath;

    static AdbHelper()
    {
        _adbPath = "adb";
    }

    public static string AdbPath => _adbPath ?? "adb";

    public static void UpdatePaths(string adbPath)
    {
        if (!string.IsNullOrWhiteSpace(adbPath) && File.Exists(adbPath))
        {
            _adbPath = adbPath;
            LogHelper.Info($"ADB 路径已更新: {adbPath}");
        }
    }

    public static bool IsAdbAvailable()
    {
        if (string.IsNullOrWhiteSpace(_adbPath) || _adbPath == "adb")
        {
            return false;
        }
        return File.Exists(_adbPath);
    }

    public static List<DeviceInfo> GetConnectedDevices()
    {
        var devices = new List<DeviceInfo>();
        try
        {
            var output = ExecuteAdbCommand("devices -l");
            LogHelper.Info("扫描设备...");
            
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var serial = parts[0];
                    var status = parts[1];
                    
                    var device = new DeviceInfo
                    {
                        SerialNumber = serial,
                        Status = status,
                        ConnectionType = serial.Contains(":") ? "WiFi" : "USB"
                    };

                    try
                    {
                        device.DeviceName = GetDeviceProperty(serial, "ro.product.model");
                        device.Model = GetDeviceProperty(serial, "ro.product.device");
                    }
                    catch
                    {
                        device.DeviceName = "未知设备";
                    }

                    devices.Add(device);
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"获取设备列表失败: {ex.Message}");
        }
        return devices;
    }

    public static string GetDeviceProperty(string serial, string property)
    {
        try
        {
            var output = ExecuteAdbCommand($"-s {serial} shell getprop {property}");
            return output.Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    public static bool ConnectWifiDevice(string ipAddress, out string error)
    {
        error = string.Empty;
        try
        {
            LogHelper.Info($"正在连接 {ipAddress}...");
            var output = ExecuteAdbCommand($"connect {ipAddress}");
            if (output.Contains("connected to") || output.Contains("already connected"))
            {
                LogHelper.Info($"连接成功: {ipAddress}");
                return true;
            }
            error = output;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            LogHelper.Error($"连接失败: {ex.Message}");
            return false;
        }
    }

    public static void DisconnectDevice(string ipAddress)
    {
        try
        {
            LogHelper.Info($"断开连接: {ipAddress}");
            ExecuteAdbCommand($"disconnect {ipAddress}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"断开连接失败: {ex.Message}");
        }
    }

    public static void EnableWirelessDebugging(string serial)
    {
        try
        {
            LogHelper.Info("开启无线调试模式...");
            ExecuteAdbCommand($"-s {serial} tcpip 5555");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"开启无线调试失败: {ex.Message}");
        }
    }

    public static string? TakeScreenshot(string serial, string savePath)
    {
        try
        {
            var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var devicePath = $"/sdcard/{fileName}";
            var localPath = Path.Combine(savePath, fileName);

            LogHelper.Info("正在截图...");
            ExecuteAdbCommand($"-s {serial} shell screencap -p {devicePath}");
            ExecuteAdbCommand($"-s {serial} pull {devicePath} \"{localPath}\"");
            ExecuteAdbCommand($"-s {serial} shell rm {devicePath}");
            LogHelper.Info($"截图已保存: {localPath}");
            return localPath;
        }
        catch (Exception ex)
        {
            LogHelper.Error($"截图失败: {ex.Message}");
            return null;
        }
    }

    public static void SendKeyEvent(string serial, int keyCode)
    {
        try
        {
            ExecuteAdbCommand($"-s {serial} shell input keyevent {keyCode}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"发送按键失败: {ex.Message}");
        }
    }

    public static bool SendText(string serial, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            LogHelper.Warning("没有输入文本");
            return false;
        }

        try
        {
            LogHelper.Info($"正在发送文本到设备: {text}");
            
            // 检查文本是否包含换行符
            var hasNewline = text.Contains('\n') || text.Contains('\r');
            
            // 方案1：纯 ASCII 且无换行符 → 用 adb input text
            var isAsciiOnly = text.All(c => c < 128);
            if (isAsciiOnly && !hasNewline)
            {
                LogHelper.Info("检测到纯 ASCII 文本且无换行符，使用 adb input text");
                var escapedText = text.Replace(" ", "%s");
                ExecuteAdbCommand($"-s {serial} shell input text \"{escapedText}\"");
                LogHelper.Info("✅ 文本已发送到设备！");
                return true;
            }
            
            // 方案2：非 ASCII 或有换行符 → 使用剪贴板 + Ctrl+V 方案
            LogHelper.Info("检测到非 ASCII 文本或有换行符，使用 PostMessage 方案");
            
            // 步骤1：设置 Windows 剪贴板
            LogHelper.Info("步骤1：设置 Windows 剪贴板");
            System.Windows.Clipboard.SetText(text);
            
            // 步骤2：查找 scrcpy 窗口
            LogHelper.Info("步骤2：查找 scrcpy 窗口");
            var scrcpyProcessId = ScrcpyHelper.ProcessId;
            if (scrcpyProcessId.HasValue)
            {
                LogHelper.Info($"   使用进程 ID 查找: {scrcpyProcessId.Value}");
            }
            var scrcpyWindow = WindowHelper.FindScrcpyWindow(scrcpyProcessId);
            if (scrcpyWindow == null)
            {
                LogHelper.Warning("⚠️ 未找到 scrcpy 窗口，请在 scrcpy 窗口中按 Ctrl+V 粘贴");
                return true;
            }
            
            // 步骤3：等待一点时间让 scrcpy 注意到剪贴板变化
            LogHelper.Info("步骤3：等待剪贴板同步（500ms）");
            System.Threading.Thread.Sleep(500);
            
            // 步骤4：直接给 scrcpy 窗口发送 Ctrl+V 消息！
            LogHelper.Info("步骤4：给 scrcpy 窗口发送 Ctrl+V 消息");
            WindowHelper.SendCtrlVToWindow(scrcpyWindow.Value.hWnd);
            
            LogHelper.Info("✅ 文本已自动粘贴到设备！");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error($"发送文本失败: {ex.Message}");
            return false;
        }
    }

    private static string ExecuteAdbCommand(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _adbPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            LogHelper.Warning($"ADB错误: {error}");
        }

        return output;
    }
}
