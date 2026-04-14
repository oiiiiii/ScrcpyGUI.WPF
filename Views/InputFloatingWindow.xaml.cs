using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScrcpyGUI.WPF.ViewModels;
using ScrcpyGUI.WPF.Helpers;

namespace ScrcpyGUI.WPF.Views;

public partial class InputFloatingWindow : Window
{
    public InputFloatingViewModel ViewModel => (InputFloatingViewModel)DataContext;
    
    private string _sendShortcutKey = "Ctrl+Enter";
    
    public string SendShortcutKey
    {
        get => _sendShortcutKey;
        set => _sendShortcutKey = value;
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
        if (ShortcutKeyHelper.IsShortcutPressed(_sendShortcutKey, e))
        {
            ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingWindow] 快捷键 '{_sendShortcutKey}' 被按下，准备发送文本");
            SendTextDirectly();
            e.Handled = true;
            return;
        }
        
        if (e.Key == Key.Enter)
        {
            var isShiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            
            if (isShiftPressed)
            {
                return;
            }
            else if (ViewModel.EnableEnterSend)
            {
                ScrcpyGUI.WPF.Helpers.LogHelper.Info("[InputFloatingWindow] Enter键被按下，准备发送文本");
                SendTextDirectly();
                e.Handled = true;
            }
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        ScrcpyGUI.WPF.Helpers.LogHelper.Info("[InputFloatingWindow] 发送按钮被点击");
        SendTextDirectly();
    }

    private void SendTextDirectly()
    {
        var text = ViewModel.InputText;
        ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingWindow] 准备直接发送文本: '{text}'");
        
        if (!string.IsNullOrWhiteSpace(text))
        {
            ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingWindow] 触发 SendRequested 事件");
            ViewModel.SendTextFromWindow(text);
            ViewModel.InputText = string.Empty;
        }
        else
        {
            ScrcpyGUI.WPF.Helpers.LogHelper.Warning("[InputFloatingWindow] 文本为空，不发送");
        }
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
