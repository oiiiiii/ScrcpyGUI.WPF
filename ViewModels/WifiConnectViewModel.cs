using System.Windows.Input;
using ScrcpyGUI.WPF.Helpers;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.ViewModels;

public class WifiConnectViewModel : ViewModelBase
{
    private string _ipAddress = string.Empty;
    private string _port = "5555";
    private string _errorMessage = string.Empty;
    private bool _isConnecting;
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
        ErrorMessage = string.Empty;
        IsConnecting = true;

        try
        {
            // 验证IP地址格式
            if (!System.Net.IPAddress.TryParse(IpAddress, out _))
            {
                ErrorMessage = "请输入有效的IP地址";
                IsConnecting = false;
                return;
            }

            // 验证端口
            if (!int.TryParse(Port, out int portNumber) || portNumber < 1 || portNumber > 65535)
            {
                ErrorMessage = "请输入有效的端口号（1-65535）";
                IsConnecting = false;
                return;
            }

            var fullAddress = $"{IpAddress}:{portNumber}";
            LogHelper.Info($"尝试连接到 {fullAddress}...");

            var connectSuccess = await System.Threading.Tasks.Task.Run(() => 
            {
                string error;
                return AdbHelper.ConnectWifiDevice(fullAddress, out error);
            });

            if (connectSuccess)
            {
                LogHelper.Info("无线连接成功！");
                
                _config.LastWifiIp = IpAddress;
                _config.LastWifiPort = portNumber;
                ConfigHelper.SaveConfig(_config);
                LogHelper.Info("已保存无线连接配置");
                
                DeviceConnected?.Invoke(this, EventArgs.Empty);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = "连接失败，请检查设备是否在同一网络且已开启无线调试";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"连接出错: {ex.Message}";
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
