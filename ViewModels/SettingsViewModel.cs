using System.IO;
using System.Windows.Input;
using ScrcpyGUI.WPF.Helpers;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private AppConfig _config;
    private string _scrcpyPath;
    private string _adbPath;
    private string _screenshotPath;
    private string _scrcpyPathError = string.Empty;
    private string _adbPathError = string.Empty;

    public AppConfig Config
    {
        get => _config;
        set => SetProperty(ref _config, value);
    }

    public string ScrcpyPath
    {
        get => _scrcpyPath;
        set
        {
            if (SetProperty(ref _scrcpyPath, value))
            {
                ValidateScrcpyPath();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string AdbPath
    {
        get => _adbPath;
        set
        {
            if (SetProperty(ref _adbPath, value))
            {
                ValidateAdbPath();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string ScrcpyPathError
    {
        get => _scrcpyPathError;
        set => SetProperty(ref _scrcpyPathError, value);
    }

    public string AdbPathError
    {
        get => _adbPathError;
        set => SetProperty(ref _adbPathError, value);
    }

    public string ScreenshotPath
    {
        get => _screenshotPath;
        set => SetProperty(ref _screenshotPath, value);
    }

    public bool EnableFloatingWindow
    {
        get => _config.EnableFloatingWindow;
        set
        {
            _config.EnableFloatingWindow = value;
            OnPropertyChanged(nameof(EnableFloatingWindow));
        }
    }

    public ICommand BrowseScrcpyCommand { get; }
    public ICommand BrowseAdbCommand { get; }
    public ICommand BrowseScreenshotPathCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler? RequestClose;

    public SettingsViewModel(AppConfig config)
    {
        _config = config;
        _scrcpyPath = config.ScrcpyPath;
        _adbPath = config.AdbPath;
        _screenshotPath = config.ScreenshotSavePath;

        BrowseScrcpyCommand = new RelayCommand(_ => BrowseScrcpy());
        BrowseAdbCommand = new RelayCommand(_ => BrowseAdb());
        BrowseScreenshotPathCommand = new RelayCommand(_ => BrowseScreenshotPath());
        SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => Cancel());
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
            ScrcpyPath = dialog.FileName;
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
            AdbPath = dialog.FileName;
        }
    }

    private void BrowseScreenshotPath()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择截图保存目录",
            ShowNewFolderButton = true,
            SelectedPath = ScreenshotPath
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ScreenshotPath = dialog.SelectedPath;
        }
    }

    private bool ValidateScrcpyPath()
    {
        if (string.IsNullOrWhiteSpace(ScrcpyPath))
        {
            ScrcpyPathError = "请选择 scrcpy.exe 文件";
            return false;
        }

        if (!File.Exists(ScrcpyPath))
        {
            ScrcpyPathError = "文件不存在";
            return false;
        }

        var fileName = Path.GetFileName(ScrcpyPath);
        if (!string.Equals(fileName, "scrcpy.exe", StringComparison.OrdinalIgnoreCase))
        {
            ScrcpyPathError = "请选择 scrcpy.exe 文件";
            return false;
        }

        ScrcpyPathError = string.Empty;
        return true;
    }

    private bool ValidateAdbPath()
    {
        if (string.IsNullOrWhiteSpace(AdbPath))
        {
            AdbPathError = "请选择 adb.exe 文件";
            return false;
        }

        if (!File.Exists(AdbPath))
        {
            AdbPathError = "文件不存在";
            return false;
        }

        var fileName = Path.GetFileName(AdbPath);
        if (!string.Equals(fileName, "adb.exe", StringComparison.OrdinalIgnoreCase))
        {
            AdbPathError = "请选择 adb.exe 文件";
            return false;
        }

        AdbPathError = string.Empty;
        return true;
    }

    private bool CanSave()
    {
        return ValidateScrcpyPath() && ValidateAdbPath();
    }

    private void Save()
    {
        Config.ScrcpyPath = ScrcpyPath;
        Config.AdbPath = AdbPath;
        Config.ScreenshotSavePath = ScreenshotPath;
        ConfigHelper.SaveConfig(Config);
        
        AdbHelper.UpdatePaths(Config.AdbPath);
        ScrcpyHelper.UpdatePaths(Config.ScrcpyPath);
        
        LogHelper.Info("设置已保存");
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}