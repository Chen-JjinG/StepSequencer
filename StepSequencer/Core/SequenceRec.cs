using StepSequencer.Model;
using System.Text.Json;

namespace StepSequencer.Core;

internal sealed record SequenceRec(int Version, int Bpm, int Channel, int Velocity, NoteInfo[] Tracks)
{
    public const int TrackCount = 8;
    public const int StepCount = 16;

    public static SequenceRec Empty() =>
        new(1, 120, 1, 100, new[] { 36, 38, 42, 46, 39, 41, 45, 49 }
            .Select(note => new NoteInfo(note, 0)).ToArray());

    public SequenceRec WithTrack(int row, NoteInfo track)
    {
        var tracks = (NoteInfo[])Tracks.Clone();
        tracks[row] = track;
        return this with { Tracks = tracks };
    }

    public void Save(string path)
    {
        string temporaryPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, Path.GetRandomFileName());
        try
        {
            using (var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                JsonSerializer.Serialize(file, this, new JsonSerializerOptions { WriteIndented = true });
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    public static SequenceRec Load(string path)
    {
        using var file = File.OpenRead(path);
        var pattern = JsonSerializer.Deserialize<SequenceRec>(file);
        if (pattern is not { Version: 1, Bpm: >= 30 and <= 300, Channel: >= 1 and <= 16,
                Velocity: >= 1 and <= 127, Tracks.Length: TrackCount }
            || pattern.Tracks.Any(track => track is not { Note: >= 0 and <= 127, Steps: >= 0 and < (1 << StepCount) }))
        {
            throw new InvalidDataException("文件不是有效的 StepSequencer 序列。");
        }

        return pattern;
    }
}
