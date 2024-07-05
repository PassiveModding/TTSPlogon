using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TTSPlogon;

public class SoundQueue : Queue<SoundQueueItem>
{
    private readonly AutoResetEvent _speechCompleted = new(false);
    private readonly object _soundLock = true;
    private DirectSoundOut? _soundOut;

    public void EnqueueSound(MemoryStream data, float volume)
    {
        Enqueue(new SoundQueueItem
        {
            Data = data,
            Volume = volume
        });
    }

    public void Play()
    {
        if (_soundOut != null)
            return;

        if (!TryDequeue(out var nextItem)) return;
        using WaveStream reader = new Mp3FileReader(nextItem.Data);
        var sampleProvider = reader.ToSampleProvider();
        var volumeSampleProvider = new VolumeSampleProvider(sampleProvider) { Volume = nextItem.Volume };

        lock (_soundLock)
        {
            _soundOut = new DirectSoundOut();
            _soundOut.PlaybackStopped += (_, _) => { _speechCompleted.Set(); };
            _soundOut.Init(volumeSampleProvider);
            _soundOut.Play();
        }

        _speechCompleted.WaitOne();

        lock (_soundLock)
        {
            _soundOut.Dispose();
            _soundOut = null;
        }
    }
}