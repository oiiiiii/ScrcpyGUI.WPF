using System.Windows;
using ScrcpyGUI.WPF.ViewModels;
using ScrcpyGUI.WPF.Models;

namespace ScrcpyGUI.WPF.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();
        var viewModel = new SettingsViewModel(config);
        viewModel.RequestClose += (s, e) => Close();
        DataContext = viewModel;
    }
}