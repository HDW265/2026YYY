# 局域网监控 · C# 发送端（阶段二）

WinForms 被控端：截主屏 → JPEG → **4 字节小端长度前缀 + JPEG** 推到阶段一接收端。

## 协议（已定死）

仅发送：`Int32 LE 长度` + `JPEG` 整帧。不发 BMP、不发裸 JPEG 流。

## 隐蔽运行（定稿）

- **无托盘图标**，不显眼
- **首次运行**：设置接收端 IP、端口、帧率/质量/最长边、**管理密码** →「保存并启动」→ 写入配置并后台连接，窗口关闭即隐藏
- **之后启动**：不弹主窗，读配置后自动连接
- **调出设置**：热键 **`Ctrl+Shift+Alt+M`** → 密码验证 → 打开设置窗
- **关窗**：隐藏，进程继续推流/重连
- **退出**：设置窗内「退出程序」才结束
- 配置目录：`%AppData%\局域网监控发送端\settings.json`（删此文件可重新走首次设置；密码仅存哈希）

## 重连（定稿）

- **默认持续重连**：收端下线再上线仍会自动连上（失败间隔约 3s 起，逐步退避至最高 30s；连上后清零）
- 可关「持续重连」改用有限次数（高级）；点「断开」立即停止重连

## 设置窗

- 主机 / 端口 / 连接·断开
- 自动重连 · 持续重连 ·（非持续时）有限次数
- 帧率 · 质量 · 最长边 · 推流开
- 改密码（可选）· 保存配置 · 隐藏窗口 · 退出程序
- 状态 / 最近发送 / 日志

## 与接收端联调

1. 接收端「开始监听」端口 `19730`
2. 发送端首次填接收端 IP / `19730` 并设密码后保存
3. 接收端应出画面；之后发送端可隐藏，靠热键调出

## Windows 发布单文件

```bat
dotnet publish src\LanMonitor.Sender\LanMonitor.Sender.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableWindowsTargeting=true -o publish\win-x64
```

或双击 `publish-win-x64.bat`（同时发布接收端与发送端）。

生成：`publish\win-x64\局域网监控发送端.exe`

## 控制台自测（可选）

```bat
dotnet run --project src\LanMonitor.TestSender -- 127.0.0.1 19730 5
```

参数：主机、端口、帧率。发送长度前缀 JPEG。

## 无托盘说明

看不见图标时，只能用热键调出或任务管理器结束进程；忘记密码可删除 `%AppData%\局域网监控发送端` 后重新首次设置。
