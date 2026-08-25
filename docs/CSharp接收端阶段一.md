# 局域网监控 · C# 接收端（阶段一）

WinForms 主控：收图铺满显示，底栏直接改保存间隔和质量，压成真正 JPEG。

## 界面

采用四段 `TableLayout`（避免控件互相叠压）：

- **顶栏两行**
  - 行1：监听状态 · 端口 · 开始/停止 · 断开客户 · **预览开**（只控画面，不影响保存）
  - 行2：客户 · fps · 最近帧大小 · 等比/拉伸 · **允许：全部/白名单(n)** · **管理…**
- **中部**：黑底预览（默认等比铺满，`F11` 全屏）；**关闭预览会清空画面**并显示「预览已关闭」
- **保存栏两行（约 100px）**
  - 行1：保存开关 · 目录 · 浏览
  - 行2：**间隔（秒）** · **质量 1–100** · 最近写入（预览与保存互不影响）
- **提示与日志**：可点击标题折叠/展开；折叠后预览区更大

允许 IP：**管理…** 弹窗可选「全部放行」或勾选白名单；连过的客户 IP 会进入已知列表。

## 本地配置记忆

配置写在 `%AppData%\LanMonitor.Receiver\settings.json`，会记住：

- 保存目录、端口、间隔、质量、预览开/关、保存开/关、日志是否展开

关闭程序或改目录/相关选项时自动保存。

窗口最小约 `1280×800`，标签列按文字 AutoSize（避免高 DPI 下「端口/保存/间隔」被裁成省略号），默认端口 **19730**（本机若保留了 `13689` 附近段会绑口失败）。

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

## 与 C# 发送端对接（阶段二，推荐）

协议：**4 字节小端长度 + JPEG**。发送端进程名 **`SF_link`**；首次设 IP/端口/密码后后台常驻，**持续重连**；热键 `Ctrl+Shift+Alt+M` + 密码调出（无托盘）。配置在 `%AppData%\SF_link`，Host 为 DPAPI 密文。详见 `docs/CSharp发送端阶段二.md`。

## 与易语言被控对接（兼容）

1. 接收端先「开始监听」（默认端口 **19730**；易语言被控「端口」编辑框也要填同一端口，且主机 IP 指向本机）
2. 允许IP 留空=全部；若要白名单填如 `192.168.111.59`
3. 被控端连这个端口发图（JPEG / BMP；支持长度前缀或组件包装载荷）
4. 画面出来后，底栏「最近写入 xx.jpg  xxx KB」应明显小于原来的数 MB BMP 伪 jpg

## 本地自测发送（可选）

```bat
dotnet run --project src\LanMonitor.TestSender -- 127.0.0.1 19730 5
```

（长度前缀 JPEG；第三参数为帧率）

## 快捷键

- `F11` 全屏预览，`Esc` 退出全屏
- `S` 切换保存（焦点不在输入框时）
