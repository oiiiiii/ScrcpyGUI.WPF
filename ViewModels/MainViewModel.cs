using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ScrcpyGUI.WPF.Helpers;
using ScrcpyGUI.WPF.Models;
using ScrcpyGUI.WPF.Views;

namespace ScrcpyGUI.WPF.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ObservableCollection<DeviceInfo> _devices = new();
    private DeviceInfo? _selectedDevice;
    private AppConfig _config;
    private string _logText = string.Empty;
    private bool _isScrcpyRunning;
    private bool _isToolsConfigured;
    private bool _isSettingsPanelVisible;
    private FloatingWindow? _floatingWindow;
    private InputFloatingWindow? _inputFloatingWindow;
    private InputMethodMonitor? _inputMonitor;
    private ScrcpyTextSender? _textSender;
    private System.Windows.Threading.DispatcherTimer? _positionUpdateTimer;

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
    public ICommand AddCurrentAppToPackageListCommand { get; }
    public ICommand ClearPackageListCommand { get; }
    public ICommand RemovePackageCommand { get; }

    public MainViewModel()
    {
        _config = ConfigHelper.LoadConfig();
        
        RefreshDevicesCommand = new RelayCommand(_ => RefreshDevices(), _ => CanRefreshDevices());
        StartScrcpyCommand = new RelayCommand(_ => StartScrcpy(), _ => CanStartScrcpy());
        StopScrcpyCommand = new RelayCommand(_ => StopScrcpy(), _ => CanStopScrcpy());
        TakeScreenshotCommand = new RelayCommand(_ => TakeScreenshot(), _ => SelectedDevice != null && IsToolsConfigured);
        SendKeyCommand = new RelayCommand(param => SendKey(param), _ => SelectedDevice != null && IsToolsConfigured);
        ClearLogCommand = new RelayCommand(_ => ClearLog());
        ToggleSettingsCommand = new RelayCommand(_ => ToggleSettings());
        BrowseScrcpyCommand = new RelayCommand(_ => BrowseScrcpy());
        BrowseAdbCommand = new RelayCommand(_ => BrowseAdb());
        BrowseScreenshotPathCommand = new RelayCommand(_ => BrowseScreenshotPath());
        SaveConfigCommand = new RelayCommand(_ => SaveConfig());
        AddCurrentAppToPackageListCommand = new RelayCommand(_ => AddCurrentAppToPackageList());
        ClearPackageListCommand = new RelayCommand(_ => ClearPackageList());
        RemovePackageCommand = new RelayCommand(param => RemovePackage(param));

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
        IsSettingsPanelVisible = !IsSettingsPanelVisible;
        if (IsSettingsPanelVisible)
        {
            LogHelper.Info("显示设置面板");
        }
        else
        {
            LogHelper.Info("隐藏设置面板");
        }
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

    private void OnLogMessage(string message)
    {
        LogText += message + Environment.NewLine;
    }

    private void OnScrcpyStarted()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
        {
            IsScrcpyRunning = true;
            
            if (Config.EnableFloatingWindow)
            {
                ShowFloatingWindow();
            }

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
        });
    }

    private async System.Threading.Tasks.Task InitializeInputFloatingWindowAsync()
    {
        if (SelectedDevice == null) return;

        try
        {
            LogHelper.Info("初始化智能输入功能...");

            _inputMonitor = new InputMethodMonitor(
                SelectedDevice.SerialNumber,
                Config.KeyboardPollingInterval,
                Config.KeyboardShowDebounce,
                Config.KeyboardHideDebounce);
            _inputMonitor.KeyboardVisibilityChanged += OnKeyboardVisibilityChanged;
            _inputMonitor.ForegroundPackageChanged += OnForegroundPackageChanged;

            _textSender = new ScrcpyTextSender(SelectedDevice.SerialNumber);
            
            LogHelper.Info($"等待 scrcpy 完全启动 ({Config.ScrcpyStartupDelay}ms)...");
            await System.Threading.Tasks.Task.Delay(Config.ScrcpyStartupDelay);
            
            try
            {
                await _textSender.ConnectAsync();
            }
            catch (Exception ex)
            {
                LogHelper.Warning($"连接 scrcpy 控制端口失败，将使用 ADB 方案: {ex.Message}");
            }

            ShowInputFloatingWindow();
            
            _inputMonitor.Start();

            _positionUpdateTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(Config.PositionUpdateInterval)
            };
            _positionUpdateTimer.Tick += OnPositionUpdateTimerTick;
            _positionUpdateTimer.Start();

            LogHelper.Info("✅ 智能输入功能初始化完成");
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
                _inputFloatingWindow.ViewModel.EnableEnterSend = Config.EnableEnterSend;
                _inputFloatingWindow.SendShortcutKey = Config.SendShortcutKey;
                _inputFloatingWindow.ViewModel.PropertyChanged += OnInputViewModelPropertyChanged;
                LogHelper.Info("输入悬浮窗创建完成，事件已连接");
            }
        });
    }

    private void OnInputViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InputFloatingViewModel.EnableEnterSend) && _inputFloatingWindow != null)
        {
            Config.EnableEnterSend = _inputFloatingWindow.ViewModel.EnableEnterSend;
            ConfigHelper.SaveConfig(Config);
        }
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

            if (isVisible)
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
                var windowWidth = rect.Width;
                var windowHeight = rect.Height * 0.30;

                _inputFloatingWindow.Width = Math.Max(300, windowWidth);
                _inputFloatingWindow.Height = Math.Max(120, windowHeight);
                _inputFloatingWindow.Left = rect.Left;
                _inputFloatingWindow.Top = rect.Bottom - _inputFloatingWindow.Height;
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
            LogHelper.Info($"准备发送文本: {text}");
            
            if (_textSender != null)
            {
                await _textSender.SendTextAsync(text);

                if (Config.EnableEnterSend)
                {
                    var currentPackage = _inputMonitor?.CurrentForegroundPackage ?? string.Empty;
                    if (!string.IsNullOrEmpty(currentPackage) && Config.EnterSendPackageList.Contains(currentPackage))
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                        await _textSender.SendEnterKeyAsync();
                        LogHelper.Info($"已向 {currentPackage} 发送回车");
                    }
                }
            }
            else
            {
                LogHelper.Info("使用 ADB 方案发送文本");
                AdbHelper.SendText(SelectedDevice.SerialNumber, text);
                
                if (Config.EnableEnterSend)
                {
                    var currentPackage = _inputMonitor?.CurrentForegroundPackage ?? string.Empty;
                    if (!string.IsNullOrEmpty(currentPackage) && Config.EnterSendPackageList.Contains(currentPackage))
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                        AdbHelper.SendKeyEvent(SelectedDevice.SerialNumber, 66);
                        LogHelper.Info($"已向 {currentPackage} 发送回车");
                    }
                }
            }

            _inputFloatingWindow?.ShowMessage("发送成功！");
            LogHelper.Info("文本发送成功");
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
                _floatingWindow.ViewModel.TextSent += OnFloatingTextSent;
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

    private void OnFloatingTakeScreenshot(object? sender, EventArgs e)
    {
        TakeScreenshot();
    }

    private void OnFloatingKeyEvent(object? sender, int keyCode)
    {
        SendKey(keyCode);
    }

    private void OnFloatingTextSent(object? sender, string text)
    {
        SendText(text);
    }

    private void SendText(string text)
    {
        if (SelectedDevice == null)
        {
            LogHelper.Warning("请先选择设备");
            return;
        }

        var success = AdbHelper.SendText(SelectedDevice.SerialNumber, text);
        if (success && _floatingWindow != null)
        {
            _floatingWindow.ShowNotification("文本发送成功！");
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
        
        LogHelper.Info("配置已保存");
    }

    private void AddCurrentAppToPackageList()
    {
        var currentPackage = _inputMonitor?.CurrentForegroundPackage;
        LogHelper.Info($"添加当前APP到列表，当前前台应用: {currentPackage}");
        
        if (string.IsNullOrEmpty(currentPackage))
        {
            LogHelper.Warning("无法获取当前应用！");
            return;
        }

        if (!Config.EnterSendPackageList.Contains(currentPackage))
        {
            Config.EnterSendPackageList.Add(currentPackage);
            ConfigHelper.SaveConfig(Config);
            OnPropertyChanged(nameof(Config));
            LogHelper.Info($"已添加应用到回车发送列表: {currentPackage}");
        }
        else
        {
            LogHelper.Info($"应用已在列表中: {currentPackage}");
        }
    }

    private void ClearPackageList()
    {
        Config.EnterSendPackageList.Clear();
        ConfigHelper.SaveConfig(Config);
        OnPropertyChanged(nameof(Config));
        LogHelper.Info("已清空回车发送列表");
    }

    private void RemovePackage(object? param)
    {
        if (param is string packageName && !string.IsNullOrEmpty(packageName))
        {
            if (Config.EnterSendPackageList.Remove(packageName))
            {
                ConfigHelper.SaveConfig(Config);
                OnPropertyChanged(nameof(Config));
                LogHelper.Info($"已从回车发送列表移除: {packageName}");
            }
        }
    }
}