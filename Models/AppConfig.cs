namespace ScrcpyGUI.WPF.Models;

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
}