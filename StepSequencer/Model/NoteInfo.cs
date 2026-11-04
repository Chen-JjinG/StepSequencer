namespace StepSequencer.Model;

internal sealed record NoteInfo(int Note, int Steps)
{
    public bool IsOn(int step) => (Steps & (1 << step)) != 0;
}
