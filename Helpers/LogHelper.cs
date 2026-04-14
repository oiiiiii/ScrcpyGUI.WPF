namespace ScrcpyGUI.WPF.Helpers;

public static class LogHelper
{
    public static event Action<string>? LogMessage;

    public static void Info(string message)
    {
        var logMessage = $"[INFO] [{DateTime.Now:HH:mm:ss}] {message}";
        LogMessage?.Invoke(logMessage);
    }

    public static void Error(string message)
    {
        var logMessage = $"[ERROR] [{DateTime.Now:HH:mm:ss}] {message}";
        LogMessage?.Invoke(logMessage);
    }

    public static void Warning(string message)
    {
        var logMessage = $"[WARN] [{DateTime.Now:HH:mm:ss}] {message}";
        LogMessage?.Invoke(logMessage);
    }
}