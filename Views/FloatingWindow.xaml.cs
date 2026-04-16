using System.Windows;
using ScrcpyGUI.WPF.ViewModels;

namespace ScrcpyGUI.WPF.Views;

public partial class FloatingWindow : System.Windows.Window
{
    private bool _isDragging;
    private System.Windows.Point _dragStartPoint;

    public FloatingViewModel ViewModel => (FloatingViewModel)DataContext;

    public FloatingWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetWindowPosition();
    }

    private void SetWindowPosition()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        
        Left = screenWidth - Width - 20;
        Top = screenHeight - Height - 60;
    }

    public void ShowNotification(string message)
    {
        NotificationText.Text = message;
        
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (s, e) =>
        {
            NotificationText.Text = string.Empty;
            timer.Stop();
        };
        timer.Start();
    }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        
        _isDragging = true;
        _dragStartPoint = e.GetPosition(this);
        CaptureMouse();
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        
        if (_isDragging)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _dragStartPoint;
            
            Left += delta.X;
            Top += delta.Y;
        }
    }

    protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        
        _isDragging = false;
        ReleaseMouseCapture();
    }
}
