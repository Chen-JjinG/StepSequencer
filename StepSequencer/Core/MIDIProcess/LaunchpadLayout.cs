namespace StepSequencer.Core.MIDIProcess;

internal enum PadColor { Off, Green, Blue, Yellow, Red }

internal sealed record LaunchpadLayout(string Name, int FirstControl, bool Legacy = false)
{
    public const int Size = 8;
    public static readonly LaunchpadLayout[] Supported =
    {
        new("Launchpad Mini MK3 / X · Programmer", 91),
        new("Launchpad MK2（RGB）· Session", 104),
        new("Launchpad Mini MK1/MK2 / S / MK1 · X-Y", 104, Legacy: true)
    };

    public bool TryReadPress(int message, out int row, out int column)
    {
        int status = message & 0xff;
        int number = (message >> 8) & 0x7f;
        int value = (message >> 16) & 0x7f;
        row = Legacy ? number / 16 : Size - number / 10;
        column = Legacy ? number % 16 : number % 10 - 1;
        return status == 0x90 && value > 0 && row is >= 0 and < Size && column is >= 0 and < Size;
    }

    public int ReadControl(int message)
    {
        int controller = ((message >> 8) & 0x7f) - FirstControl;
        return (message & 0xff) == 0xb0 && ((message >> 16) & 0x7f) > 0 && controller is >= 0 and < 4
            ? controller : -1;
    }

    public int PadLight(int row, int column, bool enabled, bool playing)
    {
        int note = Legacy ? row * 16 + column : (Size - row) * 10 + column + 1;
        var color = (enabled, playing) switch
        {
            (true, true) => PadColor.Yellow,
            (true, false) => PadColor.Green,
            (false, true) => PadColor.Blue,
            _ => PadColor.Off
        };
        return 0x90 | (note << 8) | (ColorValue(color) << 16);
    }

    public int ControlLight(int control, PadColor color) =>
        0xb0 | ((FirstControl + control) << 8) | (ColorValue(color) << 16);

    private int ColorValue(PadColor color) => (Legacy, color) switch
    {
        (true, PadColor.Green) => 60,
        (true, PadColor.Blue) => 15, // Legacy devices show the playhead in red.
        (true, PadColor.Yellow) => 62,
        (true, PadColor.Red) => 15,
        (true, _) => 12,
        (false, PadColor.Green) => 21,
        (false, PadColor.Blue) => 45,
        (false, PadColor.Yellow) => 13,
        (false, PadColor.Red) => 5,
        _ => 0
    };

    public override string ToString() => Name;
}
