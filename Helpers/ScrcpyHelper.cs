using System.Diagnostics;
using System.IO;
using System.Text;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.Helpers;

public static class ScrcpyHelper
{
    private static string? _scrcpyPath;
    private static Process? _currentProcess;
    private static int? _currentProcessId;

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
            _currentProcessId = _currentProcess.Id;
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
        StopScrcpyInternal(false);
    }

    public static void ForceStopScrcpy()
    {
        StopScrcpyInternal(true);
    }

    private static void StopScrcpyInternal(bool force)
    {
        try
        {
            if (_currentProcess != null && !_currentProcess.HasExited)
            {
                LogHelper.Info("正在停止投屏...");
                
                try
                {
                    // 首先尝试优雅关闭（发送 Ctrl+C）
                    if (!force)
                    {
                        _currentProcess.CancelErrorRead();
                        _currentProcess.CancelOutputRead();
                        
                        try
                        {
                            _currentProcess.CloseMainWindow();
                            // 等待进程优雅退出
                            if (_currentProcess.WaitForExit(3000))
                            {
                                LogHelper.Info("投屏已优雅停止");
                                return;
                            }
                        }
                        catch
                        {
                            // CloseMainWindow 可能失败（非 GUI 进程），继续使用 Kill
                        }
                    }
                    
                    // 强制终止
                    LogHelper.Info("强制终止投屏进程...");
                    _currentProcess.Kill();
                    
                    // 等待进程终止
                    if (_currentProcess.WaitForExit(5000))
                    {
                        LogHelper.Info("投屏已强制停止");
                    }
                    else
                    {
                        LogHelper.Warning("强制终止超时");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"停止投屏失败: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"停止投屏异常: {ex.Message}");
        }
        finally
        {
            CleanupProcess();
        }
    }

    private static void CleanupProcess()
    {
        if (_currentProcess != null)
        {
            try
            {
                _currentProcess.CancelErrorRead();
                _currentProcess.CancelOutputRead();
                _currentProcess.Dispose();
            }
            catch
            {
                // 忽略清理异常
            }
            _currentProcess = null;
            _currentProcessId = null;
        }
    }

    public static void CleanupAllScrcpyProcesses()
    {
        LogHelper.Info("开始清理所有 scrcpy 相关进程...");
        
        // 停止当前进程
        if (IsRunning)
        {
            ForceStopScrcpy();
        }
        
        // 查找并杀死所有残留的 scrcpy 进程
        KillAllScrcpyProcesses();
        
        LogHelper.Info("scrcpy 进程清理完成");
    }

    public static void KillAllScrcpyProcesses()
    {
        try
        {
            string scrcpyExeName = Path.GetFileName(_scrcpyPath) ?? "scrcpy.exe";
            
            // 获取所有 scrcpy 进程
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(scrcpyExeName));
            
            if (processes.Length > 0)
            {
                LogHelper.Info($"发现 {processes.Length} 个 scrcpy 进程需要清理");
                
                foreach (var process in processes)
                {
                    try
                    {
                        // 检查是否是我们启动的进程
                        bool isOurProcess = _currentProcessId.HasValue && process.Id == _currentProcessId.Value;
                        
                        LogHelper.Info($"终止进程: {process.ProcessName} (PID: {process.Id}){(isOurProcess ? " - 当前进程" : " - 残留进程")}");
                        
                        process.Kill();
                        if (process.WaitForExit(2000))
                        {
                            LogHelper.Info($"进程 {process.Id} 已终止");
                        }
                        else
                        {
                            LogHelper.Warning($"进程 {process.Id} 终止超时");
                        }
                        
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"终止进程失败: {ex.Message}");
                    }
                }
            }
            else
            {
                LogHelper.Info("未发现残留的 scrcpy 进程");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"清理 scrcpy 进程失败: {ex.Message}");
        }
    }

    public static bool HasResidualProcesses()
    {
        try
        {
            string scrcpyExeName = Path.GetFileName(_scrcpyPath) ?? "scrcpy.exe";
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(scrcpyExeName));
            return processes.Length > 0;
        }
        catch
        {
            return false;
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
        
        if (config.RecordOnStart)
        {
            sb.Append($" --record={Path.Combine(config.RecordSavePath, $"scrcpy_{DateTime.Now:yyyyMMdd_HHmmss}.mp4")}");
        }
        
        if (config.BlackScreenOnStart)
        {
            sb.Append(" --turn-screen-off");
        }
        
        if (config.ShowTouch)
        {
            sb.Append(" --show-touches");
        }
        
        if (config.RotateMirror)
        {
            sb.Append(" --rotation=270");
        }
        
        if (config.FullscreenOnStart)
        {
            sb.Append(" --fullscreen");
        }
        
        sb.Append(" --video-codec=h264");
        sb.Append(" --no-power-on");
        sb.Append(" --push-target=/sdcard/Download/");

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

    public static void PushFile(string deviceSerial, string filePath, string adbPath)
    {
        if (!File.Exists(filePath))
        {
            LogHelper.Error($"文件不存在: {filePath}");
            return;
        }

        try
        {
            var fileName = Path.GetFileName(filePath);
            var targetPath = $"/sdcard/Download/{fileName}";
            
            LogHelper.Info($"正在推送文件: {filePath} -> {targetPath}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = string.IsNullOrWhiteSpace(adbPath) ? "adb" : adbPath,
                    Arguments = $"-s {deviceSerial} push \"{filePath}\" {targetPath}",
                    WorkingDirectory = Path.GetDirectoryName(adbPath) ?? Environment.CurrentDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogHelper.Info($"[adb push] {e.Data}");
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogHelper.Error($"[adb push] {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                LogHelper.Info($"文件推送成功: {fileName}");
            }
            else
            {
                LogHelper.Error($"文件推送失败，退出代码: {process.ExitCode}");
            }

            process.Dispose();
        }
        catch (Exception ex)
        {
            LogHelper.Error($"推送文件失败: {ex.Message}");
        }
    }

    public static void InstallApk(string deviceSerial, string apkPath, string adbPath)
    {
        if (!File.Exists(apkPath))
        {
            LogHelper.Error($"APK文件不存在: {apkPath}");
            return;
        }

        if (!apkPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
        {
            LogHelper.Error("不是有效的APK文件");
            return;
        }

        try
        {
            LogHelper.Info($"正在安装APK: {apkPath}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = string.IsNullOrWhiteSpace(adbPath) ? "adb" : adbPath,
                    Arguments = $"-s {deviceSerial} install -r \"{apkPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(adbPath) ?? Environment.CurrentDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogHelper.Info($"[adb install] {e.Data}");
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogHelper.Error($"[adb install] {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                LogHelper.Info("APK安装成功");
            }
            else
            {
                LogHelper.Error($"APK安装失败，退出代码: {process.ExitCode}");
            }

            process.Dispose();
        }
        catch (Exception ex)
        {
            LogHelper.Error($"安装APK失败: {ex.Message}");
        }
    }

    public static void HandleFileDrop(string deviceSerial, string[] filePaths, string adbPath)
    {
        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath))
                continue;

            if (filePath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            {
                InstallApk(deviceSerial, filePath, adbPath);
            }
            else
            {
                PushFile(deviceSerial, filePath, adbPath);
            }
        }
    }
}