using NAudio.Midi;

namespace StepSequencer.Core.MIDIProcess;

internal sealed class MidiInProcess : IDisposable
{
    private readonly MidiIn input;

    public MidiInProcess(int device, Action<int> receive)
    {
        input = new MidiIn(device);
        try
        {
            input.MessageReceived += (_, e) => receive(e.RawMessage);
            input.Start();
        }
        catch
        {
            input.Dispose();
            throw;
        }
    }

    public void Dispose() => input.Dispose();
}
