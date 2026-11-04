# Launchpad StepSequencer

Windows 上的 8 轨 MIDI 步进器。桌面网格显示全部 16 步，Launchpad 用 A / B 两页编辑。每步是一个十六分音符，16 步循环。软件发送 MIDI 音符，由 Ableton Live、其他 DAW 或外接音源发声。

## 启动

Windows x64 试用包需要 [.NET 10 Desktop Runtime（x64）](https://dotnet.microsoft.com/download/dotnet/10.0)。解压后运行 `StepSequencer.exe`。从源码运行需要 .NET 10 SDK，在 Windows 终端执行：

```powershell
dotnet run --project StepSequencer/StepSequencer.csproj
```

没有 Launchpad 时也可以用鼠标编辑和试听。保持所有端口为「无」时可以静音预览播放位置。

## 接 Launchpad

| 设备 | 在设备上选择 | 在程序里选择 |
| --- | --- | --- |
| Launchpad Mini MK3、Launchpad X | 长按 Session 进设置，按最底部的 Scene Launch 进入 Programmer Mode | Mini MK3 / X · Programmer |
| Launchpad MK2（RGB） | 按 Session | MK2（RGB）· Session |
| Launchpad Mini MK1/MK2、Launchpad S、初代 Launchpad | 使用 X-Y 网格。连接灯光输出时程序会发送 X-Y 切换命令 | Mini MK1/MK2 / S / MK1 · X-Y |

1. 选择设备映射，再选 Launchpad 输入与灯光输出。Mini MK3、X 选标有 **MIDI** 的端口，DAW 端口留给控制面。两个方向分别选择，端口编号可以不同。
2. 点击「连接所选端口」。如果端口被 Live 的 Launchpad 控制面占用，先在 Live 的 Link/Tempo/MIDI 设置里释放对应端口，再回来连接。插拔设备后点击「刷新端口」，检查选择后重新连接。
3. 网格从上到下对应 8 条音轨，从左到右是当前页的 8 步。按一下启用，再按一下关闭。顶排左起四键依次切换 A 页、切换 B 页、播放或等待外部时钟、停止。
4. 绿灯表示已启用，蓝灯表示当前播放列，黄灯表示当前列里的启用步骤。只有红绿双色 LED 的旧型号用红灯表示当前播放列。硬件页由手动切换，播放仍会遍历 16 步。

## 接 Ableton Live 12

1. 用 [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html) 创建端口，例如 `StepSeq Notes`。
2. 在 StepSequencer 的「音符输出」选 `StepSeq Notes`，连接端口。在 Live 的 Link/Tempo/MIDI 里打开这个**输入端口**的 Track。
3. 在 Live 建立 MIDI 轨并加载 Drum Rack 或乐器。MIDI From 选 `StepSeq Notes`，Monitor 设为 In，或在 Auto 下启用录音。默认 8 轨的 MIDI 音符号是 36、38、42、46、39、41、45、49，可以直接编辑。
4. 点亮几格并播放。可调 BPM、MIDI 通道和音符力度。通过「保存」和「打开」管理 JSON 序列文件，文件包含音符、步骤与这三个设置。

### 跟随 Live 的速度与启停

再创建独立端口 `StepSeq Clock`。在 Live 的这个**输出端口**开启 Sync，在 StepSequencer 将它选为「时钟输入」并连接。然后从 Live 播放。程序收到 Start / Stop 时同步启停，收到 Continue 时从当前位置继续，Song Position Pointer 用来定位循环中的步骤。时钟断开约一秒会停止并尝试释放正在响的音符。按本程序的「停止」可暂停跟随，再按「等待外部时钟」，然后在 Live 停止并重新播放即可恢复。想用本地 BPM 时，将时钟输入切换为「无」并重新连接。

## 验证范围

本次通过 Windows 目标的 Release 交叉构建，并用模拟 MIDI 消息验证序列与映射。USB 驱动、真机灯光以及与 Live 一起播放的实际延迟仍需在 Windows 上实测。Windows 调度会带来少量步进抖动，内部时钟以单调时间为基准保持节拍。

协议参考：[Launchpad Mini MK3](https://userguides.novationmusic.com/hc/en-gb/articles/23731330800146-Launchpad-Mini-s-default-MIDI-mappings) · [Launchpad X](https://userguides.novationmusic.com/hc/en-gb/articles/24001475492498-Controlling-the-Launchpad-X-surface) · [Launchpad MK2](https://fael-downloads-prod.focusrite.com/customer/prod/s3fs-public/novation/downloads/10529/launchpad-mk2-programmers-reference-guide-v1-02.pdf) · [Launchpad S](https://fael-downloads-prod.focusrite.com/customer/prod/s3fs-public/novation/downloads/10753/launchpad-s-prm.pdf) · [Live MIDI 设置](https://help.ableton.com/hc/en-us/articles/209774205-Live-s-MIDI-Settings)
