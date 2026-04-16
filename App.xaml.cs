using System.Configuration;
using System.Data;
using System.Windows;
using ScrcpyGUI.WPF.Helpers;

namespace ScrcpyGUI.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public App()
    {
        Exit += OnAppExit;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnAppExit(object sender, ExitEventArgs e)
    {
        LogHelper.Info("应用程序正常退出");
        CleanupAllProcesses();
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogHelper.Error($"UI线程未处理异常: {e.Exception.Message}");
        LogHelper.Error($"异常详情: {e.Exception}");
        CleanupAllProcesses();
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LogHelper.Error($"未处理异常: {exception?.Message ?? "未知异常"}");
        if (exception != null)
        {
            LogHelper.Error($"异常详情: {exception}");
        }
        CleanupAllProcesses();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        LogHelper.Info("进程即将退出");
        CleanupAllProcesses();
    }

    private void CleanupScrcpyProcesses()
    {
        try
        {
            LogHelper.Info("开始清理 scrcpy 进程...");
            ScrcpyHelper.CleanupAllScrcpyProcesses();
            
            // 检查是否还有残留进程
            if (ScrcpyHelper.HasResidualProcesses())
            {
                LogHelper.Warning("仍有残留的 scrcpy 进程");
            }
            else
            {
                LogHelper.Info("scrcpy 进程清理完成");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"清理 scrcpy 进程时发生异常: {ex.Message}");
        }
    }

    private void CleanupAllProcesses()
    {
        CleanupScrcpyProcesses();
        AdbHelper.CleanupAdbProcesses();
    }
}

