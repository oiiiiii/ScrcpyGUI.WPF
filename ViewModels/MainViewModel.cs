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
    private FloatingWindow? _floatingWindow;

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

    public event EventHandler? RequestOpenSettings;

    public ICommand RefreshDevicesCommand { get; }
    public ICommand StartScrcpyCommand { get; }
    public ICommand StopScrcpyCommand { get; }
    public ICommand TakeScreenshotCommand { get; }
    public ICommand SendKeyCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public MainViewModel()
    {
        _config = ConfigHelper.LoadConfig();
        
        RefreshDevicesCommand = new RelayCommand(_ => RefreshDevices(), _ => CanRefreshDevices());
        StartScrcpyCommand = new RelayCommand(_ => StartScrcpy(), _ => CanStartScrcpy());
        StopScrcpyCommand = new RelayCommand(_ => StopScrcpy(), _ => CanStopScrcpy());
        TakeScreenshotCommand = new RelayCommand(_ => TakeScreenshot(), _ => SelectedDevice != null && IsToolsConfigured);
        SendKeyCommand = new RelayCommand(param => SendKey(param), _ => SelectedDevice != null && IsToolsConfigured);
        ClearLogCommand = new RelayCommand(_ => ClearLog());
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());

        LogHelper.LogMessage += OnLogMessage;
        ScrcpyHelper.ScrcpyStarted += OnScrcpyStarted;
        ScrcpyHelper.ScrcpyExited += OnScrcpyExited;

        InitializeTools();

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

    private void OpenSettings()
    {
        RequestOpenSettings?.Invoke(this, EventArgs.Empty);
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
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsScrcpyRunning = true;
            
            if (Config.EnableFloatingWindow)
            {
                ShowFloatingWindow();
            }
        });
    }

    private void OnScrcpyExited()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsScrcpyRunning = false;
            HideFloatingWindow();
        });
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
}