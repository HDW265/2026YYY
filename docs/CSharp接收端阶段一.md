# SF_view · C# 接收端（阶段一）

WinForms 主控：收图铺满显示，底栏直接改保存间隔和质量，压成真正 JPEG。对外进程名 **`SF_view.exe`**。

## 界面

采用四段 `TableLayout`（避免控件互相叠压）：

- **顶栏两行**
  - 行1：监听状态 · 端口 · 开始/停止 · 断开客户 · **隐藏** · **预览开**（只控画面，不影响保存）
  - 行2：客户 · fps · 最近帧大小 · 等比/拉伸 · **允许：全部/白名单(n)** · **管理…**
- **中部**：黑底预览（默认等比铺满，`F11` 全屏）；关预览时提示「预览已关闭」
- **保存栏两行**
  - 行1：保存开关 · 目录 · 浏览（默认目录 `%UserProfile%\Pictures\SF_view`）
  - 行2：**间隔（秒）** · **质量 1–100** · 最近写入（预览与保存互不影响；说明只写在日志，避免与底栏叠字）
- **日志**：最近若干行

允许 IP：**管理…** 弹窗可选「全部放行」或勾选已知 IP 白名单（无手动添加/删除；连过的客户会进入列表）。

## 登录与托盘

- 首次启动设置管理密码；之后每次启动需登录
- **关闭窗口 /「隐藏」** → 隐藏到系统托盘（不退出，任务栏不显示）
- 托盘双击或「打开」→ 密码验证后恢复窗口
- 托盘「退出」→ 密码验证后结束进程
- 配置：`%AppData%\SF_view\settings.json`
- 忘记密码：删除该目录后重新首次设置

窗口最小约 `1280×800`，默认端口 **19730**。

## 在 Windows 上发布单文件

本机需安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

在资源管理器中进入解压后的项目根目录（能看到 `LanMonitor.sln` 和 `publish-win-x64.bat`），**先打开「命令提示符」再执行**，不要用 Git Bash：

1. 地址栏输入 `cmd` 回车
2. 执行：

```bat
dotnet publish src\LanMonitor.Receiver\LanMonitor.Receiver.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableWindowsTargeting=true -o publish\win-x64
```

或双击 `publish-win-x64.bat`（须为 Windows 换行的 bat；若仍报 `/d` 不是命令，用上面这条）。

生成：`publish\win-x64\SF_view.exe`，拷到另一台 Windows 即可，不必安装 .NET。

## 与 C# 发送端对接（阶段二，推荐）

协议：**4 字节小端长度 + JPEG**。发送端进程名 **`SF_link`**；首次设 IP/端口/密码后后台常驻，**持续重连**；热键 `Ctrl+Shift+Alt+M` + 密码调出（无托盘）。配置在 `%ProgramData%\SF_link`（本机各用户共用），Host 为本机 DPAPI 密文。详见 `docs/CSharp发送端阶段二.md`。

## 与易语言被控对接（兼容）

1. 接收端先「开始监听」（默认端口 **19730**；易语言被控「端口」编辑框也要填同一端口，且主机 IP 指向本机）
2. 允许 IP：管理窗选全部或勾选白名单
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
