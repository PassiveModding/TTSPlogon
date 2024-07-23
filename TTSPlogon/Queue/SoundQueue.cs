using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TTSPlogon.Queue;

public class SoundQueue : Queue<SoundQueueItem>
{
    private readonly PluginConfig _config;

    public SoundQueue(PluginConfig config)
    {
        _config = config;
    }
    
    private readonly AutoResetEvent _speechCompleted = new(false);
    private readonly object _soundLock = true;
    private DirectSoundOut? _soundOut;

    public void EnqueueSound(SoundQueueItem item)
    {
        Enqueue(item);
    }

    public void Play()
    {
        if (_soundOut != null)
            return;

        if (!TryDequeue(out var nextItem)) return;
        
        
        if (!_config.Enabled)
        {
            nextItem.Data.Dispose();
            return;
        }

        switch (nextItem.StreamDataType)
        {
            case StreamDataType.Mp3:
            {
                using WaveStream reader = new Mp3FileReader(nextItem.Data);
                var sampleProvider = reader.ToSampleProvider();
                var volumeSampleProvider = new VolumeSampleProvider(sampleProvider) {Volume = nextItem.Volume};
                HandlePlay(volumeSampleProvider);
                break;
            }
            case StreamDataType.Pcm:
            {
                using WaveStream reader = new RawSourceWaveStream(nextItem.Data, new WaveFormat(44100, 16, 1));
                var sampleProvider = reader.ToSampleProvider();
                var volumeSampleProvider = new VolumeSampleProvider(sampleProvider) {Volume = nextItem.Volume};
                HandlePlay(volumeSampleProvider);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
        



    }

    private void HandlePlay(ISampleProvider volumeSampleProvider)
    {
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