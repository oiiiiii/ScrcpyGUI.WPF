using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ScrcpyGUI.WPF.Helpers;
using ScrcpyGUI.WPF.Models;
using ScrcpyGUI.WPF.Views;

namespace ScrcpyGUI.WPF.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private ObservableCollection<DeviceInfo> _devices = new();
    private DeviceInfo? _selectedDevice;
    private AppConfig _config;
    private readonly StringBuilder _logBuilder = new StringBuilder();
    private string _logText = string.Empty;
    private bool _isScrcpyRunning;
    private bool _isToolsConfigured;
    private bool _isSettingsPanelVisible;
    private FloatingWindow? _floatingWindow;
    private InputFloatingWindow? _inputFloatingWindow;
    private InputMethodMonitor? _inputMonitor;
    private ScrcpyTextSender? _textSender;
    private System.Windows.Threading.DispatcherTimer? _positionUpdateTimer;
    private GlobalHotKeyHelper? _hotKeyHelper;

    public ObservableCollection<DeviceInfo> Devices
    {
        get => _devices;
        set => SetProperty(ref _devices, value);
    }

    public DeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                ((RelayCommand)StartScrcpyCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopScrcpyCommand).RaiseCanExecuteChanged();
                ((RelayCommand)TakeScreenshotCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SendKeyCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public AppConfig Config
    {
        get => _config;
        set => SetProperty(ref _config, value);
    }

    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    public bool IsScrcpyRunning
    {
        get => _isScrcpyRunning;
        set
        {
            if (SetProperty(ref _isScrcpyRunning, value))
            {
                ((RelayCommand)StartScrcpyCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopScrcpyCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsToolsConfigured
    {
        get => _isToolsConfigured;
        set
        {
            if (SetProperty(ref _isToolsConfigured, value))
            {
                if (RefreshDevicesCommand != null)
                    ((RelayCommand)RefreshDevicesCommand).RaiseCanExecuteChanged();
                if (StartScrcpyCommand != null)
                    ((RelayCommand)StartScrcpyCommand).RaiseCanExecuteChanged();
                if (TakeScreenshotCommand != null)
                    ((RelayCommand)TakeScreenshotCommand).RaiseCanExecuteChanged();
                if (SendKeyCommand != null)
                    ((RelayCommand)SendKeyCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSettingsPanelVisible
    {
        get => _isSettingsPanelVisible;
        set => SetProperty(ref _isSettingsPanelVisible, value);
    }

    private bool _isAboutPanelVisible;
    public bool IsAboutPanelVisible
    {
        get => _isAboutPanelVisible;
        set => SetProperty(ref _isAboutPanelVisible, value);
    }

    public ICommand RefreshDevicesCommand { get; }
    public ICommand StartScrcpyCommand { get; }
    public ICommand StopScrcpyCommand { get; }
    public ICommand TakeScreenshotCommand { get; }
    public ICommand SendKeyCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand ToggleSettingsCommand { get; }
    public ICommand BrowseScrcpyCommand { get; }
    public ICommand BrowseAdbCommand { get; }
    public ICommand BrowseScreenshotPathCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand ShowWifiConnectWindowCommand { get; }
    public ICommand ShowAboutWindowCommand { get; }
    public ICommand ToggleAboutPanelCommand { get; }
    public ICommand GoBackToMainCommand { get; }
    public ICommand ToggleFloatingWindowCommand { get; }
    public ICommand ExecuteAdbCommand { get; }
    public ICommand ClearAdbCommand { get; }

    private string _adbCommandText = string.Empty;
    private string _adbCommandOutput = string.Empty;
    private bool _isAdbCommandExecuting;
    private string _adbCommandStatus = string.Empty;

    public string AdbCommandText
    {
        get => _adbCommandText;
        set => SetProperty(ref _adbCommandText, value);
    }

    public string AdbCommandOutput
    {
        get => _adbCommandOutput;
        set => SetProperty(ref _adbCommandOutput, value);
    }

    public bool IsAdbCommandExecuting
    {
        get => _isAdbCommandExecuting;
        set => SetProperty(ref _isAdbCommandExecuting, value);
    }

    public string AdbCommandStatus
    {
        get => _adbCommandStatus;
        set => SetProperty(ref _adbCommandStatus, value);
    }

    public MainViewModel()
    {
        _config = ConfigHelper.LoadConfig();
        
        RefreshDevicesCommand = new RelayCommand(_ => RefreshDevices(), _ => CanRefreshDevices());
        StartScrcpyCommand = new RelayCommand(_ => StartScrcpy(), _ => CanStartScrcpy());
        StopScrcpyCommand = new RelayCommand(_ => StopScrcpy(), _ => CanStopScrcpy());
        TakeScreenshotCommand = new RelayCommand(_ => TakeScreenshot(), _ => SelectedDevice != null && IsToolsConfigured);
        SendKeyCommand = new RelayCommand(param => SendKey(param), _ => SelectedDevice != null && IsToolsConfigured);
        ClearLogCommand = new RelayCommand(_ => ClearLog());
        ToggleFloatingWindowCommand = new RelayCommand(_ => ToggleFloatingWindow());
        ToggleSettingsCommand = new RelayCommand(_ => ToggleSettings());
        BrowseScrcpyCommand = new RelayCommand(_ => BrowseScrcpy());
        BrowseAdbCommand = new RelayCommand(_ => BrowseAdb());
        BrowseScreenshotPathCommand = new RelayCommand(_ => BrowseScreenshotPath());
        SaveConfigCommand = new RelayCommand(_ => SaveConfig());
        ShowWifiConnectWindowCommand = new RelayCommand(_ => ShowWifiConnectWindow());
        ShowAboutWindowCommand = new RelayCommand(_ => ShowAboutPanel());
        ToggleAboutPanelCommand = new RelayCommand(_ => ToggleAboutPanel());
        ExecuteAdbCommand = new RelayCommand(async _ => await ExecuteAdbCommandAsync(), _ => !IsAdbCommandExecuting && SelectedDevice != null && IsToolsConfigured);
        ClearAdbCommand = new RelayCommand(_ => ClearAdbOutput());
        GoBackToMainCommand = new RelayCommand(_ => GoBackToMain());

        LogHelper.LogMessage += OnLogMessage;
        ScrcpyHelper.ScrcpyStarted += OnScrcpyStarted;
        ScrcpyHelper.ScrcpyExited += OnScrcpyExited;

        InitializeTools();
        
        ConfigHelper.SaveConfig(_config);

        if (!IsToolsConfigured)
        {
            IsSettingsPanelVisible = true;
        }

        if (IsToolsConfigured)
        {
            RefreshDevices();
        }
    }

    public void InitializeTools()
    {
        if (!string.IsNullOrWhiteSpace(Config.AdbPath))
        {
            AdbHelper.UpdatePaths(Config.AdbPath);
        }
        if (!string.IsNullOrWhiteSpace(Config.ScrcpyPath))
        {
            ScrcpyHelper.UpdatePaths(Config.ScrcpyPath);
        }

        IsToolsConfigured = AdbHelper.IsAdbAvailable() && ScrcpyHelper.IsScrcpyAvailable();

        if (!IsToolsConfigured)
        {
            LogHelper.Warning("请先在设置中配置 scrcpy 和 adb 路径");
        }
        else
        {
            LogHelper.Info("工具路径配置成功");
        }
    }

    private void RefreshDevices()
    {
        if (!IsToolsConfigured)
        {
            LogHelper.Warning("请先配置 scrcpy 和 adb 路径");
            return;
        }

        Devices.Clear();
        var devices = AdbHelper.GetConnectedDevices();
        foreach (var device in devices)
        {
            Devices.Add(device);
        }
        LogHelper.Info($"找到 {devices.Count} 个设备");
    }

    private bool CanRefreshDevices()
    {
        return IsToolsConfigured;
    }

    private bool CanStartScrcpy()
    {
        return SelectedDevice != null && !IsScrcpyRunning && IsToolsConfigured;
    }

    private void ToggleSettings()
    {
        IsAboutPanelVisible = false;
        IsSettingsPanelVisible = !IsSettingsPanelVisible;
    }

    private bool CanStopScrcpy()
    {
        return IsScrcpyRunning;
    }

    private void StartScrcpy()
    {
        if (SelectedDevice != null)
        {
            ScrcpyHelper.StartScrcpy(SelectedDevice, Config);
            ConfigHelper.SaveConfig(Config);
        }
    }

    private void StopScrcpy()
    {
        ScrcpyHelper.StopScrcpy();
    }

    private void TakeScreenshot()
    {
        if (SelectedDevice != null)
        {
            var savedPath = AdbHelper.TakeScreenshot(SelectedDevice.SerialNumber, Config.ScreenshotSavePath);
            if (!string.IsNullOrEmpty(savedPath))
            {
                if (_floatingWindow != null && Config.EnableFloatingWindow)
                {
                    _floatingWindow.ShowNotification($"截图已保存\n{savedPath}");
                }
            }
        }
    }

    private void SendKey(object? param)
    {
        if (SelectedDevice == null)
            return;

        int keyCode;
        if (param is int intParam)
        {
            keyCode = intParam;
        }
        else if (param is string stringParam && int.TryParse(stringParam, out var parsedCode))
        {
            keyCode = parsedCode;
        }
        else
        {
            return;
        }

        AdbHelper.SendKeyEvent(SelectedDevice.SerialNumber, keyCode);
    }

    private void ClearLog()
    {
        LogText = string.Empty;
    }

    private readonly object _logLock = new object();
    private const int MaxLogLines = 1000;

    private void OnLogMessage(string message)
    {
        lock (_logLock)
        {
            _logBuilder.AppendLine(message);
            
            // 限制日志行数，防止内存溢出
            var lines = _logBuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            if (lines.Length > MaxLogLines)
            {
                _logBuilder.Clear();
                foreach (var line in lines.Skip(lines.Length - MaxLogLines))
                {
                    _logBuilder.AppendLine(line);
                }
            }
            
            LogText = _logBuilder.ToString();
        }
    }

    private void OnScrcpyStarted()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
        {
            IsScrcpyRunning = true;

            if (Config.EnableInputFloatingWindow && SelectedDevice != null)
            {
                await InitializeInputFloatingWindowAsync();
            }
        });
    }

    private void OnScrcpyExited()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsScrcpyRunning = false;
            HideFloatingWindow();
            CleanupInputFloatingWindow();
            WindowHelper.ResetScrcpyWindowLog();
            
            if (Config.LockScreenOnClose && SelectedDevice != null)
            {
                LogHelper.Info("关闭后锁屏");
                AdbHelper.SendKeyEvent(SelectedDevice.SerialNumber, 26);
            }
        });
    }

    private async System.Threading.Tasks.Task InitializeInputFloatingWindowAsync()
    {
        if (SelectedDevice == null) return;

        try
        {
            _inputMonitor = new InputMethodMonitor(
                SelectedDevice.SerialNumber,
                Config.KeyboardPollingInterval,
                Config.KeyboardShowDebounce,
                Config.KeyboardHideDebounce);
            _inputMonitor.KeyboardVisibilityChanged += OnKeyboardVisibilityChanged;
            _inputMonitor.ForegroundPackageChanged += OnForegroundPackageChanged;

            _textSender = new ScrcpyTextSender(SelectedDevice.SerialNumber);
            
            await System.Threading.Tasks.Task.Delay(Config.ScrcpyStartupDelay);
            
            try
            {
                await _textSender.ConnectAsync();
            }
            catch
            {
            }

            ShowInputFloatingWindow();
            
            _inputMonitor.Start();

            _positionUpdateTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(Config.PositionUpdateInterval)
            };
            _positionUpdateTimer.Tick += OnPositionUpdateTimerTick;
            _positionUpdateTimer.Start();
        }
        catch (Exception ex)
        {
            LogHelper.Error($"智能输入功能初始化失败: {ex.Message}");
        }
    }

    private void CleanupInputFloatingWindow()
    {
        if (_positionUpdateTimer != null)
        {
            _positionUpdateTimer.Stop();
            _positionUpdateTimer.Tick -= OnPositionUpdateTimerTick;
            _positionUpdateTimer = null;
        }

        if (_inputMonitor != null)
        {
            _inputMonitor.Stop();
            _inputMonitor.KeyboardVisibilityChanged -= OnKeyboardVisibilityChanged;
            _inputMonitor.ForegroundPackageChanged -= OnForegroundPackageChanged;
            _inputMonitor.Dispose();
            _inputMonitor = null;
        }

        _textSender?.Dispose();
        _textSender = null;

        HideInputFloatingWindow();
    }

    private void ShowInputFloatingWindow()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_inputFloatingWindow == null)
            {
                LogHelper.Info("创建输入悬浮窗实例...");
                _inputFloatingWindow = new InputFloatingWindow();
                LogHelper.Info("连接 SendRequested 事件...");
                _inputFloatingWindow.ViewModel.SendRequested += OnInputSendRequested;
                LogHelper.Info("连接 SendMessageRequested 事件...");
                _inputFloatingWindow.ViewModel.SendMessageRequested += OnInputSendMessageRequested;
                _inputFloatingWindow.SendToDeviceShortcutKey = Config.SendToDeviceShortcutKey;
                _inputFloatingWindow.SendWithEnterShortcutKey = Config.SendWithEnterShortcutKey;
                LogHelper.Info("输入悬浮窗创建完成，事件已连接");
            }
        });
    }
    private void HideInputFloatingWindow()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_inputFloatingWindow != null && _inputFloatingWindow.Visibility == Visibility.Visible)
            {
                _inputFloatingWindow.Visibility = Visibility.Collapsed;
            }
        });
    }

    private void OnKeyboardVisibilityChanged(object? sender, bool isVisible)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_inputFloatingWindow == null) return;

            if (isVisible && Config.AutoShowOnKeyboard)
            {
                UpdateInputWindowPosition();
                _inputFloatingWindow.Visibility = Visibility.Visible;
                
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _inputFloatingWindow.Activate();
                    _inputFloatingWindow.Focus();
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _inputFloatingWindow.FocusInputBox();
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            else
            {
                _inputFloatingWindow.Visibility = Visibility.Collapsed;
            }
        });
    }

    private void OnForegroundPackageChanged(object? sender, string packageName)
    {
        // 不再需要，包名管理已移至设置面板
    }

    private WindowHelper.RECT? _lastWindowRect;
    private const int PositionChangeThreshold = 5; // 位置变化阈值（像素）

    private void OnPositionUpdateTimerTick(object? sender, EventArgs e)
    {
        UpdateInputWindowPosition();
    }

    private void UpdateInputWindowPosition()
    {
        if (_inputFloatingWindow == null || _inputFloatingWindow.Visibility != Visibility.Visible) return;

        try
        {
            var scrcpyWindow = WindowHelper.FindScrcpyWindow(ScrcpyHelper.ProcessId, verbose: false);
            if (scrcpyWindow.HasValue)
            {
                var rect = scrcpyWindow.Value.rect;
                
                // 防抖：只有当位置或大小变化超过阈值时才更新
                if (_lastWindowRect.HasValue)
                {
                    var lastRect = _lastWindowRect.Value;
                    var positionChanged = Math.Abs(rect.Left - lastRect.Left) > PositionChangeThreshold ||
                                         Math.Abs(rect.Top - lastRect.Top) > PositionChangeThreshold ||
                                         Math.Abs(rect.Width - lastRect.Width) > PositionChangeThreshold ||
                                         Math.Abs(rect.Height - lastRect.Height) > PositionChangeThreshold;
                    
                    if (!positionChanged)
                    {
                        return; // 位置变化不大，跳过更新
                    }
                }
                
                _lastWindowRect = rect;
                
                var windowWidth = rect.Width;
                var windowHeight = rect.Height * 0.30;

                _inputFloatingWindow.Width = Math.Max(300, windowWidth);
                _inputFloatingWindow.Height = Math.Max(120, windowHeight);
                _inputFloatingWindow.Left = rect.Left;
                _inputFloatingWindow.Top = rect.Bottom - _inputFloatingWindow.Height;
            }
            else
            {
                _lastWindowRect = null;
            }
        }
        catch
        {
        }
    }

    private async void OnInputSendRequested(object? sender, string text)
    {
        if (SelectedDevice == null) return;

        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            
            if (Config.TextTransferMode == TextTransferMode.TextInjection)
            {
                if (_textSender != null)
                {
                    await _textSender.SendTextAsync(text);
                }
                else
                {
                    AdbHelper.SendText(SelectedDevice.SerialNumber, text);
                }
            }
            else
            {
                AdbHelper.SendText(SelectedDevice.SerialNumber, text);
            }

            _inputFloatingWindow?.ShowMessage("发送成功！");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"发送失败: {ex.Message}");
            _inputFloatingWindow?.ShowMessage($"发送失败: {ex.Message}");
        }
    }

    private async void OnInputSendMessageRequested(object? sender, string text)
    {
        if (SelectedDevice == null) return;

        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            
            if (Config.TextTransferMode == TextTransferMode.TextInjection)
            {
                if (_textSender != null)
                {
                    await _textSender.SendTextAsync(text);
                    await System.Threading.Tasks.Task.Delay(100);
                    await _textSender.SendEnterKeyAsync();
                }
                else
                {
                    AdbHelper.SendText(SelectedDevice.SerialNumber, text);
                    await System.Threading.Tasks.Task.Delay(100);
                    AdbHelper.SendKeyEvent(SelectedDevice.SerialNumber, 66);
                }
            }
            else
            {
                AdbHelper.SendText(SelectedDevice.SerialNumber, text);
                await System.Threading.Tasks.Task.Delay(100);
                AdbHelper.SendKeyEvent(SelectedDevice.SerialNumber, 66);
            }

            _inputFloatingWindow?.ShowMessage("发送成功！");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"发送失败: {ex.Message}");
            _inputFloatingWindow?.ShowMessage($"发送失败: {ex.Message}");
        }
    }

    private void ShowFloatingWindow()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_floatingWindow == null)
            {
                _floatingWindow = new FloatingWindow();
                _floatingWindow.ViewModel.TakeScreenshotRequested += OnFloatingTakeScreenshot;
                _floatingWindow.ViewModel.KeyEventRequested += OnFloatingKeyEvent;
                _floatingWindow.ViewModel.ExpandNotificationRequested += OnFloatingExpandNotification;
            }
            
            if (!_floatingWindow.IsVisible)
            {
                _floatingWindow.Show();
            }
        });
    }

    private void HideFloatingWindow()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_floatingWindow != null && _floatingWindow.IsVisible)
            {
                _floatingWindow.Hide();
            }
        });
    }

    private void ToggleFloatingWindow()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_floatingWindow == null)
            {
                ShowFloatingWindow();
            }
            else
            {
                if (_floatingWindow.IsVisible)
                {
                    HideFloatingWindow();
                }
                else
                {
                    ShowFloatingWindow();
                }
            }
        });
    }

    private void OnFloatingTakeScreenshot(object? sender, EventArgs e)
    {
        TakeScreenshot();
    }

    private void OnFloatingKeyEvent(object? sender, int keyCode)
    {
        SendKey(keyCode);
    }

    private void OnFloatingExpandNotification(object? sender, EventArgs e)
    {
        if (SelectedDevice != null)
        {
            AdbHelper.ExpandNotification(SelectedDevice.SerialNumber);
        }
    }

    private void BrowseScrcpy()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            Title = "选择 scrcpy.exe 文件",
            FileName = "scrcpy.exe"
        };

        if (dialog.ShowDialog() == true)
        {
            Config.ScrcpyPath = dialog.FileName;
            OnPropertyChanged(nameof(Config));
            LogHelper.Info($"Scrcpy 路径已更新: {Config.ScrcpyPath}");
        }
    }

    private void BrowseAdb()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            Title = "选择 adb.exe 文件",
            FileName = "adb.exe"
        };

        if (dialog.ShowDialog() == true)
        {
            Config.AdbPath = dialog.FileName;
            OnPropertyChanged(nameof(Config));
            LogHelper.Info($"ADB 路径已更新: {Config.AdbPath}");
        }
    }

    private void BrowseScreenshotPath()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择截图保存目录",
            ShowNewFolderButton = true,
            SelectedPath = Config.ScreenshotSavePath
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            Config.ScreenshotSavePath = dialog.SelectedPath;
            OnPropertyChanged(nameof(Config));
        }
    }

    private void SaveConfig()
    {
        ConfigHelper.SaveConfig(Config);
        
        AdbHelper.UpdatePaths(Config.AdbPath);
        ScrcpyHelper.UpdatePaths(Config.ScrcpyPath);
        
        InitializeTools();
        
        // 重新注册全局快捷键
        RegisterAllGlobalHotKeys();
        
        LogHelper.Info("配置已保存");
    }

    public void InitializeGlobalHotKeys(GlobalHotKeyHelper hotKeyHelper)
    {
        _hotKeyHelper = hotKeyHelper;
        RegisterAllGlobalHotKeys();
    }

    public void CleanupGlobalHotKeys()
    {
        _hotKeyHelper?.UnregisterAllHotKeys();
    }

    private void RegisterAllGlobalHotKeys()
    {
        if (_hotKeyHelper == null) return;

        _hotKeyHelper.UnregisterAllHotKeys();
        
        _hotKeyHelper.RegisterHotKey("SendToDevice", Config.SendToDeviceShortcutKey);
        _hotKeyHelper.RegisterHotKey("SendWithEnter", Config.SendWithEnterShortcutKey);
        _hotKeyHelper.RegisterHotKey("ShowFloatingWindow", Config.ShowFloatingWindowShortcutKey);
    }

    public void OnGlobalHotKeyPressed(string hotKeyName)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            switch (hotKeyName)
            {
                case "SendToDevice":
                    if (_inputFloatingWindow != null && _inputFloatingWindow.Visibility == Visibility.Visible)
                    {
                        _inputFloatingWindow.ViewModel.SendTextFromWindow();
                    }
                    break;
                    
                case "SendWithEnter":
                    if (_inputFloatingWindow != null && _inputFloatingWindow.Visibility == Visibility.Visible)
                    {
                        _inputFloatingWindow.ViewModel.SendMessageFromWindow();
                    }
                    break;
                    
                case "ShowFloatingWindow":
                    ToggleInputFloatingWindow();
                    break;
            }
        });
    }

    private void ToggleInputFloatingWindow()
    {
        if (_inputFloatingWindow == null) return;

        if (_inputFloatingWindow.Visibility == Visibility.Visible)
        {
            _inputFloatingWindow.Visibility = Visibility.Collapsed;
        }
        else
        {
            UpdateInputWindowPosition();
            _inputFloatingWindow.Visibility = Visibility.Visible;
            _inputFloatingWindow.Activate();
            _inputFloatingWindow.Focus();
            _inputFloatingWindow.FocusInputBox();
        }
    }

    private void ShowWifiConnectWindow()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var wifiWindow = new WifiConnectWindow(_config);
                wifiWindow.Owner = mainWindow;
                wifiWindow.DeviceConnected += (s, e) =>
                {
                    RefreshDevices();
                };
                wifiWindow.ShowDialog();
            }
        });
    }

    private void ShowAboutPanel()
    {
        IsSettingsPanelVisible = false;
        IsAboutPanelVisible = true;
    }

    private void ToggleAboutPanel()
    {
        IsAboutPanelVisible = !IsAboutPanelVisible;
    }

    private void GoBackToMain()
    {
        IsSettingsPanelVisible = false;
        IsAboutPanelVisible = false;
    }

    private async System.Threading.Tasks.Task ExecuteAdbCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(AdbCommandText))
        {
            AdbCommandStatus = "请输入 ADB 命令";
            return;
        }

        if (SelectedDevice == null)
        {
            AdbCommandStatus = "请先选择设备";
            return;
        }

        IsAdbCommandExecuting = true;
        AdbCommandStatus = "执行中...";
        AdbCommandOutput = string.Empty;

        try
        {
            var command = AdbCommandText.Trim();
            
            // 如果命令以 "adb " 开头，去掉前缀
            if (command.StartsWith("adb ", StringComparison.OrdinalIgnoreCase))
            {
                command = command.Substring(4).Trim();
            }
            
            // 如果命令不是以 -s 开头，自动添加设备序列号
            if (!command.StartsWith("-s", StringComparison.OrdinalIgnoreCase))
            {
                command = $"-s {SelectedDevice.SerialNumber} {command}";
            }

            var (output, error, exitCode) = await AdbHelper.ExecuteAdbCommandAsync(command);

            var resultBuilder = new StringBuilder();
            
            if (!string.IsNullOrEmpty(output))
            {
                resultBuilder.AppendLine("[标准输出]");
                resultBuilder.AppendLine(output);
            }

            if (!string.IsNullOrEmpty(error))
            {
                resultBuilder.AppendLine("[错误输出]");
                resultBuilder.AppendLine(error);
            }

            AdbCommandOutput = resultBuilder.ToString();

            if (exitCode == 0)
            {
                AdbCommandStatus = "执行成功";
            }
            else if (exitCode == -1)
            {
                AdbCommandStatus = "执行超时";
            }
            else
            {
                AdbCommandStatus = $"执行失败 (退出代码: {exitCode})";
            }
        }
        catch (Exception ex)
        {
            AdbCommandStatus = $"执行异常: {ex.Message}";
            AdbCommandOutput = $"异常详情:\n{ex}";
        }
        finally
        {
            IsAdbCommandExecuting = false;
            ((RelayCommand)ExecuteAdbCommand).RaiseCanExecuteChanged();
        }
    }

    private void ClearAdbOutput()
    {
        AdbCommandOutput = string.Empty;
        AdbCommandStatus = string.Empty;
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        LogHelper.LogMessage -= OnLogMessage;
        ScrcpyHelper.ScrcpyStarted -= OnScrcpyStarted;
        ScrcpyHelper.ScrcpyExited -= OnScrcpyExited;

        // 停止 scrcpy 进程
        if (ScrcpyHelper.IsRunning)
        {
            LogHelper.Info("Dispose: 停止 scrcpy 进程");
            ScrcpyHelper.StopScrcpy();
        }

        CleanupInputFloatingWindow();
        
        if (_floatingWindow != null)
        {
            _floatingWindow.ViewModel.TakeScreenshotRequested -= OnFloatingTakeScreenshot;
            _floatingWindow.ViewModel.KeyEventRequested -= OnFloatingKeyEvent;
            _floatingWindow.ViewModel.ExpandNotificationRequested -= OnFloatingExpandNotification;
            _floatingWindow.Close();
            _floatingWindow = null;
        }

        if (_positionUpdateTimer != null)
        {
            _positionUpdateTimer.Stop();
            _positionUpdateTimer.Tick -= OnPositionUpdateTimerTick;
            _positionUpdateTimer = null;
        }

        CleanupGlobalHotKeys();
    }
}