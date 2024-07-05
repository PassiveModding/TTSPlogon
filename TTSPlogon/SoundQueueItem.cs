namespace TTSPlogon;

public class SoundQueueItem
{
    public MemoryStream Data { get; set; } = new();
    public float Volume { get; set; }
}