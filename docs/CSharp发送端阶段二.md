# 局域网监控 · C# 发送端（阶段二）

WinForms 被控端：截主屏 → JPEG → **4 字节小端长度前缀 + JPEG** 推到阶段一接收端。

## 协议（已定死）

仅发送：`Int32 LE 长度` + `JPEG` 整帧。不发 BMP、不发裸 JPEG 流。

## 界面

- **连接栏**：主机 IP · 端口(默认 19730) · 连接/断开 · **自动重连** · **重连次数 0–100**（默认 5；0=不重连）· 已用/剩余 · 状态
- **采集栏**：帧率 1–30 · 质量 1–100 · 最长边（默认 1280，0=不缩放）· 推流开
- **状态**：最近发送 KB · 发送 fps
- **日志**

断线后若开启自动重连：间隔约 2.5s，最多重试「重连次数」次；达上限停止，可改次数后再点「连接」（会重置已用次数）。用户点「断开」立即停止重连。

## 与接收端联调

1. 接收端「开始监听」端口 `19730`
2. 发送端主机填接收端 IP，端口 `19730`，「连接」
3. 接收端应出画面；保存栏 JPEG 体积为数百 KB 量级（视分辨率/质量）

## Windows 发布单文件

```bat
dotnet publish src\LanMonitor.Sender\LanMonitor.Sender.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableWindowsTargeting=true -o publish\win-x64
```

或双击 `publish-win-x64.bat`（会同时发布接收端与发送端）。

生成：`publish\win-x64\局域网监控发送端.exe`

## 控制台自测（可选）

```bat
dotnet run --project src\LanMonitor.TestSender -- 127.0.0.1 19730 5
```

参数：主机、端口、帧率。发送长度前缀 JPEG。
