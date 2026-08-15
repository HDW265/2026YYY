# SF_link · C# 发送端（阶段二）

工程名 `LanMonitor.Sender`，发布进程名 **`SF_link.exe`**。截主屏 → JPEG → **4 字节小端长度前缀 + JPEG**。

## 协议

仅发送：`Int32 LE 长度` + `JPEG` 整帧。

## 隐蔽运行

- 进程名：**SF_link**（任务管理器显示此名）
- **无托盘**；热键 **`Ctrl+Shift+Alt+M`** + 密码打开设置
- 首次设置 → 保存后后台连接；关窗=隐藏；「退出程序」才结束
- 配置目录：`%AppData%\SF_link\settings.json`
- **Host 使用 DPAPI（当前用户）加密存储**，json 中为 `HostProtected`，无明文 IP
- 旧版 `%AppData%\局域网监控发送端` 会自动迁移到 `SF_link` 并加密 Host

## 截屏

- 在 UI/STA 线程执行 `BitBlt`，失败短重试，避免后台线程「句柄无效」刷屏

## 重连

默认**持续重连**（收端下线再上线仍会连上）；可选手动有限次数。

## 发布

```bat
dotnet publish src\LanMonitor.Sender\LanMonitor.Sender.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableWindowsTargeting=true -o publish\win-x64
```

或 `publish-win-x64.bat`。产物：`publish\win-x64\SF_link.exe`

## 忘记密码 / 重置

删除 `%AppData%\SF_link` 后重新首次设置。
