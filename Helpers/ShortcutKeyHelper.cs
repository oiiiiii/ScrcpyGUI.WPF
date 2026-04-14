using System.Windows.Input;

namespace ScrcpyGUI.WPF.Helpers;

public static class ShortcutKeyHelper
{
    public static bool IsShortcutPressed(string shortcut, System.Windows.Input.KeyEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(shortcut))
            return false;

        var parts = shortcut.Split('+');
        ModifierKeys modifiers = ModifierKeys.None;
        Key key = Key.None;

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            if (string.Equals(trimmedPart, "Ctrl", StringComparison.OrdinalIgnoreCase))
                modifiers |= ModifierKeys.Control;
            else if (string.Equals(trimmedPart, "Shift", StringComparison.OrdinalIgnoreCase))
                modifiers |= ModifierKeys.Shift;
            else if (string.Equals(trimmedPart, "Alt", StringComparison.OrdinalIgnoreCase))
                modifiers |= ModifierKeys.Alt;
            else
                key = ParseKey(trimmedPart);
        }

        var currentKey = e.Key;
        if (currentKey == Key.System)
        {
            currentKey = e.SystemKey;
        }

        return Keyboard.Modifiers == modifiers && currentKey == key;
    }

    private static Key ParseKey(string keyName)
    {
        if (Enum.TryParse<Key>(keyName, true, out var key))
            return key;

        return keyName switch
        {
            "+" => Key.OemPlus,
            "-" => Key.OemMinus,
            "?" => Key.OemQuestion,
            "," => Key.OemComma,
            "." => Key.OemPeriod,
            ";" => Key.OemSemicolon,
            "'" => Key.OemQuotes,
            "[" => Key.OemOpenBrackets,
            "]" => Key.OemCloseBrackets,
            "|" => Key.OemPipe,
            "\\" => Key.OemBackslash,
            "`" => Key.OemTilde,
            "Enter" => Key.Return,
            "Backspace" => Key.Back,
            "Space" => Key.Space,
            "Delete" => Key.Delete,
            "Insert" => Key.Insert,
            "Home" => Key.Home,
            "End" => Key.End,
            "PageUp" => Key.PageUp,
            "PageDown" => Key.PageDown,
            "↑" => Key.Up,
            "↓" => Key.Down,
            "←" => Key.Left,
            "→" => Key.Right,
            _ => Key.None
        };
    }
}
