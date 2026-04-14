using System.Net.Sockets;
using System.Text;

namespace ScrcpyGUI.WPF.Helpers;

public class ScrcpyTextSender : IDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private bool _disposed;
    private readonly string _serialNumber;

    public ScrcpyTextSender(string serialNumber)
    {
        _serialNumber = serialNumber;
    }

    public async Task<bool> ConnectAsync(int port = 27183)
    {
        try
        {
            LogHelper.Info($"正在连接 scrcpy 控制端口 {port}...");
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync("127.0.0.1", port);
            _stream = _tcpClient.GetStream();
            LogHelper.Info("✅ 已连接到 scrcpy 控制端口");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Warning($"连接 scrcpy 控制端口失败: {ex.Message}");
            LogHelper.Info("将使用 ADB 方案作为备用");
            return false;
        }
    }

    public async Task SendTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            if (_stream != null && _stream.CanWrite)
            {
                var command = $"text {text}\n";
                var buffer = Encoding.UTF8.GetBytes(command);
                await _stream.WriteAsync(buffer, 0, buffer.Length);
                await _stream.FlushAsync();
                LogHelper.Info($"已通过 scrcpy 发送文本: {text}");
                return;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warning($"scrcpy 发送文本失败: {ex.Message}");
        }

        LogHelper.Info("使用 ADB 方案发送文本");
        await SendTextViaAdbAsync(text);
    }

    public async Task SendEnterKeyAsync()
    {
        try
        {
            if (_stream != null && _stream.CanWrite)
            {
                var command = "key keycode ENTER\n";
                var buffer = Encoding.UTF8.GetBytes(command);
                await _stream.WriteAsync(buffer, 0, buffer.Length);
                await _stream.FlushAsync();
                LogHelper.Info("已通过 scrcpy 发送回车");
                return;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warning($"scrcpy 发送回车失败: {ex.Message}");
        }

        LogHelper.Info("使用 ADB 方案发送回车");
        SendEnterViaAdb();
    }

    private async Task SendTextViaAdbAsync(string text)
    {
        AdbHelper.SendText(_serialNumber, text);
        await Task.CompletedTask;
    }

    private void SendEnterViaAdb()
    {
        AdbHelper.SendKeyEvent(_serialNumber, 66);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _stream?.Close();
            _stream?.Dispose();
        }
        catch { }
        
        try
        {
            _tcpClient?.Close();
            _tcpClient?.Dispose();
        }
        catch { }
        
        _stream = null;
        _tcpClient = null;
    }
}
