using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScrcpyGUI.WPF.ViewModels;
using ScrcpyGUI.WPF.Helpers;

namespace ScrcpyGUI.WPF.Views;

public partial class InputFloatingWindow : Window
{
    public InputFloatingViewModel ViewModel => (InputFloatingViewModel)DataContext;
    
    private string _sendToDeviceShortcutKey = "Alt+Enter";
    private string _sendWithEnterShortcutKey = "Ctrl+Enter";
    
    public string SendToDeviceShortcutKey
    {
        get => _sendToDeviceShortcutKey;
        set => _sendToDeviceShortcutKey = value;
    }
    
    public string SendWithEnterShortcutKey
    {
        get => _sendWithEnterShortcutKey;
        set => _sendWithEnterShortcutKey = value;
    }

    public InputFloatingWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        FocusInputBox();
    }

    public void FocusInputBox()
    {
        InputTextBox.Focus();
        InputTextBox.Select(InputTextBox.Text.Length, 0);
    }

    private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (ShortcutKeyHelper.IsShortcutPressed(_sendToDeviceShortcutKey, e))
        {
            ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingWindow] 快捷键 '{_sendToDeviceShortcutKey}' 被按下，准备发送到设备");
            ViewModel.SendTextFromWindow();
            e.Handled = true;
            return;
        }
        
        if (ShortcutKeyHelper.IsShortcutPressed(_sendWithEnterShortcutKey, e))
        {
            ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingWindow] 快捷键 '{_sendWithEnterShortcutKey}' 被按下，准备发送+回车");
            ViewModel.SendMessageFromWindow();
            e.Handled = true;
            return;
        }
        
        // Shift+Enter: 始终换行，不发送
        // 普通 Enter: 让 TextBox 自己处理换行
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        ScrcpyGUI.WPF.Helpers.LogHelper.Info("[InputFloatingWindow] 发送到设备按钮被点击");
        ViewModel.SendTextFromWindow();
    }

    private void SendMessageButton_Click(object sender, RoutedEventArgs e)
    {
        ScrcpyGUI.WPF.Helpers.LogHelper.Info("[InputFloatingWindow] 发送消息按钮被点击");
        ViewModel.SendMessageFromWindow();
    }

    public void ShowMessage(string message)
    {
        ViewModel.ShowMessage(message);
    }

    protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        while (source != null)
        {
            var typeName = source.GetType().Name;
            if (typeName == "Button" || typeName == "TextBox" || typeName == "CheckBox")
            {
                return;
            }
            source = VisualTreeHelper.GetParent(source);
        }
        
        base.OnPreviewMouseLeftButtonDown(e);
        DragMove();
    }
}
