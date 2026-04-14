using System.Windows.Input;

namespace ScrcpyGUI.WPF.ViewModels;

public class FloatingViewModel : ViewModelBase
{
    private string _notificationText = string.Empty;
    private bool _isNotificationVisible;
    private double _opacity = 0.9;
    private string _inputText = string.Empty;

    public string NotificationText
    {
        get => _notificationText;
        set => SetProperty(ref _notificationText, value);
    }

    public bool IsNotificationVisible
    {
        get => _isNotificationVisible;
        set => SetProperty(ref _isNotificationVisible, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value);
    }

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

    public ICommand TakeScreenshotCommand { get; }
    public ICommand PowerKeyCommand { get; }
    public ICommand HomeKeyCommand { get; }
    public ICommand BackKeyCommand { get; }
    public ICommand SendTextCommand { get; }

    public event EventHandler? TakeScreenshotRequested;
    public event EventHandler<int>? KeyEventRequested;
    public event EventHandler<string>? TextSent;

    public FloatingViewModel()
    {
        TakeScreenshotCommand = new RelayCommand(_ => OnTakeScreenshot());
        PowerKeyCommand = new RelayCommand(_ => OnKeyEvent(26));
        HomeKeyCommand = new RelayCommand(_ => OnKeyEvent(3));
        BackKeyCommand = new RelayCommand(_ => OnKeyEvent(4));
        SendTextCommand = new RelayCommand(_ => OnSendText(), _ => !string.IsNullOrWhiteSpace(InputText));
    }

    public void ShowNotification(string message)
    {
        NotificationText = message;
        IsNotificationVisible = true;

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (s, e) =>
        {
            IsNotificationVisible = false;
            timer.Stop();
        };
        timer.Start();
    }

    private void OnTakeScreenshot()
    {
        TakeScreenshotRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnKeyEvent(int keyCode)
    {
        KeyEventRequested?.Invoke(this, keyCode);
    }

    private void OnSendText()
    {
        if (!string.IsNullOrWhiteSpace(InputText))
        {
            TextSent?.Invoke(this, InputText);
            InputText = string.Empty;
        }
    }
}
