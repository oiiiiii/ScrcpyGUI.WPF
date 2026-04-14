using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScrcpyGUI.WPF.Helpers;

public class GlobalHotKeyHelper : IDisposable
{
    public event EventHandler<GlobalHotKeyEventArgs>? HotKeyPressed;

    private const int WM_HOTKEY = 0x0312;
    
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private IntPtr _windowHandle;
    private HwndSource? _source;
    private readonly Dictionary<int, HotKeyRegistration> _registeredHotKeys = new();
    private int _nextId = 1;

    public GlobalHotKeyHelper(Window window)
    {
        _windowHandle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        _source = System.Windows.Interop.HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_registeredHotKeys.TryGetValue(id, out var registration))
            {
                HotKeyPressed?.Invoke(this, new GlobalHotKeyEventArgs(registration.Name));
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    public bool RegisterHotKey(string name, string shortcut)
    {
        try
        {
            var (modifiers, key) = ParseShortcut(shortcut);
            if (key == Key.None)
                return false;

            // 检查是否已存在同名快捷键
            foreach (var kvp in _registeredHotKeys)
            {
                if (kvp.Value.Name == name)
                {
                    UnregisterHotKey(_windowHandle, kvp.Key);
                    _registeredHotKeys.Remove(kvp.Key);
                    break;
                }
            }

            // 检查是否有冲突
            foreach (var kvp in _registeredHotKeys)
            {
                if (kvp.Value.Modifiers == modifiers && kvp.Value.Key == key)
                {
                    LogHelper.Warning($"快捷键冲突: {shortcut} 已被 {kvp.Value.Name} 使用");
                    return false;
                }
            }

            uint virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
            
            if (RegisterHotKey(_windowHandle, _nextId, modifiers, virtualKey))
            {
                _registeredHotKeys[_nextId] = new HotKeyRegistration(name, modifiers, key);
                _nextId++;
                LogHelper.Info($"已注册快捷键: {name} = {shortcut}");
                return true;
            }
            else
            {
                LogHelper.Warning($"注册快捷键失败: {shortcut} (可能与其他程序冲突)");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"注册快捷键异常: {ex.Message}");
            return false;
        }
    }

    public void UnregisterAllHotKeys()
    {
        foreach (var id in _registeredHotKeys.Keys)
        {
            UnregisterHotKey(_windowHandle, id);
        }
        _registeredHotKeys.Clear();
    }

    private (uint modifiers, Key key) ParseShortcut(string shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut))
            return (0, Key.None);

        var parts = shortcut.Split('+');
        uint modifiers = 0;
        Key key = Key.None;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.Equals(trimmed, "Ctrl", StringComparison.OrdinalIgnoreCase))
                modifiers |= 0x0002; // MOD_CONTROL
            else if (string.Equals(trimmed, "Shift", StringComparison.OrdinalIgnoreCase))
                modifiers |= 0x0004; // MOD_SHIFT
            else if (string.Equals(trimmed, "Alt", StringComparison.OrdinalIgnoreCase))
                modifiers |= 0x0001; // MOD_ALT
            else if (string.Equals(trimmed, "Win", StringComparison.OrdinalIgnoreCase))
                modifiers |= 0x0008; // MOD_WIN
            else
                key = ParseKey(trimmed);
        }

        return (modifiers, key);
    }

    private Key ParseKey(string keyName)
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

    public void Dispose()
    {
        UnregisterAllHotKeys();
        _source?.RemoveHook(WndProc);
        _source = null;
    }

    private class HotKeyRegistration
    {
        public string Name { get; }
        public uint Modifiers { get; }
        public Key Key { get; }

        public HotKeyRegistration(string name, uint modifiers, Key key)
        {
            Name = name;
            Modifiers = modifiers;
            Key = key;
        }
    }
}

public class GlobalHotKeyEventArgs : EventArgs
{
    public string HotKeyName { get; }

    public GlobalHotKeyEventArgs(string hotKeyName)
    {
        HotKeyName = hotKeyName;
    }
}
