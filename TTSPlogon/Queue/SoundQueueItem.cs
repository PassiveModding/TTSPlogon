namespace TTSPlogon.Queue;

public class SoundQueueItem
{
    public MemoryStream Data { get; set; } = new();
    public float Volume { get; set; }
    public StreamDataType StreamDataType { get; set; }
}

public enum StreamDataType
{
    Mp3,
    Pcm
}

