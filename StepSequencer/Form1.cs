using NAudio.Midi;
using StepSequencer.Core;
using StepSequencer.Core.MIDIProcess;
using System.Diagnostics;
using System.Text.Json;

namespace StepSequencer;

public partial class Form1 : Form
{
    private static readonly Color Background = Color.FromArgb(19, 23, 30);
    private static readonly Color PanelColor = Color.FromArgb(33, 40, 51);
    private static readonly Color TextColor = Color.FromArgb(230, 235, 242);
    private static readonly Color EnabledColor = Color.FromArgb(19, 105, 79);
    private static readonly Color PlayheadColor = Color.FromArgb(39, 71, 117);
    private static readonly Color TriggeredColor = Color.FromArgb(255, 218, 112);

    private readonly Sequencer sequencer = new();
    private readonly Stopwatch time = Stopwatch.StartNew();
    private readonly System.Threading.Timer playbackTimer;
    private readonly System.Windows.Forms.Timer displayTimer = new() { Interval = 20 };

    private readonly ComboBox padIn = Picker();
    private readonly ComboBox padOut = Picker();
    private readonly ComboBox noteOut = Picker();
    private readonly ComboBox clockIn = Picker();
    private readonly ComboBox layoutPicker = Picker();
    private readonly NumericUpDown bpm = NumberBox(30, 300);
    private readonly NumericUpDown channel = NumberBox(1, 16);
    private readonly NumericUpDown velocity = NumberBox(1, 127);
    private readonly NumericUpDown[] trackNotes = new NumericUpDown[SequenceRec.TrackCount];
    private readonly Button[,] cells = new Button[SequenceRec.TrackCount, SequenceRec.StepCount];
    private readonly int?[,] padLights = new int?[LaunchpadLayout.Size, LaunchpadLayout.Size];
    private readonly int?[] controlLights = new int?[4];
    private readonly Button play = MakeButton("播放");
    private readonly Button pageA = MakeButton("A · 01–08");
    private readonly Button pageB = MakeButton("B · 09–16");
    private readonly Label transport = Caption("");
    private readonly Label portStatus = Caption("");

    private SequenceRec pattern = SequenceRec.Empty();
    private LaunchpadLayout layout = LaunchpadLayout.Supported[0];
    private MidiInProcess? launchpadInput;
    private MidiInProcess? clockInput;
    private MidiOut? launchpadOutput;
    private MidiOut? musicalOutput;
    private bool externalClock;
    private bool armed;
    private bool loadingPattern;
    private bool closing;
    private int connectionGeneration;
    private int page;
    private int displayedStep = -2;
    private bool displayedRunning;

    public Form1()
    {
        InitializeComponent();
        BuildControls();
        layoutPicker.Items.AddRange(LaunchpadLayout.Supported);
        layoutPicker.SelectedIndex = 0;
        ApplyPattern(pattern);
        playbackTimer = new System.Threading.Timer(_ => sequencer.Advance(Now), null, 0, 4);
        displayTimer.Tick += (_, _) => RenderTransport();
        Shown += (_, _) =>
        {
            RefreshPorts();
            displayTimer.Start();
        };
    }

    private double Now => time.Elapsed.TotalMilliseconds;

    private void BuildControls()
    {
        BackColor = Background;
        ForeColor = TextColor;

        var root = new TableLayoutPanel
        {
            ColumnCount = 1, RowCount = 8, Dock = DockStyle.Fill, Padding = new Padding(18),
        };
        for (int row = 0; row < root.RowCount; row++)
            root.RowStyles.Add(new RowStyle(row == 6 ? SizeType.Percent : SizeType.AutoSize, 100));
        Controls.Add(root);

        var title = Caption("Launchpad StepSequencer");
        title.Font = new Font(Font.FontFamily, 18, FontStyle.Bold);
        root.Controls.Add(title, 0, 0);

        var ports = new TableLayoutPanel { ColumnCount = 4, RowCount = 2, AutoSize = true, Dock = DockStyle.Top };
        for (int column = 0; column < ports.ColumnCount; column++)
            ports.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        AddField(ports, 0, "Launchpad 输入", padIn);
        AddField(ports, 1, "Launchpad 灯光输出", padOut);
        AddField(ports, 2, "音符输出（DAW / 音源）", noteOut);
        AddField(ports, 3, "时钟输入（可选）", clockIn);
        root.Controls.Add(ports, 0, 1);

        var connectionBar = Bar();
        layoutPicker.Width = 258;
        connectionBar.Controls.AddRange(new Control[]
        {
            Caption("设备映射"), layoutPicker, MakeButton("刷新端口", RefreshPorts), MakeButton("连接所选端口", ConnectPorts)
        });
        root.Controls.Add(connectionBar, 0, 2);
        root.Controls.Add(portStatus, 0, 3);

        var settings = Bar();
        settings.Controls.AddRange(new Control[]
        {
            play, MakeButton("停止", StopPlayback), Caption("BPM"), bpm, Caption("MIDI 通道"), channel,
            Caption("力度"), velocity, MakeButton("打开", OpenPattern), MakeButton("保存", SavePattern),
            MakeButton("清空步骤", ClearSteps)
        });
        play.Click += (_, _) => StartPlayback();
        bpm.ValueChanged += (_, _) => UpdateSettings();
        channel.ValueChanged += (_, _) => UpdateSettings();
        velocity.ValueChanged += (_, _) => UpdateSettings();
        root.Controls.Add(settings, 0, 4);

        var pages = Bar();
        pages.Controls.AddRange(new Control[] { Caption("Launchpad 页"), pageA, pageB, transport });
        pageA.Click += (_, _) => SetPage(0);
        pageB.Click += (_, _) => SetPage(1);
        root.Controls.Add(pages, 0, 5);

        var grid = new TableLayoutPanel
        {
            ColumnCount = SequenceRec.StepCount + 1, RowCount = SequenceRec.TrackCount + 1, Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 280), Margin = new Padding(0, 3, 0, 6),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        for (int step = 0; step < SequenceRec.StepCount; step++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / SequenceRec.StepCount));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        for (int row = 0; row < SequenceRec.TrackCount; row++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / SequenceRec.TrackCount));

        grid.Controls.Add(Caption("MIDI 音符"), 0, 0);
        for (int step = 0; step < SequenceRec.StepCount; step++)
            grid.Controls.Add(new Label { Text = $"{step + 1:00}", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill }, step + 1, 0);

        for (int row = 0; row < SequenceRec.TrackCount; row++)
        {
            int track = row;
            var note = trackNotes[row] = NumberBox(0, 127);
            note.AccessibleName = $"第 {row + 1} 轨的 MIDI 音符";
            note.Dock = DockStyle.Fill;
            note.ValueChanged += (_, _) =>
            {
                if (!loadingPattern)
                    SetTrackNote(track, (int)note.Value);
            };
            grid.Controls.Add(note, 0, row + 1);

            for (int step = 0; step < SequenceRec.StepCount; step++)
            {
                int index = step;
                var cell = cells[row, step] = MakeButton("");
                cell.AutoSize = false;
                cell.Dock = DockStyle.Fill;
                cell.Margin = new Padding(step == 8 ? 6 : 2, 2, 2, 2);
                cell.Click += (_, _) => ToggleStep(track, index);
                grid.Controls.Add(cell, step + 1, row + 1);
            }
        }

        root.Controls.Add(grid, 0, 6);
        root.Controls.Add(Caption("绿色 · 已启用    蓝色 · 当前步    黄色 · 正在触发   |   每格 1/16 音符，循环 16 步"), 0, 7);
    }

    private static Label Caption(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(3, 6, 3, 5) };

    private static ComboBox Picker() =>
        new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210, DropDownWidth = 300 };

    private static NumericUpDown NumberBox(int min, int max) =>
        new() { Minimum = min, Maximum = max, Width = 58, BackColor = PanelColor, ForeColor = TextColor };

    private static FlowLayoutPanel Bar() =>
        new() { AutoSize = true, Dock = DockStyle.Top, WrapContents = true, Margin = Padding.Empty };

    private static Button MakeButton(string text, Action? onClick = null)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat,
            ForeColor = TextColor, BackColor = PanelColor, UseVisualStyleBackColor = false,
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(53, 62, 76);
        if (onClick != null)
            button.Click += (_, _) => onClick();

        return button;
    }

    private static void AddField(TableLayoutPanel table, int column, string label, ComboBox picker)
    {
        picker.Dock = DockStyle.Fill;
        picker.AccessibleName = label;
        table.Controls.Add(Caption(label), column, 0);
        table.Controls.Add(picker, column, 1);
    }

    private sealed record Port(int Id, string Name)
    {
        public override string ToString() => Id < 0 ? Name : $"{Name}  [{Id + 1}]";
    }

    private static void ListPorts(ComboBox picker, Port[] ports, string emptyLabel)
    {
        var previous = picker.SelectedItem as Port;
        picker.Items.Clear();
        picker.Items.Add(new Port(-1, emptyLabel));
        picker.Items.AddRange(ports);
        picker.SelectedItem = ports.FirstOrDefault(port => port == previous) ?? picker.Items[0];
    }

    private void RefreshPorts()
    {
        DisconnectPorts();
        try
        {
            var inputs = Enumerable.Range(0, MidiIn.NumberOfDevices)
                .Select(id => new Port(id, MidiIn.DeviceInfo(id).ProductName)).ToArray();
            var outputs = Enumerable.Range(0, MidiOut.NumberOfDevices)
                .Select(id => new Port(id, MidiOut.DeviceInfo(id).ProductName)).ToArray();
            ListPorts(padIn, inputs, "无（鼠标编辑）");
            ListPorts(padOut, outputs, "无（关闭灯光）");
            ListPorts(noteOut, outputs, "无（静音预览）");
            ListPorts(clockIn, inputs, "无（内部 BPM）");
            portStatus.Text = "选择端口后点击「连接所选端口」。可直接用鼠标编辑和预览。";
        }
        catch (Exception ex)
        {
            portStatus.Text = $"读取 MIDI 端口失败：{ex.Message}";
        }
    }

    private void ConnectPorts()
    {
        int inputId = ((Port?)padIn.SelectedItem)?.Id ?? -1;
        int ledId = ((Port?)padOut.SelectedItem)?.Id ?? -1;
        int noteId = ((Port?)noteOut.SelectedItem)?.Id ?? -1;
        int clockId = ((Port?)clockIn.SelectedItem)?.Id ?? -1;
        if ((ledId >= 0 && ledId == noteId) || (inputId >= 0 && inputId == clockId))
        {
            portStatus.Text = "Launchpad 与音符/时钟需要选择不同的同向端口。";
            return;
        }

        DisconnectPorts();
        layout = (LaunchpadLayout)layoutPicker.SelectedItem!;
        int generation = connectionGeneration;
        string opening = "";
        try
        {
            if (noteId >= 0)
            {
                opening = "音符输出";
                musicalOutput = new MidiOut(noteId);
                sequencer.SetOutput(musicalOutput.Send);
            }

            if (ledId >= 0)
            {
                opening = "Launchpad 灯光输出";
                launchpadOutput = new MidiOut(ledId);
                if (layout.Legacy)
                    launchpadOutput.Send(0x0100b0); // Select the X-Y grid on legacy devices.
            }

            if (inputId >= 0)
            {
                opening = "Launchpad 输入";
                launchpadInput = new MidiInProcess(inputId, message => PostPad(message, generation));
            }

            if (clockId >= 0)
            {
                opening = "时钟输入";
                clockInput = new MidiInProcess(clockId, message => PostClock(message, generation));
                externalClock = armed = true;
                sequencer.SetClockMode(true);
            }

            portStatus.Text = noteId >= 0 ? "端口已连接。" : "端口已连接。音符输出为静音预览。";
            RenderTransport(force: true);
        }
        catch (Exception ex)
        {
            DisconnectPorts();
            portStatus.Text = $"无法打开{opening}：{ex.Message}。请检查端口是否被其他程序占用。";
        }
    }

    private void PostPad(int message, int generation)
    {
        int status = message & 0xff;
        if ((status != 0x90 && status != 0xb0) || ((message >> 16) & 0x7f) == 0)
            return;

        PostMidi(generation, () =>
        {
            if (layout.TryReadPress(message, out int row, out int column))
            {
                ToggleStep(row, page * LaunchpadLayout.Size + column);
                return;
            }

            switch (layout.ReadControl(message))
            {
                case 0: SetPage(0); break;
                case 1: SetPage(1); break;
                case 2: StartPlayback(); break;
                case 3: StopPlayback(); break;
            }
        });
    }

    private void PostClock(int message, int generation)
    {
        if ((message & 0xff) is 0xf8 or 0xfa or 0xfb or 0xfc or 0xf2)
        {
            PostMidi(generation, () =>
            {
                if (armed)
                    sequencer.ReceiveClock(message, Now);
            });
        }
    }

    private void PostMidi(int generation, Action action)
    {
        // WinMM callbacks run off the UI thread. Never call WinMM output from the input callback.
        try
        {
            BeginInvoke(() =>
            {
                if (!closing && generation == connectionGeneration)
                    action();
            });
        }
        catch (InvalidOperationException)
        {
            // The window handle was destroyed during shutdown.
        }
    }

    private void DisconnectPorts()
    {
        connectionGeneration++;
        sequencer.SetOutput(null);
        sequencer.SetClockMode(false);
        externalClock = armed = false;

        launchpadInput?.Dispose();
        launchpadInput = null;
        clockInput?.Dispose();
        clockInput = null;
        musicalOutput?.Dispose();
        musicalOutput = null;
        ClosePadLights();
        RenderTransport(force: true);
    }

    private void ClosePadLights()
    {
        if (launchpadOutput is not { } device)
            return;

        launchpadOutput = null;
        Array.Clear(padLights);
        Array.Clear(controlLights);
        try
        {
            for (int row = 0; row < LaunchpadLayout.Size; row++)
            for (int column = 0; column < LaunchpadLayout.Size; column++)
                device.Send(layout.PadLight(row, column, false, false));

            for (int control = 0; control < controlLights.Length; control++)
                device.Send(layout.ControlLight(control, PadColor.Off));
        }
        catch (Exception)
        {
            // The controller may already be unplugged.
        }
        finally
        {
            device.Dispose();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        closing = true;
        displayTimer.Dispose();
        playbackTimer.Dispose();
        DisconnectPorts();
        base.OnFormClosed(e);
    }

    private void StartPlayback()
    {
        if (externalClock)
            armed = true;
        else
            sequencer.Play(Now);
        RenderTransport(force: true);
    }

    private void StopPlayback()
    {
        armed = false;
        sequencer.Stop();
        RenderTransport(force: true);
    }

    private void SetPage(int value)
    {
        page = value;
        RenderTransport(force: true);
    }

    private void ToggleStep(int row, int step)
    {
        var track = pattern.Tracks[row];
        pattern = pattern.WithTrack(row, track with { Steps = track.Steps ^ (1 << step) });
        sequencer.SetPattern(pattern);
        RenderTransport(force: true);
    }

    private void SetTrackNote(int row, int note)
    {
        pattern = pattern.WithTrack(row, pattern.Tracks[row] with { Note = note });
        sequencer.SetPattern(pattern);
    }

    private void UpdateSettings()
    {
        if (loadingPattern)
            return;

        pattern = pattern with { Bpm = (int)bpm.Value, Channel = (int)channel.Value, Velocity = (int)velocity.Value };
        sequencer.SetPattern(pattern);
    }

    private void ApplyPattern(SequenceRec value)
    {
        pattern = value;
        sequencer.SetPattern(value);
        loadingPattern = true;
        try
        {
            bpm.Value = value.Bpm;
            channel.Value = value.Channel;
            velocity.Value = value.Velocity;
            for (int row = 0; row < SequenceRec.TrackCount; row++)
                trackNotes[row].Value = value.Tracks[row].Note;
        }
        finally
        {
            loadingPattern = false;
        }

        RenderTransport(force: true);
    }

    private void ClearSteps()
    {
        if (pattern.Tracks.All(track => track.Steps == 0))
            return;
        if (MessageBox.Show(this, "清空当前序列的全部步骤？", Text, MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;

        StopPlayback();
        ApplyPattern(pattern with { Tracks = pattern.Tracks.Select(track => track with { Steps = 0 }).ToArray() });
    }

    private void OpenPattern()
    {
        using var dialog = new OpenFileDialog { Filter = "StepSequencer 序列 (*.json)|*.json" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var loaded = SequenceRec.Load(dialog.FileName);
            StopPlayback();
            ApplyPattern(loaded);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            MessageBox.Show(this, $"无法打开序列。{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SavePattern()
    {
        using var dialog = new SaveFileDialog { Filter = "StepSequencer 序列 (*.json)|*.json", DefaultExt = "json" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            pattern.Save(dialog.FileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"无法保存序列。{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenderTransport(bool force = false)
    {
        var state = sequencer.ReadStatus();
        if (state.Error != null)
            portStatus.Text = state.Error;

        if (!force && displayedStep == state.Step && displayedRunning == state.Running)
            return;

        displayedStep = state.Step;
        displayedRunning = state.Running;
        play.Text = externalClock ? "等待外部时钟" : "播放";
        play.Enabled = externalClock ? !armed : !state.Running;
        bpm.Enabled = !externalClock;
        transport.Text = state.Step >= 0 ? $"播放中 · 第 {state.Step + 1:00} 步"
            : externalClock && armed ? "等待外部时钟" : "已停止";

        pageA.BackColor = page == 0 ? PlayheadColor : PanelColor;
        pageB.BackColor = page == 1 ? PlayheadColor : PanelColor;

        for (int row = 0; row < SequenceRec.TrackCount; row++)
        for (int step = 0; step < SequenceRec.StepCount; step++)
        {
            bool enabled = pattern.Tracks[row].IsOn(step);
            bool current = step == state.Step;
            var cell = cells[row, step];
            cell.Text = enabled ? "●" : "";
            cell.AccessibleName = $"第 {row + 1} 轨第 {step + 1} 步，{(enabled ? "开启" : "关闭")}";
            cell.BackColor = current ? (enabled ? TriggeredColor : PlayheadColor) : (enabled ? EnabledColor : PanelColor);
            cell.ForeColor = current && enabled ? Background : TextColor;
        }

        RefreshPadLights(state.Step, state.Running);
    }

    private void RefreshPadLights(int step, bool running)
    {
        if (launchpadOutput is not { } device)
            return;

        try
        {
            for (int row = 0; row < LaunchpadLayout.Size; row++)
            for (int column = 0; column < LaunchpadLayout.Size; column++)
            {
                int index = page * LaunchpadLayout.Size + column;
                int message = layout.PadLight(row, column, pattern.Tracks[row].IsOn(index), index == step);
                if (padLights[row, column] == message)
                    continue;

                device.Send(message);
                padLights[row, column] = message;
            }

            PadColor[] colors =
            {
                page == 0 ? PadColor.Blue : PadColor.Off,
                page == 1 ? PadColor.Blue : PadColor.Off,
                running || armed ? PadColor.Green : PadColor.Off,
                running ? PadColor.Off : PadColor.Red
            };
            for (int control = 0; control < colors.Length; control++)
            {
                int message = layout.ControlLight(control, colors[control]);
                if (controlLights[control] == message)
                    continue;

                device.Send(message);
                controlLights[control] = message;
            }
        }
        catch (Exception ex)
        {
            ClosePadLights();
            portStatus.Text = $"Launchpad 灯光输出中断：{ex.Message}。可刷新并重新连接。";
        }
    }
}
