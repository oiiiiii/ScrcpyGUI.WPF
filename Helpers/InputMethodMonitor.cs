using System.Diagnostics;
using System.Text.RegularExpressions;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.Helpers;

public class InputMethodMonitor : IDisposable
{
    private readonly string _serialNumber;
    private System.Windows.Threading.DispatcherTimer? _timer;
    private bool _isKeyboardVisible;
    private string _currentForegroundPackage = string.Empty;
    private readonly Stopwatch _showDebounce = new();
    private readonly Stopwatch _hideDebounce = new();
    private readonly int _showDebounceMs;
    private readonly int _hideDebounceMs;
    private bool _disposed;
    
    private static readonly Regex _foregroundPackageRegex = new(@"mCurrentFocus.*u0\s+([\w.]+)/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public event EventHandler<bool>? KeyboardVisibilityChanged;
    public event EventHandler<string>? ForegroundPackageChanged;

    public bool IsKeyboardVisible => _isKeyboardVisible;
    public string CurrentForegroundPackage => _currentForegroundPackage;

    public InputMethodMonitor(string serialNumber, int pollingIntervalMs = 350, int showDebounceMs = 200, int hideDebounceMs = 300)
    {
        _serialNumber = serialNumber;
        _showDebounceMs = showDebounceMs;
        _hideDebounceMs = hideDebounceMs;
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(pollingIntervalMs)
        };
        _timer.Tick += OnTimerTick;
    }

    public void Start()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(InputMethodMonitor));
        LogHelper.Info("开始监控键盘状态和前台应用");
        _timer.Start();
    }

    public void Stop()
    {
        if (_disposed) return;
        LogHelper.Info("停止监控键盘状态和前台应用");
        _timer.Stop();
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            var isVisible = await IsKeyboardVisibleAsync();
            var foregroundPackage = await GetForegroundPackageAsync();

            UpdateKeyboardVisibility(isVisible);
            UpdateForegroundPackage(foregroundPackage);
        }
        catch (Exception ex)
        {
            LogHelper.Warning($"监控更新失败: {ex.Message}");
        }
    }

    private void UpdateKeyboardVisibility(bool isVisible)
    {
        if (isVisible)
        {
            if (!_showDebounce.IsRunning)
            {
                _showDebounce.Restart();
            }
            else if (_showDebounce.ElapsedMilliseconds >= _showDebounceMs && !_isKeyboardVisible)
            {
                _isKeyboardVisible = true;
                _hideDebounce.Reset();
                KeyboardVisibilityChanged?.Invoke(this, true);
                LogHelper.Info("检测到软键盘弹出");
            }
        }
        else
        {
            _showDebounce.Reset();
            if (!_hideDebounce.IsRunning)
            {
                _hideDebounce.Restart();
            }
            else if (_hideDebounce.ElapsedMilliseconds >= _hideDebounceMs && _isKeyboardVisible)
            {
                _isKeyboardVisible = false;
                _showDebounce.Reset();
                KeyboardVisibilityChanged?.Invoke(this, false);
                LogHelper.Info("检测到软键盘收起");
            }
        }
    }

    private void UpdateForegroundPackage(string packageName)
    {
        if (packageName != _currentForegroundPackage)
        {
            _currentForegroundPackage = packageName;
            ForegroundPackageChanged?.Invoke(this, packageName);
            LogHelper.Info($"前台应用切换: {packageName}");
        }
    }

    public async Task<bool> IsKeyboardVisibleAsync()
    {
        try
        {
            var output = await ExecuteAdbCommandAsync("shell dumpsys input_method");
            var isShown = output.Contains("mInputShown=true", StringComparison.OrdinalIgnoreCase);
            return isShown;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetForegroundPackageAsync()
    {
        try
        {
            var output = await ExecuteAdbCommandAsync("shell dumpsys window displays");
            var match = _foregroundPackageRegex.Match(output);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task<string> ExecuteAdbCommandAsync(string arguments)
    {
        return await Task.Run(() =>
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = AdbHelper.AdbPath,
                    Arguments = $"-s {_serialNumber} {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(500);
            return output;
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
    }
}
