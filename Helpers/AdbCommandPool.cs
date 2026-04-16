using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ScrcpyGUI.WPF.Helpers;

public class AdbCommandPool : IDisposable
{
    private static readonly Lazy<AdbCommandPool> _instance = new(() => new AdbCommandPool());
    public static AdbCommandPool Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, AdbShellSession> _sessions = new();
    private readonly object _lock = new object();
    private bool _disposed;

    public async Task<string> ExecuteCommandAsync(string serial, string command)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AdbCommandPool));

        var session = await GetOrCreateSessionAsync(serial);
        return await session.ExecuteCommandAsync(command);
    }

    private async Task<AdbShellSession> GetOrCreateSessionAsync(string serial)
    {
        if (_sessions.TryGetValue(serial, out var session) && session.IsConnected)
        {
            return session;
        }

        lock (_lock)
        {
            if (_sessions.TryGetValue(serial, out session) && session.IsConnected)
            {
                return session;
            }

            session = new AdbShellSession(serial);
            session.ConnectAsync().ConfigureAwait(false);
            _sessions[serial] = session;
            return session;
        }
    }

    public void CloseSession(string serial)
    {
        if (_sessions.TryRemove(serial, out var session))
        {
            session.Dispose();
            LogHelper.Info($"ADB session closed for device: {serial}");
        }
    }

    public void CloseAllSessions()
    {
        foreach (var session in _sessions.Values)
        {
            session.Dispose();
        }
        _sessions.Clear();
        LogHelper.Info("All ADB sessions closed");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CloseAllSessions();
    }
}

public class AdbShellSession : IDisposable
{
    private readonly string _serial;
    private Process? _process;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private bool _isConnected;
    private readonly object _lock = new object();
    private bool _disposed;

    public bool IsConnected => !_disposed && _isConnected && _process != null && !_process.HasExited;

    public AdbShellSession(string serial)
    {
        _serial = serial;
    }

    public async Task ConnectAsync()
    {
        lock (_lock)
        {
            if (IsConnected) return;

            try
            {
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = AdbHelper.AdbPath,
                        Arguments = $"-s {_serial} shell",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(AdbHelper.AdbPath)
                    }
                };

                _process.Start();
                _writer = _process.StandardInput;
                _reader = _process.StandardOutput;
                _isConnected = true;

                _process.Exited += (sender, args) =>
                {
                    _isConnected = false;
                    LogHelper.Warning($"ADB shell session disconnected for device: {_serial}");
                };

                LogHelper.Info($"ADB shell session connected for device: {_serial}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Failed to create ADB shell session for {_serial}: {ex.Message}");
                Dispose();
            }
        }

        await Task.CompletedTask;
    }

    public async Task<string> ExecuteCommandAsync(string command)
    {
        if (!IsConnected)
        {
            await ConnectAsync();
        }

        if (!IsConnected)
        {
            LogHelper.Warning($"ADB session not connected, falling back to direct command");
            return await ExecuteDirectCommandAsync(command);
        }

        try
        {
            string writerKey;
            lock (_lock)
            {
                _writer?.WriteLine(command);
                _writer?.Flush();
                writerKey = command;
            }

            var result = new StringBuilder();
            var timeout = DateTime.Now.AddSeconds(2);
            bool hasData = false;
            int consecutiveEmptyReads = 0;
            
            while (DateTime.Now < timeout)
            {
                string? line = null;
                lock (_lock)
                {
                    if (_reader != null && _reader.Peek() >= 0)
                    {
                        line = _reader.ReadLine();
                    }
                }
                
                if (line != null)
                {
                    result.AppendLine(line);
                    hasData = true;
                    consecutiveEmptyReads = 0;
                }
                else
                {
                    consecutiveEmptyReads++;
                    if (hasData && consecutiveEmptyReads >= 3)
                    {
                        break;
                    }
                }

                await Task.Delay(10);
            }

            var output = result.ToString().Trim();
            return output;
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Failed to execute command '{command}': {ex.Message}");
            _isConnected = false;
            return string.Empty;
        }
    }

    private async Task<string> ExecuteDirectCommandAsync(string command)
    {
        return await Task.Run(() =>
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = AdbHelper.AdbPath,
                    Arguments = $"-s {_serial} shell {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return output.Trim();
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _isConnected = false;

        try
        {
            _writer?.Close();
            _writer?.Dispose();
        }
        catch { }

        try
        {
            _reader?.Close();
            _reader?.Dispose();
        }
        catch { }

        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit(1000);
            }
            _process?.Dispose();
        }
        catch { }

        _writer = null;
        _reader = null;
        _process = null;
    }
}

public static class AdbCommandPoolExtensions
{
    private static readonly Lazy<AdbCommandPool> _instance = new(() => new AdbCommandPool());

    public static AdbCommandPool Instance => _instance.Value;

    public static async Task<string> ExecuteAdbShellCommandAsync(this string serial, string command)
    {
        return await Instance.ExecuteCommandAsync(serial, command);
    }
}