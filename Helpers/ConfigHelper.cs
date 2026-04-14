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
}

public static class ConfigHelper
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "ScrcpyGui");
    
    private static readonly string ConfigPath = Path.Combine(AppDataFolder, "settings.json");

    static ConfigHelper()
    {
        if (!Directory.Exists(AppDataFolder))
        {
            Directory.CreateDirectory(AppDataFolder);
        }
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
                        TextTransferMode = (TextTransferMode)dto.TextTransferMode
                    };
                    
                    // 兼容旧配置：如果旧配置有SendShortcutKey但没有新配置，则使用旧值
                    if (!string.IsNullOrEmpty(dto.SendShortcutKey) && 
                        (string.IsNullOrEmpty(config.SendToDeviceShortcutKey) || string.IsNullOrEmpty(config.SendWithEnterShortcutKey)))
                    {
                        if (string.IsNullOrEmpty(config.SendWithEnterShortcutKey))
                            config.SendWithEnterShortcutKey = dto.SendShortcutKey;
                    }
                    
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"加载配置失败: {ex.Message}");
        }
        return new AppConfig();
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
                TextTransferMode = (TextTransferModeDto)config.TextTransferMode
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