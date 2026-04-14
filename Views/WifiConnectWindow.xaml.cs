using System.Windows;
using ScrcpyGUI.WPF.ViewModels;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.Views;

public partial class WifiConnectWindow : Window
{
    public event EventHandler? DeviceConnected;

    public WifiConnectWindow(AppConfig config)
    {
        InitializeComponent();
        var viewModel = new WifiConnectViewModel(config);
        viewModel.RequestClose += (s, e) => Close();
        viewModel.DeviceConnected += (s, e) =>
        {
            DeviceConnected?.Invoke(this, EventArgs.Empty);
            Close();
        };
        DataContext = viewModel;
        Loaded += (s, e) => IpAddressTextBox.Focus();
    }
}
