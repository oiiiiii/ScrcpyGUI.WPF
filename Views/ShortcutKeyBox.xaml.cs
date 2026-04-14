using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace ScrcpyGUI.WPF.Views;

public partial class ShortcutKeyBox : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty ShortcutKeyProperty =
        DependencyProperty.Register(nameof(ShortcutKey), typeof(string), typeof(ShortcutKeyBox),
            new PropertyMetadata(string.Empty, OnShortcutKeyChanged));

    public string ShortcutKey
    {
        get => (string)GetValue(ShortcutKeyProperty);
        set => SetValue(ShortcutKeyProperty, value);
    }

    private Key _selectedKey = Key.None;
    private ModifierKeys _selectedModifiers = ModifierKeys.None;
    private bool _isRecording = false;

    public ShortcutKeyBox()
    {
        InitializeComponent();
        ShortcutTextBox.GotFocus += OnGotFocus;
        ShortcutTextBox.LostFocus += OnLostFocus;
        ShortcutTextBox.PreviewKeyDown += OnPreviewKeyDown;
    }

    private static void OnShortcutKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShortcutKeyBox box)
        {
            box.UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        ShortcutTextBox.Text = ShortcutKey;
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        _isRecording = true;
        ShortcutTextBox.Text = "按下快捷键...";
        ShortcutTextBox.SelectAll();
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        _isRecording = false;
        UpdateDisplay();
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isRecording)
        {
            e.Handled = true;
            return;
        }

        e.Handled = true;

        var key = e.Key;
        if (key == Key.System)
        {
            key = e.SystemKey;
        }

        if (IsModifierKey(key))
        {
            return;
        }

        if (key == Key.Escape)
        {
            _isRecording = false;
            UpdateDisplay();
            return;
        }

        _selectedKey = key;
        _selectedModifiers = Keyboard.Modifiers;

        if (_selectedKey != Key.None)
        {
            var shortcut = FormatShortcut(_selectedModifiers, _selectedKey);
            ShortcutKey = shortcut;
            UpdateDisplay();
        }

        _isRecording = false;
    }

    private bool IsModifierKey(Key key)
    {
        return key == Key.LeftCtrl || key == Key.RightCtrl ||
               key == Key.LeftShift || key == Key.RightShift ||
               key == Key.LeftAlt || key == Key.RightAlt ||
               key == Key.LWin || key == Key.RWin;
    }

    private string FormatShortcut(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");

        if (key != Key.None)
        {
            parts.Add(GetKeyDisplayString(key));
        }

        return string.Join("+", parts);
    }

    private string GetKeyDisplayString(Key key)
    {
        return key switch
        {
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.OemQuestion => "?",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "|",
            Key.OemBackslash => "\\",
            Key.OemTilde => "`",
            Key.Return => "Enter",
            Key.Back => "Backspace",
            Key.Tab => "Tab",
            Key.Space => "Space",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Up => "↑",
            Key.Down => "↓",
            Key.Left => "←",
            Key.Right => "→",
            _ => key.ToString()
        };
    }
}
