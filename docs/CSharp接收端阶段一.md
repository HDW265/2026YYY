# SF_view（C# 接收端）

WinForms 主控：收图铺满显示，底栏改保存间隔和质量，压成真正 JPEG。产品名 **SF_view**（与发送端 `SF_link` 成对）。

## 界面

采用四段 `TableLayout`（避免控件互相叠压）：

- **顶栏两行**
  - 行1：监听状态 · 端口 · 开始/停止 · 断开客户 · **预览开**（只控画面，不影响保存）
  - 行2：客户 · fps · 最近帧大小 · 等比/拉伸 · **允许：全部/白名单(n)** · **管理…**
- **中部**：黑底预览（默认等比铺满，`F11` 全屏）；**关闭预览会清空画面，全黑无字**
- **保存栏两行（约 100px）**
  - 行1：保存开关 · 目录 · 浏览
  - 行2：**间隔（秒）** · **质量 1–100** · 最近写入
- **日志**：可点击标题折叠/展开

允许 IP：**管理…** 弹窗可选「全部放行」或勾选白名单；**全部放行时也可手工添加 IPv4**；连过的客户 IP 会进入已知列表。新连接会**顶替**当前在线客户。

## 验证码与锁定

- **首次启动**：只弹出「设置验证码」（4～6 位数字），设完直接进主界面，不会马上再要一次
- **以后每次启动**、以及**退出**：需输入验证码
- **每次最小化**即隐藏锁定（无托盘）；按 **`Ctrl+Shift+Alt+V`** 唤出并验证后恢复
- 锁定期间不刷新预览画面（仍可按间隔保存）
- 忘记验证码：删除 `%AppData%\SF_view\settings.json` 后重新设置

## 本地配置记忆

配置写在 `%AppData%\SF_view\settings.json`（会从旧 `%AppData%\LanMonitor.Receiver` 自动迁移），会记住：

- 保存目录、端口、间隔、质量、预览开/关、保存开/关、日志是否展开
- 允许策略：全部/白名单、已知 IP 列表、勾选的 IP
- 验证码哈希、热键

默认保存目录：`图片\SF_view`（若你已记住其它路径则保持原路径）。

窗口最小约 `1280×800`，默认端口 **19730**。

## 在 Windows 上发布单文件

本机需安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

```bat
dotnet publish src\LanMonitor.Receiver\LanMonitor.Receiver.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableWindowsTargeting=true -o publish\win-x64
```

或双击 `publish-win-x64.bat`。

生成：`publish\win-x64\SF_view.exe`。

## 与 C# 发送端对接（推荐）

协议：**4 字节小端长度 + JPEG**。发送端进程名 **`SF_link`**；热键 `Ctrl+Shift+Alt+M`。详见 `docs/CSharp发送端阶段二.md`。

## 与易语言被控对接（兼容）

1. SF_view 先「开始监听」（默认端口 **19730**）
2. 易语言发送端 IP/端口对齐
3. 被控端发 JPEG / BMP（裸流或长度前缀均可）

## 本地自测发送（可选）

```bat
dotnet run --project src\LanMonitor.TestSender -- 127.0.0.1 19730 5
```

## 快捷键

- `Ctrl+Shift+Alt+V`：锁定后唤出（需验证码）
- `F11` 全屏预览，`Esc` 退出全屏
- `S` 切换保存（焦点不在输入框时）
