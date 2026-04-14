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
    private bool _enableEnterSend = true;
    private Func<string>? _getCurrentPackageFunc;
    private Func<List<string>>? _getPackageListFunc;
    
    public string SendShortcutKey
    {
        get => _sendShortcutKey;
        set => _sendShortcutKey = value;
    }
    
    public bool EnableEnterSend
    {
        get => _enableEnterSend;
        set => _enableEnterSend = value;
    }
    
    public void SetPackageAccessors(Func<string> getCurrentPackage, Func<List<string>> getPackageList)
    {
        _getCurrentPackageFunc = getCurrentPackage;
        _getPackageListFunc = getPackageList;
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
                // Shift+Enter: 始终换行，不发送
                return;
            }
            else if (_enableEnterSend)
            {
                // 检查当前包名是否在列表中
                var currentPackage = _getCurrentPackageFunc?.Invoke() ?? string.Empty;
                var packageList = _getPackageListFunc?.Invoke() ?? new List<string>();
                
                ScrcpyGUI.WPF.Helpers.LogHelper.Info($"[InputFloatingWindow] Enter键被按下，当前包名: {currentPackage}，列表包含: {packageList.Contains(currentPackage)}");
                
                if (packageList.Contains(currentPackage))
                {
                    // 在列表内：发送文本 + 发送回车
                    ScrcpyGUI.WPF.Helpers.LogHelper.Info("[InputFloatingWindow] 当前包名在列表中，准备发送文本");
                    SendTextDirectly();
                    e.Handled = true;
                }
                else
                {
                    // 不在列表内：只换行，不发送
                    ScrcpyGUI.WPF.Helpers.LogHelper.Info("[InputFloatingWindow] 当前包名不在列表中，只换行不发送");
                    // 不设置 e.Handled = true，让 TextBox 自己处理换行
                }
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
