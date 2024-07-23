namespace TTSPlogon.Clients.SpeechSynthesisClient;

public class SpeechSynthesisConfig
{    
    public Dictionary<string, string> VoiceCache { get; set; } = new();
    public string GetSpeakerVoiceOrDefault(string entity, string defaultVoice)
    {
        if (VoiceCache.TryGetValue(entity, out var voice))
        {
            return voice;
        }
        
        VoiceCache[entity] = defaultVoice;
        return defaultVoice;
    }
}