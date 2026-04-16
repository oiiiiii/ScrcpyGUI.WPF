using System.IO;
using System.Text.Json;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.Helpers;

public enum TextTransferModeDto
{
    CopyPaste,
    TextInjection
}

public class ConfigDto
{
    public int MaxSize { get; set; } = 0;
    public int BitRate { get; set; } = 16;
    public int MaxFps { get; set; } = 60;
    public bool EnableAudio { get; set; } = true;
    public bool EnableTouchControl { get; set; } = true;
    public bool WindowBorderless { get; set; } = false;
    public bool WindowAlwaysOnTop { get; set; } = false;
    public string ScreenshotSavePath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public string RecordSavePath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public bool EnableLogging { get; set; } = true;
    public string ScrcpyPath { get; set; } = string.Empty;
    public string AdbPath { get; set; } = string.Empty;
    public bool EnableFloatingWindow { get; set; } = true;
    public bool EnableInputFloatingWindow { get; set; } = true;
    public bool AutoShowOnKeyboard { get; set; } = true;
    public string SendShortcutKey { get; set; } = "Ctrl+Enter";
    public string SendToDeviceShortcutKey { get; set; } = "Alt+Enter";
    public string SendWithEnterShortcutKey { get; set; } = "Ctrl+Enter";
    public string ShowFloatingWindowShortcutKey { get; set; } = "Alt+V";
    public int KeyboardPollingInterval { get; set; } = 350;
    public int KeyboardShowDebounce { get; set; } = 200;
    public int KeyboardHideDebounce { get; set; } = 300;
    public int PositionUpdateInterval { get; set; } = 500;
    public int ScrcpyStartupDelay { get; set; } = 2000;
    public TextTransferModeDto TextTransferMode { get; set; } = TextTransferModeDto.TextInjection;
    public string LastWifiIp { get; set; } = string.Empty;
    public int LastWifiPort { get; set; } = 5555;
    
    public bool RecordOnStart { get; set; } = false;
    public bool BlackScreenOnStart { get; set; } = false;
    public bool ShowTouch { get; set; } = true;
    public bool RotateMirror { get; set; } = false;
    public bool FullscreenOnStart { get; set; } = false;
    public bool LockScreenOnClose { get; set; } = false;
    
    public string BackKeyShortcut { get; set; } = "MouseRight";
    public string HomeKeyShortcut { get; set; } = "MouseWheelDown";
    public string FullscreenShortcut { get; set; } = "Alt+F";
    public string Window1x1Shortcut { get; set; } = "Alt+G";
    public string RecentAppsShortcut { get; set; } = "Alt+S";
    public string VolumeUpShortcut { get; set; } = "Alt+Up";
    public string VolumeDownShortcut { get; set; } = "Alt+Down";
    public string RotateMirrorShortcut { get; set; } = "Alt+Left";
    public string LockScreenShortcut { get; set; } = "Alt+P";
    public string TurnScreenOnShortcut { get; set; } = "MouseRightDouble";
    public string TurnScreenOffShortcut { get; set; } = "Alt+O";
    public string ExpandNotificationShortcut { get; set; } = "Alt+N";
    public string CopyShortcut { get; set; } = "Ctrl+C";
    public string PasteShortcut { get; set; } = "Ctrl+V";
}

public static class ConfigHelper
{
    private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string ConfigPath = Path.Combine(AppDirectory, "settings.json");
    private static readonly string ScrcpyDirectory = Path.Combine(AppDirectory, "scrcpy");
    
    public static string DefaultAdbPath => Path.Combine(ScrcpyDirectory, "adb.exe");
    public static string DefaultScrcpyPath => Path.Combine(ScrcpyDirectory, "scrcpy.exe");

    static ConfigHelper()
    {
        // 不需要创建 AppData 文件夹了
    }

    public static AppConfig LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var dto = JsonSerializer.Deserialize<ConfigDto>(json);
                if (dto != null)
                {
                    var config = new AppConfig
                    {
                        MaxSize = dto.MaxSize,
                        BitRate = dto.BitRate,
                        MaxFps = dto.MaxFps,
                        EnableAudio = dto.EnableAudio,
                        EnableTouchControl = dto.EnableTouchControl,
                        WindowBorderless = dto.WindowBorderless,
                        WindowAlwaysOnTop = dto.WindowAlwaysOnTop,
                        ScreenshotSavePath = dto.ScreenshotSavePath,
                        RecordSavePath = dto.RecordSavePath,
                        EnableLogging = dto.EnableLogging,
                        ScrcpyPath = dto.ScrcpyPath,
                        AdbPath = dto.AdbPath,
                        EnableFloatingWindow = dto.EnableFloatingWindow,
                        EnableInputFloatingWindow = dto.EnableInputFloatingWindow,
                        AutoShowOnKeyboard = dto.AutoShowOnKeyboard,
                        SendToDeviceShortcutKey = dto.SendToDeviceShortcutKey,
                        SendWithEnterShortcutKey = dto.SendWithEnterShortcutKey,
                        ShowFloatingWindowShortcutKey = dto.ShowFloatingWindowShortcutKey,
                        KeyboardPollingInterval = dto.KeyboardPollingInterval,
                        KeyboardShowDebounce = dto.KeyboardShowDebounce,
                        KeyboardHideDebounce = dto.KeyboardHideDebounce,
                        PositionUpdateInterval = dto.PositionUpdateInterval,
                        ScrcpyStartupDelay = dto.ScrcpyStartupDelay,
                        TextTransferMode = (TextTransferMode)dto.TextTransferMode,
                        LastWifiIp = dto.LastWifiIp,
                        LastWifiPort = dto.LastWifiPort,
                        RecordOnStart = dto.RecordOnStart,
                        BlackScreenOnStart = dto.BlackScreenOnStart,
                        ShowTouch = dto.ShowTouch,
                        RotateMirror = dto.RotateMirror,
                        FullscreenOnStart = dto.FullscreenOnStart,
                        LockScreenOnClose = dto.LockScreenOnClose,
                        BackKeyShortcut = dto.BackKeyShortcut,
                        HomeKeyShortcut = dto.HomeKeyShortcut,
                        FullscreenShortcut = dto.FullscreenShortcut,
                        Window1x1Shortcut = dto.Window1x1Shortcut,
                        RecentAppsShortcut = dto.RecentAppsShortcut,
                        VolumeUpShortcut = dto.VolumeUpShortcut,
                        VolumeDownShortcut = dto.VolumeDownShortcut,
                        RotateMirrorShortcut = dto.RotateMirrorShortcut,
                        LockScreenShortcut = dto.LockScreenShortcut,
                        TurnScreenOnShortcut = dto.TurnScreenOnShortcut,
                        TurnScreenOffShortcut = dto.TurnScreenOffShortcut,
                        ExpandNotificationShortcut = dto.ExpandNotificationShortcut,
                        CopyShortcut = dto.CopyShortcut,
                        PasteShortcut = dto.PasteShortcut
                    };
                    
                    // 兼容旧配置：如果旧配置有SendShortcutKey但没有新配置，则使用旧值
                    if (!string.IsNullOrEmpty(dto.SendShortcutKey) && 
                        (string.IsNullOrEmpty(config.SendToDeviceShortcutKey) || string.IsNullOrEmpty(config.SendWithEnterShortcutKey)))
                    {
                        if (string.IsNullOrEmpty(config.SendWithEnterShortcutKey))
                            config.SendWithEnterShortcutKey = dto.SendShortcutKey;
                    }
                    
                    // 检查并设置默认路径
                    if (string.IsNullOrEmpty(config.AdbPath))
                    {
                        config.AdbPath = DefaultAdbPath;
                    }
                    if (string.IsNullOrEmpty(config.ScrcpyPath))
                    {
                        config.ScrcpyPath = DefaultScrcpyPath;
                    }
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"加载配置失败: {ex.Message}");
        }
        
        // 返回新配置时，设置默认路径
        var defaultConfig = new AppConfig
        {
            AdbPath = DefaultAdbPath,
            ScrcpyPath = DefaultScrcpyPath
        };
        return defaultConfig;
    }

    public static void SaveConfig(AppConfig config)
    {
        try
        {
            var dto = new ConfigDto
            {
                MaxSize = config.MaxSize,
                BitRate = config.BitRate,
                MaxFps = config.MaxFps,
                EnableAudio = config.EnableAudio,
                EnableTouchControl = config.EnableTouchControl,
                WindowBorderless = config.WindowBorderless,
                WindowAlwaysOnTop = config.WindowAlwaysOnTop,
                ScreenshotSavePath = config.ScreenshotSavePath,
                RecordSavePath = config.RecordSavePath,
                EnableLogging = config.EnableLogging,
                ScrcpyPath = config.ScrcpyPath,
                AdbPath = config.AdbPath,
                EnableFloatingWindow = config.EnableFloatingWindow,
                EnableInputFloatingWindow = config.EnableInputFloatingWindow,
                AutoShowOnKeyboard = config.AutoShowOnKeyboard,
                SendToDeviceShortcutKey = config.SendToDeviceShortcutKey,
                SendWithEnterShortcutKey = config.SendWithEnterShortcutKey,
                ShowFloatingWindowShortcutKey = config.ShowFloatingWindowShortcutKey,
                KeyboardPollingInterval = config.KeyboardPollingInterval,
                KeyboardShowDebounce = config.KeyboardShowDebounce,
                KeyboardHideDebounce = config.KeyboardHideDebounce,
                PositionUpdateInterval = config.PositionUpdateInterval,
                ScrcpyStartupDelay = config.ScrcpyStartupDelay,
                TextTransferMode = (TextTransferModeDto)config.TextTransferMode,
                LastWifiIp = config.LastWifiIp,
                LastWifiPort = config.LastWifiPort,
                RecordOnStart = config.RecordOnStart,
                BlackScreenOnStart = config.BlackScreenOnStart,
                ShowTouch = config.ShowTouch,
                RotateMirror = config.RotateMirror,
                FullscreenOnStart = config.FullscreenOnStart,
                LockScreenOnClose = config.LockScreenOnClose,
                BackKeyShortcut = config.BackKeyShortcut,
                HomeKeyShortcut = config.HomeKeyShortcut,
                FullscreenShortcut = config.FullscreenShortcut,
                Window1x1Shortcut = config.Window1x1Shortcut,
                RecentAppsShortcut = config.RecentAppsShortcut,
                VolumeUpShortcut = config.VolumeUpShortcut,
                VolumeDownShortcut = config.VolumeDownShortcut,
                RotateMirrorShortcut = config.RotateMirrorShortcut,
                LockScreenShortcut = config.LockScreenShortcut,
                TurnScreenOnShortcut = config.TurnScreenOnShortcut,
                TurnScreenOffShortcut = config.TurnScreenOffShortcut,
                ExpandNotificationShortcut = config.ExpandNotificationShortcut,
                CopyShortcut = config.CopyShortcut,
                PasteShortcut = config.PasteShortcut
            };
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(dto, options);
            File.WriteAllText(ConfigPath, json);
            LogHelper.Info($"配置已保存到: {ConfigPath}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"保存配置失败: {ex.Message}");
        }
    }
}