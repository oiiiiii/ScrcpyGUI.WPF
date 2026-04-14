using System.IO;
using System.Text.Json;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.Helpers;

public static class ConfigHelper
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public static AppConfig LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
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
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigPath, json);
            LogHelper.Info("配置已保存");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"保存配置失败: {ex.Message}");
        }
    }
}