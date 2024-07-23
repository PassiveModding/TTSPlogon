using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using Dalamud.Plugin.Services;
using TTSPlogon.Queue;
using TTSPlogon.Utils;

namespace TTSPlogon.Clients.SpeechSynthesisClient;

public class SpeechSynthesisClient : ISpeechClient
{
    public PluginConfig.Provider Provider => PluginConfig.Provider.SpeechSynthesis;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SoundQueue _soundQueue;
    private readonly PluginConfig _config;
    private readonly IPluginLog _log;
    private readonly LexiconHandler _lexiconHandler;
    private readonly SpeechSynthesizer _synthesizer;
    public static List<string> InstalledVoiceNames = new();

    public SpeechSynthesisClient(SoundQueue soundQueue,
        PluginConfig config,
        IPluginLog log,
        LexiconHandler lexiconHandler)
    {
        _soundQueue = soundQueue;
        _config = config;
        _log = log;
        _lexiconHandler = lexiconHandler;
        _synthesizer = new SpeechSynthesizer();
        // https://github.com/gexgd0419/NaturalVoiceSAPIAdapter
        // can be used to add more voices
        InstalledVoiceNames = _synthesizer.GetInstalledVoices().Select(x => x.VoiceInfo.Name).ToList();
    }
    
    public async Task Say(EventHandler.ActorInfo? actor, string text, float speed, float volume)
    {
        var installedVoices = _synthesizer.GetInstalledVoices();
        if (installedVoices.Count == 0)
        {
            _log.Error("No voices installed");
            return;
        }
        
        var defaultVoice = installedVoices.First().VoiceInfo.Name;
        if (actor?.Gender == GenderUtils.Gender.Female)
        {
            var fem = installedVoices.Where(x => x.VoiceInfo.Gender == VoiceGender.Female).MinBy(x => Random.Shared.Next())?.VoiceInfo.Name;
            if (fem != null)
            {
                defaultVoice = fem;
            }
        }
        else if (actor?.Gender == GenderUtils.Gender.Male)
        {
            var masc = installedVoices.Where(x => x.VoiceInfo.Gender == VoiceGender.Male).MinBy(x => Random.Shared.Next())?.VoiceInfo.Name;
            if (masc != null)
            {
                defaultVoice = masc;
            }
        }
        else
        {
            var neutral = installedVoices.Where(x => x.VoiceInfo.Gender == VoiceGender.Neutral).MinBy(x => Random.Shared.Next())?.VoiceInfo.Name;
            if (neutral != null)
            {
                defaultVoice = neutral;
            }
        }
        
        var cachedVoice = _config.SpeechSynthesisConfig.GetSpeakerVoiceOrDefault(actor?.Name ?? "Unknown", defaultVoice);
        var ssml = _lexiconHandler.AsSsml(text);
        _log.Debug($"[SpeechSynthesis][{cachedVoice}] {ssml}");
        
        await _semaphore.WaitAsync();
        try
        {
            var outStream = new MemoryStream();
            _synthesizer.SetOutputToAudioStream(outStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 44100, 16, 1, 32000, 2, null));
            _synthesizer.Volume = (int)(volume * 100);
            _synthesizer.Rate = (int)speed;
            _synthesizer.SelectVoice(cachedVoice);
            _synthesizer.SpeakSsml(ssml);
            outStream.Seek(0, SeekOrigin.Begin);

            _soundQueue.EnqueueSound(new SoundQueueItem
            {
                Data = outStream,
                Volume = volume,
                StreamDataType = StreamDataType.Pcm
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }
}