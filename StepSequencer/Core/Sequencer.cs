namespace StepSequencer.Core;

internal sealed class Sequencer
{
    private const int ClocksPerStep = 6; // MIDI clock is 24 pulses per quarter note.
    private const int ClockTimeoutMs = 1000;
    private readonly object sync = new();
    private readonly HashSet<int> activeNotes = new();
    private SequenceRec pattern = SequenceRec.Empty();
    private Action<int>? output;
    private bool external;
    private bool running;
    private int currentStep = -1;
    private int nextClock;
    private int activeChannel;
    private double nextStepAt;
    private double noteOffAt;
    private double lastClockAt;
    private string? error;

    public (bool Running, int Step, string? Error) ReadStatus()
    {
        lock (sync)
        {
            var status = (running, currentStep, error);
            error = null;
            return status;
        }
    }

    public void SetPattern(SequenceRec value)
    {
        lock (sync)
            pattern = value;
    }

    public void SetOutput(Action<int>? value)
    {
        lock (sync)
        {
            StopLocked();
            output = value;
        }
    }

    public void SetClockMode(bool useExternal)
    {
        lock (sync)
        {
            StopLocked();
            external = useExternal;
            nextClock = 0;
        }
    }

    public void Play(double now)
    {
        lock (sync)
        {
            if (external || running)
                return;

            currentStep = SequenceRec.StepCount - 1;
            nextStepAt = now;
            running = true;
            AdvanceLocked(now);
        }
    }

    public void Stop()
    {
        lock (sync)
            StopLocked();
    }

    public void Advance(double now)
    {
        lock (sync)
            AdvanceLocked(now);
    }

    private void AdvanceLocked(double now)
    {
        if (!running)
            return;

        if (external)
        {
            if (now - lastClockAt >= ClockTimeoutMs)
            {
                StopLocked();
                error ??= "MIDI Clock 已中断。请检查时钟端口，或重新启动 Live 播放。";
            }

            return;
        }

        if (now >= noteOffAt)
            ReleaseNotes();

        if (now < nextStepAt)
            return;

        double duration = 60_000.0 / pattern.Bpm / 4;
        // Skip missed steps if the scheduler was delayed. Keep the next deadline on the beat.
        long elapsedSteps = (long)((now - nextStepAt) / duration) + 1;
        nextStepAt += elapsedSteps * duration;
        noteOffAt = Math.Min(now + duration / 2, nextStepAt);
        PlayStep((int)((currentStep + elapsedSteps) % SequenceRec.StepCount));
    }

    public void ReceiveClock(int message, double now)
    {
        lock (sync)
        {
            if (!external)
                return;

            switch (message & 0xff)
            {
                case 0xfa: // Start
                    StopLocked();
                    nextClock = 0;
                    running = true;
                    lastClockAt = now;
                    break;
                case 0xfb: // Continue
                    running = true;
                    lastClockAt = now;
                    break;
                case 0xfc: // Stop
                    StopLocked();
                    break;
                case 0xf2: // Song Position Pointer, in units of one sixteenth note.
                    ReleaseNotes();
                    int position = ((message >> 8) & 0x7f) | (((message >> 16) & 0x7f) << 7);
                    nextClock = (position % SequenceRec.StepCount) * ClocksPerStep;
                    break;
                case 0xf8 when running:
                    lastClockAt = now;
                    int pulse = nextClock % ClocksPerStep;
                    int step = nextClock / ClocksPerStep;
                    nextClock = (nextClock + 1) % (SequenceRec.StepCount * ClocksPerStep);
                    if (pulse == 0)
                        PlayStep(step);
                    else if (pulse == ClocksPerStep / 2)
                        ReleaseNotes();
                    break;
            }
        }
    }

    private void PlayStep(int step)
    {
        ReleaseNotes();
        if (!running)
            return;

        currentStep = step;
        if (output is null)
            return;

        activeChannel = pattern.Channel - 1;
        foreach (var track in pattern.Tracks)
        {
            if (!track.IsOn(step) || !activeNotes.Add(track.Note))
                continue;

            if (!Send(0x90 | activeChannel | (track.Note << 8) | (pattern.Velocity << 16)))
            {
                StopLocked();
                break;
            }
        }
    }

    private void ReleaseNotes()
    {
        bool released = true;
        foreach (int note in activeNotes)
            released &= Send(0x80 | activeChannel | (note << 8));

        activeNotes.Clear();
        if (!released)
        {
            // Best-effort fallback for devices that accept All Notes Off.
            Send(0xb0 | activeChannel | (123 << 8));
            running = false;
            currentStep = -1;
        }
    }

    private bool Send(int message)
    {
        try
        {
            output?.Invoke(message);
            return true;
        }
        catch (Exception ex)
        {
            error ??= $"音符输出失败：{ex.Message}";
            return false;
        }
    }

    private void StopLocked()
    {
        running = false;
        currentStep = -1;
        ReleaseNotes();
    }
}
