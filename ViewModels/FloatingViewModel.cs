using System.Windows.Input;

namespace ScrcpyGUI.WPF.ViewModels;

public class FloatingViewModel : ViewModelBase
{
    private string _notificationText = string.Empty;
    private bool _isNotificationVisible;
    private double _opacity = 0.9;

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

    public ICommand TakeScreenshotCommand { get; }
    public ICommand PowerKeyCommand { get; }
    public ICommand HomeKeyCommand { get; }
    public ICommand BackKeyCommand { get; }
    public ICommand VolumeUpCommand { get; }
    public ICommand VolumeDownCommand { get; }
    public ICommand MenuKeyCommand { get; }
    public ICommand RecentAppsCommand { get; }
    public ICommand ExpandNotificationCommand { get; }

    public event EventHandler? TakeScreenshotRequested;
    public event EventHandler<int>? KeyEventRequested;
    public event EventHandler? ExpandNotificationRequested;

    public FloatingViewModel()
    {
        TakeScreenshotCommand = new RelayCommand(_ => OnTakeScreenshot());
        PowerKeyCommand = new RelayCommand(_ => OnKeyEvent(26));
        HomeKeyCommand = new RelayCommand(_ => OnKeyEvent(3));
        BackKeyCommand = new RelayCommand(_ => OnKeyEvent(4));
        VolumeUpCommand = new RelayCommand(_ => OnKeyEvent(24));
        VolumeDownCommand = new RelayCommand(_ => OnKeyEvent(25));
        MenuKeyCommand = new RelayCommand(_ => OnKeyEvent(82));
        RecentAppsCommand = new RelayCommand(_ => OnKeyEvent(187));
        ExpandNotificationCommand = new RelayCommand(_ => OnExpandNotification());
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

    private void OnExpandNotification()
    {
        ExpandNotificationRequested?.Invoke(this, EventArgs.Empty);
    }
}
