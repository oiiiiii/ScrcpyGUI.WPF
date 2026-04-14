using System.Windows;
using ScrcpyGUI.WPF.ViewModels;

namespace ScrcpyGUI.WPF.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RequestOpenSettings += OnRequestOpenSettings;
            
            if (!viewModel.IsToolsConfigured)
            {
                ShowSettingsWindow();
            }
        }
    }

    private void OnRequestOpenSettings(object? sender, EventArgs e)
    {
        ShowSettingsWindow();
    }

    private void ShowSettingsWindow()
    {
        if (DataContext is MainViewModel viewModel)
        {
            var settingsWindow = new SettingsWindow(viewModel.Config);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            
            viewModel.InitializeTools();
        }
    }
}