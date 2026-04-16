using System.Windows.Input;
using ScrcpyGUI.WPF.Helpers;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.ViewModels;

public enum ConnectionStatus
{
    Idle,
    Connecting,
    Success,
    Failed
}

public class WifiConnectViewModel : ViewModelBase
{
    private string _ipAddress = string.Empty;
    private string _port = "5555";
    private string _statusMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isConnecting;
    private ConnectionStatus _connectionStatus;
    private readonly AppConfig _config;

    public string IpAddress
    {
        get => _ipAddress;
        set
        {
            if (SetProperty(ref _ipAddress, value))
            {
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Port
    {
        get => _port;
        set
        {
            if (SetProperty(ref _port, value))
            {
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set
        {
            if (SetProperty(ref _isConnecting, value))
            {
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ConnectionStatus ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public ICommand ConnectCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler? RequestClose;
    public event EventHandler? DeviceConnected;

    public WifiConnectViewModel(AppConfig config)
    {
        _config = config;
        _ipAddress = config.LastWifiIp;
        _port = config.LastWifiPort.ToString();
        
        ConnectCommand = new RelayCommand(_ => Connect(), _ => CanConnect());
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    private bool CanConnect()
    {
        return !IsConnecting && 
               !string.IsNullOrWhiteSpace(IpAddress) && 
               !string.IsNullOrWhiteSpace(Port) &&
               int.TryParse(Port, out _);
    }

    private async void Connect()
    {
        StatusMessage = string.Empty;
        ErrorMessage = string.Empty;
        ConnectionStatus = ConnectionStatus.Connecting;
        IsConnecting = true;

        try
        {
            // 验证IP地址格式
            if (!System.Net.IPAddress.TryParse(IpAddress, out _))
            {
                StatusMessage = string.Empty;
                ErrorMessage = "请输入有效的IP地址";
                ConnectionStatus = ConnectionStatus.Failed;
                IsConnecting = false;
                return;
            }

            // 验证端口
            if (!int.TryParse(Port, out int portNumber) || portNumber < 1 || portNumber > 65535)
            {
                StatusMessage = string.Empty;
                ErrorMessage = "请输入有效的端口号（1-65535）";
                ConnectionStatus = ConnectionStatus.Failed;
                IsConnecting = false;
                return;
            }

            // 检查 ADB 工具是否配置
            if (!AdbHelper.IsConfigured)
            {
                StatusMessage = string.Empty;
                ErrorMessage = "ADB环境配置异常，请执行以下操作：①重新配置ADB环境变量；②验证ADB版本兼容性；③在命令行执行'adb devices'确认ADB服务是否正常运行";
                ConnectionStatus = ConnectionStatus.Failed;
                IsConnecting = false;
                return;
            }

            var fullAddress = $"{IpAddress}:{portNumber}";
            LogHelper.Info($"尝试连接到 {fullAddress}...");
            StatusMessage = "正在搜索设备...";

            // 模拟搜索延迟，让用户看到状态变化
            await System.Threading.Tasks.Task.Delay(500);
            StatusMessage = "正在建立连接...";

            string? connectError = null;
            var connectSuccess = await System.Threading.Tasks.Task.Run(() => 
            {
                try
                {
                    string error;
                    var result = AdbHelper.ConnectWifiDevice(fullAddress, out error);
                    connectError = error;
                    return result;
                }
                catch (Exception ex)
                {
                    connectError = ex.Message;
                    return false;
                }
            });

            if (connectSuccess)
            {
                LogHelper.Info("无线连接成功！");
                StatusMessage = "连接成功！";
                ConnectionStatus = ConnectionStatus.Success;
                
                _config.LastWifiIp = IpAddress;
                _config.LastWifiPort = portNumber;
                ConfigHelper.SaveConfig(_config);
                LogHelper.Info("已保存无线连接配置");
                
                // 短暂显示成功状态后关闭窗口
                await System.Threading.Tasks.Task.Delay(800);
                DeviceConnected?.Invoke(this, EventArgs.Empty);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ConnectionStatus = ConnectionStatus.Failed;
                StatusMessage = string.Empty;
                
                // 根据错误类型提供详细提示
                if (string.IsNullOrEmpty(connectError))
                {
                    // 默认错误 - 未发现设备
                    ErrorMessage = "未发现可用设备，请检查：①设备是否已开启无线调试功能；②设备与本机是否处于同一局域网；③尝试关闭并重新开启设备的无线调试";
                }
                else if (connectError.Contains("unable to connect") || connectError.Contains("failed to connect"))
                {
                    ErrorMessage = "未发现可用设备，请检查：①设备是否已开启无线调试功能；②设备与本机是否处于同一局域网；③尝试关闭并重新开启设备的无线调试";
                }
                else if (connectError.Contains("device not found") || connectError.Contains("no devices"))
                {
                    ErrorMessage = "未发现可用设备，请检查：①设备是否已开启无线调试功能；②设备与本机是否处于同一局域网；③尝试关闭并重新开启设备的无线调试";
                }
                else if (connectError.Contains("adb") && connectError.ToLower().Contains("not found"))
                {
                    ErrorMessage = "ADB环境配置异常，请执行以下操作：①重新配置ADB环境变量；②验证ADB版本兼容性；③在命令行执行'adb devices'确认ADB服务是否正常运行";
                }
                else
                {
                    // 通用错误
                    ErrorMessage = $"连接失败：{connectError}\n\n请尝试：①使用原厂USB数据线连接设备；②确保已打开开发者模式并启用'允许USB调试'选项；③重启设备和计算机后重试连接";
                }
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = ConnectionStatus.Failed;
            StatusMessage = string.Empty;
            ErrorMessage = $"连接异常：{ex.Message}\n\n请尝试：①使用原厂USB数据线连接设备；②确保已打开开发者模式并启用'允许USB调试'选项；③重启设备和计算机后重试连接";
            LogHelper.Error($"无线连接异常: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
