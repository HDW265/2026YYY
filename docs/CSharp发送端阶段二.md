# SF_link · C# 发送端（阶段二）

工程名 `LanMonitor.Sender`，发布进程名 **`SF_link.exe`**。截主屏 → JPEG → **4 字节小端长度前缀 + JPEG**。

## 协议

仅发送：`Int32 LE 长度` + `JPEG` 整帧。

## 隐蔽运行

- 进程名：**SF_link**（任务管理器显示此名）
- **无托盘**；热键 **`Ctrl+Shift+Alt+M`** + 密码打开设置
- 首次设置（整机尚未配置时）→ 保存后后台连接；关窗=隐藏；「退出程序」才结束
- **本机各 Windows 用户共用一份配置**：管理员配好后，其它用户登录直接运行即可，无需再设
- 设置窗增加 **「开机自启」**（勾选控制）：写入  
  `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`，值名 **`SF_link`** = 当前 exe 路径（需 UAC）。不写 WOW6432Node；仅清理 WOW 下旧项（不会因大小写误删刚写入的 `SF_link`）。
- 配置目录：`%ProgramData%\SF_link\settings.json`（创建时尽量授予 Users 修改权限，便于其它用户热键改参）
- **Host 使用 DPAPI（本机 LocalMachine）加密存储**，json 中为 `HostProtected`，无明文 IP；旧版 CurrentUser 密文加载后会重加密
- 旧版 `%AppData%\SF_link` 或 `%AppData%\局域网监控发送端` 会在当前用户能读到时自动迁移到 `%ProgramData%\SF_link`

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

删除 `%ProgramData%\SF_link` 后由管理员重新首次设置。若仍残留旧版 `%AppData%\SF_link`，一并删除以免再次迁移。
