# Scrcpy GUI

一个基于 WPF 开发的 Android 设备投屏工具，提供友好的图形界面和丰富的功能。主要是之前用的晨钟酱写的GUI程序停更了，也没弄清楚怎么把新版本的scrcpy程序替换进去。然后就找TRAE重写了一个，顺便解决了中文输入的问题。这个GUI程序比之QtScrcpy和EScrcpy还有很大不足，但是创新性的添加了悬浮窗功能，可以直接在投屏窗口底部输入中文，执行发送，而不需要通过虚拟键盘，极大地保证了稳定性和兼容性。兜底的解决思路是在执行发送时自动执行控制端复制+被控端粘贴的动作。在一些回车键可以发送消息的界面，用户还可以选择追加回车键，实现一键发送消息。主要就是用于平时主要在电脑操作，时不时的还要用手机回复一下消息的人群，喜欢群控、打字场景少的还是用其他版本就行，本版并不能带来其他更好的体验。

下载链接（内置scrcpy3.3.4，不包含.NET8.0运行时）：https://diaoyu.lanzouu.com/i5fY43ndxhpa


项目地址：https://github.com/oiiiiii/ScrcpyGUI.WPF


.NET8.0运行时：https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0


有bug或者特殊需求的话clone代码自己修改即可，我最近比较忙，可能改的很慢


下面是一些AI写的功能介绍，随需阅读：

。

## 功能特性

### 核心功能
- 📱 **设备投屏**：通过 ADB 连接 Android 设备，实现实时屏幕镜像
- 📡 **无线连接**：支持通过 IP 地址进行无线连接
- 📤 **文件传输**：支持拖拽文件到设备（支持 APK 安装）
- 📷 **截图功能**：一键截取设备屏幕

### 智能输入
- ⌨️ **智能悬浮窗**：检测输入法弹出自动显示输入悬浮窗
- 📝 **两种发送模式**：发送到设备 / 发送+回车
- 🔄 **文本注入法**：不污染剪贴板的文本传输方式

### 参数配置
- 🎬 **启动时录屏**：自动开始录制
- 🌑 **黑屏启动**：启动时关闭设备屏幕
- 👆 **显示触摸**：显示触摸操作轨迹
- 🔄 **旋转镜像**：270度旋转显示
- ⛶ **全屏启动**：启动时全屏显示
- 🔒 **关闭后锁屏**：停止投屏时自动锁屏

### 快捷键支持
| 功能 | 默认快捷键 |
|------|-----------|
| 返回 | 鼠标右键 |
| 主页 | 按下滚轮 |
| 全屏 | Alt+F |
| 窗口1:1显示 | Alt+G |
| 多任务 | Alt+S |
| 音量+ | Alt+↑ |
| 音量- | Alt+↓ |
| 旋转镜像 | Alt+←/→ |
| 锁屏 | Alt+P |
| 亮屏 | 右键双击 |
| 关闭背光 | Alt+O |
| 展开通知 | Alt+N |
| 显示悬浮窗 | Alt+V |
| 发送到设备 | Alt+Enter |
| 发送+回车 | Ctrl+Enter |

## 系统要求

- Windows 10/11
- .NET 8.0 Runtime
- Android 设备开启 USB 调试
- ADB 工具（已内置）
- scrcpy 工具（已内置）

## 安装说明

1. 下载最新版本的发布包
2. 解压到任意目录
3. 运行 `ScrcpyGUI.exe`
4. 设置scrcpy、adb路径

## 使用方法

### 连接设备

#### 有线连接
1. 使用 USB 数据线连接 Android 设备
2. 确保设备已开启 USB 调试
3. 点击「刷新设备」按钮
4. 在设备列表中选择要连接的设备
5. 点击「开始投屏」

#### 无线连接
1. 确保设备和电脑在同一网络
2. 点击「无线连接」按钮
3. 输入设备 IP 地址和端口（默认5555）
4. 点击「连接」
5. 设备会出现在设备列表中

### 文件传输
- 直接拖拽文件到投屏窗口
- APK 文件会自动安装
- 其他文件会保存到设备的 `/sdcard/Download/` 目录

### 快捷键设置
1. 点击「设置」按钮
2. 切换到「快捷键设置」标签页
3. 点击要修改的快捷键输入框
4. 按下新的快捷键组合
5. 点击「保存配置」

## 配置文件

配置文件位于 `settings.json`，包含以下主要配置项：

```json
{
  "AdbPath": "adb/adb.exe",
  "ScrcpyPath": "scrcpy/scrcpy.exe",
  "MaxSize": 1080,
  "BitRate": 16,
  "MaxFps": 60,
  "RecordOnStart": false,
  "BlackScreenOnStart": false,
  "ShowTouch": true,
  "RotateMirror": false,
  "FullscreenOnStart": false,
  "LockScreenOnClose": false,
  "TextTransferMode": "TextInjection",
  "AutoShowOnKeyboard": true
}
```

## 技术栈

- **框架**: WPF (.NET 8)
- **架构**: MVVM
- **数据绑定**: CommunityToolkit.Mvvm
- **图标**: Material Design Icons

## 致谢

- [scrcpy](https://github.com/Genymobile/scrcpy) - 核心投屏功能
- [Android Debug Bridge](https://developer.android.com/studio/command-line/adb) - 设备通信

## 许可证

MIT License

## 版本历史

### v1.0.0
- 初始版本
- 支持有线和无线连接
- 智能输入悬浮窗
- 文件拖拽传输
- 丰富的参数配置
- 可自定义快捷键

[![ScreenShot 2026 04 14 234924 867](https://origin.picgo.net/2026/04/14/ScreenShot_2026-04-14_234924_8670b0d0d79f0a0bd7f.th.png)](https://www.picgo.net/image/ScreenShot-2026-04-14-234924-867.1ymGzk) [![ScreenShot 2026 04 14 235034 114](https://origin.picgo.net/2026/04/14/ScreenShot_2026-04-14_235034_11417cc571e28f3fa15.th.png)](https://www.picgo.net/image/ScreenShot-2026-04-14-235034-114.1ymQFl)