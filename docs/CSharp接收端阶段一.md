# 局域网监控 · C# 接收端（阶段一）

WinForms 主控：收图铺满显示，底栏直接改保存间隔和质量，压成真正 JPEG。

## 界面

- 顶栏：端口、监听/停止、断开、接收开关、客户、fps、最近帧大小、等比/拉伸、允许IP
- 中部：黑底预览（默认等比铺满，`F11` 全屏）
- 底栏：保存开关、目录、**间隔（秒）**、**质量 1–100**（改完立即生效，不用配置文件）
- 日志：最近 8 行

## 在 Windows 上发布单文件

本机需安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

在资源管理器中进入解压后的项目根目录（能看到 `LanMonitor.sln` 和 `publish-win-x64.bat`），**先打开「命令提示符」再执行**，不要用 Git Bash：

1. 地址栏输入 `cmd` 回车
2. 执行：

```bat
dotnet publish src\LanMonitor.Receiver\LanMonitor.Receiver.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableWindowsTargeting=true -o publish\win-x64
```

或双击 `publish-win-x64.bat`（须为 Windows 换行的 bat；若仍报 `/d` 不是命令，用上面这条）。

生成：`publish\win-x64\局域网监控接收端.exe`，拷到另一台 Windows 即可，不必安装 .NET。

## 与易语言被控对接

1. 接收端先「开始监听」（默认端口 `13689`，与现有工程一致可改）
2. 允许IP 留空=全部；若要白名单填 `192.168.111.59`
3. 被控端连这个端口发图（JPEG `FFD8…FFD9` 或 BMP 整文件）
4. 画面出来后，底栏「最近写入 xx.jpg  xxx KB」应明显小于原来的 4MB BMP

## 本地自测发送（可选）

```bat
dotnet run --project src\LanMonitor.TestSender -- 127.0.0.1 13689 jpeg
dotnet run --project src\LanMonitor.TestSender -- 127.0.0.1 13689 bmp
```

## 快捷键

- `F11` 全屏预览，`Esc` 退出全屏
- `S` 切换保存（焦点不在输入框时）
