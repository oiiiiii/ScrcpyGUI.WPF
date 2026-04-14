using System.Windows.Input;

namespace ScrcpyGUI.WPF.ViewModels;

public class InputFloatingViewModel : ViewModelBase
{
    private string _inputText = string.Empty;
    private bool _enableEnterSend = true;
    private string _currentForegroundPackage = string.Empty;
    private string _statusMessage = string.Empty;

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                ((RelayCommand)SendTextCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool EnableEnterSend
    {
        get => _enableEnterSend;
        set => SetProperty(ref _enableEnterSend, value);
    }

    public string CurrentForegroundPackage
    {
        get => _currentForegroundPackage;
        set => SetProperty(ref _currentForegroundPackage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SendTextCommand { get; }
    public ICommand AddCurrentAppCommand { get; }

    public event EventHandler<string>? SendRequested;
    public event EventHandler? AddCurrentAppRequested;

    public InputFloatingViewModel()
    {
        SendTextCommand = new RelayCommand(_ => OnSendText(), _ => !string.IsNullOrWhiteSpace(InputText));
        AddCurrentAppCommand = new RelayCommand(_ => OnAddCurrentApp());
    }

    private void OnSendText()
    {
        ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingViewModel] OnSendText 被调用，InputText: '{InputText}'");
        if (!string.IsNullOrWhiteSpace(InputText))
        {
            ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingViewModel] 触发 SendRequested 事件，文本: '{InputText}'");
            SendRequested?.Invoke(this, InputText);
            InputText = string.Empty;
        }
        else
        {
            ScrcpyGUI.WPF.Helpers.LogHelper.Warning("[InputFloatingViewModel] InputText 为空，不发送");
        }
    }

    public void SendTextFromWindow(string text)
    {
        ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingViewModel] SendTextFromWindow 被调用，文本: '{text}'");
        if (!string.IsNullOrWhiteSpace(text))
        {
            ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingViewModel] 触发 SendRequested 事件");
            SendRequested?.Invoke(this, text);
        }
    }

    public void OnAddCurrentApp()
    {
        ScrcpyGUI.WPF.Helpers.LogHelper.Info("[InputFloatingViewModel] OnAddCurrentApp 被调用");
        AddCurrentAppRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ShowMessage(string message)
    {
        StatusMessage = message;
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (s, e) =>
        {
            StatusMessage = string.Empty;
            timer.Stop();
        };
        timer.Start();
    }
}
