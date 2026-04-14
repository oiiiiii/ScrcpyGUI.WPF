using System.Diagnostics;
using System.IO;
using System.Text;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.Helpers;

public static class ScrcpyHelper
{
    private static string? _scrcpyPath;
    private static Process? _currentProcess;

    static ScrcpyHelper()
    {
        _scrcpyPath = "scrcpy";
    }

    public static event Action? ScrcpyStarted;
    public static event Action? ScrcpyExited;

    public static void UpdatePaths(string scrcpyPath)
    {
        if (!string.IsNullOrWhiteSpace(scrcpyPath) && File.Exists(scrcpyPath))
        {
            _scrcpyPath = scrcpyPath;
            LogHelper.Info($"Scrcpy 路径已更新: {scrcpyPath}");
        }
    }

    public static bool IsScrcpyAvailable()
    {
        if (string.IsNullOrWhiteSpace(_scrcpyPath) || _scrcpyPath == "scrcpy")
        {
            return false;
        }
        return File.Exists(_scrcpyPath);
    }

    public static bool IsRunning => _currentProcess != null && !_currentProcess.HasExited;
    
    public static int? ProcessId => _currentProcess?.Id;

    public static void StartScrcpy(DeviceInfo device, AppConfig config)
    {
        if (IsRunning)
        {
            LogHelper.Warning("投屏已在运行中");
            return;
        }

        try
        {
            var arguments = BuildArguments(device, config);
            var workingDirectory = Path.GetDirectoryName(_scrcpyPath);
            
            LogHelper.Info($"启动投屏: {_scrcpyPath} {arguments}");
            LogHelper.Info($"工作目录: {workingDirectory}");

            _currentProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _scrcpyPath,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            _currentProcess.EnableRaisingEvents = true;
            _currentProcess.OutputDataReceived += (s, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogHelper.Info($"[scrcpy] {e.Data}");
            };
            _currentProcess.ErrorDataReceived += (s, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogHelper.Error($"[scrcpy] {e.Data}");
            };
            _currentProcess.Exited += OnScrcpyExited;
            
            _currentProcess.Start();
            _currentProcess.BeginOutputReadLine();
            _currentProcess.BeginErrorReadLine();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ScrcpyStarted?.Invoke();
            });
            LogHelper.Info("投屏已启动");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"启动投屏失败: {ex.Message}");
            LogHelper.Error($"异常详情: {ex}");
        }
    }

    public static void StopScrcpy()
    {
        try
        {
            if (_currentProcess != null && !_currentProcess.HasExited)
            {
                LogHelper.Info("正在停止投屏...");
                _currentProcess.Kill();
                // 不使用 WaitForExit，避免阻塞 UI 线程
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"停止投屏失败: {ex.Message}");
        }
        finally
        {
            // 不在 finally 中立即清理，让 Exited 事件处理
            // 这样可以避免资源竞争
        }
    }

    private static string BuildArguments(DeviceInfo device, AppConfig config)
    {
        var sb = new StringBuilder();
        
        sb.Append($"-s {device.SerialNumber}");
        
        if (config.MaxSize > 0)
        {
            sb.Append($" --max-size={config.MaxSize}");
        }
        
        if (config.BitRate > 0)
        {
            sb.Append($" --video-bit-rate={config.BitRate}M");
        }
        else
        {
            sb.Append(" --video-bit-rate=16M");
        }
        
        if (config.MaxFps > 0)
        {
            sb.Append($" --max-fps={config.MaxFps}");
        }
        
        if (!config.EnableAudio)
        {
            sb.Append(" --no-audio");
        }
        
        if (!config.EnableTouchControl)
        {
            sb.Append(" --no-control");
        }
        
        if (config.WindowBorderless)
        {
            sb.Append(" --window-borderless");
        }
        
        if (config.WindowAlwaysOnTop)
        {
            sb.Append(" --always-on-top");
        }
        
        sb.Append(" --video-codec=h264");
        sb.Append(" --no-power-on");

        return sb.ToString();
    }

    private static void OnScrcpyExited(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ScrcpyExited?.Invoke();
        });
        
        if (_currentProcess != null)
        {
            var exitCode = _currentProcess.ExitCode;
            if (exitCode != 0)
            {
                LogHelper.Error($"投屏异常停止，退出代码: {exitCode}");
            }
            else
            {
                LogHelper.Info("投屏已停止");
            }
            
            try
            {
                _currentProcess.Dispose();
            }
            catch
            {
                // 忽略 Dispose 异常
            }
            _currentProcess = null;
        }
        else
        {
            LogHelper.Info("投屏已停止");
        }
    }
}