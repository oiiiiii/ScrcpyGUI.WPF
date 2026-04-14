namespace ScrcpyGUI.WPF.Models;

public enum TextTransferMode
{
    CopyPaste,
    TextInjection
}

public class AppConfig
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
    public TextTransferMode TextTransferMode { get; set; } = TextTransferMode.TextInjection;
    public string LastWifiIp { get; set; } = string.Empty;
    public int LastWifiPort { get; set; } = 5555;
}