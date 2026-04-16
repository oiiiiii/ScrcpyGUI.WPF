using System.Windows;
using ScrcpyGUI.WPF.ViewModels;
using ScrcpyGUI.WPF.Helpers;

namespace ScrcpyGUI.WPF.Views;

public partial class MainWindow : Window
{
    private GlobalHotKeyHelper? _hotKeyHelper;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            _hotKeyHelper = new GlobalHotKeyHelper(this);
            _hotKeyHelper.HotKeyPressed += (s, args) => viewModel.OnGlobalHotKeyPressed(args.HotKeyName);
            viewModel.InitializeGlobalHotKeys(_hotKeyHelper);
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _hotKeyHelper?.Dispose();
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }
}